using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    public OrdersController(AppDbContext db)
    {
        _db = db;
    }
    [HttpPost]
    public async Task<IActionResult> CreateOrder(int productId, int quantity)
    {
        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == productId);
        if (product == null)
        {
            return NotFound("Product not found.");
        }
        // if (product.Stock < quantity)
        // {
        //     return BadRequest("Insufficient stock.");
        // }
        //product.Stock-=quantity;
        //使用原子SQL扣减库存
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var affectedRows = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE Products
            SET Stock = Stock - {quantity}
            WHERE Id = {productId}
              AND Stock >= {quantity}
        ");
            if (affectedRows == 0)
            {
                await transaction.RollbackAsync();
                return BadRequest("Insufficient stock.");
            }
            var order = new Order
            {
                ProductId = productId,
                Quantity = quantity,
                TotalPrice = product.Price * quantity,
                CreatedAt = DateTime.UtcNow
            };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            //所有操作成功则提交
            await transaction.CommitAsync();
            return Ok(order);
        }
        catch (System.Exception)
        {
            // 任意一步失败，全部回滚
            await transaction.RollbackAsync();
            throw;
        }


    }
}