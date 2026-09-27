using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace OrderService.Messaging;
public class RabbitMqPublisher
{
    private readonly ConnectionFactory _factory;
    public RabbitMqPublisher()
    {
        _factory=new ConnectionFactory()
        {
          HostName="localhost",
          Port=5672,
          UserName="guest",
          Password="guest"  
        };
    }
    // public async Task PublishAsync(string message)
    // {
    //     await using var connection=await _factory.CreateConnectionAsync();
    //     await using var channel=await connection.CreateChannelAsync();
    //     await channel.QueueDeclareAsync(
    //         queue:"order-created",
    //         durable:true,
    //         exclusive:false,
    //         autoDelete:false
    //     );
    //     var body=Encoding.UTF8.GetBytes(message);
    //     await channel.BasicPublishAsync(exchange:string.Empty,routingKey:"order-created",body:body);
    // }
    public async Task PublishAsync(OrderCreatedEvent message)
    {
        await using var connection=await _factory.CreateConnectionAsync();
        await using var channel=await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(
            queue:"order-created",
            durable:true,
            exclusive:false,
            autoDelete:false);
        var json=JsonSerializer.Serialize(message); 
        var body=Encoding.UTF8.GetBytes(json);
        await channel.BasicPublishAsync(exchange:string.Empty,routingKey:"order-created",body:body);
    }
}