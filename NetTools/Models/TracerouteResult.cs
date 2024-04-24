using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;

namespace RoutingVisualiser.Models
{
    public record TracerouteResult(IPAddress DestinationIP, string DestinationName, IReadOnlyList<TracerouteHop> Hops, long Timestamp);

    public record TracerouteHop(int Hop, IReadOnlyList<TracerouteProbe> Probes);
    public record TracerouteProbe(string Annotation, int? ASN, IPAddress IP, string Name, [property: JsonPropertyName("rtt")] float RoundtripTimeMs);
}
