using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using DragonFruit.Data;
using DragonFruit.Data.Serializers;
using Havit.Blazor.Components.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
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
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");
        
        // http services
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

        // blazor services
        builder.Services.AddHxServices();
        builder.Services.AddBlazoredLocalStorageAsSingleton(c => c.JsonSerializerOptions = JsonOptions);
        builder.Services.AddSingleton(s => new IndexedDbService(s.GetRequiredService<IJSRuntime>(), new IndexedDb(IndexDbName, 1, "geocache"), JsonOptions));

        builder.Services.AddSingleton<GeolocationService>();

        await builder.Build().RunAsync();
    }
}