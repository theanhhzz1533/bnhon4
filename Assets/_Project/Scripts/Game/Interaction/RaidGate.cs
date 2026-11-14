using Bnhon.Game.Player;
using Bnhon.Game.Raid;
using UnityEngine;

namespace Bnhon.Game.Interaction
{
    /// <summary>The gate out of the base. Starts today's raid.</summary>
    public sealed class RaidGate : MonoBehaviour, IInteractable
    {
        public string Prompt => GameRoot.Instance.Session.RaidedToday
            ? "Hôm nay đã đi raid. Hãy ngủ để sang ngày mới."
            : "E: Ra ngoài (bắt đầu raid)";

        public void Interact(PlayerController player)
        {
            RaidDirector.Instance.TryStartRaid();
        }
    }
}
