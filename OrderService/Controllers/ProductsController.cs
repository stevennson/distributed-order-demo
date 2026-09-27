using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Controllers;
[ApiController]
[Route("api/[controller]")]
public class ProductsController:ControllerBase
{
    private readonly AppDbContext _db;
    public ProductsController(AppDbContext db)
    {
        _db=db;
    }
    [HttpPost]
    public async Task<IActionResult> CreateProduct(Product product)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return Ok(product);
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var product=await _db.Products.FirstOrDefaultAsync(p=>p.Id==id);
        if(product==null)
        {
            return NotFound();
        }
        return Ok(product);
    }
}