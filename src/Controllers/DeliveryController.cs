using Microsoft.AspNetCore.Mvc;
using DeliveryHub.Models;
using DeliveryHub.Services;
namespace DeliveryHub.Controllers;
[ApiController, Route("api/delivery")]
public sealed class DeliveryController(DeliveryService service):ControllerBase {
 [HttpPost("calculate")] public IActionResult Calculate(CalculateRequest request)=>Ok(service.Calculate(request));
 [HttpGet("carriers")] public IActionResult Carriers([FromQuery]int periodDays=30,[FromQuery]string? city=null){
   if(periodDays<1 || periodDays>90) return BadRequest(new {error="periodDays должен быть от 1 до 90: учебная история содержит 90 дней."});
   if(city!=null && (string.IsNullOrWhiteSpace(city)||city.Length>100)) return BadRequest(new {error="Некорректный город."});
   return Ok(service.Statistics(periodDays,city));
 }
}
