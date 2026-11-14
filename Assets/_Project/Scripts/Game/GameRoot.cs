using Bnhon.Core;
using Bnhon.Core.Content;
using Bnhon.Core.Relationships;
using UnityEngine;

namespace Bnhon.Game
{
    /// <summary>
    /// Owns the <see cref="GameSession"/> for the running game and survives
    /// scene loads. Everything in the scene reads game state through here.
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        private float _messageUntil;

        public static GameRoot Instance { get; private set; }

        public GameSession Session { get; private set; }
        public string Message { get; private set; }
        public bool HasMessage => UnityEngine.Time.time < _messageUntil;

        public static GameRoot Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("GameRoot");
            return go.AddComponent<GameRoot>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Session = PrototypeContent.CreateNewSession(Gender.Male, System.Environment.TickCount);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Shows a short message on the debug HUD.</summary>
        public void Notify(string message, float seconds = 3.5f)
        {
            Message = message;
            _messageUntil = UnityEngine.Time.time + seconds;
            Debug.Log(message);
        }
    }
}
