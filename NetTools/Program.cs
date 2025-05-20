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
using NetTools.Geolocation;

namespace NetTools;

public class Program
{
    internal const string IndexDbName = "nettools";
    internal const string MapKey = "eyJraWQiOiIzUDNGSDkzS0wyIiwidHlwIjoiSldUIiwiYWxnIjoiRVMyNTYifQ.eyJpc3MiOiJRODI0VkhBVDlTIiwiaWF0IjoxNzQ3NzM5Mzg3LCJvcmlnaW4iOiIqLnBwYy5tb2UifQ.NezK0Sunvq0oEJaEgmJBmNFfsSOwQJ2lAtMCu8iqVRp04FoTHhv6VcTYyLAq1aDAuPbrLWN8bkOq1ZoaCcWusQ";

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        TypeInfoResolver = SerializerContext.Default,
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

        builder.Services.AddIndexedDbService();
        builder.Services.AddIndexedDb(IndexDbName, objectStores: ["geocache"], version: 1);

        builder.Services.AddScoped<GeolocationService>();

        await builder.Build().RunAsync();
    }
}