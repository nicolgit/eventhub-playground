using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Hosting;

public class Worker : BackgroundService
{
    public Worker()
    {
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using (var producerClient = new EventHubProducerClient(Settings.EventHubConnectionString, Settings.EventHubName))
        {
            try 
            {
                Console.WriteLine($"Worker running at: {DateTimeOffset.Now}");

                while (!stoppingToken.IsCancellationRequested)
                {
                    Console.WriteLine("Calling EventHUB BEGIN");
             
                     // Create a batch of events 
                    using EventDataBatch eventBatch = await producerClient.CreateBatchAsync(new CreateBatchOptions() { PartitionKey = "worker01" });
                    
                    eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes($"1st Event Timestamp { DateTime.Now.ToLongTimeString() } - {DateTime.Now.Millisecond}")));
                    await Task.Delay(100);
                    eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes($"2nd event Timestamp { DateTime.Now.ToLongTimeString() } - {DateTime.Now.Millisecond}")));
                    await Task.Delay(100);
                    eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes($"3rd event Timestamp { DateTime.Now.ToLongTimeString() } - {DateTime.Now.Millisecond}")));
                    await Task.Delay(100);
                    
                    // Use the producer client to send the batch of events to the event hub
                    await producerClient.SendAsync (eventBatch);

                    Console.WriteLine("Calling EventHUB completed");
                    await Task.Delay(4000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }

           
        }
    }
}