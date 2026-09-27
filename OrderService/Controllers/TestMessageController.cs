using Microsoft.AspNetCore.Mvc;
using OrderService.Messaging;

namespace OrderService.Controllers;
[ApiController]
[Route("api/test-message")]
public class TestMessageController : ControllerBase
{
    private readonly RabbitMqPublisher _publisher;
    public TestMessageController(RabbitMqPublisher publisher)
    {
        _publisher=publisher;
    }
    [HttpPost]
    public async Task<IActionResult> SendMessage()
    {
        var message=new OrderCreatedEvent()
        {
            EventId=Guid.NewGuid(),
            //EventId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            OrderId=123,
            ProductId=3,
            Quantity=1
        };
        await _publisher.PublishAsync(message);
        return Ok(message);
    }
}