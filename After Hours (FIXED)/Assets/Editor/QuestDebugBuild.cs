using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class QuestDebugBuild
{
    private const string ScenePath = "Assets/Scenes/CLICK THIS.unity";
    private const string OutputDirectory = @"C:\Builds\AfterHoursQuest";
    private const string OutputPath = OutputDirectory + @"\AfterHours-quest-debug.apk";
    private const string RequestPath = OutputDirectory + @"\build-request.txt";
    private const string ResultPath = OutputDirectory + @"\build-result.txt";

    [InitializeOnLoadMethod]
    private static void WatchForRequest()
    {
        if (!File.Exists(RequestPath)) return;
        EditorApplication.delayCall += RunRequestedBuild;
    }

    [MenuItem("Tools/Quest Debug Build")]
    public static void RunRequestedBuild()
    {
        if (File.Exists(RequestPath)) File.Delete(RequestPath);
        try
        {
            Build();
        }
        catch (Exception e)
        {
            WriteResult("Exception", 0, e.ToString());
            Debug.LogException(e);
        }
    }

    private static void Build()
    {
        Directory.CreateDirectory(OutputDirectory);
        File.WriteAllText(ResultPath, "RESULT=Building\n");

        EditorSceneManager.SaveOpenScenes();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        TMP_Text debugText = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name != "Debug") continue;
                TMP_Text candidate = transform.GetComponent<TMP_Text>();
                if (candidate == null) continue;
                debugText = candidate;
                break;
            }
            if (debugText != null) break;
        }

        if (debugText == null) throw new InvalidOperationException("Debug TMP text not found in CLICK THIS.");

        NetworkDebugOverlay overlay = debugText.GetComponent<NetworkDebugOverlay>();
        if (overlay == null) overlay = debugText.gameObject.AddComponent<NetworkDebugOverlay>();
        overlay.targetText = debugText;
        overlay.maxLines = 12;

        debugText.richText = true;
        debugText.enableAutoSizing = true;
        debugText.fontSizeMin = 1f;
        debugText.fontSizeMax = 36f;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = OutputPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.Development | BuildOptions.AllowDebugging
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        WriteResult(summary.result.ToString(), summary.totalErrors, OutputPath);
        Debug.Log($"[QuestDebugBuild] {summary.result} errors={summary.totalErrors} size={summary.totalSize} apk={OutputPath}");
    }

    private static void WriteResult(string result, int errors, string detail)
    {
        File.WriteAllText(ResultPath, $"RESULT={result}\nERRORS={errors}\nDETAIL={detail}\n");
    }
}
