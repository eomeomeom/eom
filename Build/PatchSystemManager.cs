using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.IO;
using SLua;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PatchSystem
{
    public class PatchSystemManager : MonoBehaviour
    {
        public enum LogMode { All, JustErrors };

        static private LogMode _LogMode = LogMode.All;
        static private PatchSystemManager _instance;
        private Queue<IEnumerator> _routineQueue = new Queue<IEnumerator>();
        static private Dictionary<string, AssetBundle> _assetBundleDic = new Dictionary<string, AssetBundle>();
        static private Dictionary<string, Dictionary<string, UnityEngine.Object[]>> _cachedAssets = new Dictionary<string, Dictionary<string, UnityEngine.Object[]>>();
        static private Dictionary<string, Dictionary<string, UnityEngine.Object[]>> _preloadCachedAssets = new Dictionary<string, Dictionary<string, UnityEngine.Object[]>>();
#if UNITY_EDITOR
        static int _SimulateAssetBundleInEditor = -1;
        const string kSimulateAssetBundles = "SimulateAssetBundles";
#endif

        static public LogMode logMode
        {
            get { return _LogMode; }
            set { _LogMode = value; }
        }
#if UNITY_EDITOR
        static public bool simulateAssetBundleInEditor
        {
            get
            {
                if (_SimulateAssetBundleInEditor == -1)
                    _SimulateAssetBundleInEditor = EditorPrefs.GetBool(kSimulateAssetBundles, true) ? 1 : 0;

                return _SimulateAssetBundleInEditor != 0;
            }
            set
            {
                int newValue = value ? 1 : 0;
                if (newValue != _SimulateAssetBundleInEditor)
                {
                    _SimulateAssetBundleInEditor = newValue;
                    EditorPrefs.SetBool(kSimulateAssetBundles, value);
                }
            }
        }
#endif
		static public int currentManifestVersion { get; set; }
        /// <summary>
        /// 에셋번들 Dictionary.
        /// 가시성에 문제가 있지만 lua에서 접근만 막는다.
        /// </summary>
        [DoNotToLua]
        static public Dictionary<string, AssetBundle> assetBundleDic { get { return _assetBundleDic; } }
        [DoNotToLua]
        static public Dictionary<string, Dictionary<string, UnityEngine.Object[]>> cachedAssets { get { return _cachedAssets; } }
        [DoNotToLua]
        static public Dictionary<string, Dictionary<string, UnityEngine.Object[]>> preloadCachedAssets { get { return _preloadCachedAssets; } }        

        /// <summary>
        /// 백그라운드에서 비동기적으로 패치가 필요한지 체크합니다.
        /// </summary>
        /// <returns></returns>
        static public CheckPatchOperation CheckPatchAsync(Action finishedCallback = null)
        {
            var operation = new CheckPatchOperation(finishedCallback);

            if (false == operation.isDone)
            {
                EnqueueCoroutine(operation.Run());
            }

            return operation;
        }
        /// <summary>
        /// 백그라운드에서 비동기적으로 패치를 진행합니다.
        /// </summary>
        /// <param name="checkPatchOperation"></param>
        /// <param name="finishedCallback"></param>
        /// <returns></returns>
        static public PatchOperation PatchAsync(List<PatchFileManifest> patchFileManifestList, Action finishedCallback = null, Action downloadFileCallback = null)
        {
            var operation = new PatchOperation(patchFileManifestList, finishedCallback, downloadFileCallback);

            if (false == operation.isDone)
            {
                EnqueueCoroutine(operation.Run());
            }

            return operation;
        }
        /// <summary>
        /// 패치파일로부터 Preload에 지정된 에셋번들을 비동기적으로 로드합니다.
        /// </summary>
        /// <param name="finishedCallback"></param>
        /// <returns></returns>
        static public PreloadAssetBundleOperation PreloadAssetBundleAsync(Action finishedCallback = null)
        {
            var operation = new PreloadAssetBundleOperation(finishedCallback);

            if (false == operation.isDone)
            {
                EnqueueCoroutine(operation.Run());
            }

            return operation;
        }
        /// <summary>
        /// 패치파일로부터 name 을 가진 에셋을 비동기적으로 로드합니다.
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <param name="assetName"></param>
        /// <param name="finishedCallback"></param>
        /// <returns></returns>
        static public LoadAssetOperation LoadAssetAsync(string assetBundleName, string assetName, Action<UnityEngine.Object> finishedCallback = null)
        {
            var operation = new LoadAssetOperation(assetBundleName, assetName, finishedCallback);

            if (false == operation.isDone)
            {
                EnqueueCoroutine(operation.Run());
            }

            return operation;
        }
        /// <summary>
        /// 패치파일로부터 name 을 가진 에셋을 로드합니다.(Simulation Mode)
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <param name="assetName"></param>
        /// <returns></returns>
        static public UnityEngine.Object LoadAssetForSimulationMode(string assetBundleName, string assetName, bool withoutError = false)
        {
#if UNITY_EDITOR
            if (PatchSystemManager.simulateAssetBundleInEditor)
            {
                string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundleAndAssetName(assetBundleName, assetName);
                if (assetPaths.Length == 0)
                {
                    if (false == withoutError)
                    {
                        Debug.LogErrorFormat("Not found asset. assetBundleName:[{0}], assetName[{1}]", assetBundleName, assetName);
                    }

                    return null;
                }

                // @TODO: Now we only get the main object from the first asset. Should consider type also.
                return AssetDatabase.LoadMainAssetAtPath(assetPaths[0]);
            }
#endif
            Debug.Log("Must run to Simulation mode in editor.");

            return null;
        }
        /// <summary>
        /// 패치파일로부터 name 의 서브 에셋을 가진 에셋을 비동기적으로 로드합니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetBundleName"></param>
        /// <param name="assetName"></param>
        /// <returns></returns>
        static public LoadAssetWithSubAssetsOperation LoadAssetWithSubAssetsAsync(string assetBundleName, string assetName, Action<UnityEngine.Object[]> finishedCallback = null)
        {
            var operation = new LoadAssetWithSubAssetsOperation(assetBundleName, assetName, finishedCallback);

            if (false == operation.isDone)
            {
                EnqueueCoroutine(operation.Run());
            }

            return operation;
        }
        /// <summary>
        /// 패치파일로부터 백그라운드에서 비동기적으로 레벨을 로드합니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetBundleName"></param>
        /// <param name="levelName"></param>
        /// <param name="loadSceneMode"></param>
        /// <returns></returns>
        static public LoadLevelOperation LoadLevelAsync(string assetBundleName, string levelName, LoadSceneMode loadSceneMode, bool autoActivation, Action finishedCallback = null)
        {
            var operation = new LoadLevelOperation(assetBundleName, levelName, loadSceneMode, autoActivation, finishedCallback);

            if (false == operation.isDone)
            {
                EnqueueCoroutine(operation.Run());
            }

            return operation;
        }
        /// <summary>
        /// 패치파일로부터 포함된 모든 에셋을 비동기적으로 로드합니다.
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <param name="finishedCallback"></param>
        /// <returns></returns>
        static public LoadAllAssetsOperation LoadAllAssetsAsync(string assetBundleName, Action<UnityEngine.Object[]> finishedCallback = null)
        {
            var operation = new LoadAllAssetsOperation(assetBundleName, finishedCallback);

            if (false == operation.isDone)
            {
                EnqueueCoroutine(operation.Run());
            }

            return operation;
        }
        /// <summary>
        /// 패치파일로부터 에셋번들을 비동기적으로 로드합니다.
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <param name="finishedCallback"></param>
        /// <returns></returns>
        static public LoadAssetBundleOperation LoadAssetBundleAsync(string assetBundleName, Action finishedCallback = null)
        {
            var operation = new LoadAssetBundleOperation(assetBundleName, finishedCallback);

            if (false == operation.isDone)
            {
                EnqueueCoroutine(operation.Run());
            }

            return operation;
        }
        /// <summary>
        /// 내부적으로 읽혀진 에셋번들을 해제합니다. 에셋번들 내의 모든 에셋을 언로드합니다.
        /// </summary>
        /// <param name="finishedCallback"></param>
        /// <returns></returns>
        static public UnloadAssetBundlesOperation UnloadAssetBundles(bool includePreloadAssetBundle, Action finishedCallback = null)
        {
            var operation = new UnloadAssetBundlesOperation(includePreloadAssetBundle, finishedCallback);

            EnqueueCoroutine(operation.Run());

            return operation;
        }
        static private void EnqueueCoroutine(IEnumerator routine)
        {
            if (null == _instance)
            {
                Init();
            }

            _instance._routineQueue.Enqueue(routine);
        }
        static public int GetRoutineQueueCount()
        {
            if (null == _instance)
            {
                Init();
            }

            return _instance._routineQueue.Count;
        }
        static private void Init()
        {
            var go = new GameObject("PatchSystemManager", typeof(PatchSystemManager));
            DontDestroyOnLoad(go);

            _instance = go.GetComponent<PatchSystemManager>();

#if UNITY_EDITOR
            if (LogMode.All == logMode &&
                PatchSystemManager.simulateAssetBundleInEditor)
            {
                Debug.Log("[PatchSystem] 시뮬레이션 모드에서 실행되었습니다.");
            }
#endif
        }
        private IEnumerator Start()
        {
            while (true)
            {
                if(_routineQueue.Count > 0)
                {
                    yield return _routineQueue.Dequeue();
                }

                yield return null;
            }
        }
    }
}