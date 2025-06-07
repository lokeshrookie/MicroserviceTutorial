using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductService.Models;
using System.Runtime;

namespace ProductService.Controllers
{
    [Route("api/[controller]/")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private static readonly List<Product> products = new List<Product>()
        {
            new Product { Id = 1, Name = "Laptop", Price = 999.9m},
            new Product { Id = 1, Name = "Mouse", Price = 24.9m},            
            new Product { Id = 1, Name = "Keyboard", Price = 49.9m},
        };


        [HttpGet]
        public ActionResult<IEnumerable<Product>> GetProducts()
        {
            return Ok(products);
        }


        [HttpGet("{id}")]
        public ActionResult<Product> GetProduct(int id)
        {
            var product = products.FirstOrDefault(x => x.Id == id);

            if(product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }


    }
}
