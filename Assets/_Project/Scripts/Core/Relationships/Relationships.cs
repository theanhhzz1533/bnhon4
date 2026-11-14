using System;
using System.Collections.Generic;
using Bnhon.Core.Calendar;

namespace Bnhon.Core.Relationships
{
    public enum Gender
    {
        Male,
        Female
    }

    public enum GiftTaste
    {
        Loved,
        Liked,
        Neutral,
        Disliked
    }

    public enum RelationshipStatus
    {
        Acquaintance,
        Dating,
        Married
    }

    public enum GiftResult
    {
        Accepted,
        AlreadyGiftedToday,
        WeeklyLimitReached,
        UnknownNpc
    }

    /// <summary>Static description of a base resident. Every romanceable NPC is an adult.</summary>
    public sealed class NpcProfile
    {
        private readonly HashSet<string> _loved = new HashSet<string>();
        private readonly HashSet<string> _liked = new HashSet<string>();
        private readonly HashSet<string> _disliked = new HashSet<string>();

        public string Id { get; }
        public string DisplayName { get; }
        public Gender Gender { get; }
        public bool IsRomanceable { get; }

        public NpcProfile(string id, string displayName, Gender gender, bool isRomanceable)
        {
            Id = id;
            DisplayName = displayName;
            Gender = gender;
            IsRomanceable = isRomanceable;
        }

        public NpcProfile Loves(params string[] itemIds) { _loved.UnionWith(itemIds); return this; }
        public NpcProfile Likes(params string[] itemIds) { _liked.UnionWith(itemIds); return this; }
        public NpcProfile Dislikes(params string[] itemIds) { _disliked.UnionWith(itemIds); return this; }

        public GiftTaste TasteFor(string itemId)
        {
            if (_loved.Contains(itemId)) return GiftTaste.Loved;
            if (_liked.Contains(itemId)) return GiftTaste.Liked;
            if (_disliked.Contains(itemId)) return GiftTaste.Disliked;
            return GiftTaste.Neutral;
        }
    }

    public sealed class Relationship
    {
        public NpcProfile Npc { get; }
        public int Points { get; internal set; }
        public bool TalkedToday { get; internal set; }
        public bool GiftedToday { get; internal set; }
        public int GiftsThisWeek { get; internal set; }
        public RelationshipStatus Status { get; internal set; }

        public int Hearts => Points / RelationshipTracker.PointsPerHeart;

        internal Relationship(NpcProfile npc)
        {
            Npc = npc;
        }
    }

    /// <summary>
    /// Stardew-style friendship: talk once a day, one gift a day and two a
    /// week. Hearts are capped at 8 until the player is dating the NPC.
    /// Only romanceable NPCs of the opposite gender can be asked out.
    /// </summary>
    public sealed class RelationshipTracker
    {
        public const int PointsPerHeart = 250;
        public const int MaxHeartsBeforeDating = 8;
        public const int MaxHearts = 10;
        public const int TalkPoints = 20;
        public const int GiftsPerWeek = 2;

        private readonly Dictionary<string, Relationship> _relationships = new Dictionary<string, Relationship>();

        public Gender PlayerGender { get; }
        public IEnumerable<Relationship> All => _relationships.Values;

        public RelationshipTracker(Gender playerGender)
        {
            PlayerGender = playerGender;
        }

        public Relationship Register(NpcProfile npc)
        {
            if (npc == null) throw new ArgumentNullException(nameof(npc));
            var relationship = new Relationship(npc);
            _relationships[npc.Id] = relationship;
            return relationship;
        }

        public Relationship Get(string npcId)
        {
            _relationships.TryGetValue(npcId, out var relationship);
            return relationship;
        }

        public static int PointsFor(GiftTaste taste)
        {
            switch (taste)
            {
                case GiftTaste.Loved: return 80;
                case GiftTaste.Liked: return 45;
                case GiftTaste.Disliked: return -40;
                default: return 20;
            }
        }

        /// <summary>Returns true if this was the first talk of the day and points were added.</summary>
        public bool Talk(string npcId)
        {
            var relationship = Get(npcId);
            if (relationship == null || relationship.TalkedToday) return false;
            relationship.TalkedToday = true;
            AddPoints(npcId, TalkPoints);
            return true;
        }

        public GiftResult Gift(string npcId, string itemId, out GiftTaste taste)
        {
            taste = GiftTaste.Neutral;
            var relationship = Get(npcId);
            if (relationship == null) return GiftResult.UnknownNpc;
            if (relationship.GiftedToday) return GiftResult.AlreadyGiftedToday;
            if (relationship.GiftsThisWeek >= GiftsPerWeek) return GiftResult.WeeklyLimitReached;

            taste = relationship.Npc.TasteFor(itemId);
            relationship.GiftedToday = true;
            relationship.GiftsThisWeek++;
            AddPoints(npcId, PointsFor(taste));
            return GiftResult.Accepted;
        }

        public void AddPoints(string npcId, int amount)
        {
            var relationship = Get(npcId);
            if (relationship == null) return;

            int capHearts = relationship.Status == RelationshipStatus.Acquaintance ? MaxHeartsBeforeDating : MaxHearts;
            // Allow the bar of the last heart to fill completely.
            int cap = (capHearts + 1) * PointsPerHeart - 1;
            relationship.Points = Math.Max(0, Math.Min(cap, relationship.Points + amount));
        }

        public bool CanAskOut(string npcId)
        {
            var relationship = Get(npcId);
            return relationship != null
                && relationship.Status == RelationshipStatus.Acquaintance
                && relationship.Npc.IsRomanceable
                && relationship.Npc.Gender != PlayerGender
                && relationship.Hearts >= MaxHeartsBeforeDating;
        }

        public bool AskOut(string npcId)
        {
            if (!CanAskOut(npcId)) return false;
            Get(npcId).Status = RelationshipStatus.Dating;
            return true;
        }

        public bool CanMarry(string npcId)
        {
            var relationship = Get(npcId);
            if (relationship == null || relationship.Status != RelationshipStatus.Dating) return false;
            if (relationship.Hearts < MaxHearts) return false;

            foreach (var other in _relationships.Values)
            {
                if (other.Status == RelationshipStatus.Married) return false;
            }
            return true;
        }

        public bool Marry(string npcId)
        {
            if (!CanMarry(npcId)) return false;
            Get(npcId).Status = RelationshipStatus.Married;
            return true;
        }

        /// <summary>Call once when a new day starts.</summary>
        public void OnNewDay(GameClock clock)
        {
            bool newWeek = clock.DayOfWeek == 0;
            foreach (var relationship in _relationships.Values)
            {
                relationship.TalkedToday = false;
                relationship.GiftedToday = false;
                if (newWeek) relationship.GiftsThisWeek = 0;
            }
        }
    }
}
