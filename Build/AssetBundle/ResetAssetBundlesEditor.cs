using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AssetBundlesEditor : EditorWindow
{
    [MenuItem("PatchSystem/AssetBundleName/Reset xx")]
    static public void ResetCardMaterials()
    {
        try
        {
            string[] guids = AssetDatabase.FindAssets("x1");

            for (int i = 0; i < guids.Length; ++i)
            {
                var firstAssetPath = AssetDatabase.GUIDToAssetPath(guids[i]);

                EditorUtility.DisplayProgressBar("x2", firstAssetPath, (float)i / guids.Length);

                AssetImporter importer = AssetImporter.GetAtPath(firstAssetPath) as AssetImporter;

                if (null == importer)
                {
                    continue;
                }

                var directoryName = Path.GetDirectoryName(firstAssetPath);
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(firstAssetPath);

                if (directoryName.Contains("x3"))
                {
                    var assetBundleName = "x4" + fileNameWithoutExtension;
                    AssetImporter.GetAtPath(firstAssetPath).SetAssetBundleNameAndVariant(assetBundleName, "");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }

        EditorUtility.ClearProgressBar();
    }

    [MenuItem("PatchSystem/AssetBundleName/Remove Unused Names")]
    static public void RemoveUnusedNames()
    {
        try
        {
            AssetDatabase.RemoveUnusedAssetBundleNames();
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }
    }
}
