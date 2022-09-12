using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Microsoft.Extensions.Hosting;
using Azure.Storage.Blobs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Messaging.EventHubs.Producer;
using System.Linq;
using Azure.Messaging.EventHubs.Primitives;
using System.Collections.Generic;

public class ReadBatch_Worker : BackgroundService
{
    
    public ReadBatch_Worker()
    {
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string consumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;

        using CancellationTokenSource cancellationSource = new CancellationTokenSource();
        cancellationSource.CancelAfter(TimeSpan.FromSeconds(999));

        string firstPartition;

        await using (var producer = new EventHubProducerClient(Settings.EventHubConnectionString,  Settings.EventHubName))
        {
            firstPartition = (await producer.GetPartitionIdsAsync()).First();
        }

        var receiver = new PartitionReceiver(
            consumerGroup,
            firstPartition,
            EventPosition.Earliest,
            Settings.EventHubConnectionString,
            Settings.EventHubName);

        try
        {
            while (!cancellationSource.IsCancellationRequested)
            {
                int batchSize = 20;
                TimeSpan waitTime = TimeSpan.FromSeconds(2);

                IEnumerable<EventData> eventBatch = await receiver.ReceiveBatchAsync(
                    batchSize,
                    waitTime,
                    cancellationSource.Token);

                Console.WriteLine($"BEGIN Batch Read from { firstPartition }");
                foreach (EventData eventData in eventBatch)
                {
                    
                    byte[] eventBodyBytes = eventData.EventBody.ToArray();
                    
                    Console.WriteLine($"{ DateTime.Now.ToLongTimeString() } - {DateTime.Now.Millisecond + 1000} -{eventData.PartitionKey}- -{eventData.SequenceNumber}- Received event: { Encoding.UTF8.GetString(eventBodyBytes) }");
                }
                Console.WriteLine($"END Batch Read");
            }
        }
        catch (TaskCanceledException tce)
        {
            Console.WriteLine($"Cancellation Requested: {tce.Message}");
            // This is expected if the cancellation token is
            // signaled.
        }
        finally
        {
            await receiver.CloseAsync();
        }   
    }
}
