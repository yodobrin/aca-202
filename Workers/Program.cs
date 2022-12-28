
using Azure.Identity;



var builder = Host.CreateDefaultBuilder(args);

// when running localy and your user is granted right rbac on the keyvault, u can use this:

builder.ConfigureAppConfiguration((context, config) =>
{
    config.AddAzureKeyVault(
        new Uri($"https://acceptvault.vault.azure.net/"),
        new DefaultAzureCredential());
});

builder.ConfigureAppConfiguration((context, config) =>
{
    string mngId = Environment.GetEnvironmentVariable("AzureADManagedIdentityClientId") ?? string.Empty;
    string keyVaultName = Environment.GetEnvironmentVariable("keyvault") ?? string.Empty;
    // check if the managed identity is not empty, or throw an exception
    if (string.IsNullOrEmpty(mngId)|| string.IsNullOrEmpty(keyVaultName))
    {
        throw new Exception("AzureADManagedIdentityClientId or keyvault is not set");
    }
    
    config.AddAzureKeyVault(
        new Uri($"https://acceptvault.vault.azure.net/"),
        new DefaultAzureCredential(
            new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = mngId
            }
        ));
});



builder.ConfigureServices((hostContext, services) =>
{
    services.AddHostedService<Worker>();
});

IHost host = builder.Build();


host.Run();
