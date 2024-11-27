using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;

public class SceneRenamer : EditorWindow
{
    private class SceneListElement
    {
        public bool Rename;
        public string ScenePath;
        public SceneAsset SceneAsset;
        public bool EnabledInBuildSettings;
    }

    private const string tempPrefix = "TEMP_SCENERENAMER_";
    private List<SceneListElement> scenes;
    private ReorderableList reorderableList;
    private string sceneName = "Scene";
    private string sceneFormat = "000";
    private int sceneSuffixIndex = 0;
    private GUIStyle renamedSceneStyle;

    /// <summary>
    /// Shows the window using a menu item
    /// </summary>
    [MenuItem("Tools/Scene Renamer")]
    public static void ShowWindow()
    {
        SceneRenamer window = GetWindow<SceneRenamer>("SceneRenamer");
        window.Init();
    }

    /// <summary>
    /// Initializes the window
    /// </summary>
    private void Init()
    {
        // Get scenes from build settings
        // And create a list containing the data structure we need
        EditorBuildSettingsScene[] buildSettingsScenes = EditorBuildSettings.scenes;
        scenes = buildSettingsScenes.Select(s =>
        {
            return new SceneListElement()
            {
                Rename = true,
                ScenePath = s.path,
                SceneAsset = AssetDatabase.LoadAssetAtPath(s.path, typeof(SceneAsset)) as SceneAsset,
                EnabledInBuildSettings = s.enabled
            };
        }).ToList();

        // Create reordable list for custom window
        reorderableList = new ReorderableList(scenes, scenes.GetType().GetGenericArguments()[0], true, false, false, false);
        reorderableList.drawHeaderCallback = DrawReordableListHeader;
        reorderableList.drawElementCallback = DrawReordableLisElement;

        EditorBuildSettings.sceneListChanged -= Init;
        EditorBuildSettings.sceneListChanged += Init;
    }
    private void OnDestroy()
    {
        EditorBuildSettings.sceneListChanged -= Init;
    }

    private Vector2 scrollPosition; // Variable to store scroll position

    /// <summary>
    /// Draws the GUI in the window
    /// </summary>
    private void OnGUI()
    {
        if (reorderableList == null)
            Init();

        if (renamedSceneStyle == null)
            renamedSceneStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };

        sceneSuffixIndex = 0;

        // Begin scroll view
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        reorderableList.DoLayoutList();

        // End scroll view
        EditorGUILayout.EndScrollView();

        // Adjust layout for input fields and buttons
        if (Screen.width > 300)
        {
            EditorGUIUtility.labelWidth = 75;
            EditorGUILayout.BeginHorizontal();
        }

        sceneName = EditorGUILayout.TextField("Base name", sceneName);
        sceneFormat = CleanFormatString(EditorGUILayout.TextField("Suffix", sceneFormat));

        if (Screen.width > 300)
            EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Rename"))
        {
            RenameScenes();
            ReorderScenes();
        }
    }



    private string CleanFormatString(string input)
    {
        return System.Text.RegularExpressions.Regex.Replace(input, "[^0#]", string.Empty);
    }

    /// <summary>
    /// Reorder the scenes in the build settings
    /// </summary>
    private void ReorderScenes()
    {
        EditorBuildSettings.scenes = scenes.Select(s => new EditorBuildSettingsScene(s.ScenePath, s.EnabledInBuildSettings)).ToArray();
    }

    /// <summary>
    /// Renames the scene
    /// </summary>
    /// <param name="temp">If <c>true</c>, set a temporary name for the scenes</param>
    private void RenameScenes(bool temp = true)
    {
        AssetDatabase.Refresh();
        for (int index = 0, sceneIndex = 0; index < reorderableList.list.Count; ++index)
        {
            SceneListElement scene = reorderableList.list[index] as SceneListElement;
            if (!scene.Rename) continue;
            RenameScene(scene, ++sceneIndex, temp);
        }
        AssetDatabase.SaveAssets();

        if (temp)
            RenameScenes(false);
    }

    /// <summary>
    /// Renames a single scene
    /// </summary>
    /// <param name="scene">The scene to rename</param>
    /// <param name="sceneIndex">The index of the scene</param>
    /// <param name="temp">If <c>true</c>, set a temporary name for the scene</param>
    private void RenameScene(SceneListElement scene, int sceneIndex, bool temp)
    {
        string newName = GetNewName(scene, sceneIndex, temp);
        string guid = AssetDatabase.AssetPathToGUID(scene.ScenePath);
        string result = AssetDatabase.RenameAsset(scene.ScenePath, newName);
        if (!string.IsNullOrEmpty(result))
        {
            Debug.LogError(result);
        }
        else
        {
            scene.SceneAsset.name = newName;
            scene.ScenePath = AssetDatabase.GUIDToAssetPath(guid);
        }
    }

    private string GetNewName(SceneListElement scene, int sceneIndex, bool temp)
    {
        return string.Format("{0}{1:" + sceneFormat + "}", (temp ? tempPrefix : string.Empty) + sceneName, sceneIndex);
    }

    /// <summary>
    /// Draws the header of the reordable list
    /// </summary>
    /// <param name="rect"></param>
    public void DrawReordableListHeader(Rect rect)
    {
        GUI.Label(rect, "Scenes in build");
    }

    /// <summary>
    /// Draws an element of the reoardable list
    /// </summary>
    /// <param name="rect">The rect the element must be drawn in</param>
    /// <param name="index">The index of the element in the list</param>
    /// <param name="isActive">Indicates whether the element is active (hovered) or not</param>
    /// <param name="isFocused">Indicates whether the element is focused or not</param>
    public void DrawReordableLisElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        SceneListElement scene = reorderableList.list[index] as SceneListElement;

        Rect checkRect = rect;
        checkRect.width = rect.height;
        scene.Rename = GUI.Toggle(checkRect, scene.Rename, null as string);

        Rect labelRect = rect;
        labelRect.x += checkRect.width;
        labelRect.width -= checkRect.width;

        GUI.Label(labelRect, scene.SceneAsset.name);
        if (scene.Rename)
            GUI.Label(labelRect, GetNewName(scene, ++sceneSuffixIndex, false), renamedSceneStyle);

    }
}