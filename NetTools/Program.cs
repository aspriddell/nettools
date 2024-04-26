using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using DragonFruit.Data;
using DragonFruit.Data.Serializers;
using Havit.Blazor.Components.Web;
using MaxMind.Db;
using MaxMind.GeoIP2;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoutingVisualiser.Geolocation;
using Tavenem.Blazor.IndexedDB;

namespace RoutingVisualiser;

public class Program
{
    private const string IndexDbName = "nettools";
    
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        TypeInfoResolver = NetToolsSerializerContext.Default,
        Converters = { new JsonIPAddressConverter() }
    };
        
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddCommandLine(args);
        builder.Configuration.AddIniFile("config.ini");
        builder.Configuration.AddEnvironmentVariables();

        builder.Services.AddServerSideBlazor().AddHubOptions(config => config.MaximumReceiveMessageSize = 1048576);
        builder.Services.AddRazorPages();
        builder.Services.Configure<RazorPagesOptions>(c => c.RootDirectory = "/");

        // blazor services
        builder.Services.AddHxServices();
        builder.Services.AddIndexedDb(new IndexedDb(IndexDbName, 1, "geocache"), JsonOptions);
        builder.Services.AddBlazoredLocalStorageAsSingleton(c => c.JsonSerializerOptions = JsonOptions);
        
        builder.Services.AddSingleton<ApiClient>(_ =>
        {
            var client = new ApiClient<ApiJsonSerializer>
            {
                Handler = () => new HttpClientHandler() // wasm can't use the default socketshandler
            };
            
            // enable proper IPAddress serialization
            client.Serializers.Configure<ApiJsonSerializer>(s => s.SerializerOptions = JsonOptions);
            return client;
        });

        builder.Services.AddSingleton<GeolocationService>();

        var geoIpFilePath = builder.Configuration["GeoIP:FilePath"];
        if (!string.IsNullOrEmpty(geoIpFilePath) && File.Exists(geoIpFilePath))
        {
            builder.Services.AddSingleton(new DatabaseReader(geoIpFilePath, FileAccessMode.Memory));
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