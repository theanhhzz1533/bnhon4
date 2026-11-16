using Bnhon.Core.Farming;
using Bnhon.Game.Audio;
using Bnhon.Game.Player;
using UnityEngine;

namespace Bnhon.Game.Interaction
{
    /// <summary>The well in the base. Refills the watering can.</summary>
    public sealed class Well : MonoBehaviour, IInteractable
    {
        public string Prompt
        {
            get
            {
                WateringCan can = GameRoot.Instance.Session.WateringCan;
                return can.IsFull
                    ? "Giếng nước · bình tưới đầy " + can.Water + "/" + can.Capacity
                    : "E: Múc nước · bình tưới " + can.Water + "/" + can.Capacity;
            }
        }

        public void Interact(PlayerController player)
        {
            WateringCan can = GameRoot.Instance.Session.WateringCan;
            if (can.IsFull) return;
            can.Refill();
            Sfx.PlayUi(SoundId.UseItem, 0.5f);
            GameRoot.Instance.Notify("Đã múc đầy bình tưới.", 1.5f);
        }
    }
}
