using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.AddressableAssets;
using UnityEditor.Compilation;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;

public class BuildPlayer
{
    private const string BUILD_PATH = "XXDefenceBuild/Binary";
    private const string CONFIG_DATA_PATH = "Assets/Game/ScriptableObjects/GameConfigData.asset";

    public static void BuildAddressables()
    {
        if (EditorApplication.isCompiling)
        {
            Debug.Log("===> Delaying until compilation is finished...");
            CompilationPipeline.compilationFinished += OnCompilationFinish;
        }
        else
        {
            BuildProcess();
        }
    }

    public static void BuildProcess()
    {
        Debug.Log("===> Building Addressables!!! START PLATFORM: platform: " + Application.platform + " target: " + EditorUserBuildSettings.selectedStandaloneTarget);

        AddressableAssetSettings.CleanPlayerContent();
        AddressableAssetSettings.BuildPlayerContent();

        Debug.Log("===> Building Addressables!!! DONE");
    }

    public static void OnCompilationFinish(object o = null)
    {
        Debug.Log("===> On Compilation Finished...");

        CompilationPipeline.compilationFinished -= OnCompilationFinish;
        BuildProcess();
    }

    private static string[] FindEnabledEditorScenes()
    {
        List<string> editorScenes = new List<string>();

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled) continue;
            editorScenes.Add(scene.path);
        }

        return editorScenes.ToArray();
    }

    // Config 파일 생성
    private static void MakeConfigFile(string path, string fileName)
    {
        var config = AssetDatabase.LoadAssetAtPath<GameConfig>(CONFIG_DATA_PATH);
        var sb = new StringBuilder();
        sb.AppendLine($"APK 파일 명 : {fileName}");
        sb.AppendLine($"GRPC TLS 통신 여부 : {config.UseServerSecure}");
        sb.AppendLine($"게임 서버 주소 : {config.GameServerHost}");
        //sb.AppendLine($"스트리밍 서버 주소 : {config.EnvData.StreamServerHost}");
        sb.AppendLine($"스키마 경로 : {config.SchemaPath}");
        sb.AppendLine($"로컬 저장 파일 이름 : {config.SaveFileName}");
        File.WriteAllText(path, sb.ToString());
    }

    // 안드로이드 빌드
    private static void BuildProcess_AOS(string appName, string appVer)
    {
        Debug.Log(string.Format("Build AOS START - {0}_{1}", appName, appVer));

        PlayerSettings.bundleVersion = appVer;
        var now = System.DateTime.Now.ToString("yyMMdd_HHmmss");
        var fileName = string.Format("{0}_{1}_{2}.apk", appName, PlayerSettings.bundleVersion, now);

        // 빌드 파일 생성
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = FindEnabledEditorScenes();
        buildPlayerOptions.locationPathName = string.Format("{0}/Android/{1}", BUILD_PATH, fileName);
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.Development;

        // 빌드 시작
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build Succeeded: " + summary.totalSize + " Bytes");

            // Config 파일 생성
            var path = $"{BUILD_PATH}/Android/Config.txt";
            MakeConfigFile(path, fileName);
        }
        if (summary.result == BuildResult.Failed)
        {
            Debug.Log("Build Failed.");
        }
    }

    // Windows 빌드
    private static void BuildProcess_Windows(string appName, string appVer)
    {
        Debug.Log(string.Format("Build Windows START - {0}_{1}", appName, appVer));

        PlayerSettings.bundleVersion = appVer;
        var now = System.DateTime.Now.ToString("yyMMdd_HHmmss");
        var fileName = string.Format("{0}_{1}_{2}/COA.exe", appName, PlayerSettings.bundleVersion, now);

        // 빌드 파일 생성
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = FindEnabledEditorScenes();
        buildPlayerOptions.locationPathName = string.Format("{0}/Windows/{1}", BUILD_PATH, fileName);
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;

        // 빌드 시작
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build Succeeded: " + summary.totalSize + " Bytes");

            // Config 파일 생성
            var path = $"{BUILD_PATH}/Windows/Config.txt";
            MakeConfigFile(path, fileName);
        }
        if (summary.result == BuildResult.Failed)
        {
            Debug.Log("Build Failed.");
        }
    }

    // 앱 빌드(Android)
    public static void Build_AOS()
    {
        string AppName = CommandLineReader.GetCustomArgument("AppName");
        string AppVer = CommandLineReader.GetCustomArgument("AppVer");
        string BuildType = CommandLineReader.GetCustomArgument("BuildType");

        Debug.Log("BuildType : " + BuildType);

        if (BuildType.Contains("APP"))
        {
            Debug.Log("안드로이드 앱 빌드 시작.");

            // 어드레서블 Play Mode - Use Existing Build 로 변경
            AddressableAssetSettingsDefaultObject.Settings.ActivePlayModeDataBuilderIndex = 2;

            // 앱 빌드 시작
            BuildProcess_AOS(AppName, AppVer);
        }
    }

    // 어드레서블 빌드(Android)
    public static void Build_Addressable_AOS()
    {
        string BuildType = CommandLineReader.GetCustomArgument("BuildType");

        Debug.Log("BuildType : " + BuildType);

        if (BuildType.Contains("ASSET"))
        {
            Debug.Log("안드로이드 에셋번들 빌드 시작.");

            // 플랫폼 Android 로 변경
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.selectedStandaloneTarget = BuildTarget.Android;

            // 어드레서블 빌드 시작
            BuildAddressables();
        }
    }

    // 로컬에서 빌드(Android)
    [MenuItem("SOA/Build/Build AOS")]
    public static void BuildMenu_AOS()
    {
        // 어드레서블 Play Mode - Use Asset DataBase 로 변경
        int prevDataBuilderIndex = AddressableAssetSettingsDefaultObject.Settings.ActivePlayModeDataBuilderIndex; 
        AddressableAssetSettingsDefaultObject.Settings.ActivePlayModeDataBuilderIndex = 0;

        // 앱 빌드 시작
        BuildProcess_AOS("XXDefence", "0.0.1");

        AddressableAssetSettingsDefaultObject.Settings.ActivePlayModeDataBuilderIndex = prevDataBuilderIndex;
    }
    
    
    // 로컬에서 Adressable Asset 빌드(Android)
    [MenuItem("SOA/Build/Build Adressables AOS")]
    public static void BuildMenu_Adressables_AOS()
    {
        // 플랫폼 Android 로 변경
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.selectedStandaloneTarget = BuildTarget.Android;

        // 어드레서블 빌드 시작
        BuildAddressables();
    }


    // 로컬에서 빌드(Windows)
    [MenuItem("SOA/Build/Build Windows")]
    public static void BuildMenu_Windows()
    {
        // 어드레서블 Play Mode - Use Asset DataBase 로 변경
        int prevDataBuilderIndex = AddressableAssetSettingsDefaultObject.Settings.ActivePlayModeDataBuilderIndex; 
        AddressableAssetSettingsDefaultObject.Settings.ActivePlayModeDataBuilderIndex = 0;

        // 앱 빌드 시작
        BuildProcess_Windows("XXDefence", "0.0.1");

        AddressableAssetSettingsDefaultObject.Settings.ActivePlayModeDataBuilderIndex = prevDataBuilderIndex;
    }
    
    // 로컬에서 Adressable Asset 빌드(Windows)
    [MenuItem("SOA/Build/Build Adressables Windows")]
    public static void BuildMenu_Adressables_Windows()
    {
        // 플랫폼 Windows 로 변경
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        EditorUserBuildSettings.selectedStandaloneTarget = BuildTarget.StandaloneWindows64;

        // 어드레서블 빌드 시작
        BuildAddressables();
    }
    

}
