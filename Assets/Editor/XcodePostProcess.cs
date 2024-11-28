using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public class XcodePostProcess
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
    {
        if (buildTarget == BuildTarget.iOS)
        {
            // 获取 Xcode 项目路径
            string projPath = PBXProject.GetPBXProjectPath(buildPath);
            PBXProject proj = new PBXProject();
            proj.ReadFromFile(projPath);

            // 获取主目标的 GUID
            string target = proj.GetUnityMainTargetGuid();

            // 添加 CoreMotion 框架
            proj.AddFrameworkToProject(target, "CoreMotion.framework", false);

            // 添加 HeadphoneMotion.mm 到构建
            string pluginPath = "Libraries/HeadphoneMotion/Plugins/iOS/HeadphoneMotion.mm";
            string guid = proj.AddFile(pluginPath, pluginPath);
            proj.AddFileToBuild(target, guid);

            // 设置 .mm 文件的编译标志
            proj.AddBuildProperty(target, "OTHER_LDFLAGS", "-ObjC");

            // 更新 Info.plist
            string plistPath = buildPath + "/Info.plist";
            PlistDocument plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));
            
            // 添加隐私描述
            var rootDict = plist.root;
            rootDict.SetString("NSMotionUsageDescription", 
                "Uses Airpods motion data to control the game");

            // 保存更改
            File.WriteAllText(plistPath, plist.WriteToString());
            proj.WriteToFile(projPath);

            Debug.Log("[XcodePostProcess] Successfully configured Xcode project for AirPods motion tracking");
        }
    }
} 