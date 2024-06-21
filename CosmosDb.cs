using Azure.Identity;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Encryption;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace CosmosdbEncryption;

public static class CosmosDb
{
    public static CosmosDbConfiguration CosmosDbConfiguration { get; set; }
    private static readonly Database Database;
  

    static CosmosDb()
    {
        CosmosDbConfiguration = BuildCosmosDbConfiguration();
        Database = GetDatabaseAsync().GetAwaiter().GetResult();
    }

    private static CosmosDbConfiguration? BuildCosmosDbConfiguration()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        return configuration.Get<CosmosDbConfiguration>();
    }

    public static async Task CreateContainerEncryptionPolicyAsync()
    {
        await CreateClientEncryptionKeyAsync();
        await CreateContainerIfNotExistsAsync();
    }

    public static async Task CreateItemsAsync()
    {
        Container containerWithEncryption = await GetContainerWithEncryption();

        SalesOrder order1 = GetSalesOrder("Account1", Guid.NewGuid().ToString());
        SalesOrder order2 = GetSalesOrder("Account2", Guid.NewGuid().ToString());
        order2.SubTotal = 552.4589m;

        await containerWithEncryption.CreateItemAsync(order1, new PartitionKey(order1.AccountNumber));
        await containerWithEncryption.CreateItemAsync(order2, new PartitionKey(order2.AccountNumber));
    }

    public static async Task GetItemById()
    {
        string salesOrderId = "134e833d-0440-4ba6-956a-5d8cc2ecf401";

        Container containerWithEncryption = await GetContainerWithEncryption();

        ItemResponse<SalesOrder> readResponse = await containerWithEncryption.ReadItemAsync<SalesOrder>(salesOrderId, new PartitionKey("Account1"));

        Console.WriteLine(JsonConvert.SerializeObject(readResponse.Resource));
    }

    public static async Task GetItemByQuery()
    {
        Container containerWithEncryption = await GetContainerWithEncryption();
        QueryDefinition queryDefinition = await BuildQueryDefition(containerWithEncryption);

        FeedIterator<SalesOrder> queryResponseIterator = containerWithEncryption.GetItemQueryIterator<SalesOrder>(queryDefinition);

        FeedResponse<SalesOrder> currentResultSet = await queryResponseIterator.ReadNextAsync();

        Console.WriteLine(JsonConvert.SerializeObject(currentResultSet.Resource));
    }

    private static async Task<Database> GetDatabaseAsync()
    {
        string databaseName = CosmosDbConfiguration.DatabaseName;
        
        CosmosClient client = GetClient();

        await client.CreateDatabaseIfNotExistsAsync(databaseName);

        return client.GetDatabase(databaseName);
    }

    private static CosmosClient GetClient()
    {
        DefaultAzureCredential tokenCredential = new DefaultAzureCredential();
        KeyResolver keyResolver = new KeyResolver(tokenCredential);
        CosmosClient cosmosClient = new CosmosClient(CosmosDbConfiguration.ConnectionStringCosmosDb);

        return cosmosClient.WithEncryption(keyResolver, KeyEncryptionKeyResolverName.AzureKeyVault);
    }

    private static async Task CreateClientEncryptionKeyAsync()
    {

        bool clientEncryptionKeyExists = await ValidateIfClientEncryptionKeyAsync();

        if (!clientEncryptionKeyExists)
        {
            EncryptionKeyWrapMetadata encryptionKeyWrapMetadata = GetEncryptionKeyWrapMetadata();
            await Database.CreateClientEncryptionKeyAsync(CosmosDbConfiguration.ClientEncryptionKeyId, DataEncryptionAlgorithm.AeadAes256CbcHmacSha256, encryptionKeyWrapMetadata);
        }
    }

    private static async Task<bool> ValidateIfClientEncryptionKeyAsync()
    {
        bool keyExists = false;

        try
        {
            ClientEncryptionKeyResponse clientEncryptionKeyResponse = await Database.GetClientEncryptionKey(CosmosDbConfiguration.ClientEncryptionKeyId).ReadAsync();
            keyExists = clientEncryptionKeyResponse.StatusCode == System.Net.HttpStatusCode.OK;
        }
        catch
        {
            Console.WriteLine("client encryption key doesn't exist");
        }

        return keyExists;
    }

    private static EncryptionKeyWrapMetadata GetEncryptionKeyWrapMetadata()
    {
        string nameEncryptionKey = "akvKey";

        return new EncryptionKeyWrapMetadata(KeyEncryptionKeyResolverName.AzureKeyVault, nameEncryptionKey, CosmosDbConfiguration.KeyIdentifierKeyVault, EncryptionAlgorithm.RsaOaep.ToString());
    }

    private static ClientEncryptionIncludedPath GetClientEncryptionIncludedPath(string path)
    {
        return new ClientEncryptionIncludedPath
        {
            Path = path,
            ClientEncryptionKeyId = CosmosDbConfiguration.ClientEncryptionKeyId,
            EncryptionType = EncryptionType.Deterministic,
            EncryptionAlgorithm = DataEncryptionAlgorithm.AeadAes256CbcHmacSha256
        };
    }

    private static async Task CreateContainerIfNotExistsAsync()
    {
        ClientEncryptionIncludedPath path1 = GetClientEncryptionIncludedPath("/SubTotal");
        ClientEncryptionIncludedPath path2 = GetClientEncryptionIncludedPath("/Items");
        ClientEncryptionIncludedPath path3 = GetClientEncryptionIncludedPath("/OrderDate");

        await Database.DefineContainer(CosmosDbConfiguration.NameContainer, "/AccountNumber")
          .WithClientEncryptionPolicy()
          .WithIncludedPath(path1)
          .WithIncludedPath(path2)
          .WithIncludedPath(path3)
          .Attach()
          .CreateIfNotExistsAsync();
    }

    private static async Task<Container> GetContainerWithEncryption()
    {
        Container container = Database.GetContainer(CosmosDbConfiguration.NameContainer);

        return await container.InitializeEncryptionAsync();
    }

    private static async Task<QueryDefinition> BuildQueryDefition(Container containerWithEncryption)
    {
        string query = "SELECT * FROM c where c.SubTotal = @SubTotal";

        QueryDefinition withEncryptedParameter = containerWithEncryption.CreateQueryDefinition(query);

        await withEncryptedParameter.AddParameterAsync("@SubTotal", 552.4589m, "/SubTotal");

        return withEncryptedParameter;
    }

    private static SalesOrder GetSalesOrder(string account, string orderId)
    {
        return new SalesOrder
        {
            Id = orderId,
            AccountNumber = account,
            PurchaseOrderNumber = "PO18009186470",
            OrderDate = new DateTime(2005, 7, 1),
            SubTotal = 419.4589m,
            TaxAmount = 12.5838m,
            Freight = 472.3108m,
            TotalDue = 985.018m,
            Items =
            [
                new SalesOrderDetail
                    {
                        OrderQty = 1,
                        ProductId = 760,
                        UnitPrice = 419.4589m,
                        LineTotal = 419.4589m
                    },

                    new SalesOrderDetail
                    {
                        OrderQty = 2,
                        ProductId = 761,
                        UnitPrice = 420.4589m,
                        LineTotal = 420.4589m
                    }
            ],
            TimeToLive = 60 * 60 * 24 * 30
        };
    }
}