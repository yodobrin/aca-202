using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Queues;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure.Storage.Blobs.Models;
using System.Text;


namespace Aca.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class TasksController : ControllerBase
{
    

    private readonly ILogger<TasksController> _logger;

    private IConfiguration _configuration;

    

    public TasksController(ILogger<TasksController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

     [HttpPost("QueueTask")]
    public async Task<ActionResult<string>> QueueTaskRequest(TaskRequest item)
    {
        string storageCS = _configuration.GetValue<string>("StorageCS") ?? string.Empty;
        string queueName = _configuration.GetValue<string>("TaskQueueName") ?? string.Empty;
        string blobContainerName = _configuration.GetValue<string>("TaskBlobContainerName") ?? string.Empty;
        double sasTokenExpiry = _configuration.GetValue<double>("SasTokenExpiry");
        // verify none of the above is empty
        if (string.IsNullOrEmpty(storageCS) || string.IsNullOrEmpty(queueName) || string.IsNullOrEmpty(blobContainerName))
        {
            _logger.LogError("StorageCS, TaskQueueName or TaskBlobContainerName is not set in the configuration");
            return BadRequest("StorageCS, TaskQueueName or TaskBlobContainerName is not set in the configuration");
        }

        try{
            QueueClient queueClient = new QueueClient(storageCS, queueName);
            await queueClient.CreateIfNotExistsAsync();
            // generate a blob and update the task with blob uri
            BlobContainerClient blobContainerClient = new BlobContainerClient(storageCS,blobContainerName);
            await blobContainerClient.CreateIfNotExistsAsync();
            
            string blobName = $"{item.TaskId.ToString()}-202";
            BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);
            item.returnUri = blobClient.Uri.ToString();
            
            // upload the task content to blob
            _logger.LogInformation($"Uploading task {item.TaskId} to blob {blobName} with content {item.ToJson()}");
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(item.ToJson()));
            await blobClient.UploadAsync(stream);
            
            //push to queue
            await queueClient.SendMessageAsync(item.ToJson());
            return Accepted(CreateSasToken(blobClient,blobContainerName,blobName,sasTokenExpiry) );
        }catch(Exception ex){
            _logger.LogError(ex, "Error in processing the TaskRequest");
            return BadRequest(ex.Message);
        }

    }

    private string CreateSasToken(BlobClient blob, string blobContainerName,string blobName,double sasTokenExpiry)
    {
    
        if(!blob.Exists())
        {
            _logger.LogError($"File {blobName} does not exist");
            return $"File {blobName} not found";
        }
        BlobSasBuilder sasBuilder = new BlobSasBuilder()
        {
            BlobContainerName = blobContainerName,
            BlobName = blobName,
            Resource = "b"
        };
        sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(sasTokenExpiry);
        sasBuilder.SetPermissions(BlobSasPermissions.Read); 

        return blob.GenerateSasUri(sasBuilder).AbsoluteUri;

    }

    
}


