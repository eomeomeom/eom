using UnityEngine;
using UnityEditor;

namespace PatchSystem
{
    public class BuildAssetBundlesEditor : EditorWindow
    {
        [MenuItem("PatchSystem/Build AssetBundles")]
        public static void LocalizerLanguageOverride()
        {
            var window = GetWindow<BuildAssetBundlesEditor>("Build AssetBundles 설정");
            window.minSize = new Vector2(300.0f, 150.0f);
            window.Show();
        }

        private BuildAssetBundlesScript.Platform _platform;
        private int _appBuildNumber = 1;

        void OnGUI()
        {
            _platform = (BuildAssetBundlesScript.Platform)EditorGUILayout.EnumPopup("Platform", _platform);

            EditorGUILayout.Separator();

            EditorGUILayout.HelpBox("에셋번들 버전으로 사용됨", MessageType.Info);
            _appBuildNumber = EditorGUILayout.IntField("AppBuildNumber", _appBuildNumber);

            EditorGUILayout.Separator();

            EditorGUILayout.HelpBox("ios는 osx에서만 지원합니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(BuildAssetBundlesScript.Platform.ios == _platform && Application.platform != RuntimePlatform.OSXEditor))
            {
                if (GUILayout.Button("Build AssetBundles"))
                {
                    BuildAssetBundlesScript.BuildAssetBundles(_platform, _appBuildNumber);
                }
            }
        }
    }
}