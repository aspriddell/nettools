using System.Net;
using DragonFruit.Data;
using DragonFruit.Data.Requests;

namespace NetTools.Geolocation;

/// <summary>
/// Request for info about a single IP address using the ip-api.com service.
/// </summary>
/// <param name="address">The address to lookup</param>
internal partial class IpApiRequest(IPAddress address) : ApiRequest
{
    public override string RequestPath => $"http://ip-api.com/json/{Address}";

    public IPAddress Address { get; } = address;

    public GeolocationFields? Fields { get; set; }

    [RequestParameter(ParameterType.Query, "fields")]
    protected int? FieldValue => (int?)Fields;

    [RequestParameter(ParameterType.Query, "lang")]
    public string Language { get; set; }

    [RequestParameter(ParameterType.Query, "callback")]
    public string Callback { get; set; }
}