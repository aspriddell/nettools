using System.Net;
using DragonFruit.Data;
using DragonFruit.Data.Requests;

namespace RoutingVisualiser.Geolocation;

/// <summary>
/// Request for info about a single IP address using the ip-api.com service.
/// </summary>
/// <param name="address">The address to lookup</param>
public partial class IpApiRequest(IPAddress address) : ApiRequest
{
    public override string RequestPath => $"https://ip-api.com/json/{address}";
    
    [EnumOptions(EnumOption.Numeric)]
    [RequestParameter(ParameterType.Query, "fields")]
    public GeolocationFields? Fields { get; set; }
    
    [RequestParameter(ParameterType.Query, "lang")]
    public string Language { get; set; }
    
    [RequestParameter(ParameterType.Query, "callback")]
    public string Callback { get; set; }
}