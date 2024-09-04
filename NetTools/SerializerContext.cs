using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;
using NetTools.Geolocation;
using NetTools.Models;

namespace NetTools;

[JsonSerializable(typeof(PingResult)), JsonSerializable(typeof(PingResponse))]
[JsonSerializable(typeof(TracerouteResult)), JsonSerializable(typeof(TracerouteProbe))]
[JsonSerializable(typeof(IpGeolocation)), JsonSerializable(typeof(CachedIpGeolocation)), JsonSerializable(typeof(IEnumerable<IPAddress>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower, Converters = [typeof(JsonIPAddressConverter)])]
internal partial class SerializerContext : JsonSerializerContext;