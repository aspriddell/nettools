using Havit.Blazor.Components.Web;
using MaxMind.Db;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceCollectionExtensions;

namespace RoutingVisualiser;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddCommandLine(args);
        builder.Configuration.AddIniFile("config.ini");
        builder.Configuration.AddEnvironmentVariables();

        builder.Services.AddServerSideBlazor();
        builder.Services.AddRazorPages();

        builder.Services.AddLeafletServices();
        builder.Services.AddHxServices();

        builder.Services.Configure<RazorPagesOptions>(c => c.RootDirectory = "/");

        var geoIpFilePath = builder.Configuration["GeoIP:FilePath"];
        if (!string.IsNullOrEmpty(geoIpFilePath) && File.Exists(geoIpFilePath))
        {
            builder.Services.AddSingleton(new Reader(geoIpFilePath, FileAccessMode.Memory));
        }
        else
        {
            throw new FileNotFoundException("GeoIP database file not found. Please check the configuration.");
        }
        
        var app = builder.Build();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.UseRouting();

        app.MapBlazorHub();
        app.MapFallbackToPage("/Index");

        await app.RunAsync().ConfigureAwait(false);
    }
}