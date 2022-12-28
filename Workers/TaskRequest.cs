
// this class would be used to create a new task and monitor its progress
using Newtonsoft.Json;


public class TaskRequest
{
    public enum TaskPriority
    {
        Low,
        Medium,
        High
    }

    public enum TaskStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }

    

    public string TaskName { get; set; }
    public string TaskDescription { get; set; }
    public TaskStatus Status { get; set; }
    
    public Guid TaskId { get; set; }

    public TaskPriority Priority { get; set; }

    public string ?returnUri { get; set; }

    public TaskRequest()
    {
        TaskName = GenerateRandomString(5);
        TaskDescription = GenerateRandomString(30);
        Status = RandomStatus();
        Priority = RandomPriority();
        TaskId = Guid.NewGuid();
    }
    // publuic method to convert the task to a json string
    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }

    public static TaskRequest FromJson(string json)
    {
        return JsonConvert.DeserializeObject<TaskRequest>(json);
    }
    // private method to randomly select task status
    private TaskStatus RandomStatus()
    {
        var random = new Random();
        var values = Enum.GetValues(typeof(TaskStatus));
        return (TaskStatus)values.GetValue(random.Next(values.Length));
    }

    // private method to randomly select task priority
    private TaskPriority RandomPriority()
    {
        var random = new Random();
        var values = Enum.GetValues(typeof(TaskPriority));
        return (TaskPriority)values.GetValue(random.Next(values.Length));
    }
    // private method to generate a random string for the task attributes by length
    private string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[new Random().Next(s.Length)]).ToArray());
    }
}

