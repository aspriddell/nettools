using System;

namespace NetTools.Geolocation;

/// <summary>
/// Represents the fields that can be requested from the ip-api.com service.
/// </summary>
[Flags]
public enum GeolocationFields
{
    Status = 0x4000,
    Message = 0x8000,

    Continent = 0x100000,
    ContinentCode = 0x200000,

    Country = 0x1,
    CountryCode = 0x2,

    Region = 0x4,
    RegionName = 0x8,

    City = 0x10,
    District = 0x80000,
    Zip = 0x20,

    Latitude = 0x40,
    Longitude = 0x80,

    Timezone = 0x100,
    UtcOffset = 0x2000000,
    Currency = 0x800000,

    Isp = 0x200,
    Org = 0x400,
    As = 0x800,
    AsName = 0x400000,
    //ReverseDns = 0x1000,

    Mobile = 0x10000,
    IsProxy = 0x20000,
    IsHostingProvider = 0x1000000,

    QueryIp = 0x2000
}