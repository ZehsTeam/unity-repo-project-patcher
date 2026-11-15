using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Nomnom.UnityProjectPatcher.Editor.Steps;
using UnityEditor;
using UnityEngine;
#if REPO_PATCHER_LOCALIZATION
using UnityEditor.AddressableAssets;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
#endif

namespace Kesomannen.RepoProjectPatcher.Editor {
    public struct FixLocalizationStep : IPatcherStep {
        const string GameFolder = "Assets/REPO/Game";
        const string DestFolder = "Assets/REPO/Game/Localization";

        // script GUIDs from com.unity.localization
        const string StringTableCollectionScript = "5be51871efa6c3e4eae1703925c8f5ac";
        const string SheetsServiceProviderScript = "3888c1c1b587a654a919e0f68b03567e";
        const string SharedTableDataScript = "5b11a58205ec3474ca216360e9fa74a8";
        const string StringTableScript = "e9620f8c34305754d8cc9a7e49e852d9";

        static readonly Regex MetaGuidRegex = new(@"^guid: ([0-9a-f]{32})", RegexOptions.Multiline | RegexOptions.Compiled);
        static readonly Regex ScriptRegex = new(@"^  m_Script: \{fileID: -?\d+, guid: ([0-9a-f]{32})", RegexOptions.Multiline | RegexOptions.Compiled);
        static readonly Regex NameRegex = new(@"^  m_Name: (.*?)\r?$", RegexOptions.Multiline | RegexOptions.Compiled);
        static readonly Regex CollectionNameRegex = new(@"^  m_TableCollectionName: (.*?)\r?$", RegexOptions.Multiline | RegexOptions.Compiled);
        static readonly Regex SharedDataRegex = new(@"^  m_SharedData: \{fileID: -?\d+, guid: ([0-9a-f]{32})", RegexOptions.Multiline | RegexOptions.Compiled);
        static readonly Regex GuidRefRegex = new(@"guid: ([0-9a-f]{32})", RegexOptions.Compiled);

        static readonly HashSet<string> YamlExtensions = new(StringComparer.OrdinalIgnoreCase) {
            ".asset", ".prefab", ".unity", ".controller", ".overrideController", ".anim", ".mat",
            ".playable", ".mask", ".preset", ".spriteatlas", ".lighting", ".signal", ".physicMaterial",
            ".physicsMaterial2D", ".flare", ".renderTexture", ".mixer", ".guiskin", ".fontsettings"
        };

        [MenuItem("Tools/R.E.P.O. Project Patcher/Patch Assets/Localization")]
        static void MenuItem() {
            FixLocalization();
        }

        public UniTask<StepResult> Run() {
            FixLocalization();
            return UniTask.FromResult(StepResult.Success);
        }

        public void OnComplete(bool failed) { }

        class SourceAsset {
            public string RelativePath;
            public string Guid;
            public string ScriptGuid;
            public string Name;
            public string Text;
        }

        class ProjectAsset {
            public string Path;
            public string Guid;
            public string ScriptGuid;
            public string Name;
        }

        internal static void FixLocalization() {
            var sourceFolder = RepoAssets.GetFolder("Localization");
            if (sourceFolder == null) {
                Debug.LogWarning("Skipping FixLocalizationStep because the localization files are not available");
                return;
            }

            var sources = ReadSourceAssets(sourceFolder);
            if (sources.Count == 0) {
                Debug.LogError($"Skipping FixLocalizationStep because {sourceFolder} contains no localization assets with .meta files. " +
                               "The .meta files hold the original GUIDs and are required.");
                return;
            }

            var projectAssets = ReadProjectAssets();
            var scriptMap = BuildScriptMap(sourceFolder, sources, projectAssets);

            foreach (var source in sources) {
                source.ScriptGuid = Remap(source.ScriptGuid, scriptMap);
                source.Text = GuidRefRegex.Replace(source.Text, m => "guid: " + Remap(m.Groups[1].Value, scriptMap));
            }

            var byKey = projectAssets
                .GroupBy(x => (x.ScriptGuid, x.Name))
                .ToDictionary(x => x.Key, x => x.ToList());
            var sourceGuids = new HashSet<string>(sources.Select(x => x.Guid));
            var rippedToReal = new Dictionary<string, string>();
            var rippedPaths = new List<string>();

            foreach (var source in sources) {
                if (!byKey.TryGetValue((source.ScriptGuid, source.Name), out var matches)) continue;

                foreach (var match in matches) {
                    if (sourceGuids.Contains(match.Guid)) continue;

                    rippedToReal[match.Guid] = source.Guid;
                    rippedPaths.Add(match.Path);
                }
            }

            Debug.Log($"Found {sources.Count} localization assets, replacing {rippedToReal.Count} ripped assets");
            WarnUnresolvedReferences(sources, sourceGuids);

            AssetDatabase.StartAssetEditing();
            try {
                foreach (var path in rippedPaths) {
                    DeleteWithMeta(path);
                }

                foreach (var source in sources) {
                    var destPath = Path.Combine(DestFolder, source.RelativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

                    if (File.Exists(destPath + ".meta")) {
                        var existingGuid = ReadMetaGuid(destPath + ".meta");
                        if (existingGuid != null && existingGuid != source.Guid) {
                            rippedToReal[existingGuid] = source.Guid;
                            DeleteWithMeta(destPath);
                        }
                    }

                    File.WriteAllText(destPath, source.Text);
                    File.Copy(Path.Combine(sourceFolder, source.RelativePath + ".meta"), destPath + ".meta", overwrite: true);
                }

                WriteTableCollections(sources);

                var changed = RemapProjectReferences(rippedToReal);
                Debug.Log($"Remapped localization references in {changed} files");
            } finally {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            RegisterWithLocalization(rippedToReal.Keys);
        }

        static List<SourceAsset> ReadSourceAssets(string sourceFolder) {
            var result = new List<SourceAsset>();

            foreach (var path in Directory.GetFiles(sourceFolder, "*.asset", SearchOption.AllDirectories)) {
                var metaPath = path + ".meta";
                if (!File.Exists(metaPath)) continue;

                var text = File.ReadAllText(path);
                var scriptGuid = Match(ScriptRegex, text);

                if (scriptGuid is SheetsServiceProviderScript or StringTableCollectionScript) continue;

                result.Add(new SourceAsset {
                    RelativePath = Path.GetRelativePath(sourceFolder, path),
                    Guid = ReadMetaGuid(metaPath),
                    ScriptGuid = scriptGuid,
                    Name = Unquote(Match(NameRegex, text)),
                    Text = text
                });
            }

            return result;
        }

        static List<ProjectAsset> ReadProjectAssets() {
            var result = new List<ProjectAsset>();

            foreach (var path in Directory.EnumerateFiles(GameFolder, "*.asset", SearchOption.AllDirectories)) {
                var metaPath = path + ".meta";
                if (!File.Exists(metaPath)) continue;

                var header = ReadHeader(path);
                if (header == null) continue;

                var scriptGuid = Match(ScriptRegex, header);
                if (scriptGuid == null) continue;

                result.Add(new ProjectAsset {
                    Path = path.Replace('\\', '/'),
                    Guid = ReadMetaGuid(metaPath),
                    ScriptGuid = scriptGuid,
                    Name = Unquote(Match(NameRegex, header))
                });
            }

            return result;
        }

        static Dictionary<string, string> BuildScriptMap(string sourceFolder, List<SourceAsset> sources, List<ProjectAsset> projectAssets) {
            var map = new Dictionary<string, string>();
            var scriptsFolder = Path.Combine(GameFolder, "Scripts");

            foreach (var metaPath in Directory.GetFiles(sourceFolder, "*.cs.meta", SearchOption.AllDirectories)) {
                var realGuid = ReadMetaGuid(metaPath);
                var fileName = Path.GetFileName(metaPath);

                var candidates = Directory.GetFiles(scriptsFolder, fileName, SearchOption.AllDirectories);
                var projectMeta = candidates.FirstOrDefault(x => x.Contains("Assembly-CSharp")) ?? candidates.FirstOrDefault();
                if (projectMeta == null) continue;

                var projectGuid = ReadMetaGuid(projectMeta);
                if (realGuid != null && projectGuid != null && realGuid != projectGuid) {
                    map[realGuid] = projectGuid;
                }
            }

            var byName = projectAssets.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.ToList());
            var unresolved = sources
                .Select(x => x.ScriptGuid)
                .Where(x => x != null && !map.ContainsKey(x) && string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(x)))
                .Distinct();

            foreach (var scriptGuid in unresolved) {
                var vote = sources
                    .Where(x => x.ScriptGuid == scriptGuid && x.Name != null && byName.ContainsKey(x.Name))
                    .SelectMany(x => byName[x.Name])
                    .GroupBy(x => x.ScriptGuid)
                    .OrderByDescending(x => x.Count())
                    .FirstOrDefault();

                if (vote != null) {
                    map[scriptGuid] = vote.Key;
                } else {
                    Debug.LogWarning($"Could not find the project script for localization script GUID {scriptGuid}");
                }
            }

            return map;
        }

        static void WarnUnresolvedReferences(List<SourceAsset> sources, HashSet<string> sourceGuids) {
            foreach (var source in sources) {
                var unresolved = GuidRefRegex.Matches(source.Text)
                    .Select(m => m.Groups[1].Value)
                    .Where(x => !sourceGuids.Contains(x) && string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(x)))
                    .Distinct()
                    .ToList();

                if (unresolved.Count > 0) {
                    Debug.LogWarning($"{source.RelativePath} references assets that don't exist in this project ({string.Join(", ", unresolved)}), those references will be missing");
                }
            }
        }

        static void WriteTableCollections(List<SourceAsset> sources) {
            var tablesBySharedData = sources
                .Where(x => x.ScriptGuid == StringTableScript)
                .GroupBy(x => Match(SharedDataRegex, x.Text))
                .ToDictionary(x => x.Key ?? "", x => x.OrderBy(t => t.Name).ToList());

            foreach (var sharedData in sources.Where(x => x.ScriptGuid == SharedTableDataScript)) {
                var collectionName = Unquote(Match(CollectionNameRegex, sharedData.Text));
                if (string.IsNullOrEmpty(collectionName)) continue;

                tablesBySharedData.TryGetValue(sharedData.Guid, out var tables);
                tables ??= new List<SourceAsset>();

                var builder = new StringBuilder();
                builder.Append("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n");
                builder.Append("  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n");
                builder.Append("  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n");
                builder.Append($"  m_Script: {{fileID: 11500000, guid: {StringTableCollectionScript}, type: 3}}\n");
                builder.Append($"  m_Name: {collectionName}\n  m_EditorClassIdentifier: \n");
                builder.Append($"  m_SharedTableData: {{fileID: 11400000, guid: {sharedData.Guid}, type: 2}}\n");
                builder.Append(tables.Count == 0 ? "  m_Tables: []\n" : "  m_Tables:\n");
                foreach (var table in tables) {
                    builder.Append($"  - {{fileID: 11400000, guid: {table.Guid}, type: 2}}\n");
                }
                builder.Append($"  m_Extensions: []\n  m_Group: {collectionName}\n  references:\n    version: 2\n    RefIds: []\n");

                var folder = Path.Combine(DestFolder, Path.GetDirectoryName(sharedData.RelativePath)!);
                var path = Path.Combine(folder, collectionName + ".asset");
                File.WriteAllText(path, builder.ToString());
                File.WriteAllText(path + ".meta",
                    $"fileFormatVersion: 2\nguid: {DeterministicGuid("REPO.StringTableCollection:" + sharedData.Guid)}\n" +
                    "NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n");
            }
        }

        static int RemapProjectReferences(Dictionary<string, string> map) {
            if (map.Count == 0) return 0;

            var changed = 0;
            var destFull = Path.GetFullPath(DestFolder);

            foreach (var path in Directory.EnumerateFiles("Assets", "*", SearchOption.AllDirectories)) {
                if (!YamlExtensions.Contains(Path.GetExtension(path))) continue;
                if (Path.GetFullPath(path).StartsWith(destFull, StringComparison.OrdinalIgnoreCase)) continue;
                if (!IsYaml(path)) continue;

                var text = File.ReadAllText(path);
                var replaced = false;
                var newText = GuidRefRegex.Replace(text, m => {
                    if (!map.TryGetValue(m.Groups[1].Value, out var real)) return m.Value;
                    replaced = true;
                    return "guid: " + real;
                });

                if (!replaced) continue;

                File.WriteAllText(path, newText);
                changed++;
            }

            return changed;
        }

        static void RegisterWithLocalization(IEnumerable<string> rippedGuids) {
#if REPO_PATCHER_LOCALIZATION
            var settingsGuid = AssetDatabase.FindAssets("t:LocalizationSettings", new[] { DestFolder }).FirstOrDefault();
            if (settingsGuid == null) {
                Debug.LogWarning("No LocalizationSettings found in the localization folder");
                return;
            }

            var settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(AssetDatabase.GUIDToAssetPath(settingsGuid));
            LocalizationEditorSettings.ActiveLocalizationSettings = settings;

            var addressables = AddressableAssetSettingsDefaultObject.GetSettings(true);
            foreach (var guid in rippedGuids) {
                addressables.RemoveAssetEntry(guid, false);
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Locale", new[] { DestFolder })) {
                var locale = AssetDatabase.LoadAssetAtPath<Locale>(AssetDatabase.GUIDToAssetPath(guid));
                if (locale != null) LocalizationEditorSettings.AddLocale(locale);
            }

            foreach (var collection in LocalizationEditorSettings.GetStringTableCollections()) {
                collection.RefreshAddressables();
            }

            EditorUtility.SetDirty(addressables);
            AssetDatabase.SaveAssets();
            Debug.Log("Registered game localization settings, locales and tables");
#else
            Debug.LogWarning("com.unity.localization is not installed, skipping localization registration");
#endif
        }

        static void DeleteWithMeta(string path) {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".meta")) File.Delete(path + ".meta");
        }

        static string Remap(string guid, Dictionary<string, string> map) {
            return guid != null && map.TryGetValue(guid, out var mapped) ? mapped : guid;
        }

        static string Match(Regex regex, string text) {
            var match = regex.Match(text);
            return match.Success ? match.Groups[1].Value : null;
        }

        static string Unquote(string value) {
            if (value == null) return null;
            value = value.Trim();
            if (value.Length >= 2 && value[0] == '\'' && value[^1] == '\'') return value[1..^1].Replace("''", "'");
            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"') return value[1..^1];
            return value;
        }

        static string ReadMetaGuid(string metaPath) {
            return Match(MetaGuidRegex, File.ReadAllText(metaPath));
        }

        static string ReadHeader(string path) {
            using var reader = new StreamReader(path);
            var buffer = new char[2048];
            var read = reader.ReadBlock(buffer, 0, buffer.Length);
            var header = new string(buffer, 0, read);
            return header.StartsWith("%YAML") ? header : null;
        }

        static bool IsYaml(string path) {
            using var stream = File.OpenRead(path);
            var buffer = new byte[5];
            return stream.Read(buffer, 0, 5) == 5 && Encoding.ASCII.GetString(buffer) == "%YAML";
        }

        static string DeterministicGuid(string seed) {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(seed));
            return string.Concat(hash.Select(x => x.ToString("x2")));
        }
    }
}
