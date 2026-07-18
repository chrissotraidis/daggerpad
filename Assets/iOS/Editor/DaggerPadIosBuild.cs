#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace DaggerPad.Editor
{
    public static class DaggerPadIosBuild
    {
        private const string BundleIdentifier = "com.chrissotraidis.daggerpad";
        private const string MinimumIosVersion = "15.0";

        [MenuItem("DaggerPad/Build/iPadOS Device")]
        public static void BuildDevice()
        {
            Build(false);
        }

        [MenuItem("DaggerPad/Build/iPad Simulator")]
        public static void BuildSimulator()
        {
            Build(true);
        }

        private static void Build(bool simulator)
        {
            string defaultPath = simulator ? "Builds/iOS/Simulator" : "Builds/iOS/Device";
            string outputPath = GetCommandLineValue("-daggerpadBuildPath") ?? defaultPath;
            outputPath = Path.GetFullPath(outputPath);

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, BundleIdentifier);
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.iOS, ApiCompatibilityLevel.NET_Unity_4_8);
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.iOS, ManagedStrippingLevel.Low);
            PlayerSettings.iOS.targetOSVersionString = MinimumIosVersion;
            PlayerSettings.iOS.sdkVersion = simulator ? iOSSdkVersion.SimulatorSDK : iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.CleanBuildCache,
            };

            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult addressablesResult);
            if (!string.IsNullOrEmpty(addressablesResult.Error))
                throw new BuildFailedException($"DaggerPad Addressables build failed: {addressablesResult.Error}");

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"DaggerPad iOS export failed: {report.summary.result} ({report.summary.totalErrors} errors)");

            Debug.Log($"DaggerPad {(simulator ? "simulator" : "device")} Xcode project exported to {outputPath}");
        }

        private static string GetCommandLineValue(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (arguments[index] == name)
                    return arguments[index + 1];
            }

            return null;
        }

        [PostProcessBuild(200)]
        public static void ConfigureXcodeProject(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS)
                return;

            string plistPath = Path.Combine(buildPath, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetBoolean("UIFileSharingEnabled", true);
            plist.root.SetBoolean("LSSupportsOpeningDocumentsInPlace", true);
            plist.root.SetBoolean("UIApplicationSupportsIndirectInputEvents", true);
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            plist.WriteToFile(plistPath);

            string projectPath = PBXProject.GetPBXProjectPath(buildPath);
            PBXProject project = new PBXProject();
            project.ReadFromFile(projectPath);

            string appTarget = project.GetUnityMainTargetGuid();
            string frameworkTarget = project.GetUnityFrameworkTargetGuid();
            ConfigureTarget(project, appTarget);
            ConfigureTarget(project, frameworkTarget);
            project.AddFrameworkToProject(frameworkTarget, "GameController.framework", true);
            project.WriteToFile(projectPath);

            PatchFrameworkMinimumOs(buildPath);
        }

        private static void ConfigureTarget(PBXProject project, string targetGuid)
        {
            project.SetBuildProperty(targetGuid, "IPHONEOS_DEPLOYMENT_TARGET", MinimumIosVersion);
            project.SetBuildProperty(targetGuid, "TARGETED_DEVICE_FAMILY", "2");
        }

        private static void PatchFrameworkMinimumOs(string buildPath)
        {
            string[] frameworkPlists = Directory.GetFiles(buildPath, "Info.plist", SearchOption.AllDirectories)
                .Where(path => path.IndexOf("UnityFramework", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();

            foreach (string frameworkPlistPath in frameworkPlists)
            {
                PlistDocument frameworkPlist = new PlistDocument();
                frameworkPlist.ReadFromFile(frameworkPlistPath);
                frameworkPlist.root.SetString("MinimumOSVersion", MinimumIosVersion);
                frameworkPlist.WriteToFile(frameworkPlistPath);
            }
        }
    }
}
#endif
