using System;
using System.IO;

/// <summary>Every map in Resources/Maps parses, and every marker on it can be walked to.</summary>
public static class MapCheck
{
    public static int Run()
    {
        int bad = 0;
        foreach (string file in Directory.GetFiles(Path.Combine(RepoRoot(), "Assets/_Project/Resources/Maps"), "*.txt"))
        {
            var map = Bnhon.Core.Maps.RaidMap.Parse(File.ReadAllText(file));
            var unreachable = map.FindUnreachableMarkers();
            Console.WriteLine("map " + Path.GetFileName(file) + " " + map.Width + "x" + map.Height + ", markers " + map.Markers.Count + ", unreachable " + unreachable.Count);
            foreach (var marker in unreachable) Console.WriteLine("  " + marker.Symbol + " " + marker.X + "," + marker.Y);
            bad += unreachable.Count;
        }
        return bad;
    }

    /// <summary>BNHON_ROOT if set, otherwise the first folder upward that holds Assets/_Project.</summary>
    public static string RepoRoot()
    {
        string fromEnv = Environment.GetEnvironmentVariable("BNHON_ROOT");
        if (!string.IsNullOrEmpty(fromEnv)) return fromEnv;
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Assets/_Project"))) dir = dir.Parent;
        if (dir == null) throw new Exception("Run from inside the repo, or set BNHON_ROOT.");
        return dir.FullName;
    }
}
