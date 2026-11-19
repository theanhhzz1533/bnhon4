using Bnhon.Core;
using Bnhon.Core.Content;
using Bnhon.Core.Raid;
using Bnhon.Game.Raid;
using UnityEngine;

namespace Bnhon.Game.UI
{
    /// <summary>The choice of where to raid, shown at the gate once more than one area is known.</summary>
    public sealed class AreaScreen : ModalScreen
    {
        private static readonly Color Dim = new Color(0.7f, 0.7f, 0.72f);
        private static readonly Color Warning = new Color(1f, 0.55f, 0.45f);
        private static readonly Color Good = new Color(0.55f, 0.9f, 0.55f);

        public static AreaScreen Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this) Instance = null;
        }

        protected override Rect PanelRect(float width, float height)
        {
            float panelWidth = Mathf.Min(760f, width - 40f);
            float panelHeight = Mathf.Min(430f, height - 60f);
            return new Rect((width - panelWidth) * 0.5f, (height - panelHeight) * 0.5f, panelWidth, panelHeight);
        }

        protected override void DrawContents()
        {
            GameSession session = GameRoot.Instance.Session;
            int threat = Threat.Level(session.Clock.TotalDays);

            GUILayout.Label("Hôm nay đi đâu?", TitleStyle);
            ColoredLabel("Mức đe dọa " + threat + "/" + Threat.MaxLevel
                + ": cứ hai ngày thế giới lại nguy hiểm thêm một bậc, zombie chạy và bọc thép xuất hiện nhiều hơn.", Dim);
            GUILayout.Space(12f);

            foreach (RaidArea area in PrototypeContent.RaidAreas)
            {
                bool unlocked = area.IsUnlocked(session);

                GUILayout.BeginHorizontal();
                GUILayout.Label(area.DisplayName, HeaderStyle);
                GUILayout.FlexibleSpace();
                RaidArea target = area;
                int rides = PrototypeContent.RideLevel(session);
                if (Button(!unlocked ? "Chưa biết đường" : rides > 0 ? "Đi bộ từ rìa" : "Lên đường", unlocked, 150f))
                {
                    Defer(() => Go(target, -1));
                }
                GUILayout.EndHorizontal();

                if (unlocked && rides > 0) DrawRides(session, target, rides);

                GUILayout.Label(area.Description, TextStyle);
                if (!unlocked) ColoredLabel("Làm theo mục tiêu trên màn hình để mở khu này.", Dim);
                else if (area.BossFlag != null)
                {
                    if (area.BossDefeated(session)) ColoredLabel("Trùm khu vực đã bị hạ.", Good);
                    else ColoredLabel("Trùm khu vực còn sống.", Warning);
                }
                GUILayout.Space(14f);
            }
        }
        /// <summary>Vy's ride: a random drop-off point, a choice of two, or any of them.</summary>
        private void DrawRides(GameSession session, RaidArea area, int rides)
        {
            int count = area.InsertionNames != null ? area.InsertionNames.Count : 0;
            if (count == 0) return;

            GUILayout.BeginHorizontal();
            ColoredLabel("Vy chở:", Good, GUILayout.Width(70f));
            if (rides == 1)
            {
                if (Button("Thả ở điểm ngẫu nhiên", true, 220f)) Defer(() => Go(area, session.Rng.Next(count)));
            }
            else
            {
                // Two choices that stay the same for the whole day; or all of them.
                int first = System.Math.Abs(session.Clock.TotalDays * 7 + area.Id.Length) % count;
                for (int i = 0; i < count; i++)
                {
                    bool offered = rides >= 3 || i == first || i == (first + 1) % count;
                    if (!offered) continue;
                    int point = i;
                    if (Button(area.InsertionName(i), true, 210f)) Defer(() => Go(area, point));
                }
            }
            GUILayout.EndHorizontal();
        }

        private void Go(RaidArea area, int insertion)
        {
            Close();
            RaidDirector.Instance.TryStartRaid(area, insertion);
        }
    }
}
