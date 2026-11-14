using Bnhon.Core.Content;
using Bnhon.Core.Items;
using Bnhon.Core.Quests;
using Bnhon.Core.Relationships;
using Bnhon.Game.Player;
using UnityEngine;

namespace Bnhon.Game.Interaction
{
    /// <summary>
    /// A base resident. One key: turn in a finished quest, else accept a new
    /// one, else give a gift from the backpack, else just talk.
    /// </summary>
    public sealed class NpcView : MonoBehaviour, IInteractable
    {
        private string _npcId;

        public string Prompt
        {
            get
            {
                Relationship relationship = GameRoot.Instance.Session.Relationships.Get(_npcId);
                if (relationship == null) return string.Empty;
                return "E: Nói chuyện với " + relationship.Npc.DisplayName + " (" + relationship.Hearts + " tim)";
            }
        }

        public void Init(string npcId)
        {
            _npcId = npcId;
        }

        public void Interact(PlayerController player)
        {
            var root = GameRoot.Instance;
            var session = root.Session;
            Relationship relationship = session.Relationships.Get(_npcId);
            if (relationship == null) return;
            string name = relationship.Npc.DisplayName;

            foreach (DeliveryQuest quest in session.Quests.All)
            {
                if (quest.GiverNpcId != _npcId || quest.State != QuestState.Active) continue;
                if (!session.Quests.TurnIn(quest.Id, session.Backpack, session.Wallet, session.Relationships)) continue;

                root.Notify(name + ": Cảm ơn! Xong \"" + quest.Title + "\" (+" + quest.RewardMoney + " tem phiếu).");
                return;
            }

            foreach (DeliveryQuest quest in session.Quests.All)
            {
                if (quest.GiverNpcId != _npcId || quest.State != QuestState.Available) continue;

                session.Quests.Accept(quest.Id);
                ItemDefinition item = PrototypeContent.FindItem(quest.ItemId);
                string itemName = item != null ? item.DisplayName : quest.ItemId;
                root.Notify(name + ": \"" + quest.Title + "\" - mang cho tôi " + quest.Count + " " + itemName + " (để trong balo).", 6f);
                return;
            }

            if (TryGiveGift(relationship)) return;

            bool firstTalk = session.Relationships.Talk(_npcId);
            string line = firstTalk ? name + ": Hôm nay bên ngoài thế nào?" : name + ": Mình nói chuyện rồi mà.";
            if (session.Relationships.CanAskOut(_npcId)) line += " (Đã đủ thân để ngỏ lời hẹn hò.)";
            root.Notify(line);
        }

        private bool TryGiveGift(Relationship relationship)
        {
            var root = GameRoot.Instance;
            var session = root.Session;
            if (relationship.GiftedToday || relationship.GiftsThisWeek >= RelationshipTracker.GiftsPerWeek) return false;

            ItemDefinition gift = null;
            foreach (var placed in session.Backpack.Items)
            {
                if (placed.Item.Category == ItemCategory.Gift)
                {
                    gift = placed.Item;
                    break;
                }
            }
            if (gift == null) return false;
            if (!session.Backpack.TryConsume(gift.Id, 1)) return false;

            session.Relationships.Gift(relationship.Npc.Id, gift.Id, out GiftTaste taste);
            string reaction;
            switch (taste)
            {
                case GiftTaste.Loved: reaction = "Trời ơi, mình thích lắm!"; break;
                case GiftTaste.Liked: reaction = "Cảm ơn nhé, đẹp đấy."; break;
                case GiftTaste.Disliked: reaction = "Ờ... cảm ơn."; break;
                default: reaction = "Cảm ơn bạn."; break;
            }
            root.Notify(relationship.Npc.DisplayName + " nhận " + gift.DisplayName + ": " + reaction);
            return true;
        }
    }
}
