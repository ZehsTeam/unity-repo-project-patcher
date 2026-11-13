using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Search;

[CustomPropertyDrawer(typeof(PrefabRef<>))]
[CustomPropertyDrawer(typeof(PrefabRef))]
public class PrefabRefDrawer : PropertyDrawer {
	const float IconWidth = 18f;
	const float PickerButtonWidth = 19f;
	const float NameColumnWidth = 450f;
	const float PathColumnWidth = 600f;
	const float PickerWindowWidth = 1100f;
	const float PickerWindowHeight = 600f;

	static bool CanReference(Object obj) => PrefabRefPaths.CanReference(AssetDatabase.GetAssetPath(obj));

	System.Type GetAssetType() {
		var type = fieldInfo.FieldType;
		if(type.IsArray) type = type.GetElementType();
		else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) type = type.GetGenericArguments()[0];

		while(type != null && !(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PrefabRef<>))) type = type.BaseType;
		return type != null ? type.GetGenericArguments()[0] : typeof(GameObject);
	}

	static void Assign(SerializedProperty property, Object value) {
		if(value != null && !CanReference(value)){
			Debug.LogWarning("PrefabRef can only reference base-game assets inside \"" + PrefabRefPaths.GameResourcesFolder + "\". \"" + AssetDatabase.GetAssetPath(value) + "\" was rejected.");
			return;
		}

		property.FindPropertyRelative("prefab").objectReferenceValue = value;

		var prefabNameProp = property.FindPropertyRelative("prefabName");
		var resourcePathProp = property.FindPropertyRelative("resourcePath");

		var cachedFinalizedProp = property.FindPropertyRelative("cachedFinalized");

		if(value == null){
			if(prefabNameProp != null) prefabNameProp.stringValue = string.Empty;
			if(resourcePathProp != null) resourcePathProp.stringValue = string.Empty;
			if(cachedFinalizedProp != null) cachedFinalizedProp.boolValue = true;
			return;
		}

		var path = AssetDatabase.GetAssetPath(value);
		var newName = System.IO.Path.GetFileNameWithoutExtension(path);

		if(prefabNameProp != null) prefabNameProp.stringValue = newName;
		if(resourcePathProp != null) resourcePathProp.stringValue = PrefabRefAssetProcessor.ResolveResourcePath(path, newName);
		if(cachedFinalizedProp != null && PrefabRefAssetProcessor.TryGetFinalized(value, out bool finalized)) cachedFinalizedProp.boolValue = finalized;
	}

	static Object ResolveFromResourcePath(SerializedProperty property, System.Type assetType) {
		var resourcePath = property.FindPropertyRelative("resourcePath")?.stringValue;
		if(string.IsNullOrEmpty(resourcePath)) return null;

		return resourcePath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase)
			? AssetDatabase.LoadAssetAtPath(resourcePath, assetType)
			: Resources.Load(resourcePath, assetType);
	}

	static Rect GetPickerWindowRectNearField(Rect fieldRect) {
		var _fieldBottomLeftOnScreen = GUIUtility.GUIToScreenPoint(new Vector2(fieldRect.xMin, fieldRect.yMax));
		var _fieldTopLeftOnScreen = GUIUtility.GUIToScreenPoint(new Vector2(fieldRect.xMin, fieldRect.yMin));
		var _mainEditorWindowRect = EditorGUIUtility.GetMainWindowPosition();

		var _windowX = Mathf.Clamp(_fieldBottomLeftOnScreen.x, _mainEditorWindowRect.xMin, _mainEditorWindowRect.xMax - PickerWindowWidth);
		var _windowY = _fieldBottomLeftOnScreen.y;

		bool _wouldOverflowBottom = _windowY + PickerWindowHeight > _mainEditorWindowRect.yMax;
		if(_wouldOverflowBottom) _windowY = Mathf.Max(_mainEditorWindowRect.yMin, _fieldTopLeftOnScreen.y - PickerWindowHeight);

		return new Rect(_windowX, _windowY, PickerWindowWidth, PickerWindowHeight);
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		EditorGUI.BeginProperty(position, label, property);
		var prefabProp = property.FindPropertyRelative("prefab");
		position.height = EditorGUIUtility.singleLineHeight;

		var assetType = GetAssetType();
		var previousValue = prefabProp.objectReferenceValue;
		if(previousValue == null) previousValue = ResolveFromResourcePath(property, assetType);

		bool unloadable = previousValue != null && !CanReference(previousValue);
		var fieldRect = position;
		if(unloadable){
			fieldRect.width -= IconWidth;
			var iconRect = new Rect(fieldRect.xMax + 2f, position.y, IconWidth - 2f, position.height);
			var icon = new GUIContent(EditorGUIUtility.IconContent("console.erroricon.sml").image, "\"" + previousValue.name + "\" is a base-game asset outside a Resources folder and won't load in-game.");
			GUI.Label(iconRect, icon);
		}

		var pickerRect = new Rect(fieldRect.xMax - PickerButtonWidth, fieldRect.y, PickerButtonWidth, fieldRect.height);
		if(Event.current.type == EventType.MouseDown && Event.current.button == 0 && pickerRect.Contains(Event.current.mousePosition)){
			var serializedObject = property.serializedObject;
			var propertyPath = property.propertyPath;
			var context = SearchService.CreateContext("asset", "t:" + assetType.Name);
			var _pickerState = SearchViewState.CreatePickerState("Select " + assetType.Name, context, (item, canceled) => {
				if(canceled || serializedObject == null || serializedObject.targetObject == null) return;
				serializedObject.Update();
				Assign(serializedObject.FindProperty(propertyPath), item?.ToObject(assetType));
				serializedObject.ApplyModifiedProperties();
			}, null, item => {
				var path = SearchUtils.GetAssetPath(item);
				if(!PrefabRefPaths.CanReference(path)) return false;
				var mainType = AssetDatabase.GetMainAssetTypeAtPath(path);
				return (mainType != null && assetType.IsAssignableFrom(mainType)) || item.ToObject(assetType) != null;
			}, SearchViewFlags.TableView);

			var _nameColumn = new SearchColumn("Name", "label", new GUIContent("Name"), SearchColumnFlags.Default);
			_nameColumn.width = NameColumnWidth;

			var _pathColumn = new SearchColumn("Path", "description", new GUIContent("Path"), SearchColumnFlags.Default);
			_pathColumn.width = PathColumnWidth;

			_pickerState.tableConfig = new SearchTable("PrefabRefPicker", new[]{ _nameColumn, _pathColumn });
			_pickerState.itemSize = (float)DisplayMode.Table;
			_pickerState.position = new Rect(0f, 0f, PickerWindowWidth, PickerWindowHeight);
			var _pickerWindow = SearchService.ShowPicker(_pickerState) as EditorWindow;
			if(_pickerWindow) _pickerWindow.position = GetPickerWindowRectNearField(fieldRect);
			Event.current.Use();
		}

		var previousBackground = GUI.backgroundColor;
		if(unloadable) GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);

		EditorGUI.BeginChangeCheck();
		var newValue = EditorGUI.ObjectField(fieldRect, label, previousValue, assetType, false);
		GUI.backgroundColor = previousBackground;
		if(EditorGUI.EndChangeCheck()) Assign(property, newValue);

		EditorGUI.EndProperty();
	}
}
