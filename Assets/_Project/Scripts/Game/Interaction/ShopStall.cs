using Bnhon.Game.Player;
using Bnhon.Game.UI;
using UnityEngine;

namespace Bnhon.Game.Interaction
{
    /// <summary>The trading stall in the base. Opens the shop menu.</summary>
    public sealed class ShopStall : MonoBehaviour, IInteractable
    {
        public string Prompt => "E: Mua bán";

        public void Interact(PlayerController player)
        {
            if (ShopScreen.Instance != null) ShopScreen.Instance.Open();
        }
    }
}
