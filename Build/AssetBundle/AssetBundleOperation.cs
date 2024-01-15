using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace PatchSystem
{
    public abstract class AssetBundleOperation<OPERATION> : BaseOperation<OPERATION>
        where OPERATION : BaseOperation<OPERATION>
    {
        protected void UnloadAssetBundles(bool includePreloadAssetBundle)
        {
            if(includePreloadAssetBundle)
            {
                foreach (var v in PatchSystemManager.preloadCachedAssets)
                {
                    AssetBundle assetBundle;
                    if (PatchSystemManager.assetBundleDic.TryGetValue(v.Key, out assetBundle))
                    {
                        assetBundle.Unload(false);
                        PatchSystemManager.assetBundleDic.Remove(v.Key);

                        OnLog("[PatchSystem] 에셋번들이 언로드되었습니다. name:[{0}]", v.Key);
                    }
                }

                PatchSystemManager.preloadCachedAssets.Clear();
            }

            foreach (var v in PatchSystemManager.cachedAssets)
            {
                AssetBundle assetBundle;
                if (PatchSystemManager.assetBundleDic.TryGetValue(v.Key, out assetBundle))
                {
                    assetBundle.Unload(false);
                    PatchSystemManager.assetBundleDic.Remove(v.Key);

                    OnLog("[PatchSystem] 에셋번들이 언로드되었습니다. name:[{0}]", v.Key);
                }
            }

            PatchSystemManager.cachedAssets.Clear();
        }
        protected IEnumerator LoadAssetBundle(string assetBundleName, Action<AssetBundle> result = null)
        {
            if (string.IsNullOrEmpty(assetBundleName))
            {
                OnException(new PatchSystemException(PatchSystemException.ErrorCode.InavlidAssetBundleName, string.Format("assetBundleName:[{0}]", assetBundleName)));

                yield break;
            }

            var manifestFullPath = Path.Combine(Utility.PatchRootPath, Utility.AssetbundleNameToCRC(assetBundleName) + Utility.PatchFileManifestFileName);

            if (false == File.Exists(manifestFullPath))
            {
                OnException(new PatchSystemException(PatchSystemException.ErrorCode.NotFoundManifest, string.Format("path:[{0}]", manifestFullPath)));

                yield break;
            }

            string text = File.ReadAllText(manifestFullPath);
            var patchFileData = JsonUtility.FromJson<PatchFileManifest>(text);

            foreach (var v in patchFileData.dependencies ?? Enumerable.Empty<string>())
            {
                yield return _LoadAssetBundle(v, (r) => { });
            }

            yield return _LoadAssetBundle(assetBundleName, result);
        }
        private IEnumerator _LoadAssetBundle(string assetBundleName, Action<AssetBundle> result = null)
        {
            if (string.IsNullOrEmpty(assetBundleName))
            {
                OnException(new PatchSystemException(PatchSystemException.ErrorCode.InavlidAssetBundleName, string.Format("assetBundleName:[{0}]", assetBundleName)));

                yield break;
            }

            AssetBundle assetBundle;
            if (PatchSystemManager.assetBundleDic.TryGetValue(assetBundleName, out assetBundle))
            {
                result?.Invoke(assetBundle);

                yield break;
            }

            var dataFullPath = Path.Combine(Utility.PatchRootPath, Utility.AssetbundleNameToCRC(assetBundleName));

            if (false == File.Exists(dataFullPath))
            {
                OnException(new PatchSystemException(PatchSystemException.ErrorCode.NotFoundAssetBundle, string.Format("path:[{0}]", dataFullPath)));

                yield break;
            }

            /*
            AssetBundleCreateRequest request = null;

            try
            {
                request = AssetBundle.LoadFromFileAsync(dataFullPath);
            }
            catch (Exception e)
            {
                OnException(new PatchSystemException(PatchSystemException.ErrorCode.InternalError, e.ToString()));
            }

            yield return request;

            OnLog("[PatchSystem] 에셋번들이 로드되었습니다. name:[{0}]", assetBundleName);
            PatchSystemManager.assetBundleDic.Add(assetBundleName, request.assetBundle);

            result?.Invoke(request.assetBundle);
            */

            try
            {
                assetBundle = AssetBundle.LoadFromFile(dataFullPath);
            }
            catch (Exception e)
            {
                OnException(new PatchSystemException(PatchSystemException.ErrorCode.InternalError, e.ToString()));
            }

            OnLog("[PatchSystem] 에셋번들이 로드되었습니다. name:[{0}]", assetBundleName);
            PatchSystemManager.assetBundleDic.Add(assetBundleName, assetBundle);

            result?.Invoke(assetBundle);

            yield break;
        }
        protected UnityEngine.Object[] GetCachedAsset(string assetBundleName, string assetName)
        {
            Dictionary<string, UnityEngine.Object[]> dic = null;

            if (false == PatchSystemManager.preloadCachedAssets.TryGetValue(assetBundleName, out dic))
            {
                if (false == PatchSystemManager.cachedAssets.TryGetValue(assetBundleName, out dic))
                {
                    return null;
                }
            }            

            UnityEngine.Object[] asset = null;

            dic.TryGetValue(Path.GetFileNameWithoutExtension(assetName).ToLower(), out asset);

            return asset;
        }
    }
}