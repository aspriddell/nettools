using System.Text.Json.Serialization;
using RoutingVisualiser.Geolocation;

namespace RoutingVisualiser;

[JsonSerializable(typeof(IpGeolocation))]
[JsonSerializable(typeof(CachedIpGeolocation))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, Converters = [typeof(JsonIPAddressConverter)])]
internal partial class NetToolsSerializerContext : JsonSerializerContext
{
}