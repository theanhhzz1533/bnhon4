using Bnhon.Game.Player;
using Bnhon.Game.UI;
using UnityEngine;

namespace Bnhon.Game.Interaction
{
    /// <summary>The trading stall in the base. Opens the shop menu.</summary>
    public sealed class ShopStall : MonoBehaviour, IInteractable
    {
        public string Prompt
        {
            get
            {
                var session = GameRoot.Instance.Session;
                if (Bnhon.Core.Content.PrototypeContent.ShopOpen(session)) return "E: Mua bán với Chú Sáu";
                return Bnhon.Core.Npcs.Residents.LivesAtBase(session, Bnhon.Core.Content.PrototypeContent.NpcChuSau)
                    ? "Quầy trống. Nói chuyện với Chú Sáu để mở lại quầy."
                    : "Quầy trống, chưa có ai đứng bán.";
            }
        }

        public void Interact(PlayerController player)
        {
            if (!Bnhon.Core.Content.PrototypeContent.ShopOpen(GameRoot.Instance.Session)) return;
            if (ShopScreen.Instance != null) ShopScreen.Instance.Open();
        }
    }
}
