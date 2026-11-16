using Bnhon.Core;
using Bnhon.Core.Content;
using Bnhon.Core.Relationships;
using Bnhon.Core.Saving;
using UnityEngine;

namespace Bnhon.Game
{
    /// <summary>
    /// Owns the <see cref="GameSession"/> for the running game and survives
    /// scene loads. Everything in the scene reads game state through here.
    /// Loads the save file on start and writes it at the game's save points.
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
            Session = LoadOrCreateSession();
        }

        /// <summary>Continues from the save file if there is a usable one, otherwise starts a new game.</summary>
        private GameSession LoadOrCreateSession()
        {
            int seed = System.Environment.TickCount;
            SaveData data = SaveSystem.Load(out string problem);

            if (data != null)
            {
                try
                {
                    GameSession loaded = PrototypeContent.LoadSession(data, seed, out LoadReport report);
                    if (report.AbandonedRaid)
                    {
                        Notify("Bạn đã thoát game giữa chuyến raid: tính như gục ngã, mất đồ trong balo và vũ khí đang mang.", 9f);
                        // Write the outcome down, so reloading again does not repeat it.
                        SaveSystem.Save(loaded, false);
                    }
                    else
                    {
                        Notify("Đã tải game: ngày " + loaded.Clock.Day + ", năm " + loaded.Clock.Year + ".", 4f);
                    }
                    if (report.Dropped.Count > 0) Debug.LogWarning("File lưu có mục không còn tồn tại, đã bỏ qua: " + string.Join(", ", report.Dropped));
                    return loaded;
                }
                catch (System.Exception error)
                {
                    problem = error.Message;
                }
            }

            if (problem != null)
            {
                SaveSystem.SetAsideBrokenSave();
                Notify("File lưu bị lỗi (" + problem + "). Bắt đầu game mới; file cũ được giữ lại với đuôi .bad.", 10f);
            }
            return PrototypeContent.CreateNewGame(Gender.Male, seed);
        }

        /// <summary>Writes the current state to the save file.</summary>
        public void Save(bool inRaid = false)
        {
            if (Session != null) SaveSystem.Save(Session, inRaid);
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
