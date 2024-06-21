namespace CosmosdbEncryption;

public class CosmosDbConfiguration
{
    public required string ConnectionStringCosmosDb { get; set; }
    public required string KeyIdentifierKeyVault { get; set; }
    public required string ClientEncryptionKeyId { get; set; }
    public required string NameContainer { get; set; }
    public required string DatabaseName { get; set; }
}
