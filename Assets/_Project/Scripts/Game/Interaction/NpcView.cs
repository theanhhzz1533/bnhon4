using System;
using Bnhon.Core;
using Bnhon.Core.Content;
using Bnhon.Core.Npcs;
using Bnhon.Core.Quests;
using Bnhon.Core.Relationships;
using Bnhon.Game.Player;
using Bnhon.Game.UI;
using UnityEngine;

namespace Bnhon.Game.Interaction
{
    /// <summary>
    /// A base resident. Stands where their schedule puts them for this half of the day,
    /// and opens the talk screen on E.
    /// </summary>
    public sealed class NpcView : MonoBehaviour, IInteractable
    {
        private string _npcId;
        private NpcLife _life;
        private Func<string, Vector2> _spotPosition;
        private string _spot;
        private int _placedDay = -1;
        private DayPhase _placedPhase;

        public string Prompt
        {
            get
            {
                GameSession session = GameRoot.Instance.Session;
                Relationship relationship = session.Relationships.Get(_npcId);
                if (relationship == null) return string.Empty;

                string prompt = "E: Nói chuyện với " + relationship.Npc.DisplayName + " (" + relationship.Hearts + " tim)";
                if (_life == null) return prompt;

                if (session.Story.Has(_life.MetFlag) && _life.NextEvent(relationship.Hearts, session.Phase, session.Story) != null)
                {
                    return prompt + "  ♥ có chuyện muốn nói";
                }
                foreach (DeliveryQuest quest in session.Quests.All)
                {
                    if (quest.GiverNpcId != _npcId) continue;
                    if (quest.State == QuestState.Available) return prompt + "  · có nhiệm vụ";
                    if (session.Quests.CanTurnIn(quest.Id, session.Backpack)) return prompt + "  · giao được nhiệm vụ";
                }
                return prompt;
            }
        }

        /// <param name="spotPosition">Where in the scene each named spot of the base is.</param>
        public void Init(string npcId, Func<string, Vector2> spotPosition)
        {
            _npcId = npcId;
            _life = PrototypeContent.FindNpcLife(npcId);
            _spotPosition = spotPosition;
            Place();
        }

        private bool _present = true;

        private void Update()
        {
            // Someone who has not been rescued yet is not at the base: not drawn, not in the way.
            bool here = Bnhon.Core.Npcs.Residents.LivesAtBase(GameRoot.Instance.Session, _npcId);
            if (here != _present)
            {
                _present = here;
                foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>()) renderer.enabled = here;
                foreach (Collider2D collider in GetComponents<Collider2D>()) collider.enabled = here;
            }
            if (here) Place();
        }

        /// <summary>
        /// Moves to the scheduled spot when the day or its half changes. Both only change
        /// while the player is asleep or away on a raid, so nobody is seen jumping.
        /// </summary>
        private void Place()
        {
            if (_life == null || _spotPosition == null || GameRoot.Instance == null) return;

            GameSession session = GameRoot.Instance.Session;
            if (session.IsInRaid) return;
            if (session.Clock.TotalDays == _placedDay && session.Phase == _placedPhase) return;

            _placedDay = session.Clock.TotalDays;
            _placedPhase = session.Phase;
            _spot = _life.Schedule.SpotFor(session.Clock.DayOfWeek, session.Phase);
            transform.position = _spotPosition(_spot);
        }

        public void Interact(PlayerController player)
        {
            if (!_present) return;
            if (NpcScreen.Instance != null) NpcScreen.Instance.Show(_npcId, _spot);
        }
    }
}
