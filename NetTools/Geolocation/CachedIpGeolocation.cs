using System;
using System.Text.Json.Serialization;
using Tavenem.DataStorage;

namespace RoutingVisualiser.Geolocation;

internal class CachedIpGeolocation : IpGeolocation, IIdItem
{
    public string Id => QueryAddress.ToString();
    
    [JsonPropertyName("created")]
    public long CreatedEpoch { get; set; } = DateTimeOffset.Now.ToUnixTimeSeconds();

    [JsonIgnore]
    public DateTimeOffset CreatedAt
    {
        get => DateTimeOffset.FromUnixTimeSeconds(CreatedEpoch);
        set => CreatedEpoch = value.ToUnixTimeSeconds();
    }
    
    public bool Equals(IIdItem other)
    {
        return other is CachedIpGeolocation geolocation && geolocation.QueryAddress.Equals(QueryAddress);
    }
}