using System;
using Bnhon.Game.Player;
using UnityEngine;

namespace Bnhon.Game.Raid
{
    /// <summary>An invisible area that does something once, the first time the player walks into it.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class StoryTrigger : MonoBehaviour
    {
        private Action _onEnter;
        private bool _fired;

        public void Init(Action onEnter)
        {
            _onEnter = onEnter;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_fired || other.GetComponent<PlayerController>() == null) return;
            _fired = true;
            if (_onEnter != null) _onEnter();
        }
    }
}
