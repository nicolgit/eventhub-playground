using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.EventHubs.Consumer;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Messaging.EventHubs.Processor;

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
        string consumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;

        // Create a blob container client that the event processor will use 
        BlobContainerClient storageClient = new BlobContainerClient(Settings.BlobStorageConnectionString, Settings.BlobContainerName);

        // Create an event processor client to process events in the event hub
        EventProcessorClient processor = new EventProcessorClient(storageClient, consumerGroup, Settings.EventHubConnectionString, Settings.EventHubName);

        // Register handlers for processing events and handling errors
        processor.ProcessEventAsync += ProcessEventHandler;
        processor.ProcessErrorAsync += ProcessErrorHandler;

        // Start the processing
        await processor.StartProcessingAsync();

        // Wait for 10 seconds for the events to be processed
        await Task.Delay(TimeSpan.FromSeconds(999));

        // Stop the processing
        await processor.StopProcessingAsync();
    }
    
    async Task ProcessEventHandler(ProcessEventArgs eventArgs)
    {
        // Write the body of the event to the console window
        _logger.LogInformation("\tReceived event: {0}", Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray()));

        // Update checkpoint in the blob storage so that the app receives only new events the next time it's run
        await eventArgs.UpdateCheckpointAsync(eventArgs.CancellationToken);
    }

    Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
    {
        // Write details about the error to the console window
        _logger.LogError($"\tPartition '{ eventArgs.PartitionId}': an unhandled exception was encountered. This was not expected to happen.");
        _logger.LogError(eventArgs.Exception.Message);
        return Task.CompletedTask;
    }    
}
