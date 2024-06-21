
using Newtonsoft.Json;

namespace CosmosdbEncryption;

public class SalesOrder
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }
    public string AccountNumber { get; set; }

    [JsonProperty(PropertyName = "ponumber")]
    public string PurchaseOrderNumber { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Freight { get; set; }
    public decimal TotalDue { get; set; }
    public SalesOrderDetail[] Items { get; set; }

    [JsonProperty(PropertyName = "ttl", NullValueHandling = NullValueHandling.Ignore)]
    public int TimeToLive { get; set; }
}

public class SalesOrderDetail
{
    public int OrderQty { get; set; }
    public int ProductId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}