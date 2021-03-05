using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Channel;

namespace client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using IHost host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, configuration) =>
                    {
                        configuration.Sources.Clear();

                        IHostEnvironment env = hostingContext.HostingEnvironment;

                        configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                        IConfigurationRoot configurationRoot = configuration.Build();

                        Settings.EventHubConnectionString = configurationRoot["EventHubConnectionString"];
                        Settings.EventHubName = configurationRoot["EventHubName"];
                        Settings.BlobContainerName = configurationRoot["BlobContainerName"];
                        Settings.BlobStorageConnectionString = configurationRoot["BlobStorageConnectionString"];                                                
                    })
                .ConfigureServices((hostContext, services) =>
                    {
                        services.AddHostedService<Worker>();
                        services.AddApplicationInsightsTelemetryWorkerService();
                        services.AddSingleton<ITelemetryInitializer, CloudRoleNameTelemetryInitializer>();

                    });
    }

    public class CloudRoleNameTelemetryInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
        // set custom role name here
        telemetry.Context.Cloud.RoleName = "NicolConsumer";
        }
    }
}
