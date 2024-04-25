using System.Collections.Generic;
using System.Net;

namespace RoutingVisualiser.Models;

internal record PingResult(
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
    long Timestamp,
    IReadOnlyList<PingResponse> Responses);

internal record PingResponse(
    string Type,
    int Bytes,
    IPAddress ResponseIP,
    int IMCPSeq,
    int TTL,
    float TimeMs,
    bool Duplicate);