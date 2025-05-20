using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;

namespace NetTools.Models;

internal record TracerouteResult(
    [property: JsonPropertyName("timestamp")] long Timestamp,
    [property: JsonPropertyName("destination_name")] string Destination,
    [property: JsonPropertyName("destination_ip")] IPAddress DestinationIP,
    [property: JsonPropertyName("hops")] IReadOnlyList<TracerouteHop> Hops) : IResult;

internal record TracerouteHop(
    [property: JsonPropertyName("hop")] int Hop,
    [property: JsonPropertyName("probes")] IReadOnlyList<TracerouteProbe> Probes);

internal record TracerouteProbe(
    [property: JsonPropertyName("ip")] IPAddress IP,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("rtt")] float RoundtripTimeMs);