using System.Text.Json;
using DeliveryHub.Models;
namespace DeliveryHub.Services;
public sealed class BatchService {
 private readonly object gate=new();
 private readonly string path;
 private List<Batch> batches;
 public BatchService(IConfiguration config) {
   var dir=config["DATA_DIR"]??Path.Combine(AppContext.BaseDirectory,"data");
   Directory.CreateDirectory(dir); path=Path.Combine(dir,"batches.json");
   batches=File.Exists(path)?JsonSerializer.Deserialize<List<Batch>>(File.ReadAllText(path))??[]:[];
 }
 public Batch[] Get(){lock(gate) return batches.ToArray();}
 public object Optimize(OptimizeRequest r){
   var result=new List<Batch>();
   foreach(var city in r.Orders.GroupBy(o=>o.DestinationCity.Trim(),StringComparer.OrdinalIgnoreCase)) {
     var sorted=city.OrderBy(o=>o.ReadyAt).ThenBy(o=>o.Id).ToList();
     while(sorted.Count>0){
       var deadline=sorted[0].ReadyAt.AddHours(r.MaxWaitHours);
       var group=sorted.TakeWhile(o=>o.ReadyAt<=deadline).ToArray();
       sorted.RemoveRange(0,group.Length);
       decimal weight=group.Sum(o=>o.WeightKg);
       // Same educational tariff before and after grouping: one fixed dispatch fee per batch.
       decimal original=group.Length*200m+40m*weight, combined=200m+40m*weight;
       result.Add(new Batch(Guid.NewGuid(),city.Key,group.Select(o=>o.Id).ToArray(),weight,group.Max(o=>o.ReadyAt),original,combined,original-combined,$"Один город; разница готовности не более {r.MaxWaitHours} ч. Учебная модель 200 руб. за отправку + 40 руб./кг.",DateTimeOffset.UtcNow));
     }
   }
   lock(gate){
     var next=batches.Concat(result).TakeLast(5000).ToList();
     var temp=path+".tmp";
     File.WriteAllText(temp,JsonSerializer.Serialize(next));
     File.Move(temp,path,true); batches=next;
   }
   return new {batches=result,totalSavings=result.Sum(b=>b.SavedAmount),tariffMode="educational"};
 }
}
