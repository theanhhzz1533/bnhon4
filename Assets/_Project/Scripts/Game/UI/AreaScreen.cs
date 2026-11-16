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
                if (Button(unlocked ? "Lên đường" : "Chưa biết đường", unlocked, 150f))
                {
                    Defer(() =>
                    {
                        Close();
                        RaidDirector.Instance.TryStartRaid(target);
                    });
                }
                GUILayout.EndHorizontal();

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
    }
}
