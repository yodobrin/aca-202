using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Azure.Storage.Blobs;

using System.Text;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    private IConfiguration _configuration;


    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string storageCS = _configuration.GetValue<string>("StorageCS") ?? string.Empty;
        string queueName = _configuration.GetValue<string>("TaskQueueName") ?? string.Empty;
        string blobContainerName = _configuration.GetValue<string>("TaskBlobContainerName") ?? string.Empty;

        // verify the above is not empty and throw an exception if it is
        if (string.IsNullOrEmpty(storageCS) || string.IsNullOrEmpty(queueName) || string.IsNullOrEmpty(blobContainerName))
        {
            _logger.LogError("StorageCS, TaskQueueName or TaskBlobContainerName is not set in the configuration");
            throw new Exception("StorageCS, TaskQueueName or TaskBlobContainerName is not set in the configuration");
        }
        QueueClient queueClient = new QueueClient(storageCS, queueName);
        await queueClient.CreateIfNotExistsAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            // read a mesasge from the queue
            QueueMessage msg = await queueClient.ReceiveMessageAsync();
            // verify valid message
            if (msg == null)
            {
                _logger.LogInformation("No message received");
                await Task.Delay(1000, stoppingToken);
                continue;
            }
            string body = msg.Body.ToString();
            _logger.LogInformation($"Received message {msg.MessageId} with content {body}");
            // marshal the message to a TaskRequest object
            TaskRequest taskRequest = TaskRequest.FromJson(body);
            // update the blob with new status
            BlobContainerClient blobContainerClient = new BlobContainerClient(storageCS, blobContainerName);
            await blobContainerClient.CreateIfNotExistsAsync();
            BlobClient blobClient = blobContainerClient.GetBlobClient($"{taskRequest.TaskId.ToString()}-202");
            // update the task status
            taskRequest.Status = TaskRequest.TaskStatus.Completed;
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(taskRequest.ToJson()));
            // upload and overwrite the blob
            await blobClient.UploadAsync(stream, overwrite: true);
            // delete the message from the queue
            await queueClient.DeleteMessageAsync(msg.MessageId, msg.PopReceipt);
            
            _logger.LogInformation("Worker processed task id {id} at: {time}",taskRequest.TaskId, DateTimeOffset.Now);
            await Task.Delay(10, stoppingToken);
        }
    }
}
