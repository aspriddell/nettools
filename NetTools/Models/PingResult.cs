using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;

namespace NetTools.Models;

public record PingResult(
    [property: JsonPropertyName("destination")] string Destination,
    [property: JsonPropertyName("destination_ip")] IPAddress DestinationIP,
    [property: JsonPropertyName("data_bytes")] int DataBytes,
    [property: JsonPropertyName("packets_transmitted")] int PacketsTransmitted,
    [property: JsonPropertyName("packets_received")] int PacketsReceived,
    [property: JsonPropertyName("packet_loss_percent")] float? PacketLossPercent,
    [property: JsonPropertyName("time_ms")] float TotalTimeMs,
    [property: JsonPropertyName("round_trip_time_min")] float RoundTripTimeMin,
    [property: JsonPropertyName("round_trip_time_max")] float RoundTripTimeMax,
    [property: JsonPropertyName("round_trip_time_avg")] float RoundTripTimeAvg,
    [property: JsonPropertyName("round_trip_time_stdev")] float RoundTripTimeStddev,
    [property: JsonPropertyName("timestamp")] long? Timestamp,
    [property: JsonPropertyName("responses")] IReadOnlyList<PingResponse> Responses);

public record PingResponse(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("bytes")] int BytesSent,
    [property: JsonPropertyName("response_ip")] IPAddress ResponseIP,
    [property: JsonPropertyName("icmp_seq")] int IMCPSeq,
    [property: JsonPropertyName("ttl")] int TTL,
    [property: JsonPropertyName("time_ms")] float TimeMs,
    [property: JsonPropertyName("duplicate")] bool Duplicate);