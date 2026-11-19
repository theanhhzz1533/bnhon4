using Bnhon.Core.Content;
using Bnhon.Core.Npcs;
using Bnhon.Game.Player;
using Bnhon.Game.UI;
using UnityEngine;

namespace Bnhon.Game.Interaction
{
    /// <summary>Quân's lab bench. Opens the charm screen once he has set it up.</summary>
    public sealed class LabStation : MonoBehaviour, IInteractable
    {
        public string Prompt
        {
            get
            {
                var session = GameRoot.Instance.Session;
                if (PrototypeContent.LabOpen(session)) return "E: Phòng thí nghiệm (pha bùa)";
                return Residents.LivesAtBase(session, PrototypeContent.NpcQuan)
                    ? "Phòng thí nghiệm chưa dựng xong. Nói chuyện với Quân."
                    : "Một góc trống. Chưa ai dùng tới.";
            }
        }

        public void Interact(PlayerController player)
        {
            if (!PrototypeContent.LabOpen(GameRoot.Instance.Session)) return;
            if (CharmScreen.Instance != null) CharmScreen.Instance.Open();
        }
    }
}
