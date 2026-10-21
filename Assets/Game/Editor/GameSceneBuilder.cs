using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Creates Assets/Game/Scenes/Arena.unity: an orthographic camera and the MatchManager.</summary>
public static class GameSceneBuilder
{
    public const string ScenePath = "Assets/Game/Scenes/Arena.unity";

    [MenuItem("Tools/Game/Build Arena Scene")]
    public static void Build()
    {
        if (EditorSceneManager.GetActiveScene().isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var db = ContentTools.GetDatabase();
        if (db.fighters.Count == 0) PlaceholderArt.GenerateAll();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = ArenaView.Height * 0.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
        camGo.transform.position = new Vector3(0f, 1.9f, -10f);

        var match = new GameObject("MatchManager").AddComponent<MatchManager>();
        match.database = db;

        ContentTools.EnsureFolder("Assets/Game/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);

        // Make it the first scene in the build.
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("Arena scene built: " + ScenePath);
    }
}
