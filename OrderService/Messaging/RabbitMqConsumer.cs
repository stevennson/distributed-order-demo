using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using OrderService.Data;
using OrderService.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderService.Messaging;

public class RabbitMqConsumer : BackgroundService
{
    private readonly ConnectionFactory _factory;
    private readonly IServiceScopeFactory _scopeFactory;
    public RabbitMqConsumer(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory=scopeFactory;
        _factory = new ConnectionFactory()
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest"
        };
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var connection = await _factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(
            queue: "order-created",
            durable: true,
            exclusive: false,
            autoDelete: false
        );
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<OrderCreatedEvent>(json);
                if (message == null)
                {
                    await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    return;
                }
                Console.WriteLine(
                    $"[Consumer] Received EventId={message.EventId}, " +
                    $"OrderId={message.OrderId}, " +
                    $"ProductId={message.ProductId}, " +
                    $"Quantity={message.Quantity}");
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await using var transaction=await db.Database.BeginTransactionAsync();
                //1.检查消息是否已处理过
                var alreadyProcessed = await db.ProcessedMessages.AnyAsync(x => x.EventId == message.EventId);
                if (alreadyProcessed)
                {
                    Console.WriteLine(
                        $"[Consumer] Duplicate message: " +
                        $"{message.EventId}");
                    await transaction.CommitAsync();
                    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                    return;
                }
                //2.模拟真正的业务处理
                var product=await db.Products.FirstOrDefaultAsync(x=>x.Id==message.ProductId);
                if (product == null)
                {
                    Console.WriteLine(
                        $"[Consumer] Product not found: " +
                        $"{message.ProductId}");
                    await transaction.RollbackAsync();
                    await channel.BasicNackAsync(deliveryTag:ea.DeliveryTag,multiple:false,requeue:false);
                    return;
                }
                // Console.WriteLine(
                //     $"[Consumer] Processing OrderId=" +
                //     $"{message.OrderId}");
                // await Task.Delay(100);

                //check stock
                if (product.Stock < message.Quantity)
                {
                    Console.WriteLine(
                        $"[Consumer] Insufficient stock. " +
                        $"ProductId={message.ProductId}, " +
                        $"Stock={product.Stock}, " +
                        $"Quantity={message.Quantity}");
                    await transaction.RollbackAsync();
                    await channel.BasicNackAsync(deliveryTag:ea.DeliveryTag,multiple:false,requeue:false);
                    return;
                }
                //减去库存
                product.Stock-=message.Quantity;
                Console.WriteLine(
                    $"[Consumer] Stock deducted. " +
                    $"ProductId={product.Id}, " +
                    $"RemainingStock={product.Stock}");

                //3.记录EventId
                db.ProcessedMessages.Add(new ProcessedMessage()
                {
                    EventId = message.EventId,
                    ProcessedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
                //最终提交transaction
                await transaction.CommitAsync();

                Console.WriteLine(
                    $"[Consumer] Transaction committed. " +
                    $"EventId={message.EventId}");

                //4.业务成功后ACK
                await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                Console.WriteLine(
                    $"[Consumer] Message ACKed." +
                    $"EventId={message.EventId}");
                //throw new Exception("Simulated processing failure");
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(
                    $"[Consumer] Error: {ex.Message}");
                await channel.BasicNackAsync(deliveryTag:ea.DeliveryTag,multiple:false,requeue:true);
            }
            
        };
        await channel.BasicConsumeAsync(
            queue: "order-created",
            autoAck: false,
            consumer: consumer);

        Console.WriteLine(
            "[Consumer] Waiting for messages...");

        await Task.Delay(
            Timeout.Infinite,
            stoppingToken);
    }
}