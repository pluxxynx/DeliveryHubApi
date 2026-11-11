using Microsoft.AspNetCore.Mvc;
using DeliveryHub.Models;
using DeliveryHub.Services;
namespace DeliveryHub.Controllers;
[ApiController, Route("api/batch")]
public sealed class BatchController(BatchService service):ControllerBase {
 [HttpGet] public IActionResult Get()=>Ok(service.Get());
 [HttpPost("optimize")] public IActionResult Optimize(OptimizeRequest request){
   if(request.Orders.Any(o=>o==null || o.ReadyAt==default || o.ReadyAt.Year>2100 || o.ReadyAt.Year<2000)) return BadRequest(new {error="Для каждого заказа требуется readyAt с годом от 2000 до 2100."});
   if(request.Orders.Select(o=>o.Id.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=request.Orders.Count) return BadRequest(new {error="Идентификаторы заказов должны быть уникальны."});
   return Ok(service.Optimize(request));
 }
}
