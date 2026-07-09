namespace BaroManager.Models;

public class ManagedItemCollection
{
    public int ManagedItemId { get; set; }

    public ManagedItem ManagedItem { get; set; } = null!;

    public int CollectionId { get; set; }

    public ManagedCollection Collection { get; set; } = null!;
}