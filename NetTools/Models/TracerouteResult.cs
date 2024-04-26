using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;

namespace NetTools.Models;

internal record TracerouteResult(
    long Timestamp,
    string DestinationName,
    IPAddress DestinationIP,
    IReadOnlyList<TracerouteHop> Hops);

internal record TracerouteHop(int Hop, IReadOnlyList<TracerouteProbe> Probes);

internal record TracerouteProbe(IPAddress IP, string Name, [property: JsonPropertyName("rtt")] float RoundtripTimeMs);