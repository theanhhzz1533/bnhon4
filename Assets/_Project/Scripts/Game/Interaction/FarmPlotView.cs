using Bnhon.Core.Content;
using Bnhon.Core.Farming;
using Bnhon.Game.Player;
using UnityEngine;

namespace Bnhon.Game.Interaction
{
    /// <summary>Scene view of one <see cref="FarmPlot"/>. One key does the next sensible action.</summary>
    public sealed class FarmPlotView : MonoBehaviour, IInteractable
    {
        private static readonly Color UntilledColor = new Color(0.45f, 0.36f, 0.24f);
        private static readonly Color TilledColor = new Color(0.33f, 0.24f, 0.15f);
        private static readonly Color WateredColor = new Color(0.22f, 0.17f, 0.12f);
        private static readonly Color GrowingColor = new Color(0.3f, 0.55f, 0.25f);
        private static readonly Color ReadyColor = new Color(0.95f, 0.75f, 0.2f);
        private static readonly Color DeadColor = new Color(0.4f, 0.4f, 0.4f);

        private FarmPlot _plot;
        private SpriteRenderer _renderer;

        public string Prompt
        {
            get
            {
                if (_plot == null) return string.Empty;
                switch (_plot.State)
                {
                    case PlotState.Untilled: return "E: Cuốc đất";
                    case PlotState.Dead: return "E: Dọn cây chết";
                    case PlotState.Tilled: return "E: Gieo hạt";
                    case PlotState.Ready: return "E: Thu hoạch " + _plot.Crop.DisplayName;
                    default:
                        string progress = _plot.Crop.DisplayName + " " + _plot.GrowthDays + "/" + _plot.Crop.DaysToMature + " ngày";
                        return _plot.IsWatered ? progress + " (đã tưới)" : "E: Tưới nước · " + progress;
                }
            }
        }

        public void Init(FarmPlot plot)
        {
            _plot = plot;
        }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (_plot == null || _renderer == null) return;
            _renderer.color = ColorFor(_plot);
        }

        public void Interact(PlayerController player)
        {
            var root = GameRoot.Instance;
            var session = root.Session;

            switch (_plot.State)
            {
                case PlotState.Untilled:
                case PlotState.Dead:
                    _plot.Till();
                    root.Notify("Đã cuốc đất.");
                    break;

                case PlotState.Tilled:
                    CropDefinition crop = TakeSeed();
                    if (crop == null)
                    {
                        root.Notify("Không có hạt giống hợp mùa trong balo hoặc kho.");
                    }
                    else
                    {
                        _plot.Plant(crop, session.Clock.Season);
                        root.Notify("Đã gieo " + crop.DisplayName + ".");
                    }
                    break;

                case PlotState.Growing:
                    root.Notify(_plot.Water() ? "Đã tưới nước." : "Hôm nay đã tưới rồi.");
                    break;

                case PlotState.Ready:
                    CropDefinition harvested = _plot.Crop;
                    int amount = _plot.Harvest(session.Rng);
                    int leftover = session.Backpack.Add(harvested.Produce, amount);
                    if (leftover > 0) session.Stash.Add(harvested.Produce, leftover);
                    root.Notify("Thu hoạch " + harvested.Produce.DisplayName + " x" + amount + ".");
                    break;
            }
        }

        /// <summary>Consumes one seed that can grow this season, from the backpack first, then the stash.</summary>
        private static CropDefinition TakeSeed()
        {
            var session = GameRoot.Instance.Session;
            foreach (CropDefinition crop in PrototypeContent.Crops)
            {
                if (!crop.GrowsIn(session.Clock.Season)) continue;
                if (session.Backpack.TryConsume(crop.Seed.Id, 1) || session.Stash.TryConsume(crop.Seed.Id, 1)) return crop;
            }
            return null;
        }

        private static Color ColorFor(FarmPlot plot)
        {
            switch (plot.State)
            {
                case PlotState.Untilled: return UntilledColor;
                case PlotState.Tilled: return plot.IsWatered ? WateredColor : TilledColor;
                case PlotState.Growing:
                    float t = plot.Crop.DaysToMature > 0 ? (float)plot.GrowthDays / plot.Crop.DaysToMature : 0f;
                    Color soil = plot.IsWatered ? WateredColor : TilledColor;
                    return Color.Lerp(soil, GrowingColor, 0.35f + 0.65f * t);
                case PlotState.Ready: return ReadyColor;
                default: return DeadColor;
            }
        }
    }
}
