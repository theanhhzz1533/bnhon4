using System;
using System.Collections.Generic;

namespace Bnhon.Core.Saving
{
    // Plain data for one save file. Public fields and [Serializable] only, with lists
    // instead of dictionaries, so Unity's JsonUtility can read and write it as is.
    // Things are stored by id, never by reference, so content can change between versions.

    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int playerGender;
        public int totalDays;
        public int money;
        public float hunger;
        public float thirst;
        public bool raidedToday;

        /// <summary>True in a save written when a raid began. Loading such a save means the game was quit mid-raid.</summary>
        public bool inRaid;

        public GridSave stash = new GridSave();
        public GridSave backpack = new GridSave();
        public GridSave securePouch = new GridSave();
        public List<WeaponSlotSave> loadout = new List<WeaponSlotSave>();
        public List<PlotSave> farm = new List<PlotSave>();
        public List<RelationshipSave> relationships = new List<RelationshipSave>();
        public List<QuestSave> quests = new List<QuestSave>();
    }

    [Serializable]
    public sealed class GridSave
    {
        public List<ItemSave> items = new List<ItemSave>();
    }

    [Serializable]
    public sealed class ItemSave
    {
        public string id;
        public int count;
        public int x;
        public int y;
        public bool rotated;
    }

    [Serializable]
    public sealed class WeaponSlotSave
    {
        /// <summary>Empty string for an empty slot.</summary>
        public string id;
        public int loaded;
    }

    [Serializable]
    public sealed class PlotSave
    {
        public int state;
        public string cropId;
        public int growthDays;
        public int dryDays;
        public bool watered;
    }

    [Serializable]
    public sealed class RelationshipSave
    {
        public string npcId;
        public int points;
        public bool talkedToday;
        public bool giftedToday;
        public int giftsThisWeek;
        public int status;
    }

    [Serializable]
    public sealed class QuestSave
    {
        public string id;
        public int state;
    }
}
