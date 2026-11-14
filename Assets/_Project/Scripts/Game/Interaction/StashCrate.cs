using Bnhon.Game.Player;
using UnityEngine;

namespace Bnhon.Game.Interaction
{
    /// <summary>Prototype stash: one press moves the whole backpack into the base stash.</summary>
    public sealed class StashCrate : MonoBehaviour, IInteractable
    {
        public string Prompt => "E: Cất hết đồ trong balo vào kho";

        public void Interact(PlayerController player)
        {
            var root = GameRoot.Instance;
            var session = root.Session;
            if (session.Backpack.Items.Count == 0)
            {
                root.Notify("Balo đang trống.");
                return;
            }

            int value = session.Backpack.TotalValue();
            int leftBehind = session.Backpack.MoveAllTo(session.Stash);
            root.Notify(leftBehind == 0
                ? "Đã cất đồ vào kho (giá trị " + value + ")."
                : "Kho đầy, còn " + leftBehind + " món trong balo.");
        }
    }
}
