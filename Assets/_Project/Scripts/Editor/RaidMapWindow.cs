using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bnhon.Core.Maps;
using UnityEditor;
using UnityEngine;

namespace Bnhon.EditorTools
{
    /// <summary>
    /// Bnhon > Raid Map Editor. Shows a raid map text file as a picture and lets you paint
    /// it: pick a symbol, left-drag to paint, right-click to pick up the symbol under the
    /// pointer. Saving writes the same text format the game reads (see <see cref="RaidMap"/>).
    /// </summary>
    public sealed class RaidMapWindow : EditorWindow
    {
        private const string DefaultMapPath = "Assets/_Project/Resources/Maps/outskirts.txt";

        private static readonly char[] Symbols = { '.', '#', 'P', 'E', 'C', 'M', 'S', 'W', 'Z', 'R', 'G', 'K', 'T', 'H', 'N', 'V', 'A', 'B', ',', '=', ':', '~', 'X', 'J', 'U' };
        private static readonly string[] SymbolNames =
        {
            ". Sàn", "# Tường", "P Chỗ vào", "E Rút lui", "C Tủ nhà", "M Tủ thuốc", "S Hạt giống", "W Vũ khí",
            "Z Zombie", "R Zombie chạy", "G Zombie gào",
            "K Tủ cốt truyện", "T Bẫy thả đàn", "H Zombie của đàn", "N Người cứu",
            "V Két sắt", "A Zombie bọc thép", "B Trùm",
            ", Cỏ", "= Đường nhựa", ": Đất", "~ Ruộng nước", "X Cầu dao", "J Lối ra cần điện", "U Lối ra cần hạ trùm"
        };

        private TextAsset _asset;
        private readonly List<string> _header = new List<string>();
        private char[,] _cells;
        private int _width;
        private int _height;
        private Texture2D _texture;
        private bool _dirty;
        private string _loadedText;
        private string _status = string.Empty;
        private bool _statusIsError;

        private int _tool = 1;
        private float _zoom = 10f;
        private Vector2 _scroll;
        private int _hoverX = -1;
        private int _hoverY = -1;
        private GUIStyle _letterStyle;

        [MenuItem("Bnhon/Raid Map Editor")]
        public static void Open()
        {
            var window = GetWindow<RaidMapWindow>("Raid Map");
            window.minSize = new Vector2(520f, 360f);
            window.Show();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            if (_asset == null) _asset = AssetDatabase.LoadAssetAtPath<TextAsset>(DefaultMapPath);
            if (_asset != null && _cells == null) Load();
        }

        private void OnFocus()
        {
            // Pick up edits made in a text editor, unless there are unsaved changes here.
            if (_asset != null && !_dirty && _asset.text != _loadedText) Load();
        }

        private void OnDestroy()
        {
            if (_texture != null) DestroyImmediate(_texture);
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (_cells == null)
            {
                EditorGUILayout.HelpBox("Chọn một file bản đồ (.txt trong Resources/Maps) ở ô phía trên.", MessageType.Info);
                return;
            }

            _tool = GUILayout.SelectionGrid(_tool, SymbolNames, 6);
            EditorGUILayout.HelpBox(_status, _statusIsError ? MessageType.Error : MessageType.None);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            Rect area = GUILayoutUtility.GetRect(_width * _zoom, _height * _zoom, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            GUI.DrawTexture(area, _texture, ScaleMode.StretchToFill);
            if (_zoom >= 9f) DrawLetters(area);
            HandleMouse(area);
            EditorGUILayout.EndScrollView();

            string hover = _hoverX >= 0
                ? "Ô (" + _hoverX + ", " + _hoverY + "): " + _cells[_hoverX, _hoverY]
                : "Kéo chuột trái để vẽ · chuột phải để lấy ký hiệu tại ô đó";
            EditorGUILayout.LabelField(hover);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            var picked = (TextAsset)EditorGUILayout.ObjectField(_asset, typeof(TextAsset), false, GUILayout.Width(220f));
            if (EditorGUI.EndChangeCheck() && picked != _asset)
            {
                if (!_dirty || EditorUtility.DisplayDialog("Bản đồ chưa lưu", "Bỏ các thay đổi chưa lưu?", "Bỏ", "Hủy"))
                {
                    _asset = picked;
                    if (_asset != null) Load();
                    else _cells = null;
                }
            }

            GUI.enabled = _cells != null && _dirty;
            if (GUILayout.Button(_dirty ? "Lưu *" : "Lưu", GUILayout.Width(70f))) Save();
            if (GUILayout.Button("Hoàn tác hết", GUILayout.Width(100f))) Load();
            GUI.enabled = true;

            GUILayout.Label("Phóng to", GUILayout.Width(60f));
            _zoom = Mathf.Round(GUILayout.HorizontalSlider(_zoom, 4f, 28f, GUILayout.Width(140f)));

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void HandleMouse(Rect area)
        {
            Event current = Event.current;
            Vector2 mouse = current.mousePosition;

            int x = -1;
            int y = -1;
            if (area.Contains(mouse))
            {
                x = Mathf.Clamp((int)((mouse.x - area.x) / _zoom), 0, _width - 1);
                y = Mathf.Clamp((int)((mouse.y - area.y) / _zoom), 0, _height - 1);
            }

            if (x != _hoverX || y != _hoverY)
            {
                _hoverX = x;
                _hoverY = y;
                Repaint();
            }

            if (x < 0) return;
            bool press = current.type == EventType.MouseDown || current.type == EventType.MouseDrag;
            if (!press) return;

            if (current.button == 1)
            {
                int index = Array.IndexOf(Symbols, _cells[x, y]);
                if (index >= 0) _tool = index;
                current.Use();
                Repaint();
            }
            else if (current.button == 0)
            {
                Paint(x, y, Symbols[_tool]);
                current.Use();
            }
        }

        private void Paint(int x, int y, char symbol)
        {
            // The outer ring stays wall, so the map is always closed.
            bool border = x == 0 || y == 0 || x == _width - 1 || y == _height - 1;
            if (border && symbol != RaidMap.Wall) return;
            if (_cells[x, y] == symbol) return;

            if (symbol == RaidMap.PlayerSpawn)
            {
                // There is exactly one entry point: painting a new one moves it.
                for (int cx = 0; cx < _width; cx++)
                {
                    for (int cy = 0; cy < _height; cy++)
                    {
                        if (_cells[cx, cy] == RaidMap.PlayerSpawn) _cells[cx, cy] = RaidMap.Floor;
                    }
                }
            }

            _cells[x, y] = symbol;
            _dirty = true;
            RebuildTexture();
            Validate();
            Repaint();
        }

        private void DrawLetters(Rect area)
        {
            if (_letterStyle == null)
            {
                _letterStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };
                _letterStyle.normal.textColor = Color.black;
                _letterStyle.padding = new RectOffset(0, 0, 0, 0);
            }
            _letterStyle.fontSize = Mathf.Clamp((int)(_zoom * 0.8f), 7, 18);

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    char symbol = _cells[x, y];
                    if (symbol == RaidMap.Wall || symbol == RaidMap.Floor) continue;
                    GUI.Label(new Rect(area.x + x * _zoom, area.y + y * _zoom, _zoom, _zoom), symbol.ToString(), _letterStyle);
                }
            }
        }

        private void Load()
        {
            _loadedText = _asset.text;
            _header.Clear();
            var rows = new List<string>();

            foreach (string raw in _loadedText.Replace("\r", string.Empty).Split('\n'))
            {
                string line = raw.TrimEnd();
                if (line.Length == 0) continue;
                if (line[0] == ';')
                {
                    _header.Add(line);
                    continue;
                }
                rows.Add(line);
            }

            _width = 0;
            foreach (string row in rows) _width = Mathf.Max(_width, row.Length);
            _height = rows.Count;
            if (_width == 0 || _height == 0)
            {
                _cells = null;
                return;
            }

            _cells = new char[_width, _height];
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    char symbol = x < rows[y].Length ? rows[y][x] : RaidMap.Wall;
                    _cells[x, y] = symbol == ' ' ? RaidMap.Floor : symbol;
                }
            }

            _dirty = false;
            RebuildTexture();
            Validate();
            Repaint();
        }

        private string ToText()
        {
            var text = new StringBuilder();
            foreach (string line in _header) text.Append(line).Append('\n');
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++) text.Append(_cells[x, y]);
                text.Append('\n');
            }
            return text.ToString();
        }

        private void Save()
        {
            string path = AssetDatabase.GetAssetPath(_asset);
            if (string.IsNullOrEmpty(path)) return;

            string text = ToText();
            File.WriteAllText(path, text, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
            _loadedText = text;
            _dirty = false;
            Validate();
        }

        /// <summary>Counts what is on the map and runs the same checks the game does when it loads it.</summary>
        private void Validate()
        {
            int loot = 0;
            int zombies = 0;
            int exits = 0;
            foreach (char symbol in _cells)
            {
                if (RaidMap.LootSymbols.IndexOf(symbol) >= 0) loot++;
                else if (RaidMap.ZombieSymbols.IndexOf(symbol) >= 0) zombies++;
                else if (symbol == RaidMap.Extraction) exits++;
            }

            string summary = _width + "×" + _height + " ô · " + loot + " thùng đồ · " + zombies + " zombie · " + exits + " điểm rút lui";
            try
            {
                RaidMap map = RaidMap.Parse(ToText());
                List<MapMarker> unreachable = map.FindUnreachableMarkers();
                if (unreachable.Count == 0)
                {
                    _status = summary + " · hợp lệ";
                    _statusIsError = false;
                }
                else
                {
                    MapMarker first = unreachable[0];
                    _status = summary + " · " + unreachable.Count + " vị trí bị tường bít kín, ví dụ '" + first.Symbol + "' ở (" + first.X + ", " + first.Y + ")";
                    _statusIsError = true;
                }
            }
            catch (FormatException error)
            {
                _status = summary + " · LỖI: " + error.Message;
                _statusIsError = true;
            }
        }

        private void RebuildTexture()
        {
            if (_texture == null || _texture.width != _width || _texture.height != _height)
            {
                if (_texture != null) DestroyImmediate(_texture);
                _texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
                _texture.filterMode = FilterMode.Point;
                _texture.wrapMode = TextureWrapMode.Clamp;
                _texture.hideFlags = HideFlags.HideAndDontSave;
            }

            var pixels = new Color32[_width * _height];
            for (int y = 0; y < _height; y++)
            {
                // Texture rows run bottom to top; map rows run top to bottom.
                int textureRow = _height - 1 - y;
                for (int x = 0; x < _width; x++) pixels[textureRow * _width + x] = ColorOf(_cells[x, y]);
            }
            _texture.SetPixels32(pixels);
            _texture.Apply();
        }

        private static Color32 ColorOf(char symbol)
        {
            switch (symbol)
            {
                case '#': return new Color32(40, 42, 48, 255);
                case '.': return new Color32(112, 114, 120, 255);
                case 'P': return new Color32(70, 150, 255, 255);
                case 'E': return new Color32(70, 220, 110, 255);
                case 'C': return new Color32(200, 150, 80, 255);
                case 'M': return new Color32(235, 235, 240, 255);
                case 'S': return new Color32(170, 200, 90, 255);
                case 'W': return new Color32(120, 140, 100, 255);
                case 'Z': return new Color32(130, 180, 90, 255);
                case 'R': return new Color32(240, 140, 60, 255);
                case 'G': return new Color32(190, 100, 230, 255);
                case ',': return new Color32(78, 104, 62, 255);
                case '=': return new Color32(60, 62, 66, 255);
                case ':': return new Color32(122, 98, 70, 255);
                case '~': return new Color32(74, 104, 96, 255);
                case 'V': return new Color32(90, 110, 170, 255);
                case 'A': return new Color32(150, 160, 180, 255);
                case 'B': return new Color32(255, 0, 0, 255);
                case 'X': return new Color32(255, 255, 0, 255);
                case 'J': return new Color32(240, 220, 40, 255);
                case 'U': return new Color32(240, 120, 200, 255);
                case 'K': case 'T': case 'H': case 'N': return new Color32(255, 255, 255, 255);
                default: return new Color32(255, 0, 255, 255);
            }
        }
    }
}
