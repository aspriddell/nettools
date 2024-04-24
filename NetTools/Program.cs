using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Havit.Blazor.Components.Web;
using MaxMind.Db;
using MaxMind.GeoIP2;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RoutingVisualiser
{
    public class Program
    {
        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
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
            builder.Services.AddHxServices();

            builder.Services.Configure<RazorPagesOptions>(c => c.RootDirectory = "/");

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
}