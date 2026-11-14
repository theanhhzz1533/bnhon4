using Bnhon.Game.Player;
using Bnhon.Game.UI;
using UnityEngine;

namespace Bnhon.Game.Interaction
{
    /// <summary>The base stash. Opens the inventory screen with the stash next to the backpack.</summary>
    public sealed class StashCrate : MonoBehaviour, IInteractable
    {
        public string Prompt => "E: Mở kho";

        public void Interact(PlayerController player)
        {
            if (InventoryScreen.Instance == null) return;
            InventoryScreen.Instance.Open(GameRoot.Instance.Session.Stash, "Kho căn cứ");
        }
    }
}
