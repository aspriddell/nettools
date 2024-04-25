using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ApexCharts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using RoutingVisualiser.Models;

namespace RoutingVisualiser.Pages;

public partial class Ping : ComponentBase
{
    private readonly ApexChartOptions<PingResult> _packetLossChartOptions = new()
    {
        Chart = new Chart
        {
            Height = "100%",
            Stacked = true
        },
        Theme = new Theme
        {
            Mode = Mode.Dark,
        },
        PlotOptions = new PlotOptions
        {
            Bar = new PlotOptionsBar
            {
                Horizontal = false
            }
        }
    };
    
    private bool ShowUploadDialog { get; set; }
    
    private ILookup<string, PingResult> PingResults { get; set; }
    private IGrouping<string, PingResult> SelectedHost { get; set; }
    
    private async Task SelectedFileChanged(InputFileChangeEventArgs obj)
    {
        IReadOnlyCollection<PingResult> results;
        
        switch (obj.File.ContentType)
        {
            case "application/json":
            {
                using var stream = obj.File.OpenReadStream();
                var result = await JsonSerializer.DeserializeAsync<PingResult>(stream, Program.JsonOptions);
                results = new[] { result };
                
                break;
            }

            case "application/zip":
            {
                var memoryStream = new MemoryStream();
                await using (var file = obj.File.OpenReadStream())
                {
                    await file.CopyToAsync(memoryStream).ConfigureAwait(false);
                }

                using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read, false);
                var archiveResults = new List<PingResult>(archive.Entries.Count);
                
                foreach (var entry in archive.Entries.Where(x => Path.GetExtension(x.Name) == ".json"))
                {
                    await using var entryStream = entry.Open();

                    try
                    {
                        var result = await JsonSerializer.DeserializeAsync<PingResult>(entryStream, Program.JsonOptions);
                        archiveResults.Add(result);
                    }
                    catch
                    {
                        // ignore deserialization errors
                    }
                }
                
                results = archiveResults;
                break;
            }
            
            default:
                return;
        }

        PingResults = results.OrderBy(x => x.Timestamp).ToLookup(x => x.Destination);
        SelectedHost = PingResults.Count == 1 ? PingResults.First() : null;
    }
}