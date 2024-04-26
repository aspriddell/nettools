using System.Text.Json.Serialization;
using RoutingVisualiser.Geolocation;
using RoutingVisualiser.Models;

namespace RoutingVisualiser;

[JsonSerializable(typeof(IpGeolocation)), JsonSerializable(typeof(CachedIpGeolocation))]
[JsonSerializable(typeof(TracerouteResult)), JsonSerializable(typeof(TracerouteProbe))]
[JsonSerializable(typeof(PingResult)), JsonSerializable(typeof(PingResponse))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, Converters = [typeof(JsonIPAddressConverter)])]
internal partial class NetToolsSerializerContext : JsonSerializerContext
{
}