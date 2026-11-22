using System;
namespace UnityEngine {
 public enum FilterMode { Point } public enum TextureWrapMode { Clamp } public enum TextureFormat { RGBA32 } public enum HideFlags { HideAndDontSave } public enum ScaleMode { StretchToFill } public enum TextAnchor { MiddleCenter } public enum EventType { MouseDown, MouseDrag }
 public class Object { public HideFlags hideFlags; public static void DestroyImmediate(Object o){} public static bool operator ==(Object a,Object b)=>ReferenceEquals(a,b); public static bool operator !=(Object a,Object b)=>!ReferenceEquals(a,b); public override bool Equals(object o)=>false; public override int GetHashCode()=>0; }
 public class ScriptableObject : Object {} public class TextAsset : Object { public string text; }
 public class Texture : Object { public int width, height; public FilterMode filterMode; public TextureWrapMode wrapMode; }
 public class Texture2D : Texture { public Texture2D(int w,int h,TextureFormat f,bool m){} public void SetPixels32(Color32[] c){} public void Apply(){} }
 public struct Color32 { public Color32(byte r,byte g,byte b,byte a){} } public struct Color { public static Color black; }
 public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
 public struct Rect { public float x,y,width,height; public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;} public bool Contains(Vector2 p)=>false; }
 public class RectOffset { public RectOffset(int a,int b,int c,int d){} } public class GUIStyleState { public Color textColor; }
 public class GUIStyle { public GUIStyle(GUIStyle o){} public TextAnchor alignment; public GUIStyleState normal; public RectOffset padding; public int fontSize; }
 public class GUILayoutOption {} public class Event { public static Event current; public Vector2 mousePosition; public EventType type; public int button; public void Use(){} }
 public static class GUI { public static bool enabled; public static void DrawTexture(Rect r,Texture t,ScaleMode m){} public static void Label(Rect r,string s,GUIStyle st){} }
 public static class GUILayout { public static int SelectionGrid(int s,string[] t,int x)=>0; public static GUILayoutOption ExpandWidth(bool b)=>null; public static GUILayoutOption ExpandHeight(bool b)=>null; public static GUILayoutOption Width(float w)=>null; public static bool Button(string s,params GUILayoutOption[] o)=>false; public static void Label(string s,params GUILayoutOption[] o){} public static float HorizontalSlider(float v,float a,float b,params GUILayoutOption[] o)=>v; public static void FlexibleSpace(){} }
 public static class GUILayoutUtility { public static Rect GetRect(float w,float h,params GUILayoutOption[] o)=>default; }
 public static class Application { public static string persistentDataPath; } public static class Mathf { public static int Clamp(int v,int a,int b)=>v; public static int Max(int a,int b)=>a; public static float Round(float f)=>f; }
}
namespace UnityEditor {
 using UnityEngine;
 public enum TextureImporterType { Sprite } public enum SpriteImportMode { Single } public enum TextureImporterCompression { Uncompressed } public enum MessageType { None, Info, Error }
 public class AssetImporter {} public class TextureImporter : AssetImporter { public TextureImporterType textureType; public SpriteImportMode spriteImportMode; public FilterMode filterMode; public TextureImporterCompression textureCompression; public bool mipmapEnabled, alphaIsTransparency; public TextureWrapMode wrapMode; }
 public class AssetPostprocessor { public string assetPath; public AssetImporter assetImporter; }
 public class MenuItem : Attribute { public MenuItem(string s){} }
 public class EditorWindow : ScriptableObject { public bool wantsMouseMove; public Vector2 minSize; public static T GetWindow<T>(string t) where T:EditorWindow=>null; public void Show(){} public void Repaint(){} }
 public static class AssetDatabase { public static T LoadAssetAtPath<T>(string p) where T:Object=>null; public static string GetAssetPath(Object o)=>null; public static void ImportAsset(string p){} }
 public static class EditorGUILayout { public static void HelpBox(string s,MessageType t){} public static Vector2 BeginScrollView(Vector2 v)=>v; public static void EndScrollView(){} public static void LabelField(string s){} public static void BeginHorizontal(){} public static void EndHorizontal(){} public static Object ObjectField(Object o,Type t,bool a,params GUILayoutOption[] op)=>o; }
 public static class EditorGUI { public static void BeginChangeCheck(){} public static bool EndChangeCheck()=>false; }
 public static class EditorUtility { public static bool DisplayDialog(string a,string b,string c,string d)=>true; public static bool DisplayDialog(string a,string b,string c)=>true; public static void RevealInFinder(string p){} } public static class EditorApplication { public static bool isPlaying; }
 public static class EditorStyles { public static GUIStyle miniBoldLabel; }
}
