using System;
using System.Collections.Generic;
using Bnhon.Core.Inventory;
using Bnhon.Core.Items;

namespace Bnhon.Core.Crafting
{
    public struct ItemAmount
    {
        public ItemDefinition Item;
        public int Count;

        public ItemAmount(ItemDefinition item, int count)
        {
            Item = item;
            Count = count;
        }
    }

    public sealed class Recipe
    {
        public string Id { get; }
        public ItemAmount Result { get; }
        public IReadOnlyList<ItemAmount> Ingredients { get; }

        /// <summary>Lowest workbench level that can make this.</summary>
        public int WorkbenchLevel { get; }

        /// <summary>The blueprint item that must be registered first, or null if the recipe is known from the start.</summary>
        public ItemDefinition Blueprint { get; }

        public Recipe(string id, ItemDefinition result, int resultCount, int workbenchLevel, ItemDefinition blueprint, params ItemAmount[] ingredients)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (resultCount < 1) throw new ArgumentOutOfRangeException(nameof(resultCount));
            Id = id;
            Result = new ItemAmount(result, resultCount);
            WorkbenchLevel = workbenchLevel;
            Blueprint = blueprint;
            Ingredients = ingredients ?? new ItemAmount[0];
        }
    }

    /// <summary>Why something at the workbench cannot be done right now.</summary>
    public enum CraftBlock
    {
        None,
        WorkbenchLevel,
        Blueprint,
        Materials,
        NoRoom,
        MaxLevel
    }

    /// <summary>
    /// The base workbench, modelled on Escape From Duckov: recipes are gated by workbench
    /// level and by blueprints found on raids, which only count once brought home and
    /// registered. Materials are taken from the stash first, then the backpack, so nothing
    /// has to be carried to the bench. Unwanted items can be dismantled into materials.
    /// </summary>
    public sealed class Workshop
    {
        public const int MaxLevel = 3;

        private readonly List<Recipe> _recipes = new List<Recipe>();
        private readonly Dictionary<int, ItemAmount[]> _upgradeCosts = new Dictionary<int, ItemAmount[]>();
        private readonly Dictionary<string, ItemAmount[]> _dismantle = new Dictionary<string, ItemAmount[]>();
        private readonly HashSet<string> _blueprints = new HashSet<string>();

        public int Level { get; private set; } = 1;
        public IReadOnlyList<Recipe> Recipes => _recipes;
        public IEnumerable<string> KnownBlueprints => _blueprints;

        public void AddRecipe(Recipe recipe)
        {
            _recipes.Add(recipe ?? throw new ArgumentNullException(nameof(recipe)));
        }

        /// <summary>Materials needed to raise the workbench to <paramref name="level"/>.</summary>
        public void SetUpgradeCost(int level, params ItemAmount[] cost)
        {
            _upgradeCosts[level] = cost;
        }

        public void SetDismantle(ItemDefinition item, params ItemAmount[] output)
        {
            _dismantle[item.Id] = output;
        }

        public bool KnowsBlueprint(ItemDefinition blueprint)
        {
            return blueprint == null || _blueprints.Contains(blueprint.Id);
        }

        public static int CountOwned(string itemId, GridInventory stash, GridInventory backpack)
        {
            return (stash != null ? stash.CountOf(itemId) : 0) + (backpack != null ? backpack.CountOf(itemId) : 0);
        }

        public CraftBlock CheckCraft(Recipe recipe, GridInventory stash, GridInventory backpack)
        {
            if (recipe.WorkbenchLevel > Level) return CraftBlock.WorkbenchLevel;
            if (!KnowsBlueprint(recipe.Blueprint)) return CraftBlock.Blueprint;
            return HasAll(recipe.Ingredients, stash, backpack) ? CraftBlock.None : CraftBlock.Materials;
        }

        /// <summary>Makes the recipe once. The result goes to the stash, or the backpack if the stash is full.</summary>
        public CraftBlock Craft(Recipe recipe, GridInventory stash, GridInventory backpack)
        {
            CraftBlock block = CheckCraft(recipe, stash, backpack);
            if (block != CraftBlock.None) return block;

            List<Taken> taken = TakeAll(recipe.Ingredients, stash, backpack);

            ItemDefinition result = recipe.Result.Item;
            int toStash = recipe.Result.Count - stash.Add(result, recipe.Result.Count);
            int toBackpack = recipe.Result.Count - toStash;
            int leftover = toBackpack > 0 ? backpack.Add(result, toBackpack) : 0;
            if (leftover == 0) return CraftBlock.None;

            // Nowhere to put all of it: undo, so no materials are lost.
            if (toStash > 0) stash.TryConsume(result.Id, toStash);
            if (toBackpack - leftover > 0) backpack.TryConsume(result.Id, toBackpack - leftover);
            foreach (Taken entry in taken) entry.Grid.Add(entry.Item, entry.Count);
            return CraftBlock.NoRoom;
        }

        /// <summary>Cost of the next workbench level, or null at the highest level.</summary>
        public IReadOnlyList<ItemAmount> NextUpgradeCost
        {
            get
            {
                _upgradeCosts.TryGetValue(Level + 1, out ItemAmount[] cost);
                return Level < MaxLevel ? cost : null;
            }
        }

        public CraftBlock Upgrade(GridInventory stash, GridInventory backpack)
        {
            IReadOnlyList<ItemAmount> cost = NextUpgradeCost;
            if (cost == null) return CraftBlock.MaxLevel;
            if (!HasAll(cost, stash, backpack)) return CraftBlock.Materials;

            TakeAll(cost, stash, backpack);
            Level++;
            return CraftBlock.None;
        }

        /// <summary>
        /// Spends a blueprint item to learn its recipes for good. Returns false if the item is
        /// not a blueprint or is already known (the duplicate is kept, to sell).
        /// </summary>
        public bool RegisterBlueprint(GridInventory grid, PlacedItem item)
        {
            if (grid == null || item == null || !grid.Contains(item)) return false;
            if (item.Item.Category != ItemCategory.Blueprint || _blueprints.Contains(item.Item.Id)) return false;

            string id = item.Item.Id;
            if (!grid.TakeFromStack(item, 1)) return false;
            _blueprints.Add(id);
            return true;
        }

        /// <summary>What dismantling one of this item gives, or null if it cannot be dismantled.</summary>
        public IReadOnlyList<ItemAmount> DismantleOutput(ItemDefinition item)
        {
            if (item == null) return null;
            _dismantle.TryGetValue(item.Id, out ItemAmount[] output);
            return output;
        }

        /// <summary>Breaks one item into materials, placed in the same grid, or <paramref name="overflow"/> if it is full.</summary>
        public bool Dismantle(GridInventory grid, PlacedItem item, GridInventory overflow)
        {
            if (grid == null || item == null || !grid.Contains(item)) return false;
            IReadOnlyList<ItemAmount> output = DismantleOutput(item.Item);
            if (output == null) return false;
            if (!grid.TakeFromStack(item, 1)) return false;

            foreach (ItemAmount part in output)
            {
                int leftover = grid.Add(part.Item, part.Count);
                if (leftover > 0 && overflow != null && overflow != grid) overflow.Add(part.Item, leftover);
            }
            return true;
        }

        /// <summary>Sets progress from a save.</summary>
        public void Restore(int level, IEnumerable<string> blueprintIds)
        {
            Level = Math.Max(1, Math.Min(MaxLevel, level));
            _blueprints.Clear();
            if (blueprintIds == null) return;
            foreach (string id in blueprintIds)
            {
                if (!string.IsNullOrEmpty(id)) _blueprints.Add(id);
            }
        }

        private struct Taken
        {
            public GridInventory Grid;
            public ItemDefinition Item;
            public int Count;
        }

        private static bool HasAll(IReadOnlyList<ItemAmount> amounts, GridInventory stash, GridInventory backpack)
        {
            foreach (ItemAmount amount in amounts)
            {
                if (CountOwned(amount.Item.Id, stash, backpack) < amount.Count) return false;
            }
            return true;
        }

        private static List<Taken> TakeAll(IReadOnlyList<ItemAmount> amounts, GridInventory stash, GridInventory backpack)
        {
            var taken = new List<Taken>();
            foreach (ItemAmount amount in amounts)
            {
                int fromStash = Math.Min(amount.Count, stash != null ? stash.CountOf(amount.Item.Id) : 0);
                int fromBackpack = amount.Count - fromStash;
                if (fromStash > 0 && stash.TryConsume(amount.Item.Id, fromStash))
                {
                    taken.Add(new Taken { Grid = stash, Item = amount.Item, Count = fromStash });
                }
                if (fromBackpack > 0 && backpack.TryConsume(amount.Item.Id, fromBackpack))
                {
                    taken.Add(new Taken { Grid = backpack, Item = amount.Item, Count = fromBackpack });
                }
            }
            return taken;
        }
    }
}
