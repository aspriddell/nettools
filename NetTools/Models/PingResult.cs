using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;

namespace NetTools.Models;

public record PingResult(
    IPAddress DestinationIP,
    string Destination,
    int DataBytes,
    int PacketsTransmitted,
    int PacketsReceived,
    float PacketLossPercent,
    float TimeMs,
    float RoundTripTimeMin,
    float RoundTripTimeMax,
    float RoundTripTimeAvg,
    float RoundTripTimeStddev,
    long? Timestamp,
    IReadOnlyList<PingResponse> Responses);

public record PingResponse(
    string Type,
    int Bytes,
    IPAddress ResponseIP,
    [property: JsonPropertyName("icmp_seq")] int IMCPSeq,
    int TTL,
    float TimeMs,
    bool Duplicate);