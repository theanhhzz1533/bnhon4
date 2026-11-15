using System;
using System.IO;
using Bnhon.Core;
using Bnhon.Core.Saving;
using UnityEngine;

namespace Bnhon.Game
{
    /// <summary>
    /// Reads and writes the single save file, <c>save.json</c> in the game's persistent data
    /// folder. The game saves by itself: when a raid starts (marked as in progress), when it
    /// ends, and on sleeping. There is no manual save.
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "save.json";

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool Exists => File.Exists(FilePath);

        /// <summary>Writes the session to disk. Returns false (and logs why) if it could not.</summary>
        public static bool Save(GameSession session, bool inRaid)
        {
            try
            {
                string json = JsonUtility.ToJson(SaveMapper.Capture(session, inRaid), true);

                // Write beside the real file and swap, so a crash mid-write cannot corrupt the save.
                string temporary = FilePath + ".tmp";
                File.WriteAllText(temporary, json);
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(temporary, FilePath);
                return true;
            }
            catch (Exception error)
            {
                Debug.LogError("Không lưu được game: " + error.Message);
                return false;
            }
        }

        /// <summary>Reads the save file. Returns null if there is none or it cannot be read.</summary>
        public static SaveData Load(out string problem)
        {
            problem = null;
            if (!Exists) return null;

            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                if (data == null) problem = "file lưu trống";
                return data;
            }
            catch (Exception error)
            {
                problem = error.Message;
                return null;
            }
        }

        /// <summary>Moves a save that cannot be used out of the way, keeping it for inspection.</summary>
        public static void SetAsideBrokenSave()
        {
            try
            {
                if (!Exists) return;
                string aside = FilePath + ".bad";
                if (File.Exists(aside)) File.Delete(aside);
                File.Move(FilePath, aside);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Không dời được file lưu hỏng: " + error.Message);
            }
        }

        public static void Delete()
        {
            if (Exists) File.Delete(FilePath);
        }
    }
}
