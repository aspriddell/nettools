using System.Linq;
using ApexCharts;
using Microsoft.AspNetCore.Components;
using NetTools.Models;

namespace NetTools.Pages;

public partial class Ping : ComponentBase
{
    private readonly ApexChartOptions<PingResult> _packetLossChartOptions = new()
    {
        Theme = new Theme { Mode = Mode.Dark },
        Chart = new Chart { Height = "100%", Stacked = true },
        PlotOptions = new PlotOptions { Bar = new PlotOptionsBar { Horizontal = false } }
    };

    private readonly ApexChartOptions<PingResult> _hostPacketLossChartOptions = new()
    {
        Chart = new Chart
        {
            Stacked = true,
            Height = "100%",
            StackType = StackType.Percent100
        },
        Title = new Title { Align = Align.Center },
        Legend = new Legend { Position = LegendPosition.Bottom },
        Theme = new Theme { Mode = Mode.Dark },
        Colors = ["#4caf4f", "#ffeb3b", "#ffc107", "#f44336"],
        PlotOptions = new PlotOptions { Bar = new PlotOptionsBar { Horizontal = true } },
        Xaxis = new XAxis
        {
            Labels = new XAxisLabels { Style = new AxisLabelStyle { FontSize = "12px" } },
            Title = new AxisTitle
            {
                Text = "Percentage",
                Style = new AxisTitleStyle
                {
                    FontSize = "14px"
                }
            }
        },
        Yaxis =
        [
            new YAxis
            {
                Title = new AxisTitle { Text = "Date", Style = new AxisTitleStyle { FontSize = "14px" } },
                Labels = new YAxisLabels { Style = new AxisLabelStyle { FontSize = "12px" } }
            }
        ]
    };

    private readonly ApexChartOptions<PingResult> _hostDualPingLossChartOptions = new()
    {
        Chart = new Chart { Height = "100%" },
        Theme = new Theme { Mode = Mode.Dark },
        Title = new Title { Align = Align.Center },
        Tooltip = new Tooltip { X = new TooltipX { Format = "dd/MM/yyyy HH:mm" } },
        Xaxis = new XAxis
        {
            Type = XAxisType.Datetime,
            Labels = new XAxisLabels
            {
                Style = new AxisLabelStyle { FontSize = "10px" },
                Format = "dd/MM/yyyy"
            },
            Title = new AxisTitle
            {
                Text = "Date",
                Style = new AxisTitleStyle { FontSize = "14px" }
            }
        },
        Yaxis =
        [
            new YAxis
            {
                Title = new AxisTitle
                {
                    Text = "RTT (ms)",
                    Style = new AxisTitleStyle { FontSize = "14px" }
                }
            },
            new YAxis
            {
                Opposite = true,
                Title = new AxisTitle
                {
                    Text = "Packet Loss (%)",
                    Style = new AxisTitleStyle { FontSize = "14px" }
                }
            }
        ]
    };

    private ILookup<string, PingResult> PingResults { get; set; }
    private IGrouping<string, PingResult> SelectedHost { get; set; }

    private void SetProcessedItems(ILookup<string, PingResult> results)
    {
        PingResults = results;
        SelectedHost = PingResults.Count == 1 ? PingResults.First() : null;
     }
}