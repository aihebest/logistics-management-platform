namespace LogisticsApi.Models.Entities;

public class MaterialTransportItem
{
    public Guid Id { get; set; }
    public Guid MaterialTransportRequestId { get; set; }
    public int SNo { get; set; }
    public string Material { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Quantity { get; set; }

    public MaterialTransportRequest Request { get; set; } = null!;
}
