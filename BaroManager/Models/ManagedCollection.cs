namespace BaroManager.Models;

public class ManagedCollection
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<ManagedItemCollection> Items { get; set; } = new List<ManagedItemCollection>();
}