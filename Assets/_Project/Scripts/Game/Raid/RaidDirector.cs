using System;
using System.Collections.Generic;
using Bnhon.Core.Content;
using Bnhon.Core.Loot;
using Bnhon.Game.AI;
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
        private Action<bool> _onZoneChange;

        public static RaidDirector Instance { get; private set; }

        public void Init(PlayerController player, CameraFollow cameraFollow, Vector2 baseSpawn, Vector2 raidSpawn, Action<bool> onZoneChange)
        {
            _player = player;
            _playerHealth = player.GetComponent<Health>();
            _camera = cameraFollow;
            _baseSpawn = baseSpawn;
            _raidSpawn = raidSpawn;
            _onZoneChange = onZoneChange;
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

        /// <summary>Changes where raids begin. Used when the raid map is swapped.</summary>
        public void SetRaidSpawn(Vector2 raidSpawn)
        {
            _raidSpawn = raidSpawn;
        }

        public void PutPlayerAtRaidSpawn()
        {
            MovePlayer(true);
        }

        public void PutPlayerAtBase()
        {
            MovePlayer(false);
        }

        /// <summary>Where the current or last raid went. Null during the opening.</summary>
        public Bnhon.Core.Raid.RaidArea CurrentArea { get; private set; }

        /// <summary>Called with the chosen area before a raid starts, so its map can be put in place.</summary>
        public Action<Bnhon.Core.Raid.RaidArea> AreaChosen { get; set; }

        public bool TryStartRaid(Bnhon.Core.Raid.RaidArea area = null)
        {
            var root = GameRoot.Instance;
            if (area == null) area = PrototypeContent.AreaOutskirts;
            if (!area.IsUnlocked(root.Session))
            {
                root.Notify("Bạn chưa biết đường tới " + area.DisplayName + ".");
                return false;
            }
            if (root.Session.TryStartRaid() == null)
            {
                root.Notify("Hôm nay bạn đã đi raid rồi. Hãy ngủ để sang ngày mới.");
                return false;
            }

            CurrentArea = area;
            if (AreaChosen != null) AreaChosen(area);

            // Saved as "in a raid": quitting from here on loads as a death.
            root.Save(true);
            MovePlayer(true);
            root.Notify(area.DisplayName + ". Tìm vùng xanh lá để rút lui.");
            return true;
        }

        public void Extract()
        {
            var root = GameRoot.Instance;
            var session = root.Session;
            if (!session.IsInRaid) return;

            // Whoever is following close behind makes it out too, and moves in for good.
            List<string> rescued = SurvivorAI.TakeRescued(_player.transform.position, 8f);
            foreach (string npcId in rescued) Bnhon.Core.Npcs.Residents.MoveIn(session, npcId);

            session.CurrentRaid.Extract();
            session.Story.Set(Bnhon.Core.Story.StoryIds.Extracted);
            Sfx.PlayUi(SoundId.Extract, 0.8f);
            root.Save();
            MovePlayer(false);
            root.Notify("Rút lui thành công. Giá trị balo: " + session.Backpack.TotalValue() + ".", 5f);
            PlayArrivals(rescued, 0);
        }

        /// <summary>Plays the arrival scene of each rescued person, one after another.</summary>
        private static void PlayArrivals(List<string> npcIds, int index)
        {
            if (index >= npcIds.Count || SceneScreen.Instance == null) return;

            Bnhon.Core.Npcs.RescueDefinition rescue = null;
            foreach (var candidate in PrototypeContent.Rescues)
            {
                if (candidate.NpcId == npcIds[index]) rescue = candidate;
            }

            var bond = GameRoot.Instance.Session.Relationships.Get(npcIds[index]);
            string title = bond != null ? bond.Npc.DisplayName + " về căn cứ" : "Người mới";
            if (rescue == null || rescue.Arrival == null)
            {
                PlayArrivals(npcIds, index + 1);
                return;
            }
            SceneScreen.Instance.Play(title, rescue.Arrival, () => PlayArrivals(npcIds, index + 1));
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

            if (session.InIntro && StoryDirector.Instance != null)
            {
                Sfx.PlayUi(SoundId.PlayerDeath);
                StoryDirector.Instance.RestartIntro();
                return;
            }

            int lostCount = 0;
            if (session.IsInRaid)
            {
                List<LootDrop> lost = session.CurrentRaid.Die();
                foreach (LootDrop drop in lost) lostCount += drop.Count;
            }

            Sfx.PlayUi(SoundId.PlayerDeath);
            _playerHealth.ResetFull();
            MovePlayer(false);
            string message = "Bạn đã gục ngã, mất " + lostCount + " món trong balo và vũ khí đang mang. Túi an toàn vẫn còn.";
            if (PrototypeContent.GrantFallbackWeapon(session)) message += " Minh để lại trong kho một khẩu súng ngắn cũ.";
            root.Save();
            root.Notify(message, 8f);
        }

        /// <summary>Switches the world to the raid map or the base, then puts the player at its entry point.</summary>
        private void MovePlayer(bool toRaid)
        {
            if (InventoryScreen.Instance != null) InventoryScreen.Instance.Close();
            if (_onZoneChange != null) _onZoneChange(toRaid);
            _player.Teleport(toRaid ? _raidSpawn : _baseSpawn);
            if (_camera != null) _camera.SnapToTarget();
        }
    }
}
