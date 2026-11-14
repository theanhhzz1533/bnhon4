using Bnhon.Core.Inventory;

namespace Bnhon.Game.UI
{
    /// <summary>
    /// Lets a container keep some of its items unidentified. The inventory
    /// screen draws those as dim unknown blocks and does not let the player
    /// pick them up.
    /// </summary>
    public interface IItemConcealer
    {
        bool IsHidden(PlacedItem item);

        /// <summary>0 to 1 for the item being identified right now, 0 for every other item.</summary>
        float RevealProgress(PlacedItem item);
    }
}
