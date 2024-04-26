using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using DragonFruit.Data;
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

    public GeolocationService(IndexedDbService geolocationCache, ILocalStorageService localStorage, ApiClient client)
    {
        _geolocationCache = geolocationCache;
        _localStorage = localStorage;
        _client = client;

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
        var publiclyRoutable = addresses.Select(x => x.IsIPv4MappedToIPv6 ? x.MapToIPv4() : x).Where(x => _privateNetworks.All(n => !n.Contains(x))).ToList();

        if (publiclyRoutable.Count == 0)
        {
            return Array.Empty<IpGeolocation>();
        }

        var addressStrings = publiclyRoutable.Select(x => x.ToString()).ToList();
        var cacheIgnoreBefore = DateTimeOffset.UtcNow.AddDays(-CacheExpiryDays).ToUnixTimeSeconds();

        // cache the method used for quick reuse
        Task<IReadOnlyList<CachedIpGeolocation>> CheckCache() => _geolocationCache
            .Query(NetToolsSerializerContext.Default.CachedIpGeolocation)
            .Where(x => addressStrings.Contains(x.Id) && x.CreatedEpoch < cacheIgnoreBefore)
            .ToListAsync();

        var cachedResults = await CheckCache().ConfigureAwait(false);
        var missing = publiclyRoutable.Except(cachedResults.Select(x => x.QueryAddress)).ToHashSet();

        // return cached results only if all requested addresses are cached or there's a cooldown in effect that hasn't cleared.
        if (missing.Count == 0 || CooldownWaiter?.IsCompleted == false)
        {
            return cachedResults;
        }

        using (await _lock.LockAsync().ConfigureAwait(false))
        {
            // reload cache in case another operation has updated it
            cachedResults = await CheckCache().ConfigureAwait(false);
            missing.ExceptWith(cachedResults.Select(x => x.QueryAddress));

            if (missing.Count == 0)
            {
                return cachedResults;
            }

            IEnumerable<IpGeolocation> resultsChain = cachedResults;
            
            while (CooldownWaiter?.IsCompleted != false && missing.Count > 0)
            {
                ApiRequest request = missing.Count == 1 ? new IpApiRequest(missing.Single()) { Fields = ResponseFields } : new BatchIpApiRequest(missing.Take(100)) { Fields = ResponseFields };
                using var httpResponse = await _client.PerformAsync(request).ConfigureAwait(false);
            
                await ProcessRatelimitUpdate(httpResponse).ConfigureAwait(false);

                // deserialize single item
                if (request is IpApiRequest)
                {
                    var item = await httpResponse.Content.ReadFromJsonAsync(NetToolsSerializerContext.Default.IpGeolocation).ConfigureAwait(false);
                    await _geolocationCache.StoreItemAsync(MappingUtils.ToCachedIpGeolocation(item)).ConfigureAwait(false);

                    resultsChain = resultsChain.Append(item);
                    missing.Remove(item.QueryAddress);
                }
                else
                {
                    var collectionListing = new List<IpGeolocation>();
                    await foreach (var item in httpResponse.Content.ReadFromJsonAsAsyncEnumerable(NetToolsSerializerContext.Default.IpGeolocation).ConfigureAwait(false))
                    {
                        await _geolocationCache.StoreItemAsync(MappingUtils.ToCachedIpGeolocation(item)).ConfigureAwait(false);
                        collectionListing.Add(item);

                        missing.Remove(item.QueryAddress);
                    }

                    resultsChain = resultsChain.Concat(collectionListing);
                }
            }
            
            // recycle the cache (remove expired entries)
            var expiredEntries = _geolocationCache.Query(NetToolsSerializerContext.Default.CachedIpGeolocation).Where(x => x.CreatedEpoch < cacheIgnoreBefore);
            await foreach (var expiredEntry in expiredEntries.AsAsyncEnumerable())
            {
                await _geolocationCache.RemoveItemAsync(expiredEntry).ConfigureAwait(false);
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