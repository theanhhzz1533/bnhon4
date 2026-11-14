using Bnhon.Game.Player;
using Bnhon.Game.Raid;
using UnityEngine;

namespace Bnhon.Game.Interaction
{
    /// <summary>Sleep to save and advance to the next day.</summary>
    public sealed class Bed : MonoBehaviour, IInteractable
    {
        public string Prompt => "E: Ngủ (sang ngày mới)";

        public void Interact(PlayerController player)
        {
            RaidDirector.Instance.Sleep();
        }
    }
}
