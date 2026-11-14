using System;
using System.Collections.Generic;
using Bnhon.Core.Economy;
using Bnhon.Core.Inventory;
using Bnhon.Core.Relationships;

namespace Bnhon.Core.Quests
{
    public enum QuestState
    {
        Available,
        Active,
        Completed
    }

    /// <summary>"Bring N of item X to an NPC" quest, the staple of the extraction loop.</summary>
    public sealed class DeliveryQuest
    {
        public string Id { get; }
        public string Title { get; }
        public string GiverNpcId { get; }
        public string ItemId { get; }
        public int Count { get; }
        public int RewardMoney { get; }
        public int RewardFriendship { get; }
        public QuestState State { get; internal set; } = QuestState.Available;

        public DeliveryQuest(string id, string title, string giverNpcId, string itemId, int count, int rewardMoney, int rewardFriendship)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            Id = id;
            Title = title;
            GiverNpcId = giverNpcId;
            ItemId = itemId;
            Count = count;
            RewardMoney = rewardMoney;
            RewardFriendship = rewardFriendship;
        }
    }

    public sealed class QuestLog
    {
        private readonly Dictionary<string, DeliveryQuest> _quests = new Dictionary<string, DeliveryQuest>();
        private readonly List<DeliveryQuest> _ordered = new List<DeliveryQuest>();

        public IReadOnlyList<DeliveryQuest> All => _ordered;

        public void Add(DeliveryQuest quest)
        {
            if (quest == null) throw new ArgumentNullException(nameof(quest));
            _quests.Add(quest.Id, quest);
            _ordered.Add(quest);
        }

        public DeliveryQuest Get(string questId)
        {
            _quests.TryGetValue(questId, out var quest);
            return quest;
        }

        public bool Accept(string questId)
        {
            var quest = Get(questId);
            if (quest == null || quest.State != QuestState.Available) return false;
            quest.State = QuestState.Active;
            return true;
        }

        public bool CanTurnIn(string questId, GridInventory inventory)
        {
            var quest = Get(questId);
            return quest != null
                && quest.State == QuestState.Active
                && inventory != null
                && inventory.CountOf(quest.ItemId) >= quest.Count;
        }

        /// <summary>Takes the items, pays the reward and adds friendship with the quest giver.</summary>
        public bool TurnIn(string questId, GridInventory inventory, Wallet wallet, RelationshipTracker relationships)
        {
            if (!CanTurnIn(questId, inventory)) return false;

            var quest = Get(questId);
            inventory.TryConsume(quest.ItemId, quest.Count);
            wallet?.Add(quest.RewardMoney);
            relationships?.AddPoints(quest.GiverNpcId, quest.RewardFriendship);
            quest.State = QuestState.Completed;
            return true;
        }
    }
}
