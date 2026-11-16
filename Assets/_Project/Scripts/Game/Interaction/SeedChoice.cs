using System.Collections.Generic;
using Bnhon.Core;
using Bnhon.Core.Content;
using Bnhon.Core.Crafting;
using Bnhon.Core.Farming;

namespace Bnhon.Game.Interaction
{
    /// <summary>Which seed the player sows next. Only seeds they own that grow this season can be chosen.</summary>
    public static class SeedChoice
    {
        private static readonly List<CropDefinition> Options = new List<CropDefinition>();
        private static string _preferredId;

        /// <summary>The crop that E will sow now, or null if the player has no seed that grows this season.</summary>
        public static CropDefinition Current(GameSession session)
        {
            Refresh(session);
            if (Options.Count == 0) return null;

            foreach (CropDefinition crop in Options)
            {
                if (crop.Id == _preferredId) return crop;
            }
            return Options[0];
        }

        /// <summary>Moves to the next available seed. Returns the new choice, or null if there is none.</summary>
        public static CropDefinition Cycle(GameSession session)
        {
            CropDefinition current = Current(session);
            if (current == null) return null;

            CropDefinition next = Options[(Options.IndexOf(current) + 1) % Options.Count];
            _preferredId = next.Id;
            return next;
        }

        public static int Owned(GameSession session, CropDefinition crop)
        {
            return Workshop.CountOwned(crop.Seed.Id, session.Stash, session.Backpack);
        }

        public static int OptionCount(GameSession session)
        {
            Refresh(session);
            return Options.Count;
        }

        private static void Refresh(GameSession session)
        {
            Options.Clear();
            foreach (CropDefinition crop in PrototypeContent.Crops)
            {
                if (crop.GrowsIn(session.Clock.Season) && Owned(session, crop) > 0) Options.Add(crop);
            }
        }
    }
}
