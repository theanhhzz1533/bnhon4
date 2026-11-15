using System;
using System.Collections.Generic;
using Bnhon.Core.Content;
using Bnhon.Core.Loot;
using Bnhon.Game.Audio;
using Bnhon.Game.Combat;
using Bnhon.Game.Player;
using Bnhon.Game.UI;
using UnityEngine;

namespace Bnhon.Game.Raid
{
    /// <summary>
    /// Moves the player between the base and the raid zone and applies the
    /// outcome of a raid. In the prototype both zones live in one scene.
    /// </summary>
    public sealed class RaidDirector : MonoBehaviour
    {
        private PlayerController _player;
        private Health _playerHealth;
        private CameraFollow _camera;
        private Vector2 _baseSpawn;
        private Vector2 _raidSpawn;
        private Action _rebuildRaidZone;

        public static RaidDirector Instance { get; private set; }

        public void Init(PlayerController player, CameraFollow cameraFollow, Vector2 baseSpawn, Vector2 raidSpawn, Action rebuildRaidZone)
        {
            _player = player;
            _playerHealth = player.GetComponent<Health>();
            _camera = cameraFollow;
            _baseSpawn = baseSpawn;
            _raidSpawn = raidSpawn;
            _rebuildRaidZone = rebuildRaidZone;
            _playerHealth.Died += OnPlayerDied;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (_playerHealth != null) _playerHealth.Died -= OnPlayerDied;
            if (Instance == this) Instance = null;
        }

        public bool TryStartRaid()
        {
            var root = GameRoot.Instance;
            if (root.Session.TryStartRaid() == null)
            {
                root.Notify("Hôm nay bạn đã đi raid rồi. Hãy ngủ để sang ngày mới.");
                return false;
            }

            // Saved as "in a raid": quitting from here on loads as a death.
            root.Save(true);
            MovePlayer(_raidSpawn);
            root.Notify("Bắt đầu raid. Tìm vùng xanh lá để rút lui.");
            return true;
        }

        public void Extract()
        {
            var root = GameRoot.Instance;
            var session = root.Session;
            if (!session.IsInRaid) return;

            session.CurrentRaid.Extract();
            Sfx.PlayUi(SoundId.Extract, 0.8f);
            root.Save();
            MovePlayer(_baseSpawn);
            root.Notify("Rút lui thành công. Giá trị balo: " + session.Backpack.TotalValue() + ".", 5f);
        }

        public bool Sleep()
        {
            var root = GameRoot.Instance;
            var session = root.Session;
            if (!session.Sleep())
            {
                root.Notify("Không thể ngủ khi đang ở ngoài.");
                return false;
            }

            _playerHealth.ResetFull();
            root.Save();
            if (_rebuildRaidZone != null) _rebuildRaidZone();
            root.Notify("Ngày " + session.Clock.Day + ", " + SeasonName(session.Clock.Season) + ", năm " + session.Clock.Year + ".", 5f);
            return true;
        }

        public static string SeasonName(Bnhon.Core.Calendar.Season season)
        {
            switch (season)
            {
                case Bnhon.Core.Calendar.Season.Spring: return "mùa xuân";
                case Bnhon.Core.Calendar.Season.Summer: return "mùa hạ";
                case Bnhon.Core.Calendar.Season.Autumn: return "mùa thu";
                default: return "mùa đông";
            }
        }

        private void OnPlayerDied(Health health)
        {
            var root = GameRoot.Instance;
            var session = root.Session;

            int lostCount = 0;
            if (session.IsInRaid)
            {
                List<LootDrop> lost = session.CurrentRaid.Die();
                foreach (LootDrop drop in lost) lostCount += drop.Count;
            }

            Sfx.PlayUi(SoundId.PlayerDeath);
            _playerHealth.ResetFull();
            MovePlayer(_baseSpawn);
            string message = "Bạn đã gục ngã, mất " + lostCount + " món trong balo và vũ khí đang mang. Túi an toàn vẫn còn.";
            if (PrototypeContent.GrantFallbackWeapon(session)) message += " Minh để lại trong kho một khẩu súng ngắn cũ.";
            root.Save();
            root.Notify(message, 8f);
        }

        private void MovePlayer(Vector2 position)
        {
            if (InventoryScreen.Instance != null) InventoryScreen.Instance.Close();
            _player.Teleport(position);
            if (_camera != null) _camera.SnapToTarget();
        }
    }
}
