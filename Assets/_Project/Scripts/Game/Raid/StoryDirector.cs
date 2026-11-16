using System;
using Bnhon.Core;
using Bnhon.Core.Content;
using Bnhon.Core.Story;
using Bnhon.Game.Audio;
using Bnhon.Game.Combat;
using Bnhon.Game.Player;
using Bnhon.Game.UI;
using UnityEngine;

namespace Bnhon.Game.Raid
{
    /// <summary>
    /// Runs the opening storyline: completes objectives as their conditions are met, and
    /// stages the hospital opening (waking up, the dagger, the horde in the canteen, the
    /// rescue by Minh). Nothing is saved until the player reaches the base, so quitting
    /// during the opening starts it again.
    /// </summary>
    public sealed class StoryDirector : MonoBehaviour
    {
        private PlayerController _player;
        private Func<int> _releaseHorde;
        private Action _leaveHospital;
        private Action _rebuildHospital;
        private bool _hordeReleased;
        private bool _started;

        public static StoryDirector Instance { get; private set; }

        /// <summary>The objective to show on the HUD, or null.</summary>
        public string Objective
        {
            get
            {
                if (GameRoot.Instance == null) return null;
                StoryStep step = PrototypeContent.OpeningStory.Current(GameRoot.Instance.Session);
                return step != null ? step.Text : null;
            }
        }

        /// <param name="releaseHorde">Spawns the canteen horde and returns how many zombies appeared.</param>
        /// <param name="leaveHospital">Swaps the hospital for the normal raid map and moves the player to the base.</param>
        /// <param name="rebuildHospital">Puts the hospital's zombies and containers back and the player on the bed.</param>
        public void Init(PlayerController player, Func<int> releaseHorde, Action leaveHospital, Action rebuildHospital)
        {
            _player = player;
            _releaseHorde = releaseHorde;
            _leaveHospital = leaveHospital;
            _rebuildHospital = rebuildHospital;
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
            if (GameRoot.Instance == null || _player == null) return;
            GameSession session = GameRoot.Instance.Session;

            if (!_started)
            {
                _started = true;
                RaidPressure.Suspended = false;
                if (session.InIntro) BeginIntro(session);
            }

            if (session.InIntro) TakeDagger(session);

            StoryStep done = PrototypeContent.OpeningStory.Advance(session);
            if (done != null && !string.IsNullOrEmpty(done.DoneMessage))
            {
                GameRoot.Instance.Notify(done.DoneMessage, 6f);
                Sfx.PlayUi(SoundId.ItemReveal, 0.6f);
            }
        }

        private void BeginIntro(GameSession session)
        {
            // The opening is played as a raid, so fog of war, hunger and combat all work as
            // they will later. It also uses up the day: the player reaches the base at dusk.
            session.TryStartRaid();
            RaidPressure.Suspended = true;
            _hordeReleased = false;
            if (_rebuildHospital != null) _rebuildHospital();

            if (SceneScreen.Instance != null) SceneScreen.Instance.Play("Bệnh viện thị trấn", PrototypeContent.IntroWakeScene);
        }

        /// <summary>The dagger is not carried as an item: taking it out of the cabinet gives the player their knife for good.</summary>
        private static void TakeDagger(GameSession session)
        {
            // Any dagger picked up is used up here, including a second one after a retry.
            bool taken = session.Backpack.TryConsume(PrototypeContent.Dagger.Id, 1)
                || session.SecurePouch.TryConsume(PrototypeContent.Dagger.Id, 1);
            if (!taken || session.Story.Has(StoryIds.HasKnife)) return;
            session.Story.Set(StoryIds.HasKnife);
            Sfx.PlayUi(SoundId.Reload, 0.6f);
        }

        /// <summary>The player has walked into the canteen.</summary>
        public void OnReachedCanteen()
        {
            GameSession session = GameRoot.Instance.Session;
            if (!session.InIntro || _hordeReleased) return;

            _hordeReleased = true;
            session.Story.Set(StoryIds.ReachedCanteen);
            if (_releaseHorde != null) _releaseHorde();
            Sfx.PlayUi(SoundId.ZombieGrowl, 1f);
            GameRoot.Instance.Notify("Cửa bếp bật tung. Cả một đàn! CHẠY về phía tây!", 6f);
        }

        /// <summary>The player has reached Minh.</summary>
        public void OnRescued()
        {
            GameSession session = GameRoot.Instance.Session;
            if (!session.InIntro) return;

            if (session.IsInRaid) session.CurrentRaid.Extract();
            PrototypeContent.FinishIntro(session);
            RaidPressure.Suspended = false;

            Health health = _player.GetComponent<Health>();
            if (health != null) health.ResetFull();
            if (_leaveHospital != null) _leaveHospital();
            GameRoot.Instance.Save();
            Sfx.PlayUi(SoundId.Extract, 0.8f);

            if (SceneScreen.Instance != null) SceneScreen.Instance.Play("Minh", PrototypeContent.IntroRescueScene);
        }

        /// <summary>Dying in the opening costs nothing: the player wakes on the hospital bed again.</summary>
        public void RestartIntro()
        {
            _hordeReleased = false;
            Health health = _player.GetComponent<Health>();
            if (health != null) health.ResetFull();
            if (_rebuildHospital != null) _rebuildHospital();
            GameRoot.Instance.Notify("Mọi thứ tối sầm... rồi bạn lại mở mắt trên giường bệnh. Thử lại, và lần này đừng đứng lại đánh cả đàn.", 8f);
        }
    }
}
