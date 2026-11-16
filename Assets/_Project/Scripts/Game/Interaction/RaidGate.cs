using Bnhon.Core;
using Bnhon.Core.Content;
using Bnhon.Core.Raid;
using Bnhon.Game.Player;
using Bnhon.Game.Raid;
using Bnhon.Game.UI;
using UnityEngine;

namespace Bnhon.Game.Interaction
{
    /// <summary>The gate out of the base. Starts today's raid, asking where to once there is a choice.</summary>
    public sealed class RaidGate : MonoBehaviour, IInteractable
    {
        public string Prompt => GameRoot.Instance.Session.RaidedToday
            ? "Hôm nay đã đi raid. Hãy ngủ để sang ngày mới."
            : "E: Ra ngoài (bắt đầu raid)";

        public void Interact(PlayerController player)
        {
            GameSession session = GameRoot.Instance.Session;
            if (session.RaidedToday)
            {
                GameRoot.Instance.Notify("Hôm nay bạn đã đi raid rồi. Hãy ngủ để sang ngày mới.");
                return;
            }

            RaidArea only = null;
            int unlocked = 0;
            foreach (RaidArea area in PrototypeContent.RaidAreas)
            {
                if (!area.IsUnlocked(session)) continue;
                unlocked++;
                only = area;
            }

            if (unlocked > 1 && AreaScreen.Instance != null) AreaScreen.Instance.Open();
            else RaidDirector.Instance.TryStartRaid(only);
        }
    }
}
