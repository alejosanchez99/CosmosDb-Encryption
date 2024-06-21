using CosmosdbEncryption;

CosmosDb.CreateContainerEncryptionPolicyAsync().GetAwaiter().GetResult();
CosmosDb.CreateItemsAsync().GetAwaiter().GetResult();
CosmosDb.GetItemById().GetAwaiter().GetResult();
CosmosDb.GetItemByQuery().GetAwaiter().GetResult();
