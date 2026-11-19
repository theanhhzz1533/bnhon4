using Bnhon.Game.Audio;
using Bnhon.Game.Combat;
using Bnhon.Game.Player;
using Bnhon.Game.Interaction;
using UnityEngine;

namespace Bnhon.Game.Raid
{
    /// <summary>
    /// A power switch for the generator. Turning it on opens the exits that need power, but
    /// the running generator is loud: it draws zombies toward it for the rest of the raid.
    /// </summary>
    public sealed class GeneratorSwitch : MonoBehaviour, IInteractable
    {
        private const float HumInterval = 7f;
        private const float HumRadius = 14f;

        private float _nextHum;

        /// <summary>True once the power is on in the current raid. Reset each time a raid map is built.</summary>
        public static bool PowerOn { get; private set; }

        public static void ResetPower()
        {
            PowerOn = false;
        }

        public string Prompt => PowerOn ? "Máy phát đang chạy" : "E: Bật cầu dao (máy phát sẽ rất ồn)";

        public void Interact(PlayerController player)
        {
            if (PowerOn) return;

            PowerOn = true;
            _nextHum = UnityEngine.Time.time + HumInterval;
            Vector2 position = transform.position;
            Noise.Emit(position, HumRadius + 6f);
            Sfx.Play(SoundId.Reload, position, 1f, 25f);
            Sfx.Play(SoundId.ZombieHit, position, 0.8f, 25f);
            GameRoot.Instance.Notify("Máy phát nổ máy. Cửa cuốn sau gara đã mở, nhưng tiếng máy sẽ kéo zombie tới đây.", 7f);

            var renderer = GetComponentInChildren<SpriteRenderer>();
            if (renderer != null && PlaceholderArt.IsPlaceholder(renderer)) renderer.color = new Color(0.4f, 0.95f, 0.4f);
        }

        private void Update()
        {
            if (!PowerOn || UnityEngine.Time.time < _nextHum) return;
            _nextHum = UnityEngine.Time.time + HumInterval;
            Noise.Emit(transform.position, HumRadius);
        }
    }
}
