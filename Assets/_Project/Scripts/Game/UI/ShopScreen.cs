using Bnhon.Core.Inventory;
using Bnhon.Core.Trading;
using Bnhon.Game.Audio;
using UnityEngine;

namespace Bnhon.Game.UI
{
    /// <summary>
    /// The base shop menu: buy from a limited daily stock on the left, sell anything from
    /// the backpack or stash on the right. Bought goods go to the backpack, or the stash
    /// if it is full.
    /// </summary>
    public sealed class ShopScreen : ModalScreen
    {
        private static readonly Color Dim = new Color(0.7f, 0.7f, 0.72f);
        private static readonly Color Money = new Color(0.95f, 0.85f, 0.4f);

        private Vector2 _buyScroll;
        private Vector2 _sellScroll;

        public static ShopScreen Instance { get; private set; }

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
            Shop shop = session.Shop;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Quầy đổi chác", TitleStyle);
            GUILayout.FlexibleSpace();
            ColoredLabel("Tem phiếu: " + session.Wallet.Money, Money);
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(400f));
            GUILayout.Label("Mua (hàng về lại mỗi sáng)", HeaderStyle);
            _buyScroll = GUILayout.BeginScrollView(_buyScroll);
            foreach (ShopEntry entry in shop.Entries) DrawBuy(shop, entry);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(16f);

            GUILayout.BeginVertical();
            GUILayout.Label("Bán (được nửa giá trị)", HeaderStyle);
            _sellScroll = GUILayout.BeginScrollView(_sellScroll);
            int rows = DrawSell(shop, session.Backpack, "balo") + DrawSell(shop, session.Stash, "kho");
            if (rows == 0) ColoredLabel("Không có gì để bán.", Dim);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawBuy(Shop shop, ShopEntry entry)
        {
            var session = GameRoot.Instance.Session;
            bool inStock = entry.Stock > 0;
            bool affordable = session.Wallet.Money >= entry.Price;

            GUILayout.BeginHorizontal();
            string name = entry.Item.DisplayName + (entry.Bundle > 1 ? " x" + entry.Bundle : string.Empty);
            ColoredLabel(name, inStock ? Color.white : Dim, GUILayout.Width(170f));
            ColoredLabel(entry.Price.ToString(), affordable ? Money : Dim, GUILayout.Width(50f));
            ColoredLabel(inStock ? "còn " + entry.Stock : "hết hàng", Dim, GUILayout.Width(70f));

            if (Button("Mua", inStock && affordable, 70f))
            {
                Defer(() =>
                {
                    if (shop.Buy(entry, session.Wallet, session.Backpack, session.Stash)) Sfx.PlayUi(SoundId.ItemReveal, 0.6f);
                    else GameRoot.Instance.Notify("Balo và kho đều không đủ chỗ.", 2.5f);
                });
            }
            GUILayout.EndHorizontal();
        }

        private int DrawSell(Shop shop, GridInventory grid, string where)
        {
            var session = GameRoot.Instance.Session;
            int shown = 0;

            foreach (PlacedItem item in grid.Items)
            {
                int price = Shop.SellPrice(item.Item);
                if (price <= 0) continue;
                shown++;

                GUILayout.BeginHorizontal();
                ColoredLabel(item.Item.DisplayName + " x" + item.Count + "  (" + where + ")", Color.white, GUILayout.Width(230f));
                ColoredLabel(price + " mỗi cái", Money, GUILayout.Width(90f));

                PlacedItem target = item;
                if (Button("Bán 1", true, 60f))
                {
                    Defer(() => SellNow(shop, grid, target, 1));
                }
                if (Button("Bán hết", item.Count > 1, 70f))
                {
                    Defer(() => SellNow(shop, grid, target, target.Count));
                }
                GUILayout.EndHorizontal();
            }
            return shown;
        }

        private static void SellNow(Shop shop, GridInventory grid, PlacedItem item, int count)
        {
            if (shop.Sell(grid, item, count, GameRoot.Instance.Session.Wallet) > 0) Sfx.PlayUi(SoundId.UseItem, 0.5f);
        }
    }
}
