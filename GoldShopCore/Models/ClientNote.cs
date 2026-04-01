namespace GoldShopCore.Models;

public class ClientNote
{
    public int Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
