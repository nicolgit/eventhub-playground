using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private TelemetryClient _telemetryClient;

    public Worker(ILogger<Worker> logger, TelemetryClient tc)
    {
        _logger = logger;
        _telemetryClient = tc;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using (var producerClient = new EventHubProducerClient(Settings.EventHubConnectionString, Settings.EventHubName))
        {
            // Create a batch of events 
            using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                using (_telemetryClient.StartOperation<RequestTelemetry>("operation"))
                {
                    _logger.LogWarning("A sample warning message.By default, logs with severity Warning or higher is captured by Application Insights");
                    _logger.LogInformation("Calling EventHUB");
                    
                    eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes($"First event { DateTime.Now.ToLongTimeString() }")));
                    eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes("Second event")));
                    eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes("Third event")));

                    // Use the producer client to send the batch of events to the event hub
                    await producerClient.SendAsync(eventBatch);

                    _logger.LogInformation("Calling EventHUB completed");
                    _telemetryClient.TrackEvent("event completed");
                }

                await Task.Delay(1000, stoppingToken);
            }

            // Add events to the batch. An event is a represented by a collection of bytes and metadata. 
            
        }
    }
}