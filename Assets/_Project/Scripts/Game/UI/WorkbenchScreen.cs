using System.Collections.Generic;
using System.Text;
using Bnhon.Core.Crafting;
using Bnhon.Core.Inventory;
using Bnhon.Core.Items;
using Bnhon.Game.Audio;
using UnityEngine;

namespace Bnhon.Game.UI
{
    /// <summary>
    /// The workbench menu: craft from recipes, upgrade the bench, register blueprints
    /// brought back from raids, and dismantle items into materials. Materials come from
    /// the stash first, then the backpack.
    /// </summary>
    public sealed class WorkbenchScreen : ModalScreen
    {
        private static readonly Color Good = new Color(0.55f, 0.9f, 0.55f);
        private static readonly Color Bad = new Color(0.95f, 0.5f, 0.45f);
        private static readonly Color Dim = new Color(0.7f, 0.7f, 0.72f);

        private readonly StringBuilder _text = new StringBuilder();
        private Vector2 _scroll;

        public static WorkbenchScreen Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }

        protected override void DrawContents()
        {
            var root = GameRoot.Instance;
            var session = root.Session;
            Workshop workshop = session.Workshop;
            GridInventory stash = session.Stash;
            GridInventory backpack = session.Backpack;

            GUILayout.Label("Bàn chế tạo · cấp " + workshop.Level + "/" + Workshop.MaxLevel, TitleStyle);
            DrawUpgrade(workshop, stash, backpack);
            GUILayout.Space(6f);

            _scroll = GUILayout.BeginScrollView(_scroll);

            GUILayout.Label("Chế tạo", HeaderStyle);
            foreach (Recipe recipe in workshop.Recipes) DrawRecipe(workshop, recipe, stash, backpack);

            GUILayout.Space(10f);
            GUILayout.Label("Bản vẽ mang về", HeaderStyle);
            int blueprints = DrawBlueprints(workshop, stash, "kho") + DrawBlueprints(workshop, backpack, "balo");
            if (blueprints == 0) ColoredLabel("Không có bản vẽ nào. Chúng nằm trong hòm vũ khí và tủ thuốc ngoài kia; phải sống sót mang về mới đăng ký được.", Dim);

            GUILayout.Space(10f);
            GUILayout.Label("Tháo dỡ", HeaderStyle);
            int parts = DrawDismantle(workshop, stash, backpack, "kho") + DrawDismantle(workshop, backpack, stash, "balo");
            if (parts == 0) ColoredLabel("Không có món nào tháo dỡ được.", Dim);

            GUILayout.EndScrollView();
            ColoredLabel("Vật liệu được lấy từ kho trước, rồi tới balo. Đồ chế xong nằm trong kho.", Dim);
        }

        private void DrawUpgrade(Workshop workshop, GridInventory stash, GridInventory backpack)
        {
            IReadOnlyList<ItemAmount> cost = workshop.NextUpgradeCost;
            if (cost == null)
            {
                ColoredLabel("Bàn chế tạo đã ở cấp cao nhất.", Dim);
                return;
            }

            GUILayout.BeginHorizontal();
            bool affordable = AppendAmounts(cost, stash, backpack);
            ColoredLabel("Nâng lên cấp " + (workshop.Level + 1) + ":  " + _text, affordable ? Good : Color.white);
            if (Button("Nâng cấp", affordable, 110f))
            {
                Defer(() =>
                {
                    if (workshop.Upgrade(stash, backpack) == CraftBlock.None)
                    {
                        Sfx.PlayUi(SoundId.Extract, 0.6f);
                        GameRoot.Instance.Notify("Bàn chế tạo lên cấp " + workshop.Level + ".", 3f);
                    }
                });
            }
            GUILayout.EndHorizontal();
        }

        private void DrawRecipe(Workshop workshop, Recipe recipe, GridInventory stash, GridInventory backpack)
        {
            CraftBlock block = workshop.CheckCraft(recipe, stash, backpack);
            bool locked = block == CraftBlock.WorkbenchLevel || block == CraftBlock.Blueprint;

            GUILayout.BeginHorizontal();
            string name = recipe.Result.Item.DisplayName + (recipe.Result.Count > 1 ? " x" + recipe.Result.Count : string.Empty);
            ColoredLabel(name, locked ? Dim : Color.white, GUILayout.Width(190f));

            AppendAmounts(recipe.Ingredients, stash, backpack);
            ColoredLabel(_text.ToString(), locked ? Dim : (block == CraftBlock.None ? Good : Color.white));

            if (block == CraftBlock.WorkbenchLevel)
            {
                ColoredLabel("Cần bàn cấp " + recipe.WorkbenchLevel, Bad, GUILayout.Width(150f));
            }
            else if (block == CraftBlock.Blueprint)
            {
                ColoredLabel("Cần " + recipe.Blueprint.DisplayName, Bad, GUILayout.Width(150f));
            }
            else if (Button(block == CraftBlock.None ? "Chế tạo" : "Thiếu vật liệu", block == CraftBlock.None, 150f))
            {
                Defer(() =>
                {
                    CraftBlock result = workshop.Craft(recipe, stash, backpack);
                    if (result == CraftBlock.None) Sfx.PlayUi(SoundId.ItemReveal, 0.6f);
                    else if (result == CraftBlock.NoRoom) GameRoot.Instance.Notify("Kho và balo đều đầy, không có chỗ để đồ chế xong.", 3f);
                });
            }
            GUILayout.EndHorizontal();
        }

        private int DrawBlueprints(Workshop workshop, GridInventory grid, string where)
        {
            int shown = 0;
            foreach (PlacedItem item in grid.Items)
            {
                if (item.Item.Category != ItemCategory.Blueprint) continue;
                shown++;

                bool known = workshop.KnowsBlueprint(item.Item);
                GUILayout.BeginHorizontal();
                ColoredLabel(item.Item.DisplayName + "  (" + where + ")", Color.white, GUILayout.Width(300f));
                if (known)
                {
                    ColoredLabel("Đã đăng ký rồi. Bản thừa này có thể đem bán.", Dim);
                }
                else
                {
                    GUILayout.FlexibleSpace();
                    PlacedItem target = item;
                    if (Button("Đăng ký", true, 150f))
                    {
                        Defer(() =>
                        {
                            if (workshop.RegisterBlueprint(grid, target))
                            {
                                Sfx.PlayUi(SoundId.Extract, 0.6f);
                                GameRoot.Instance.Notify("Đã đăng ký " + target.Item.DisplayName + ". Công thức mở vĩnh viễn.", 3.5f);
                            }
                        });
                    }
                }
                GUILayout.EndHorizontal();
            }
            return shown;
        }

        private int DrawDismantle(Workshop workshop, GridInventory grid, GridInventory overflow, string where)
        {
            int shown = 0;
            foreach (PlacedItem item in grid.Items)
            {
                IReadOnlyList<ItemAmount> output = workshop.DismantleOutput(item.Item);
                if (output == null) continue;
                shown++;

                _text.Length = 0;
                for (int i = 0; i < output.Count; i++)
                {
                    if (i > 0) _text.Append(" + ");
                    _text.Append(output[i].Item.DisplayName).Append(" x").Append(output[i].Count);
                }

                GUILayout.BeginHorizontal();
                ColoredLabel(item.Item.DisplayName + " x" + item.Count + "  (" + where + ")", Color.white, GUILayout.Width(300f));
                ColoredLabel("ra: " + _text, Dim);
                PlacedItem target = item;
                if (Button("Tháo 1", true, 150f))
                {
                    Defer(() =>
                    {
                        if (workshop.Dismantle(grid, target, overflow)) Sfx.PlayUi(SoundId.Reload, 0.5f);
                    });
                }
                GUILayout.EndHorizontal();
            }
            return shown;
        }

        /// <summary>Writes "Wood 3/6 · Scrap 6/6" into the shared buffer. Returns true if everything is in hand.</summary>
        private bool AppendAmounts(IReadOnlyList<ItemAmount> amounts, GridInventory stash, GridInventory backpack)
        {
            _text.Length = 0;
            bool enough = true;
            for (int i = 0; i < amounts.Count; i++)
            {
                int owned = Workshop.CountOwned(amounts[i].Item.Id, stash, backpack);
                if (owned < amounts[i].Count) enough = false;
                if (i > 0) _text.Append("  ·  ");
                _text.Append(amounts[i].Item.DisplayName).Append(' ').Append(owned).Append('/').Append(amounts[i].Count);
            }
            return enough;
        }
    }
}
