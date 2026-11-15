using Bnhon.Game.Player;
using Bnhon.Game.UI;
using UnityEngine;

namespace Bnhon.Game.Interaction
{
    /// <summary>The workbench in the base. Opens the crafting menu.</summary>
    public sealed class WorkbenchStation : MonoBehaviour, IInteractable
    {
        public string Prompt => "E: Bàn chế tạo (cấp " + GameRoot.Instance.Session.Workshop.Level + ")";

        public void Interact(PlayerController player)
        {
            if (WorkbenchScreen.Instance != null) WorkbenchScreen.Instance.Open();
        }
    }
}
