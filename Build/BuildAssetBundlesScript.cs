using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Text;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace PatchSystem
{
    static public class BuildAssetBundlesScript
    {
        public enum Platform
        {
            aos,
            ios,
        }

        // PatchSystemAssetBundleManager.txt 와 PatchSystemAssetBundleManager_Local.txt 저장 위치
        public static readonly string assetBundleManagerResourcesDirectory = "Assets/PatchSystem/Resources";

        static public void MakePatchSystemManifest()
        {
            string outputPath = Path.Combine(Utility.AssetBundlesOutputPath, Utility.GetPlatformName());

            if (false == Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            // 이름 자체가 삭제되거나 포함된 에셋이 하나도 없는 에셋번들 제거
            List<string> assetBundleNames = new List<string>();
            {
                foreach (var v in AssetDatabase.GetAllAssetBundleNames())
                {
                    if (AssetDatabase.GetAssetPathsFromAssetBundle(v).Length > 0)
                    {
                        assetBundleNames.Add(v);
                    }
                }

                foreach (var v in Directory.GetFiles(outputPath))
                {
                    string fileName = Path.GetFileName(v);
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(v);

                    if (fileName == Utility.PatchSystemManifestFileName)
                    {
                        // 패치파일 목록은 건너뛴다.
                        continue;
                    }

                    if (Utility.GetPlatformName() == fileNameWithoutExtension)
                    {
                        // 플래폼이름의 파일은 건너뛴다.
                        continue;
                    }

                    if (-1 == assetBundleNames.FindIndex((i) => i == fileNameWithoutExtension))
                    {
                        File.Delete(v);
                    }
                }
            }

            // 유니티 에셋번들 빌드
            BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);

            // 의존성 정보를 로드
            AssetBundle assetBundle = AssetBundle.LoadFromFile(Path.Combine(outputPath, Utility.GetPlatformName()));
            AssetBundleManifest assetBundleManifest = (AssetBundleManifest)assetBundle.LoadAsset("AssetBundleManifest");


            // 패치시스템매니페스트 작성
            string patchFileManifestFullPath = Path.Combine(outputPath, Utility.PatchSystemManifestFileName);

            PatchSystemManifest patchFileManifest = new PatchSystemManifest()
            {
                list = new List<PatchFileManifest>()
            };

            foreach (var v in Directory.GetFiles(outputPath))
            {
                string fileName = Path.GetFileName(v);

                if (fileName == Utility.PatchSystemManifestFileName)
                {
                    // 패치파일 목록은 건너뛴다.
                    continue;
                }

                if (Utility.GetPlatformName() == Path.GetFileNameWithoutExtension(v))
                {
                    // 플래폼이름의 파일은 건너뛴다.
                    continue;
                }

                if (Path.GetExtension(v) == ".manifest")
                {
                    // 메니페스트 정보도 건너뛴다.
                    continue;
                }

                FileInfo f = new FileInfo(v);

                patchFileManifest.list.Add(new PatchFileManifest()
                {
                    fileName = Path.GetFileName(v),
                    size = f.Length,
                    md5 = Utility.Md5Sum(v),
                    lastWriteTimeUtc = f.LastWriteTimeUtc.ToString("MM/dd/yyyy HH:mm:ss.fff"),
                    dependencies = assetBundleManifest.GetAllDependencies(fileName)
                });
            }

            // 오름차순으로 정렬을 해준다.
            patchFileManifest.list.Sort((x, y) => x.fileName.CompareTo(y.fileName));

            if (File.Exists(patchFileManifestFullPath))
            {
                File.Delete(patchFileManifestFullPath);
            }

            var textAsset = (TextAsset)Resources.Load("app_build_number");
            if (null != textAsset)
            {
                UnityEngine.Debug.LogFormat("app_build_number.txt 읽음 text:{0}", textAsset.text);

                int.TryParse(textAsset.text, out patchFileManifest.version);

                UnityEngine.Debug.LogFormat("patchFileManifest.version:{0}", patchFileManifest.version);
            }

            File.WriteAllText(patchFileManifestFullPath, JsonUtility.ToJson(patchFileManifest, true));
        }
        static public void MakeCrcAssetBundles()
        {
            string assetBundlesPath = Path.Combine(Utility.AssetBundlesOutputPath, Utility.GetPlatformName());
            string crcAssetBundlesPath = Utility.CrcAssetBundlesOutputPath;

            if (Directory.Exists(crcAssetBundlesPath))
                Directory.Delete(crcAssetBundlesPath, true);

            if (false == Directory.Exists(crcAssetBundlesPath))
                Directory.CreateDirectory(crcAssetBundlesPath);

            // 파일명 crc로 복사
            List<string> assetBundleNames = new List<string>();
            {
                foreach (var v in Directory.GetFiles(assetBundlesPath))
                {
                    string fileName = Path.GetFileName(v);
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(v);

                    if (Utility.GetPlatformName() == fileNameWithoutExtension)
                    {
                        // 플래폼이름의 파일은 건너뛴다.
                        continue;
                    }

                    if (Path.GetExtension(v) == ".manifest")
                    {
                        // 메니페스트 정보도 건너뛴다.
                        continue;
                    }

                    string crcFileName = Utility.AssetbundleNameToCRC(fileNameWithoutExtension);

                    System.IO.File.Copy(Path.Combine(assetBundlesPath, fileNameWithoutExtension), Path.Combine(crcAssetBundlesPath, crcFileName));

                    UnityEngine.Debug.LogFormat("CrcAssetbundle 작성 완료. {0} -> {1}", fileName, crcFileName);
                }
            }
        }

        static public void BuildClientForJenkins()
        {
            Platform platform = (Platform)Enum.Parse(typeof(Platform), Environment.GetEnvironmentVariable("PLATFORM"));
            string jenkinsJobBaseName = Environment.GetEnvironmentVariable("JOB_BASE_NAME");
            int appBuildNumber = int.Parse(Environment.GetEnvironmentVariable("APP_BUILD_NUMBER"));

            BuildAssetBundles(platform, appBuildNumber, jenkinsJobBaseName);
        }

        static public void BuildAssetBundles(Platform platform, int appBuildNumber, string jenkinsJobBaseName = "")
        {
            UnityEngine.Debug.LogFormat("BuildAssetBundles platform:{0}, appBuildNumber:{1}, jenkinsJobBaseName:{2}", platform, appBuildNumber, jenkinsJobBaseName);

            string jenkinsResourcePath = Path.Combine(Application.dataPath, "Jenkins", "Resources");
            if (false == Directory.Exists(jenkinsResourcePath))
            {
                Directory.CreateDirectory(jenkinsResourcePath);
            }

            string jenkinsJobBaseNameFilePath = Path.Combine(Application.dataPath, "Jenkins", "Resources", "jenkins_job_base_name.txt");
            File.WriteAllText(jenkinsJobBaseNameFilePath, Environment.GetEnvironmentVariable("JOB_BASE_NAME"));

            string appBuildNumberFilePath = Path.Combine(Application.dataPath, "Jenkins", "Resources", "app_build_number.txt");
            File.WriteAllText(appBuildNumberFilePath, appBuildNumber.ToString());

            AssetDatabase.Refresh();

            if (Platform.aos == platform)
            {
                EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                }
            }
            else if (Platform.ios == platform)
            {
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
                }
            }

            MakePatchSystemManifest();
            MakeCrcAssetBundles();
        }

        public static void WriteLocalServerURL()
        {
            string downloadURL;
            System.Net.IPHostEntry host;
            string localIP = "";
            host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (System.Net.IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            downloadURL = "http://" + localIP + ":7888/Crc/";

            string assetBundleUrlPath = Path.Combine(assetBundleManagerResourcesDirectory, "PatchSystemAssetBundleServerURL_Local.bytes");
            Directory.CreateDirectory(assetBundleManagerResourcesDirectory);
            File.WriteAllText(assetBundleUrlPath, downloadURL);
            AssetDatabase.Refresh();
        }

        public static void DeleteLocalServerURL()
        {
            string assetBundleUrlPath = Path.Combine(assetBundleManagerResourcesDirectory, "PatchSystemAssetBundleServerURL_Local.bytes");
            Directory.CreateDirectory(assetBundleManagerResourcesDirectory);
            if (File.Exists(assetBundleUrlPath))
            {
                File.Delete(assetBundleUrlPath);
            }
            AssetDatabase.Refresh();
        }
    }
}