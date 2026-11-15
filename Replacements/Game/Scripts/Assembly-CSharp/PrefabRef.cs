using System;
using System.IO;
using UnityEngine;

[Serializable]
public class PrefabRef<T> where T : UnityEngine.Object {
	#if UNITY_EDITOR
	[SerializeField] private T prefab;
	#endif

	[SerializeField] private string prefabName;
	[SerializeField] private string resourcePath;
	[SerializeField] private AssetBundle bundle;
	[SerializeField] private bool cachedFinalized = true;

	public string PrefabName => prefabName;
	public string ResourcePath => bundle ? $"AssetBundles/{bundle.name}/{resourcePath}" : resourcePath;
	public AssetBundle Bundle => bundle;
	public bool IsFinalized => cachedFinalized;

	public T Prefab {
		get {
			#if UNITY_EDITOR
			if(prefab != null) return prefab;
			#endif

			if(string.IsNullOrEmpty(resourcePath)) return null;

			string cachedPath;
			if(bundle){
				cachedPath = ResourcePath;
			}else{
				cachedPath = resourcePath;
			}

			if(RunManager.instance.singleplayerPool.TryGetValue(cachedPath, out var cachedGameObject)){
				return cachedGameObject as T;
			}
			if(RunManager.instance.multiplayerPool.ResourceCache.TryGetValue(cachedPath, out var cachedPrefab)){
				return cachedPrefab as T;
			}

			T cachedObject;
			if(bundle){
				cachedObject = bundle.LoadAsset<T>(resourcePath);
			}else{
				cachedObject = Resources.Load<T>(resourcePath);
			}

			if(cachedObject == null){
				if(bundle){
					Debug.LogError("PrefabRef failed to load \"" + resourcePath + "\" as " + typeof(T).Name + " from \"" + bundle.name + "\" asset bundle.");
				}else{
					Debug.LogError("PrefabRef failed to load \"" + resourcePath + "\" as " + typeof(T).Name + ". Make sure it's in a \"Resources\" folder.");
				}
				return null;
			}

			RunManager.instance.singleplayerPool.Add(cachedPath, cachedObject);
			return cachedObject;
		}
	}

	public bool IsValid(){
		#if UNITY_EDITOR
		if(prefab != null) return true;
		#endif

		return !string.IsNullOrEmpty(resourcePath);
	}

	public void SetPrefab(T _prefab, string _resourcePath = null){
		#if UNITY_EDITOR
		prefab = _prefab;
		#endif

		if(_prefab != null){
			prefabName = _prefab.name;

			#if UNITY_EDITOR
			resourcePath = PrefabRefPaths.FromAssetPath(UnityEditor.AssetDatabase.GetAssetPath(_prefab));
			#else
			resourcePath = _resourcePath;
			#endif
		}else{
			prefabName = null;
			resourcePath = null;
		}
	}

	public void SetPrefab(AssetBundle _bundle, string _resourcePath = null){
		bundle = _bundle;

		if(bundle != null){
			prefabName = Path.GetFileNameWithoutExtension(_resourcePath);
			resourcePath = _resourcePath;
		}else{
			prefabName = null;
			resourcePath = null;
		}
	}
}

[Serializable]
public class PrefabRef : PrefabRef<GameObject> { }

#if UNITY_EDITOR
// Modded project only: decides what a PrefabRef stores so it loads in-game.
// Base-game assets in Resources use their Resources path, so the game loads its own copy with Resources.Load.
// Everything else (mod assets) uses the full asset path, which is the asset's name inside the mod's bundle.
// REPOLib points those at the bundle that contains them.
public static class PrefabRefPaths {
	public const string GameFolder = "Assets/REPO/Game/";
	public const string GameResourcesFolder = "Assets/REPO/Game/Resources/";

	public static string FromAssetPath(string assetPath) {
		if(string.IsNullOrEmpty(assetPath)) return assetPath;

		var cleanPath = assetPath.Replace("//", "/");
		if(!IsGameResource(cleanPath)) return cleanPath;

		var relativePath = cleanPath.Substring(GameResourcesFolder.Length);
		int extension = relativePath.LastIndexOf(".", StringComparison.Ordinal);
		return extension < 0 ? relativePath : relativePath.Substring(0, extension);
	}

	public static bool IsGameResource(string assetPath) {
		return assetPath.Replace("//", "/").StartsWith(GameResourcesFolder, StringComparison.OrdinalIgnoreCase);
	}

	// Base-game assets outside Resources can't be loaded by path in-game
	public static bool CanReference(string assetPath) {
		if(string.IsNullOrEmpty(assetPath)) return false;
		var cleanPath = assetPath.Replace("//", "/");
		return IsGameResource(cleanPath) || !cleanPath.StartsWith(GameFolder, StringComparison.OrdinalIgnoreCase);
	}
}
#endif
