using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoutingVisualiser.Geolocation;

/// <summary>
/// Response containing information about a queried IP address.
/// </summary>
public class IpGeolocation
{
    [JsonPropertyName("query")]
    public IPAddress QueryAddress { get; set; }
    
    [JsonPropertyName("message")]
    public string ErrorMessage { get; set; }
    
    [JsonPropertyName("country")]
    public string Country { get; set; }
    
    [JsonPropertyName("countryCode")]
    public string CountryCode { get; set; }
    
    [JsonPropertyName("region")]
    public string Region { get; set; }
    
    [JsonPropertyName("regionName")]
    public string RegionName { get; set; }
    
    [JsonPropertyName("city")]
    public string City { get; set; }
    
    [JsonPropertyName("lat")]
    public float? Latitude { get; set; }
    
    [JsonPropertyName("lon")]
    public float? Longitude { get; set; }
    
    [JsonPropertyName("isp")]
    public string Isp { get; set; }
    
    [JsonPropertyName("org")]
    public string Organisation { get; set; }
    
    [JsonPropertyName("as")]
    public string As { get; set; }
    
    [JsonPropertyName("asname")]
    public string AsName { get; set; }
    
    [JsonPropertyName("hosting")]
    public bool IsHosting { get; set; }
    
    [JsonExtensionData]
    public IDictionary<string, JsonElement> AdditionalProperties { get; set; }
}