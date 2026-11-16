using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace Kesomannen.RepoProjectPatcher.Editor {
    /// <summary>
    /// AssetRipper exports TextMeshPro's shaders as dummies (they only sample the atlas), so text renders as blocks.
    /// The real shaders ship with the ugui package inside "TMP Essential Resources.unitypackage"; this extracts its
    /// shaders and include files so they can replace the ripped ones (keeping the ripped GUIDs).
    /// </summary>
    public static class TmpShaders {
        const string PackagePath = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
        const string ShadersPrefix = "Assets/TextMesh Pro/Shaders/";
        const string CacheFolder = "Library/RepoProjectPatcher/TMPShaders";

        /// <summary>
        /// Extracts the TMP shader folder to a cache folder and returns its path, or null if the package isn't found.
        /// </summary>
        public static string GetFolder() {
            var packagePath = Path.GetFullPath(PackagePath);
            if (!File.Exists(packagePath)) {
                Debug.LogWarning($"TMP Essential Resources not found at {packagePath}, TextMeshPro shaders won't be replaced");
                return null;
            }

            try {
                if (Directory.Exists(CacheFolder)) Directory.Delete(CacheFolder, true);
                Directory.CreateDirectory(CacheFolder);
                var count = Extract(packagePath, CacheFolder);
                Debug.Log($"Extracted {count} TextMeshPro shader files from {packagePath}");
                return count > 0 ? CacheFolder : null;
            } catch (Exception e) {
                Debug.LogError($"Failed to extract TextMeshPro shaders from {packagePath}: {e}");
                return null;
            }
        }

        // A .unitypackage is a gzipped tar where each asset is a "<guid>/" folder holding "pathname" and "asset" files.
        static int Extract(string packagePath, string destFolder) {
            using var file = File.OpenRead(packagePath);
            using var gzip = new GZipStream(file, CompressionMode.Decompress);

            var pathnames = new System.Collections.Generic.Dictionary<string, string>();
            var assets = new System.Collections.Generic.Dictionary<string, byte[]>();

            var header = new byte[512];
            while (ReadExactly(gzip, header)) {
                if (Array.TrueForAll(header, b => b == 0)) break;

                var name = ReadString(header, 0, 100);
                var prefix = ReadString(header, 345, 155);
                if (prefix.Length > 0) name = prefix + "/" + name;
                var sizeField = ReadString(header, 124, 12).Trim();
                var size = sizeField.Length > 0 ? Convert.ToInt64(sizeField, 8) : 0;
                var type = (char)header[156];

                var data = new byte[size];
                if (!ReadExactly(gzip, data)) break;
                var padding = (512 - size % 512) % 512;
                if (padding > 0 && !ReadExactly(gzip, new byte[padding])) break;

                if (type != '0' && type != '\0') continue;

                if (name.StartsWith("./")) name = name[2..];
                var slash = name.IndexOf('/');
                if (slash < 0) continue;
                var guid = name[..slash];
                var entry = name[(slash + 1)..];

                if (entry == "pathname") {
                    pathnames[guid] = Encoding.UTF8.GetString(data).Split('\n')[0].Trim();
                } else if (entry == "asset") {
                    assets[guid] = data;
                }
            }

            var count = 0;
            foreach (var pair in pathnames) {
                var guid = pair.Key;
                var path = pair.Value;
                if (!path.StartsWith(ShadersPrefix)) continue;
                var ext = Path.GetExtension(path);
                if (ext != ".shader" && ext != ".cginc" && ext != ".hlsl") continue;
                if (!assets.TryGetValue(guid, out var data)) continue;

                File.WriteAllBytes(Path.Combine(destFolder, path[ShadersPrefix.Length..]), data);
                count++;
            }

            return count;
        }

        static bool ReadExactly(Stream stream, byte[] buffer) {
            var read = 0;
            while (read < buffer.Length) {
                var n = stream.Read(buffer, read, buffer.Length - read);
                if (n == 0) return false;
                read += n;
            }
            return true;
        }

        static string ReadString(byte[] buffer, int offset, int length) {
            var end = Array.IndexOf(buffer, (byte)0, offset, length);
            if (end < 0) end = offset + length;
            return Encoding.UTF8.GetString(buffer, offset, end - offset);
        }
    }
}
