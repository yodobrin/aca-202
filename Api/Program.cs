using Azure.Identity;


var builder = WebApplication.CreateBuilder(args);

var mngId = Environment.GetEnvironmentVariable("AzureADManagedIdentityClientId") ?? string.Empty;

// if mngId is empty, try to login using the default credentials (usualy running locally)

if(string.IsNullOrEmpty(mngId))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri($"https://{builder.Configuration["keyvault"]}.vault.azure.net/"),
        new DefaultAzureCredential());
}
else
{
    builder.Configuration.AddAzureKeyVault(
        new Uri($"https://{builder.Configuration["keyvault"]}.vault.azure.net/"),
        new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = builder.Configuration["AzureADManagedIdentityClientId"]
        }));
}


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// app.UseAuthorization();

app.MapControllers();

app.Run();
