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
        bool shownWaitMessage = false;
        while (!stoppingToken.IsCancellationRequested)
        {
            // read a mesasge from the queue
            QueueMessage msg = await queueClient.ReceiveMessageAsync();
            // verify valid message
            
            if (msg == null)
            {
                if(!shownWaitMessage) _logger.LogInformation("Waiting for more messages ...");
                else shownWaitMessage = true;
                await Task.Delay(5000, stoppingToken);
                continue;
            }
            shownWaitMessage = false;
            string body = msg.Body.ToString();
            _logger.LogInformation($"Received message {msg.MessageId} with content {body}");
            // marshal the message to a TaskRequest object
            // wrap the next call in a try catch block
            TaskRequest taskRequest;
            try{
                taskRequest = TaskRequest.FromJson(body);
            }catch(Exception ex){
                _logger.LogError($"Error parsing message {msg.MessageId} with content {body} to TaskRequest object. Error: {ex.Message}, removing message from queue.");
                // delete the message from the queue
                await queueClient.DeleteMessageAsync(msg.MessageId, msg.PopReceipt);
                continue;
            }
            try{
                // update the blob with new status
                BlobContainerClient blobContainerClient = new BlobContainerClient(storageCS, blobContainerName);
                await blobContainerClient.CreateIfNotExistsAsync();
                BlobClient blobClient = blobContainerClient.GetBlobClient($"{taskRequest.TaskId.ToString()}-202");
                // update the task status
                taskRequest.Status = TaskRequest.TaskStatus.Completed;
                Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(taskRequest.ToJson()));
                // upload and overwrite the blob
                await blobClient.UploadAsync(stream, overwrite: true);

            }catch(Exception ex){
                _logger.LogError($"Error updating blob for message {msg.MessageId} with content {body}. Error: {ex.Message}, removing message from queue.");
                // delete the message from the queue
                await queueClient.DeleteMessageAsync(msg.MessageId, msg.PopReceipt);
                continue;
            }
            // delete the message from the queue
            await queueClient.DeleteMessageAsync(msg.MessageId, msg.PopReceipt);
            
            _logger.LogInformation($"Worker processed task id {taskRequest.TaskId} at: { DateTimeOffset.Now}");
            // await Task.Delay(10, stoppingToken);
        }
    }
}
