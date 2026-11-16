using Bnhon.Game.Player;
using Bnhon.Game.UI;
using UnityEngine;

namespace Bnhon.Game.Interaction
{
    /// <summary>The cooking stove in the base. Opens the kitchen menu.</summary>
    public sealed class KitchenStation : MonoBehaviour, IInteractable
    {
        public string Prompt => "E: Nấu ăn";

        public void Interact(PlayerController player)
        {
            if (KitchenScreen.Instance != null) KitchenScreen.Instance.Open();
        }
    }
}
