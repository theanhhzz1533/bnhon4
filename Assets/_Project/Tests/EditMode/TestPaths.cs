using System;
using System.IO;

namespace Bnhon.Core.Tests
{
    /// <summary>Finds project files from tests. Unity runs tests from the project folder; other runners can set BNHON_ROOT.</summary>
    public static class TestPaths
    {
        private const string MapsFolder = "Assets/_Project/Resources/Maps";

        public static string Maps
        {
            get
            {
                string root = Environment.GetEnvironmentVariable("BNHON_ROOT");
                if (!string.IsNullOrEmpty(root)) return Path.Combine(root, MapsFolder);

                string folder = Directory.GetCurrentDirectory();
                while (!string.IsNullOrEmpty(folder))
                {
                    string candidate = Path.Combine(folder, MapsFolder);
                    if (Directory.Exists(candidate)) return candidate;
                    folder = Path.GetDirectoryName(folder);
                }
                return MapsFolder;
            }
        }
    }
}
