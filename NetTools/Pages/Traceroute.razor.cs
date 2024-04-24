using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using RoutingVisualiser.Models;

namespace RoutingVisualiser.Pages
{
    public record TracerouteRouteGroup(int Id, string Destination, IReadOnlyList<TracerouteProbe> Hops, IReadOnlyList<DateTimeOffset> TimesEncountered);
    
    public partial class Traceroute : ComponentBase
    {
        private ILookup<string, TracerouteRouteGroup> HostTraces { get; set; }

        private async Task ProcessArchive(InputFileChangeEventArgs obj)
        {
            var tempStream = new MemoryStream();

            await using (var stream = obj.File.OpenReadStream())
            {
                await stream.CopyToAsync(tempStream);
            }

            var listing = new List<TracerouteResult>(100);
            using (var archiveStream = new ZipArchive(tempStream, ZipArchiveMode.Read, false))
            {
                foreach (var entry in archiveStream.Entries.Where(x => !string.IsNullOrEmpty(x.Name)))
                {
                    await using var entryStream = entry.Open();
                    var deserializedEntry = await JsonSerializer.DeserializeAsync<TracerouteResult>(entryStream, Program.JsonOptions);

                    if (deserializedEntry == null)
                    {
                        continue;
                    }

                    listing.Add(deserializedEntry);
                }
            }

            var tracesByHost = listing.OrderBy(x => x.Timestamp).GroupBy(x => x.DestinationName);
            var distinctRoutes = new List<TracerouteRouteGroup>();

            foreach (var hostTraceGroup in tracesByHost)
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
            
            HostTraces = distinctRoutes.ToLookup(x => x.Destination);
        }
    }
}