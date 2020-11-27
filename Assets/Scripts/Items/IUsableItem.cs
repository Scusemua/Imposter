public interface IUsableItem
{
    /// <summary>
    /// The item's name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The item's unique ID.
    /// </summary>
    int ItemId { get; }
}