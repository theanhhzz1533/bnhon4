using Bnhon.Core;
using Bnhon.Core.Charms;
using Bnhon.Core.Content;
using Bnhon.Game.Audio;
using UnityEngine;

namespace Bnhon.Game.UI
{
    /// <summary>
    /// Quân's lab: brew charms from samples, wear up to two (three later), and break spare
    /// copies back into samples. Charms are never lost on death.
    /// </summary>
    public sealed class CharmScreen : ModalScreen
    {
        private static readonly Color Dim = new Color(0.7f, 0.7f, 0.72f);
        private static readonly Color Gold = new Color(0.95f, 0.85f, 0.4f);
        private static readonly Color RareColor = new Color(0.45f, 0.7f, 1f);
        private static readonly Color EpicColor = new Color(0.85f, 0.5f, 1f);

        private Vector2 _scroll;
        private string _result;

        public static CharmScreen Instance { get; private set; }

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
            GameSession session = GameRoot.Instance.Session;
            CharmCollection charms = session.Charms;
            int samples = Bnhon.Core.Crafting.Workshop.CountOwned(PrototypeContent.Sample.Id, session.Stash, session.Backpack);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Phòng thí nghiệm của Quân", TitleStyle);
            GUILayout.FlexibleSpace();
            ColoredLabel("Mẫu vật: " + samples, Gold);
            GUILayout.EndHorizontal();
            ColoredLabel("Bùa không nằm trong balo nên chết cũng không mất. Mẫu vật lấy từ xác zombie; zombie chạy, gào và bọc thép cho nhiều hơn.", Dim);
            GUILayout.Space(8f);

            DrawBrewing(session, samples);
            if (!string.IsNullOrEmpty(_result))
            {
                GUILayout.Space(4f);
                ColoredLabel(_result, Gold);
            }

            GUILayout.Space(10f);
            GUILayout.Label("Đang đeo (" + charms.Slots + " ô)", HeaderStyle);
            for (int slot = 0; slot < charms.Slots; slot++)
            {
                CharmDefinition worn = charms.WornIn(slot);
                GUILayout.BeginHorizontal();
                if (worn == null)
                {
                    ColoredLabel("Ô " + (slot + 1) + ": trống", Dim);
                }
                else
                {
                    ColoredLabel("Ô " + (slot + 1) + ": " + worn.DisplayName + " · " + worn.Description, RarityColor(worn.Rarity));
                    GUILayout.FlexibleSpace();
                    int index = slot;
                    if (Button("Tháo", true, 90f)) Defer(() => charms.TakeOff(index));
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10f);
            GUILayout.Label("Bộ sưu tập", HeaderStyle);
            _scroll = GUILayout.BeginScrollView(_scroll);
            if (charms.Owned.Count == 0) ColoredLabel("Chưa có bùa nào.", Dim);
            bool canBreak = session.Story.Has(PrototypeContent.FlagLabBreakdown);
            foreach (CharmDefinition charm in charms.Owned)
            {
                int count = charms.CountOf(charm.Id);
                GUILayout.BeginHorizontal();
                ColoredLabel(charm.DisplayName + (count > 1 ? " x" + count : string.Empty) + "  [" + RarityName(charm.Rarity) + "]", RarityColor(charm.Rarity), GUILayout.Width(250f));
                ColoredLabel(charm.Description, Color.white);
                GUILayout.FlexibleSpace();

                CharmDefinition target = charm;
                bool worn = charms.IsWorn(charm);
                if (Button(worn ? "Đang đeo" : "Đeo", !worn && charms.FreeSlot() >= 0, 90f))
                {
                    Defer(() => charms.Wear(target, charms.FreeSlot()));
                }
                if (canBreak && Button("Phân rã (+" + CharmLab.BreakdownValue(charm) + ")", count > 1, 130f))
                {
                    Defer(() =>
                    {
                        int got = PrototypeContent.BreakDownCharm(session, target);
                        if (got > 0) _result = "Phân rã 1 " + target.DisplayName + ", được " + got + " mẫu vật.";
                    });
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        private void DrawBrewing(GameSession session, int samples)
        {
            bool free = PrototypeContent.FirstBrewFree(session);
            GUILayout.BeginHorizontal();
            string label = free ? "Pha bùa (miễn phí lần đầu)" : "Pha bùa (" + CharmLab.SamplesPerBrew + " mẫu vật)";
            if (Button(label, free || samples >= CharmLab.SamplesPerBrew, 240f)) Defer(() => Show(PrototypeContent.BrewCharm(session)));
            ColoredLabel("70% thường · 25% hiếm · 5% quý", Dim);
            GUILayout.EndHorizontal();

            if (session.Story.Has(PrototypeContent.FlagLabGroups))
            {
                GUILayout.BeginHorizontal();
                ColoredLabel("Pha nhắm (" + CharmLab.SamplesPerGroupBrew + " mẫu vật):", Color.white, GUILayout.Width(180f));
                bool enough = samples >= CharmLab.SamplesPerGroupBrew;
                if (Button("Lén lút", enough, 100f)) Defer(() => Show(PrototypeContent.BrewCharm(session, CharmGroup.Stealth)));
                if (Button("Sinh tồn", enough, 100f)) Defer(() => Show(PrototypeContent.BrewCharm(session, CharmGroup.Survival)));
                if (Button("Chiến đấu", enough, 100f)) Defer(() => Show(PrototypeContent.BrewCharm(session, CharmGroup.Combat)));
                GUILayout.EndHorizontal();
            }

            if (session.Story.Has(PrototypeContent.FlagLabCores))
            {
                GUILayout.BeginHorizontal();
                ColoredLabel("Pha từ lõi trùm (luôn hiếm hoặc quý):", Color.white, GUILayout.Width(260f));
                DrawCoreButton(session, PrototypeContent.CoreElectrician);
                DrawCoreButton(session, PrototypeContent.CoreButcher);
                GUILayout.EndHorizontal();
            }
        }

        private void DrawCoreButton(GameSession session, Bnhon.Core.Items.ItemDefinition core)
        {
            int have = Bnhon.Core.Crafting.Workshop.CountOwned(core.Id, session.Stash, session.Backpack);
            if (Button(core.DisplayName + " (" + have + ")", have > 0, 200f)) Defer(() => Show(PrototypeContent.BrewFromCore(session, core)));
        }

        private void Show(CharmDefinition charm)
        {
            if (charm == null)
            {
                _result = "Không đủ nguyên liệu.";
                return;
            }

            int count = GameRoot.Instance.Session.Charms.CountOf(charm.Id);
            _result = "Quân đưa bạn một lọ: " + charm.DisplayName + " [" + RarityName(charm.Rarity) + "]. " + charm.Description
                + (count > 1 ? " (đã có, giờ có " + count + ")" : string.Empty);
            Sfx.PlayUi(charm.Rarity == CharmRarity.Common ? SoundId.ItemReveal : SoundId.Extract, 0.7f);

            // A new charm goes straight into a free slot.
            CharmCollection charms = GameRoot.Instance.Session.Charms;
            if (count == 1 && charms.FreeSlot() >= 0) charms.Wear(charm, charms.FreeSlot());
        }

        private static string RarityName(CharmRarity rarity)
        {
            return rarity == CharmRarity.Epic ? "quý" : rarity == CharmRarity.Rare ? "hiếm" : "thường";
        }

        private static Color RarityColor(CharmRarity rarity)
        {
            return rarity == CharmRarity.Epic ? EpicColor : rarity == CharmRarity.Rare ? RareColor : Color.white;
        }
    }
}
