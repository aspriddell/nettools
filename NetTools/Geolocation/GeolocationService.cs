using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using DragonFruit.Data;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Riok.Mapperly.Abstractions;
using Tavenem.Blazor.IndexedDB;

namespace RoutingVisualiser.Geolocation;

/// <summary>
/// Provides geolocation information for IP addresses with caching and rate limit handling
/// </summary>
public partial class GeolocationService
{
    /// <summary>
    /// Represents a cooldown that has been applied to this session.
    /// </summary>
    private record CooldownEntry(DateTimeOffset CooldownEnds);

    private const int CacheExpiryDays = 21;
    private const string PersistedCooldownRecord = "geocache-cooldown-epoch";

    private const GeolocationFields ResponseFields = GeolocationFields.Message |
                                                     GeolocationFields.Country |
                                                     GeolocationFields.CountryCode |
                                                     GeolocationFields.Region |
                                                     GeolocationFields.RegionName |
                                                     GeolocationFields.City |
                                                     GeolocationFields.Latitude |
                                                     GeolocationFields.Longitude |
                                                     GeolocationFields.Isp |
                                                     GeolocationFields.Org |
                                                     GeolocationFields.As |
                                                     GeolocationFields.AsName |
                                                     GeolocationFields.IsHostingProvider |
                                                     GeolocationFields.QueryIp;
    
    private readonly IndexedDbService _geolocationCache;
    private readonly ILocalStorageService _localStorage;
    private readonly ApiClient _client;
    private readonly ILogger<GeolocationService> _logger;

    private readonly AsyncLock _lock = new();
    private readonly IEnumerable<IPNetwork2> _privateNetworks = new[]
    {
        IPNetwork2.Parse("127.0.0.0/8"), // IPv4 loopback
        IPNetwork2.Parse("100.64.0.0/10"), // IPv4 cgNAT

        IPNetwork2.IANA_ABLK_RESERVED1, // 10.0.0.0/8
        IPNetwork2.IANA_BBLK_RESERVED1, // 172.16.0.0/12
        IPNetwork2.IANA_CBLK_RESERVED1, // 192.168.0.0/16
        
        IPNetwork2.Parse("::1/128"), // IPv6 loopback
        IPNetwork2.Parse("fd00::/8"), // IPv6 ULAs
        IPNetwork2.Parse("fe80::/10") // IPv6 link-local
    };

    private Task _cooldownWaiter;

    public GeolocationService(IndexedDbService geolocationCache, ILocalStorageService localStorage, ApiClient client, ILogger<GeolocationService> logger)
    {
        _geolocationCache = geolocationCache;
        _localStorage = localStorage;
        _client = client;
        _logger = logger;

        _cooldownWaiter = LoadCooldownInformation().AsTask();
    }

    /// <summary>
    /// Task that once completed, ratelimits have been reset.
    /// </summary>
    public Task CooldownWaiter
    {
        get => _cooldownWaiter;
        private set
        {
            _cooldownWaiter = value;
            CooldownChanged?.Invoke();
        }
    }

    /// <summary>
    /// Event invoked when the cooldown has been updated (i.e. a new cooldown has been set)
    /// </summary>
    public event Action CooldownChanged;
    
    /// <summary>
    /// Performs a geolocation lookup for the given IP address.
    /// </summary>
    public async Task<IpGeolocation> PerformLookup(IPAddress address)
    {
        var results = await PerformLookup(new[] { address }).ConfigureAwait(false);
        return results.FirstOrDefault();
    }
    
    /// <summary>
    /// Performs a geolocation lookup for the given IP addresses.
    /// </summary>
    public async Task<IReadOnlyCollection<IpGeolocation>> PerformLookup(IEnumerable<IPAddress> addresses)
    {
        var publiclyRoutable = addresses
            .Select(x => x.IsIPv4MappedToIPv6 ? x.MapToIPv4() : x)
            .Where(x => _privateNetworks.All(n => !n.Contains(x)))
            .ToHashSet();

        if (publiclyRoutable.Count == 0)
        {
            return Array.Empty<IpGeolocation>();
        }

        var cacheIgnoreBefore = DateTimeOffset.UtcNow.AddDays(-CacheExpiryDays).ToUnixTimeSeconds();
        _logger.LogInformation("Performing cache lookup for {count} addresses", publiclyRoutable.Count);

        async Task<IReadOnlyCollection<IpGeolocation>> CheckCache()
        {
            var output = new LinkedList<IpGeolocation>();

            await foreach (var item in _geolocationCache.GetAllAsync(NetToolsSerializerContext.Default.CachedIpGeolocation))
            {
                if (publiclyRoutable.Contains(item.QueryAddress) && item.CreatedEpoch > cacheIgnoreBefore)
                {
                    output.AddLast(item);
                }
            }

            return output;
        }

        var cachedResults = await CheckCache().ConfigureAwait(false);
        var missing = publiclyRoutable.Except(cachedResults.Select(x => x.QueryAddress)).ToHashSet();
        
        _logger.LogInformation("Missing {count} from cache (discovered {n} total)", missing.Count, cachedResults.Count);

        // return cached results only if all requested addresses are cached or there's a cooldown in effect that hasn't cleared.
        if (missing.Count == 0 || CooldownWaiter?.IsCompleted == false)
        {
            _logger.LogInformation("All addresses were found cached or a cooldown is active and cannot process any more requests.");
            return cachedResults;
        }

        using (await _lock.LockAsync().ConfigureAwait(false))
        {
            // reload cache in case another operation has updated it
            cachedResults = await CheckCache().ConfigureAwait(false);
            missing.ExceptWith(cachedResults.Select(x => x.QueryAddress));

            if (missing.Count == 0)
            {
                _logger.LogInformation("All addresses were found cached.");
                return cachedResults;
            }

            IEnumerable<IpGeolocation> resultsChain = cachedResults;
            
            while (CooldownWaiter?.IsCompleted == true && missing.Count > 0)
            {
                ApiRequest request;
                if (missing.Count == 1)
                {
                    request = new IpApiRequest(missing.Single()) { Fields = ResponseFields };
                    _logger.LogInformation("Making a single request for {address}", missing.Single().ToString());
                }
                else
                {
                    request = new BatchIpApiRequest(missing.Take(100)) { Fields = ResponseFields };
                    _logger.LogInformation("Making a batch request for {count} addresses", missing.Count);
                }

                try
                {
                    using var httpResponse = await _client.PerformAsync(request).ConfigureAwait(false);
                    await ProcessRatelimitUpdate(httpResponse).ConfigureAwait(false);

                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Request failed with status {status}: {reason}", httpResponse.StatusCode, await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false));
                        break;
                    }

                    // deserialize single item
                    if (request is IpApiRequest)
                    {
                        var item = await httpResponse.Content.ReadFromJsonAsync(NetToolsSerializerContext.Default.IpGeolocation).ConfigureAwait(false);
                        await _geolocationCache.StoreItemAsync(MappingUtils.ToCachedIpGeolocation(item), NetToolsSerializerContext.Default.CachedIpGeolocation).ConfigureAwait(false);

                        resultsChain = resultsChain.Append(item);
                        missing.Remove(item.QueryAddress);
                    }
                    else
                    {
                        var collectionListing = new List<IpGeolocation>();
                        await foreach (var item in httpResponse.Content.ReadFromJsonAsAsyncEnumerable(NetToolsSerializerContext.Default.IpGeolocation).ConfigureAwait(false))
                        {
                            await _geolocationCache.StoreItemAsync(MappingUtils.ToCachedIpGeolocation(item), NetToolsSerializerContext.Default.CachedIpGeolocation).ConfigureAwait(false);
                            collectionListing.Add(item);

                            missing.Remove(item.QueryAddress);
                        }

                        resultsChain = resultsChain.Concat(collectionListing);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to perform geolocation lookup: {message}", e.Message);
                    break;
                }
            }

            return resultsChain.ToList();
        }
    }

    /// <summary>
    /// Checks the response headers for ratelimit information and updates the local cooldown record.
    /// </summary>
    private async ValueTask ProcessRatelimitUpdate(HttpResponseMessage response)
    {
        // check for X-Rl and X-Ttl headers
        if (response.Headers.TryGetValues("X-Rl", out var rl) && int.TryParse(rl.ToString(), out var requestsLeft) && requestsLeft <= 1)
        {
            var cooldown = response.Headers.TryGetValues("X-Ttl", out var ttl) && int.TryParse(ttl.ToString(), out var seconds) ? TimeSpan.FromSeconds(seconds) : TimeSpan.FromMinutes(1);
            var cooldownEnds = DateTimeOffset.UtcNow.Add(cooldown);
            
            CooldownWaiter = Task.Delay(cooldown);
            await _localStorage.SetItemAsync(PersistedCooldownRecord, new CooldownEntry(cooldownEnds));
        }
    }

    /// <summary>
    /// Loads cooldown information from the browser's local storage
    /// This will reapply any cooldowns that were in effect before reloading.
    /// </summary>
    private async ValueTask LoadCooldownInformation()
    {
        if (!await _localStorage.ContainKeyAsync(PersistedCooldownRecord))
        {
            return;
        }

        var cooldownEntry = await _localStorage.GetItemAsync<CooldownEntry>(PersistedCooldownRecord);

        // reapply cooldown
        if (cooldownEntry?.CooldownEnds > DateTimeOffset.UtcNow)
        {
            CooldownWaiter = Task.Delay(cooldownEntry.CooldownEnds - DateTimeOffset.UtcNow);
        }
        else if (cooldownEntry != null)
        {
            // remove stale record
            await _localStorage.RemoveItemAsync(PersistedCooldownRecord).ConfigureAwait(false);
        }
    }

    [Mapper]
    private static partial class MappingUtils
    {
        public static partial CachedIpGeolocation ToCachedIpGeolocation(IpGeolocation geolocation);
    }
}