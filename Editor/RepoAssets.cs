using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using UnityEditor;
using UnityEngine;

namespace Kesomannen.RepoProjectPatcher.Editor {
    public static class RepoAssets {
        const string DownloadUrl = "https://codeload.github.com/Semiwork-Studios-AB/REPOAssets/zip/refs/heads/main";
        const string CacheFolder = "Library/RepoProjectPatcher/REPOAssets";
        const string OverrideFolderName = "REPOAssets~";

        static bool _downloaded;

        /// <summary>
        /// Returns the path to a top-level folder of the REPOAssets repository, downloading it once per editor session.
        /// A non-empty folder with the same name in the package's REPOAssets~ folder is used instead, for testing
        /// files that aren't pushed yet. Falls back to a previous download when offline. Returns null if the folder isn't available.
        /// </summary>
        public static string GetFolder(string folderName) {
            var overrideFolder = Path.Combine(GetPackageRoot(), OverrideFolderName, folderName);
            if (Directory.Exists(overrideFolder) && Directory.EnumerateFiles(overrideFolder, "*", SearchOption.AllDirectories).Any()) {
                Debug.Log($"Using local {folderName} files from {overrideFolder}");
                return overrideFolder;
            }

            if (!_downloaded) {
                try {
                    Download();
                    _downloaded = true;
                } catch (Exception e) {
                    if (Directory.Exists(CacheFolder)) {
                        Debug.LogWarning($"Failed to download REPOAssets, using the previously downloaded files: {e.Message}");
                    } else {
                        Debug.LogError($"Failed to download REPOAssets from {DownloadUrl}: {e}");
                        return null;
                    }
                }
            }

            var folder = Path.Combine(CacheFolder, folderName);
            if (Directory.Exists(folder)) return folder;

            Debug.LogError($"REPOAssets has no {folderName} folder");
            return null;
        }

        public static string GetPackageRoot() {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(RepoAssets).Assembly);
            return package != null ? package.resolvedPath : Path.GetFullPath($"Packages/{Constants.PackageName}");
        }

        static void Download() {
            EditorUtility.DisplayProgressBar("REPOAssets", "Downloading REPOAssets...", 0);
            try {
                using var client = new HttpClient();
                var bytes = client.GetByteArrayAsync(DownloadUrl).GetAwaiter().GetResult();

                using var zip = new ZipArchive(new MemoryStream(bytes));
                var tempFolder = CacheFolder + ".tmp";
                if (Directory.Exists(tempFolder)) Directory.Delete(tempFolder, true);

                var count = 0;
                foreach (var entry in zip.Entries) {
                    var slash = entry.FullName.IndexOf('/');
                    var path = slash < 0 ? "" : entry.FullName[(slash + 1)..];
                    if (path.Length == 0 || path.EndsWith("/")) continue;

                    var destPath = Path.Combine(tempFolder, path);
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                    using (var input = entry.Open())
                    using (var output = File.Create(destPath)) {
                        input.CopyTo(output);
                    }
                    count++;
                }

                if (Directory.Exists(CacheFolder)) Directory.Delete(CacheFolder, true);
                Directory.Move(tempFolder, CacheFolder);
                Debug.Log($"Downloaded {count} files from REPOAssets");
            } finally {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
