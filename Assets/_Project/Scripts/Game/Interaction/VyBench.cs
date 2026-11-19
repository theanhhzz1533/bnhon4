using Bnhon.Core.Content;
using Bnhon.Core.Npcs;
using Bnhon.Game.Player;
using Bnhon.Game.UI;
using UnityEngine;

namespace Bnhon.Game.Interaction
{
    /// <summary>Vy's electric bench. Opens her shop once she has it set up.</summary>
    public sealed class VyBench : MonoBehaviour, IInteractable
    {
        public string Prompt
        {
            get
            {
                var session = GameRoot.Instance.Session;
                if (PrototypeContent.VyShopOpen(session)) return "E: Bàn điện của Vy (mua đồ điện)";
                return Residents.LivesAtBase(session, PrototypeContent.NpcVy)
                    ? "Bàn điện của Vy, chưa ráp xong. Nói chuyện với Vy."
                    : "Một cái bàn trống cạnh giếng.";
            }
        }

        public void Interact(PlayerController player)
        {
            var session = GameRoot.Instance.Session;
            if (!PrototypeContent.VyShopOpen(session) || ShopScreen.Instance == null) return;
            ShopScreen.Instance.Open(session.ShopOf(PrototypeContent.NpcVy), "Bàn điện của Vy");
        }
    }
}
