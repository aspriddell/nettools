using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using DragonFruit.Data;
using DragonFruit.Data.Requests;

namespace NetTools.Geolocation;

/// <summary>
/// Request for batch geolocation lookups using the ip-api.com service.
/// </summary>
internal partial class BatchIpApiRequest(IEnumerable<IPAddress> addresses) : ApiRequest
{
    public override string RequestPath => "http://ip-api.com/batch";
    public override HttpMethod RequestMethod => HttpMethod.Post;
    
    public GeolocationFields? Fields { get; set; }

    [RequestParameter(ParameterType.Query, "fields")]
    protected int? FieldValue => (int?)Fields;
    
    [RequestParameter(ParameterType.Query, "lang")]
    public string Language { get; set; }

    [RequestBody]
    public IEnumerable<IPAddress> Addresses { get; set; } = addresses;
}