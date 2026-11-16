using Bnhon.Core.Farming;
using Bnhon.Game.Player;
using UnityEngine;

namespace Bnhon.Game.Interaction
{
    /// <summary>
    /// Scene view of one <see cref="FarmPlot"/>. One key does the next sensible action.
    /// The soil shows whether it is tilled and watered, and the crop on it grows from a
    /// sprout to the full plant. Art is used when present (plot_dry, plot_wet, crop_sprout
    /// and one sprite per crop id); otherwise everything is coloured squares.
    /// </summary>
    public sealed class FarmPlotView : MonoBehaviour, IInteractable
    {
        private static readonly Color UntilledColor = new Color(0.45f, 0.36f, 0.24f);
        private static readonly Color TilledColor = new Color(0.33f, 0.24f, 0.15f);
        private static readonly Color WateredColor = new Color(0.22f, 0.17f, 0.12f);
        private static readonly Color GrowingColor = new Color(0.3f, 0.6f, 0.25f);
        private static readonly Color ReadyColor = new Color(0.95f, 0.75f, 0.2f);
        private static readonly Color DeadColor = new Color(0.4f, 0.4f, 0.4f);

        private FarmPlot _plot;
        private SpriteRenderer _soil;
        private bool _soilIsPlaceholder;
        private SpriteRenderer _wet;
        private SpriteRenderer _crop;

        public string Prompt
        {
            get
            {
                if (_plot == null) return string.Empty;
                switch (_plot.State)
                {
                    case PlotState.Untilled: return "E: Cuốc đất";
                    case PlotState.Dead: return "E: Dọn cây chết";
                    case PlotState.Tilled: return SowPrompt();
                    case PlotState.Ready: return "E: Thu hoạch " + _plot.Crop.DisplayName;
                    default:
                        string progress = _plot.Crop.DisplayName + " " + _plot.GrowthDays + "/" + _plot.Crop.DaysToMature + " ngày";
                        if (_plot.IsWatered) return progress + " (đã tưới)";
                        WateringCan can = GameRoot.Instance.Session.WateringCan;
                        return can.IsEmpty
                            ? "Bình tưới cạn, ra giếng múc nước · " + progress
                            : "E: Tưới nước (bình " + can.Water + "/" + can.Capacity + ") · " + progress;
                }
            }
        }

        private static string SowPrompt()
        {
            var session = GameRoot.Instance.Session;
            CropDefinition crop = SeedChoice.Current(session);
            if (crop == null) return "Không có hạt giống hợp mùa (mua ở quầy hoặc tìm ngoài kia)";

            string text = "E: Gieo " + crop.DisplayName + " (còn " + SeedChoice.Owned(session, crop) + " hạt, "
                + crop.DaysToMature + " ngày" + (crop.RegrowDays > 0 ? ", thu nhiều lần" : string.Empty) + ")";
            return SeedChoice.OptionCount(session) > 1 ? text + " · F: đổi hạt" : text;
        }

        /// <summary>Called when the player presses the alternate key while facing this plot.</summary>
        public void AlternateAction()
        {
            if (_plot == null || _plot.State != PlotState.Tilled) return;
            SeedChoice.Cycle(GameRoot.Instance.Session);
        }

        public void Init(FarmPlot plot)
        {
            _plot = plot;
            _soil = GetComponentInChildren<SpriteRenderer>();
            _soilIsPlaceholder = PlaceholderArt.IsPlaceholder(_soil);

            // Wet soil is a second picture laid over the dry one. Without art the square is recoloured instead.
            Sprite wetArt = PlaceholderArt.FindSprite("plot_wet");
            if (wetArt != null && !_soilIsPlaceholder)
            {
                _wet = NewRenderer("Wet", 2);
                _wet.sprite = wetArt;
                Vector3 size = wetArt.bounds.size;
                Vector3 footprint = transform.localScale;
                _wet.transform.localScale = new Vector3(footprint.x / Mathf.Max(0.0001f, size.x), footprint.y / Mathf.Max(0.0001f, size.y), 1f);
                _wet.enabled = false;
            }

            _crop = NewRenderer("Crop", 3);
            _crop.enabled = false;
        }

        /// <summary>A renderer on a sibling object at this plot's position, so the plot's own scale does not distort it.</summary>
        private SpriteRenderer NewRenderer(string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform.parent, false);
            go.transform.position = transform.position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = PlaceholderArt.UnlitMaterial;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void Update()
        {
            if (_plot == null || _soil == null) return;

            bool tilled = _plot.State != PlotState.Untilled;
            if (_soilIsPlaceholder)
            {
                _soil.color = !tilled ? UntilledColor : (_plot.IsWatered ? WateredColor : TilledColor);
            }
            else
            {
                // Untilled ground shows the soil picture faintly, so the plot can still be found.
                _soil.color = tilled ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                if (_wet != null) _wet.enabled = tilled && _plot.IsWatered;
                else if (tilled && _plot.IsWatered) _soil.color = new Color(0.7f, 0.7f, 0.75f, 1f);
            }

            UpdateCrop();
        }

        private void UpdateCrop()
        {
            bool hasPlant = _plot.State == PlotState.Growing || _plot.State == PlotState.Ready || _plot.State == PlotState.Dead;
            _crop.enabled = hasPlant;
            if (!hasPlant) return;

            CropDefinition crop = _plot.Crop;
            bool growing = _plot.State == PlotState.Growing;
            float growth = growing && crop != null && crop.DaysToMature > 0 ? Mathf.Clamp((float)_plot.GrowthDays / crop.DaysToMature, 0f, 1f) : 1f;
            float size = growing ? 0.4f + 0.5f * growth : 0.95f;

            string key = crop == null ? null : (growing && growth < 0.34f ? "crop_sprout" : crop.Id);
            Sprite art = PlaceholderArt.FindSprite(key);

            if (art != null)
            {
                Vector3 bounds = art.bounds.size;
                float longest = Mathf.Max(0.0001f, Mathf.Max(bounds.x, bounds.y));
                _crop.sprite = art;
                _crop.color = _plot.State == PlotState.Dead ? new Color(0.45f, 0.4f, 0.35f, 1f) : Color.white;
                _crop.transform.localScale = new Vector3(size / longest, size / longest, 1f);
            }
            else
            {
                _crop.sprite = PlaceholderArt.Square;
                _crop.color = _plot.State == PlotState.Ready ? ReadyColor : (_plot.State == PlotState.Dead ? DeadColor : GrowingColor);
                _crop.transform.localScale = new Vector3(size * 0.7f, size * 0.7f, 1f);
            }
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
                    if (_plot.IsWatered) root.Notify("Hôm nay đã tưới rồi.");
                    else if (session.WateringCan.WaterPlot(_plot)) root.Notify("Đã tưới nước. Bình còn " + session.WateringCan.Water + "/" + session.WateringCan.Capacity + ".");
                    else root.Notify("Bình tưới cạn. Ra giếng múc nước.");
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

        /// <summary>Consumes one of the chosen seed, from the backpack first, then the stash.</summary>
        private static CropDefinition TakeSeed()
        {
            var session = GameRoot.Instance.Session;
            CropDefinition crop = SeedChoice.Current(session);
            if (crop == null) return null;
            return session.Backpack.TryConsume(crop.Seed.Id, 1) || session.Stash.TryConsume(crop.Seed.Id, 1) ? crop : null;
        }
    }
}
