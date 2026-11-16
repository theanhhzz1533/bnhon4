using System.Text;
using Bnhon.Core.Content;
using Bnhon.Core.Crafting;
using Bnhon.Core.Inventory;
using Bnhon.Core.Vitals;
using Bnhon.Game.Audio;
using UnityEngine;

namespace Bnhon.Game.UI
{
    /// <summary>
    /// The kitchen menu: cook meals from produce. Ingredients come from the stash first,
    /// then the backpack, and the meal goes to the stash.
    /// </summary>
    public sealed class KitchenScreen : ModalScreen
    {
        private static readonly Color Good = new Color(0.55f, 0.9f, 0.55f);
        private static readonly Color Dim = new Color(0.7f, 0.7f, 0.72f);
        private static readonly Color Effect = new Color(0.95f, 0.85f, 0.4f);

        private readonly StringBuilder _text = new StringBuilder();
        private Vector2 _scroll;

        public static KitchenScreen Instance { get; private set; }

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
            var session = GameRoot.Instance.Session;
            Workshop kitchen = session.Kitchen;
            GridInventory stash = session.Stash;
            GridInventory backpack = session.Backpack;

            GUILayout.Label("Bếp", TitleStyle);
            ColoredLabel("Món nấu chín no lâu hơn đồ sống và cho một hiệu ứng. Thời gian hiệu ứng chỉ chạy khi ở ngoài, và hết khi bạn đi ngủ.", Dim);
            GUILayout.Space(8f);

            _scroll = GUILayout.BeginScrollView(_scroll);
            foreach (Recipe recipe in kitchen.Recipes)
            {
                CraftBlock block = kitchen.CheckCraft(recipe, stash, backpack);

                _text.Length = 0;
                for (int i = 0; i < recipe.Ingredients.Count; i++)
                {
                    ItemAmount amount = recipe.Ingredients[i];
                    if (i > 0) _text.Append("  ·  ");
                    _text.Append(amount.Item.DisplayName).Append(' ')
                        .Append(Workshop.CountOwned(amount.Item.Id, stash, backpack)).Append('/').Append(amount.Count);
                }

                GUILayout.BeginHorizontal();
                ColoredLabel(recipe.Result.Item.DisplayName, Color.white, GUILayout.Width(190f));
                ColoredLabel(_text.ToString(), block == CraftBlock.None ? Good : Color.white);
                if (Button(block == CraftBlock.None ? "Nấu" : "Thiếu nguyên liệu", block == CraftBlock.None, 150f))
                {
                    Recipe chosen = recipe;
                    Defer(() =>
                    {
                        CraftBlock result = kitchen.Craft(chosen, stash, backpack);
                        if (result == CraftBlock.None) Sfx.PlayUi(SoundId.ItemReveal, 0.6f);
                        else if (result == CraftBlock.NoRoom) GameRoot.Instance.Notify("Kho và balo đều đầy, không có chỗ để món ăn.", 3f);
                    });
                }
                GUILayout.EndHorizontal();

                ColoredLabel("      " + EffectLine(recipe), Effect);
                GUILayout.Space(6f);
            }
            GUILayout.EndScrollView();

            ColoredLabel("Nguyên liệu được lấy từ kho trước, rồi tới balo. Món nấu xong nằm trong kho; mở balo và bấm F để ăn.", Dim);
        }

        private static string EffectLine(Recipe recipe)
        {
            ConsumableDefinition effect = PrototypeContent.FindConsumable(recipe.Result.Item.Id);
            if (effect == null) return string.Empty;

            string text = string.Empty;
            if (effect.Heal > 0f) text += "+" + effect.Heal + " máu  ";
            if (effect.Hunger > 0f) text += "+" + effect.Hunger + " no  ";
            if (effect.Thirst > 0f) text += "+" + effect.Thirst + " nước  ";
            if (effect.Buff != null) text += "·  " + effect.Buff.DisplayName + " (" + Mathf.RoundToInt(effect.Buff.Seconds / 60f) + " phút)";
            return text;
        }
    }
}
