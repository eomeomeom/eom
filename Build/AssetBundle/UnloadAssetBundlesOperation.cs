using System;
using System.Collections;

namespace PatchSystem
{
    public class UnloadAssetBundlesOperation : AssetBundleOperation<UnloadAssetBundlesOperation>
    {
        private Action finishedCallback { get; set; }

        public UnloadAssetBundlesOperation(bool includePreloadAssetBundle, Action finishedCallback)
        {
            this.finishedCallback = finishedCallback;

            UnloadAssetBundles(includePreloadAssetBundle);

            isDone = true;

            finishedCallback?.Invoke();
        }
        public IEnumerator Run()
        {
            yield break;
        }
    }
}