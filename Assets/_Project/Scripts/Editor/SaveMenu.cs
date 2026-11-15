using System.IO;
using UnityEditor;
using UnityEngine;

namespace Bnhon.EditorTools
{
    /// <summary>Bnhon menu entries for working with the save file while developing.</summary>
    public static class SaveMenu
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        [MenuItem("Bnhon/Save/Xóa file lưu (chơi lại từ đầu)")]
        private static void DeleteSave()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Đang Play", "Dừng Play trước khi xóa file lưu.", "OK");
                return;
            }
            if (!File.Exists(SavePath))
            {
                EditorUtility.DisplayDialog("Không có file lưu", "Chưa có file lưu nào.", "OK");
                return;
            }
            if (EditorUtility.DisplayDialog("Xóa file lưu", "Xóa toàn bộ tiến trình và chơi lại từ đầu?", "Xóa", "Hủy"))
            {
                File.Delete(SavePath);
            }
        }

        [MenuItem("Bnhon/Save/Mở thư mục chứa file lưu")]
        private static void RevealSave()
        {
            EditorUtility.RevealInFinder(File.Exists(SavePath) ? SavePath : Application.persistentDataPath);
        }
    }
}
