using System.ComponentModel.DataAnnotations;
namespace DeliveryHub.Models;
public sealed class Address {
 [Required, StringLength(100, MinimumLength=1)] public string City {get;set;} = "";
 [Required, RegularExpression(@"^\d{6}$")] public string PostalCode {get;set;} = "";
}
public sealed class CalculateRequest {
 [Required] public Address Sender {get;set;} = null!;
 [Required] public Address Receiver {get;set;} = null!;
 [Range(typeof(decimal),"0.001","1000", ParseLimitsInInvariantCulture=true)] public decimal WeightKg {get;set;}
 [Range(typeof(decimal),"0.1","300", ParseLimitsInInvariantCulture=true)] public decimal LengthCm {get;set;}
 [Range(typeof(decimal),"0.1","300", ParseLimitsInInvariantCulture=true)] public decimal WidthCm {get;set;}
 [Range(typeof(decimal),"0.1","300", ParseLimitsInInvariantCulture=true)] public decimal HeightCm {get;set;}
 [Required, RegularExpression("^(standard|express|economy)$")] public string Priority {get;set;} = "standard";
 [Range(typeof(decimal),"0","10000000", ParseLimitsInInvariantCulture=true)] public decimal DeclaredValue {get;set;}
}
public sealed class Order {
 [Required, StringLength(100)] public string Id {get;set;} = "";
 [Required, StringLength(100)] public string DestinationCity {get;set;} = "";
 [Range(typeof(decimal),"0.001","1000", ParseLimitsInInvariantCulture=true)] public decimal WeightKg {get;set;}
 public DateTimeOffset ReadyAt {get;set;}
}
public sealed class OptimizeRequest {
 [Required, MinLength(1), MaxLength(500)] public List<Order> Orders {get;set;} = [];
 [Range(0,168)] public int MaxWaitHours {get;set;} = 8;
}
public record Quote(string CarrierName, decimal Price, int Days, decimal Reliability, decimal Score);
public record Batch(Guid BatchId, string DestinationCity, string[] ShipmentIds, decimal TotalWeightKg, DateTimeOffset DispatchAt, decimal OriginalCost, decimal BatchCost, decimal SavedAmount, string Explanation, DateTimeOffset CreatedAt);

