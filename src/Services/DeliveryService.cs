using DeliveryHub.Models;
namespace DeliveryHub.Services;
public sealed class DeliveryService {
 public static readonly (string Name, decimal Base, decimal Kg, int Days, decimal Reliability)[] Tariffs = [("СДЭК",300m,60m,3,.97m),("Boxberry",250m,50m,4,.95m),("Почта России",200m,40m,6,.92m)];
 public object Calculate(CalculateRequest r) {
   var weights = r.Priority switch {"economy" => (Price: .8m, Days: .1m, Reliability: .1m),"express" => (Price: .1m, Days: .8m, Reliability: .1m),_ => (Price: .4m, Days: .3m, Reliability: .3m)};
   var billable = Math.Max(r.WeightKg,r.LengthCm*r.WidthCm*r.HeightCm/5000m);
   bool local = string.Equals(r.Sender.City.Trim(),r.Receiver.City.Trim(),StringComparison.OrdinalIgnoreCase);
   var quotes = Tariffs.Select(t => {
     var price = Math.Round((t.Base+t.Kg*billable)*(local?.8m:1m)+r.DeclaredValue*.005m,2,MidpointRounding.AwayFromZero);
     var days = local?Math.Max(1,t.Days-1):t.Days;
     return new Quote(t.Name,price,days,t.Reliability,weights.Price/price+weights.Days/days+weights.Reliability*t.Reliability);
   }).OrderByDescending(q=>q.Score).ThenBy(q=>q.Price).ToArray();
   return new {selected=quotes[0],alternatives=quotes.Skip(1),billableWeightKg=billable,priority=r.Priority,weights=new {price=weights.Price,days=weights.Days,reliability=weights.Reliability},tariffMode="educational",explanation=$"Выбран {quotes[0].CarrierName}: максимальный балл wPrice/price + wDays/days + wReliability*reliability. Учтён объёмный вес. Тарифы учебные; расстояние между городами не рассчитывается."};
 }
 public object Statistics(int periodDays,string? city) {
   // Reproducible educational delivery events, never actual carrier performance.
   var now=DateTimeOffset.UtcNow;
   var events=Enumerable.Range(0,90).SelectMany(day=>Enumerable.Range(0,3).Select(i=>new {At=now.AddDays(-day),Carrier=Tariffs[i].Name,City=day%2==0?"Москва":"Казань",Price=Tariffs[i].Base+Tariffs[i].Kg*(1+day%5),OnTime=(day+i)%10!=0,Damaged=(day+i)%25==0}));
   var filtered=events.Where(x=>x.At>now.AddDays(-periodDays) && (city==null || string.Equals(x.City,city.Trim(),StringComparison.OrdinalIgnoreCase))).ToArray();
   return new {periodDays,city,dataSource="educational-fixtures",carriers=Tariffs.Select(t=> {var rows=filtered.Where(x=>x.Carrier==t.Name).ToArray();return new {carrierName=t.Name,ordersCount=rows.Length,avgPrice=rows.Length==0?0:Math.Round(rows.Average(x=>x.Price),2),onTimeRate=rows.Length==0?(decimal?)null:Math.Round((decimal)rows.Count(x=>x.OnTime)/rows.Length,4),damageRate=rows.Length==0?(decimal?)null:Math.Round((decimal)rows.Count(x=>x.Damaged)/rows.Length,4)};})};
 }
}

