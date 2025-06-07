using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderService.Models;

namespace OrderService.Controllers
{
    [Route("api/[controller]/")]
    [ApiController]
    public class OrdersController : ControllerBase
    {

        private static readonly List<Order> orders = new()
        {
            new Order(1,1,2, DateTime.UtcNow.AddDays(-1)),
            new Order(2,2,3, DateTime.UtcNow.AddHours(-2))
        };

        [HttpGet]
        public ActionResult<IEnumerable<Order>> GetOrders()
        {
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public IActionResult GetOrderById(int id)
        {
            var order = orders.FirstOrDefault(o => o.Id == id);

            if(order == null)
            {
                return NotFound();
            }

            return Ok(order);
        }
    }
}
