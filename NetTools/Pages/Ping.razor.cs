using System.Linq;
using Microsoft.AspNetCore.Components;
using NetTools.Models;

namespace NetTools.Pages;

public partial class Ping : ComponentBase
{
    private ILookup<string, PingResult> PingResults { get; set; }
    private IGrouping<string, PingResult> SelectedHost { get; set; }

    private void SetProcessedItems(ILookup<string, PingResult> results)
    {
        PingResults = results;
        SelectedHost = null;

        if (PingResults.Count == 1)
        {
            SelectedHost = PingResults.Single();
        }
    }
}