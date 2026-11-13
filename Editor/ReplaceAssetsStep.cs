using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;
using UnityEngine;

namespace Kesomannen.RepoProjectPatcher.Editor {
    public struct ReplaceAssetsStep : IPatcherStep {
        const string GameShadersFolder = "Assets/REPO/Game/Shaders";
        const string CustomShadersFolderName = "Shaders~";
        static readonly Regex ShaderNameRegex = new(@"^\s*Shader\s+""([^""]+)""", RegexOptions.Multiline);

        [MenuItem("Tools/R.E.P.O. Project Patcher/Patch Assets/All")]
        static void MenuItem() {
            ReplaceAssets(null);
            FixLocalizationStep.FixLocalization();
        }

        [MenuItem("Tools/R.E.P.O. Project Patcher/Patch Assets/Scripts")]
        static void MenuItemScripts() {
            ReplaceAssets(".cs");
        }

        [MenuItem("Tools/R.E.P.O. Project Patcher/Patch Assets/Shaders")]
        static void MenuItemShaders() {
            ReplaceAssets(".shader");
        }

        static void ReplaceAssets(string extensionFilter) {
            var gameFolder = "Assets/REPO";
            var customFolder = Path.GetFullPath($"Packages/{Constants.PackageName}/Replacements");

            if (!Directory.Exists(gameFolder)) {
                Debug.LogWarning($"Game folder not found at: {gameFolder}");
                return;
            }

            if (!Directory.Exists(customFolder)) {
                Debug.LogWarning($"Replacements folder not found at: {customFolder}");
                return;
            }

            AssetDatabase.StartAssetEditing();
            var count = 0;

            try {
                var assets = Directory.GetFiles(customFolder, "*", SearchOption.AllDirectories);
                foreach (var srcPath in assets) {
                    var ext = Path.GetExtension(srcPath);
                    if (ext == ".meta") continue;
                    if (!string.IsNullOrEmpty(extensionFilter) && ext != extensionFilter) continue;

                    var relativePath = Path.GetRelativePath(customFolder, srcPath);
                    var destPath = Path.Combine(gameFolder, relativePath).Replace("EditorScripts", "Editor");
                    var destDir = Path.GetDirectoryName(destPath);

                    if (!Directory.Exists(destDir)) {
                        Directory.CreateDirectory(destDir);
                    }

                    File.Copy(srcPath, destPath, overwrite: true);
                    count++;
                }

                if (extensionFilter is null or ".shader") {
                    count += ReplaceGameShaders();
                }

                Debug.Log($"Copied {count} replacement assets" + (extensionFilter != null ? $" ({extensionFilter})" : ""));
            } finally {
                AssetDatabase.StopAssetEditing();
                if (count > 0) {
                    AssetDatabase.Refresh();
                }
            }
        }

        static int ReplaceGameShaders() {
            var projectShaders = new Dictionary<string, string>();
            foreach (var path in Directory.GetFiles(GameShadersFolder, "*.shader", SearchOption.AllDirectories)) {
                var name = GetShaderName(path);
                if (name != null) projectShaders.TryAdd(name, path);
            }

            var count = CopyShaders(Path.Combine(RepoAssets.GetPackageRoot(), CustomShadersFolderName), projectShaders, "custom replacements");

            var repoAssetsFolder = RepoAssets.GetFolder("Shaders");
            if (repoAssetsFolder != null) {
                count += CopyShaders(repoAssetsFolder, projectShaders, "REPOAssets");
            }

            return count;
        }

        static int CopyShaders(string sourceFolder, Dictionary<string, string> projectShaders, string sourceName) {
            if (!Directory.Exists(sourceFolder)) return 0;

            var count = 0;
            foreach (var srcPath in Directory.GetFiles(sourceFolder, "*.shader", SearchOption.AllDirectories)) {
                var name = GetShaderName(srcPath);
                if (name == null) {
                    Debug.LogWarning($"Could not read the shader name of {srcPath}");
                    continue;
                }

                if (!projectShaders.TryGetValue(name, out var destPath)) {
                    destPath = Path.Combine(GameShadersFolder, Path.GetFileName(srcPath));
                    if (File.Exists(destPath)) {
                        Debug.LogWarning($"Skipping shader \"{name}\" because {destPath} already contains a different shader");
                        continue;
                    }
                    projectShaders[name] = destPath;
                }

                File.Copy(srcPath, destPath, overwrite: true);
                count++;
            }

            Debug.Log($"Replaced {count} shaders with {sourceName}");
            return count;
        }

        static string GetShaderName(string path) {
            var match = ShaderNameRegex.Match(File.ReadAllText(path));
            return match.Success ? match.Groups[1].Value : null;
        }

        public UniTask<StepResult> Run() {
            ReplaceAssets(null);
            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }
    }
}