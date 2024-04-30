using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NetTools.Geolocation;
using NetTools.Models;
using GeolocationService = NetTools.Geolocation.GeolocationService;

namespace NetTools.Pages;

public partial class Traceroute : ComponentBase, IAsyncDisposable
{
    private record TracerouteRouteGroup(int Id, string Destination, IReadOnlyList<TracerouteProbe> Hops, IReadOnlyList<DateTimeOffset> TimesEncountered);

    private IJSObjectReference _mapRef, _markerLayerRef;
    private ILookup<string, TracerouteRouteGroup> _hostTraces;

    [Inject]
    private IJSRuntime JsRuntime { get; set; }
    
    [Inject]
    private GeolocationService GeolocationService { get; set; }

    private IReadOnlyDictionary<IPAddress, IpGeolocation> HostGeolocationCache { get; set; }

    private ILookup<string, TracerouteRouteGroup> HostTraces
    {
        get => _hostTraces;
        set
        {
            _hostTraces = value;
            _ = SetHost(value.FirstOrDefault()?.Key);
        }
    }

    private string SelectedHost { get; set; }
    private TracerouteRouteGroup SelectedTrace { get; set; }

    private ISet<IPAddress> IgnoredHops { get; } = new HashSet<IPAddress>();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _mapRef = await JsRuntime.InvokeAsync<IJSObjectReference>("initMap", "map");
            _markerLayerRef = await JsRuntime.InvokeAsync<IJSObjectReference>("createLayer", _mapRef);
        }
    }

    private async Task SetHost(string host)
    {
        if (_markerLayerRef != null)
        {
            await JsRuntime.InvokeVoidAsync("clearLayer", _markerLayerRef);
        }

        IgnoredHops.Clear();
        
        if (string.IsNullOrEmpty(host))
        {
            SelectedHost = null;
            SelectedTrace = null;
            return;
        }

        // get all ips for the host, then resolve all addresses with geolocation service
        var allIps = HostTraces[host].SelectMany(x => x.Hops?.Select(y => y?.IP) ?? Enumerable.Empty<IPAddress>()).Where(y => y != null).ToHashSet();
        var geolocatedEntries = await GeolocationService.PerformLookup(allIps).ConfigureAwait(false);

        SelectedHost = host;
        HostGeolocationCache = geolocatedEntries.ToDictionary(x => x.QueryAddress);

        await SetTrace(HostTraces[host].FirstOrDefault());
    }

    private async Task SetTrace(TracerouteRouteGroup route)
    {
        if (_markerLayerRef != null)
        {
            await JsRuntime.InvokeVoidAsync("clearLayer", _markerLayerRef);
        }

        if (route == null)
        {
            SelectedTrace = null;
            return;
        }

        double[] lastLocation = null;
        var markers = new List<MapMarker>();

        foreach (var hop in route.Hops.Where(x => x != null && !IgnoredHops.Contains(x.IP)))
        {
            if (!HostGeolocationCache.TryGetValue(hop.IP, out var ipInfo) || !ipInfo.Latitude.HasValue || !ipInfo.Longitude.HasValue)
            {
                continue;
            }
                
            double[] location = [ipInfo.Latitude.Value, ipInfo.Longitude.Value];

            if (lastLocation?.SequenceEqual(location) != true)
            {
                markers.Add(new MapMarker(location, $"{hop.Name} ({hop.IP})"));
                lastLocation = location;
            }
        }

        // set ui state
        SelectedTrace = route;
        SelectedHost = route.Destination;

        await JsRuntime.InvokeVoidAsync("addMarkers", _mapRef, _markerLayerRef, markers.ToArray(), true);
        await InvokeAsync(StateHasChanged);
    }

    private Task ToggleHostVisibility(IPAddress address)
    {
        if (!IgnoredHops.Add(address))
        {
            IgnoredHops.Remove(address);
        }

        return SetTrace(SelectedTrace);
    }
    
    private IEnumerable<TracerouteRouteGroup> ProcessRoutes(IReadOnlyCollection<TracerouteResult> results)
    {
        var distinctRoutes = new List<TracerouteRouteGroup>();
        foreach (var hostTraceGroup in results.OrderBy(x => x.Timestamp).GroupBy(x => x.DestinationName))
        {
            var routeCount = hostTraceGroup.Count();
            var processedRoutes = new List<TracerouteResult>();
                
            // get the most complete trace for each host.
            // a route is considered the same if the number of hops is the same, and the ip addresses in one route is an exact match or a superset of the other.
            // this will be calculated by taking a set of addresses, and using it to compare against all the other routes that match the criteria, replacing the contents of the set to get the most complete route.
            while (processedRoutes.Count != routeCount)
            {
                List<DateTimeOffset> routeEncountered = [];
                TracerouteResult mostCompleteRoute = null;
                ISet<IPAddress> routeAddresses = new HashSet<IPAddress>();
                    
                foreach (var trace in hostTraceGroup.Except(processedRoutes))
                {
                    var hopAddresses = trace.Hops.Select(x => x.Probes.FirstOrDefault()?.IP).Where(x => x != null);

                    // perform initial population if needed
                    if (routeAddresses.Count == 0)
                    {
                        mostCompleteRoute = trace;
                        routeAddresses.UnionWith(hopAddresses);
                    }
                        
                    if (mostCompleteRoute!.Hops.Count != trace.Hops.Count)
                    {
                        // ignore paths with different hop count (different route?)
                        continue;
                    }
                        
                    if (routeAddresses.IsProperSubsetOf(hopAddresses))
                    {
                        // route is more complete than the most complete route, replace it.
                        mostCompleteRoute = trace;
                        routeAddresses.UnionWith(hopAddresses);
                    }
                }
                    
                // add the most complete route to the list of distinct routes
                distinctRoutes.Add(new TracerouteRouteGroup(distinctRoutes.Count + 1, hostTraceGroup.Key, mostCompleteRoute!.Hops.Select(x => x.Probes.FirstOrDefault()).ToList(), routeEncountered));
                    
                // now the most complete route has been set, mark all routes that are subsets as processed
                foreach (var trace in hostTraceGroup.Except(processedRoutes))
                {
                    var hopAddresses = trace.Hops.Select(x => x.Probes.FirstOrDefault()?.IP).Where(x => x != null).ToList();

                    if (routeAddresses.IsProperSupersetOf(hopAddresses) || routeAddresses.SetEquals(hopAddresses))
                    {
                        processedRoutes.Add(trace);
                        routeEncountered.Add(DateTimeOffset.FromUnixTimeSeconds(trace.Timestamp));
                    }
                }
            }
        }

        return distinctRoutes;
    }

    public async ValueTask DisposeAsync()
    {
        if (_mapRef != null)
        {
            await JsRuntime.InvokeVoidAsync("disposeMap", _mapRef);
                
            await _mapRef.DisposeAsync();
            await _markerLayerRef.DisposeAsync();
        }
    }
}