using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;

namespace RoutingVisualiser.Models;

public record TracerouteResult(
    long Timestamp,
    string DestinationName,
    IPAddress DestinationIP,
    IReadOnlyList<TracerouteHop> Hops);

public record TracerouteHop(int Hop, IReadOnlyList<TracerouteProbe> Probes);

public record TracerouteProbe(
    IPAddress IP,
    string Name,
    [property: JsonPropertyName("rtt")] float RoundtripTimeMs);