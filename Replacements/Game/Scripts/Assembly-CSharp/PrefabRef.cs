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
					Debug.LogError("PrefabRef failed to load \"" + resourcePath + "\" as " + typeof(T).Name + "from \"" + bundle.name + "\" asset bundle.");
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
			var path = UnityEditor.AssetDatabase.GetAssetPath(_prefab);
			var cleanPath = path.Replace("//", "/");
			resourcePath = cleanPath;
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