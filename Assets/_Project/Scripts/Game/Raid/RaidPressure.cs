using System;
using Bnhon.Game.AI;
using Bnhon.Game.Audio;
using UnityEngine;

namespace Bnhon.Game.Raid
{
    /// <summary>
    /// Keeps a cleared map from being safe. Nothing happens while any of the zombies placed
    /// by the map are still alive. Once the last one is dead, groups of zombies start arriving
    /// at intervals and head for the player; each group is bigger than the last. The player
    /// can clear the map, but cannot then loot it at leisure.
    /// </summary>
    public sealed class RaidPressure : MonoBehaviour
    {
        [SerializeField] private float delayAfterClear = 25f;
        [SerializeField] private float waveInterval = 40f;
        [SerializeField] private int firstWaveSize = 2;
        [SerializeField] private int maxWaveSize = 6;
        [SerializeField] private int maxZombiesAlive = 40;

        private Func<int, int> _spawn;
        private bool _wasInRaid;
        private float _nextWaveTime;
        private bool _mapCleared;
        private int _wavesSent;

        /// <summary>Seconds spent in the current raid, or 0 in the base.</summary>
        public float RaidSeconds { get; private set; }

        public static RaidPressure Instance { get; private set; }

        /// <param name="spawn">Spawns up to N zombies out of the player's sight and returns how many it placed.</param>
        public void Init(Func<int, int> spawn)
        {
            _spawn = spawn;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            bool inRaid = GameRoot.Instance != null && GameRoot.Instance.Session.IsInRaid;
            if (!inRaid)
            {
                _wasInRaid = false;
                RaidSeconds = 0f;
                return;
            }

            if (!_wasInRaid)
            {
                _wasInRaid = true;
                RaidSeconds = 0f;
                _wavesSent = 0;
                _mapCleared = false;
            }

            RaidSeconds += UnityEngine.Time.deltaTime;

            if (!_mapCleared)
            {
                if (ZombieAI.AliveCount > 0) return;
                _mapCleared = true;
                _nextWaveTime = RaidSeconds + delayAfterClear;
                GameRoot.Instance.Notify("Khu vực đã yên... nhưng sẽ không lâu.", 4f);
            }

            if (RaidSeconds < _nextWaveTime || _spawn == null) return;

            _nextWaveTime += waveInterval;
            int room = maxZombiesAlive - ZombieAI.AliveCount;
            int size = Mathf.Min(Mathf.Min(firstWaveSize + _wavesSent, maxWaveSize), room);
            _wavesSent++;
            if (size <= 0) return;

            if (_spawn(size) > 0)
            {
                GameRoot.Instance.Notify("Có thêm zombie đang kéo tới...", 3f);
                Sfx.PlayUi(SoundId.ZombieGrowl, 0.5f);
            }
        }
    }
}
