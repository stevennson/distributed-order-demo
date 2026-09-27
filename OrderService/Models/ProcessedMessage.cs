namespace OrderService.Models;
public class ProcessedMessage
{
    public int Id{get;set;}
    public Guid EventId{get;set;}
    public DateTime ProcessedAt{get;set;}
}