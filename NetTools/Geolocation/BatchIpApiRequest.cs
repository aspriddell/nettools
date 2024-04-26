using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using DragonFruit.Data;
using DragonFruit.Data.Requests;

namespace RoutingVisualiser.Geolocation;

/// <summary>
/// Request for batch geolocation lookups using the ip-api.com service.
/// </summary>
public partial class BatchIpApiRequest(IEnumerable<IPAddress> addresses) : ApiRequest
{
    public override string RequestPath => "https://ip-api.com/batch";
    public override HttpMethod RequestMethod => HttpMethod.Post;
    
    [EnumOptions(EnumOption.Numeric)]
    [RequestParameter(ParameterType.Query, "fields")]
    public GeolocationFields? Fields { get; set; }
    
    [RequestParameter(ParameterType.Query, "lang")]
    public string Language { get; set; }

    [RequestBody]
    public IEnumerable<IPAddress> Addresses { get; set; } = addresses;
}