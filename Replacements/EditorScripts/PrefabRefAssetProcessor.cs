using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class PrefabRefAssetProcessor : AssetPostprocessor, IPreprocessBuildWithReport {

    public static string ResolveResourcePath(string assetPath, string newName) {
        return PrefabRefPaths.FromAssetPath(assetPath);
    }

    public static bool TryGetFinalized(UnityEngine.Object target, out bool finalized) {
        finalized = true;

        if(target is not GameObject go) return false;

        var module = go.GetComponent<Module>();
        if(module != null) return TryReadStatusFinalized(new SerializedObject(module), out finalized);

        var valuableObject = go.GetComponent<ValuableObject>();
        if(valuableObject != null) return TryReadStatusFinalized(new SerializedObject(valuableObject), out finalized);

        return false;
    }

    static bool TryReadStatusFinalized(SerializedObject serialized, out bool finalized) {
        finalized = true;
        var statusProp = serialized.FindProperty("status");
        if(statusProp == null || statusProp.propertyType != SerializedPropertyType.Enum) return false;

        finalized = statusProp.enumNames[statusProp.enumValueIndex] == "Finalized";
        return true;
    }

    static bool UpdateAssetValues(HashSet<string> targetGuids, SerializedObject serialized) {
        bool changed = false;
        var iterator = serialized.GetIterator();
        while(iterator.NextVisible(true)){
            if(iterator.propertyType != SerializedPropertyType.Generic || !iterator.type.StartsWith("PrefabRef")) continue;

            var prefabProp = iterator.FindPropertyRelative("prefab");
            if(prefabProp == null) continue;
            var referencedObject = prefabProp.objectReferenceValue;
            if(referencedObject == null) continue;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(referencedObject, out string refGuid, out long _);
            if(targetGuids != null && !targetGuids.Contains(refGuid)) continue;

            string path = AssetDatabase.GetAssetPath(referencedObject);
            string newName = System.IO.Path.GetFileNameWithoutExtension(path);

            var prefabNameProp = iterator.FindPropertyRelative("prefabName");
            if(prefabNameProp.stringValue != newName){
                prefabNameProp.stringValue = newName;
                changed = true;
            }

            var resourcePathProp = iterator.FindPropertyRelative("resourcePath");
            string newResourcePath = ResolveResourcePath(path, newName);
            if(resourcePathProp.stringValue != newResourcePath){
                resourcePathProp.stringValue = newResourcePath;
                changed = true;
            }

            var cachedFinalizedProp = iterator.FindPropertyRelative("cachedFinalized");
            if(cachedFinalizedProp != null && TryGetFinalized(referencedObject, out bool finalized) && cachedFinalizedProp.boolValue != finalized){
                cachedFinalizedProp.boolValue = finalized;
                changed = true;
            }
        }
        return changed;
    }

    private static void ProcessPaths(IEnumerable<string> paths, HashSet<string> targetGuids, List<UnityEngine.Object> dirtyAssets, List<GameObject> dirtyPrefabs) {
        foreach(var path in paths){
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(go != null){
                bool anyChanged = false;
                foreach(var comp in go.GetComponentsInChildren<MonoBehaviour>(true)){
                    if(comp == null) continue;
                    try{
                        var serialized = new SerializedObject(comp);
                        if(!UpdateAssetValues(targetGuids, serialized)) continue;
                        serialized.ApplyModifiedProperties();
                        EditorUtility.SetDirty(comp);
                        anyChanged = true;
                    }catch(Exception ex){
                        Debug.LogError(ex);
                    }
                }

                if(!anyChanged) continue;
                EditorUtility.SetDirty(go);
                dirtyPrefabs.Add(go);
                continue;
            }

            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path)){
                if(asset == null) continue;
                try{
                    var serialized = new SerializedObject(asset);
                    if(!UpdateAssetValues(targetGuids, serialized)) continue;
                    serialized.ApplyModifiedProperties();
                    EditorUtility.SetDirty(asset);
                    dirtyAssets.Add(asset);
                }catch(Exception ex){
                    Debug.LogError(ex);
                }
            }
        }
    }

    private static int UpdatePrefabRefs(HashSet<string> targetGuids = null, bool modAssetsOnly = false) {
        List<UnityEngine.Object> dirtyAssets = new();
        List<GameObject> dirtyPrefabs = new();

        try{
            var holderPaths = AssetDatabase.FindAssets("t:ScriptableObject")
                .Concat(AssetDatabase.FindAssets("t:Prefab"))
                .Distinct()
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !modAssetsOnly || !IsGameAsset(p))
                .Where(System.IO.File.Exists)
                .ToList();

            var dependents = new List<string>();
            for(int i = 0; i < holderPaths.Count; i++){
                if((i & 63) == 0 && EditorUtility.DisplayCancelableProgressBar("Updating PrefabRefs", $"Finding PrefabRefs ({i + 1}/{holderPaths.Count})", (float)i / holderPaths.Count * 0.5f)){
                    Debug.LogWarning("[PrefabRefAssetProcessor] Update cancelled by user.");
                    return 0;
                }

                var text = System.IO.File.ReadAllText(holderPaths[i]);
                if(!text.Contains("resourcePath:")) continue;
                if(targetGuids != null && !targetGuids.Any(text.Contains)) continue;
                dependents.Add(holderPaths[i]);
            }

            EditorUtility.DisplayProgressBar("Updating PrefabRefs", $"Updating {dependents.Count} assets...", 0.5f);
            ProcessPaths(dependents, targetGuids, dirtyAssets, dirtyPrefabs);

            if(dirtyAssets.Count > 0){
                EditorUtility.DisplayProgressBar("Updating PrefabRefs", "Saving modified assets...", 0.95f);
                AssetDatabase.StartAssetEditing();
                try{
                    foreach(var obj in dirtyAssets){
                        AssetDatabase.SaveAssetIfDirty(obj);
                    }
                }finally{
                    AssetDatabase.StopAssetEditing();
                }
            }

            var distinctPrefabs = dirtyPrefabs.Distinct().ToList();
            for(int i = 0; i < distinctPrefabs.Count; i++){
                EditorUtility.DisplayProgressBar(
                    "Updating PrefabRefs",
                    $"Saving prefab {distinctPrefabs[i].name} ({i + 1}/{distinctPrefabs.Count})",
                    0.95f + 0.05f * ((float)i / Math.Max(1, distinctPrefabs.Count)));
                PrefabUtility.SavePrefabAsset(distinctPrefabs[i]);
            }
        }finally{
            EditorUtility.ClearProgressBar();
        }

        int total = dirtyAssets.Count + dirtyPrefabs.Distinct().Count();
        if(total > 0){
            AssetDatabase.SaveAssets();
            Debug.Log($"[PrefabRefAssetProcessor] Updated PrefabRefs: {dirtyAssets.Count} assets, {dirtyPrefabs.Distinct().Count()} prefabs.");
        }
        return total;
    }

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
        var movedGuids = new HashSet<string>(movedAssets
            .Where(x => !IsGameAsset(x) && !AssetDatabase.IsValidFolder(x))
            .Select(AssetDatabase.AssetPathToGUID)
            .Where(x => !string.IsNullOrEmpty(x)));
        if(movedGuids.Count == 0) return;

        EditorApplication.delayCall += () => UpdatePrefabRefs(movedGuids, modAssetsOnly: true);
    }

    static bool IsGameAsset(string path) {
        return path.StartsWith(PrefabRefPaths.GameFolder, StringComparison.OrdinalIgnoreCase);
    }

    [MenuItem("Prefabs/Update All PrefabRef")]
    public static void UpdateAllPrefabRefsMenu() {
        int changed = UpdatePrefabRefs();
        EditorUtility.DisplayDialog("PrefabRef", $"Updated {changed} PrefabRef(s).", "OK");
    }

    [MenuItem("Assets/Update PrefabRef", false, 30)]
    public static void UpdatePrefabRefsForSelectedMenu() {
        string targetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        int changed = UpdatePrefabRefs(new HashSet<string> { AssetDatabase.AssetPathToGUID(targetPath) });
        EditorUtility.DisplayDialog("PrefabRef", $"Updated {changed} PrefabRef(s) referencing {System.IO.Path.GetFileName(targetPath)}.", "OK");
    }

    [MenuItem("Assets/Update PrefabRef", true)]
    public static bool UpdatePrefabRefsForSelectedValidate() {
        if(Selection.activeObject == null) return false;
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        return !string.IsNullOrEmpty(path) && System.IO.File.Exists(path) && !AssetDatabase.IsValidFolder(path);
    }

    public int callbackOrder => 1;

    public void OnPreprocessBuild(BuildReport report) {
        UpdatePrefabRefs();
    }
}
