// Project:         Daggerfall Unity
// Copyright:       Copyright (C) 2009-2024 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: marcoscampi
// Contributors: Vivian V Wing (vwing@multitude.city)
// 
// Notes:
//

using System;
using System.IO;
using System.Linq;
using UnityEngine;
using ICSharpCode.SharpZipLib.Zip;
using DaggerfallWorkshop.Game;
namespace DaggerfallWorkshop
{
    public sealed class Paths
    {
        public static Paths Instance => lazy.Value;
        public static string StreamingAssetsPath => lazy.Value.streamingAssetsPath;
        public static string DataPath => lazy.Value.dataPath;
        public static string PersistentDataPath => lazy.Value.persistentDataPath;
        public static string StoragePath => lazy.Value.storagePath;
        public static string LayoutsPath => TouchscreenLayoutsManager.LayoutsPath;

        private string streamingAssetsPath;
        private string dataPath;
        private string persistentDataPath;
        private string storagePath;

        private static readonly Lazy<Paths> lazy = new Lazy<Paths>(() => new Paths());

        private Paths()
        {
            Console.WriteLine(Application.systemLanguage);
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    this.initAndroid();
                    break;
                case RuntimePlatform.IPhonePlayer:
                    this.initIOS();
                    break;
                default:
                    this.init();
                    break;
            }
        }
        private void init()
        {
            storagePath = "/";
            streamingAssetsPath = Application.streamingAssetsPath;
            dataPath = Application.dataPath;
            persistentDataPath = Application.persistentDataPath;

        }
        /**
        * Application.dataPath refers to where the APK is located
        * Application.streamingAssets refers to assets inside Application.dataPath
        * Application.persistentDataPath refers to some private storage, so must be overriden
        */
        private void initAndroid()
        {
            var apkPath = Application.dataPath;
            
            storagePath = Application.persistentDataPath;
            dataPath = DaggerfallUnityApplication.PersistentDataPath;
            persistentDataPath = dataPath;
            streamingAssetsPath = Path.Combine(dataPath, "assets");

            if (!Directory.Exists(streamingAssetsPath))
            {
                Directory.CreateDirectory(dataPath);
                // Extract apk
                {
                    FastZip fastZip = new FastZip();
                    fastZip.ExtractZip(apkPath, dataPath, @".*;-^(?!assets);-^\/assets\/bin");
                }
            }
        }

        /// <summary>
        /// iOS packages StreamingAssets inside the read-only application bundle. Mirror shipped
        /// assets into Documents so game-data, layout, and asset-only mod imports have one writable
        /// root. App updates add newly shipped files without overwriting user-modified files.
        /// </summary>
        private void initIOS()
        {
            storagePath = Application.persistentDataPath;
            dataPath = Path.Combine(Application.persistentDataPath, "DaggerfallUnity");
            persistentDataPath = dataPath;
            streamingAssetsPath = Path.Combine(dataPath, "StreamingAssets");

            Directory.CreateDirectory(dataPath);
            MirrorStreamingAssetsIfNeeded(Application.streamingAssetsPath, streamingAssetsPath);
        }

        private static void MirrorStreamingAssetsIfNeeded(string sourcePath, string destinationPath)
        {
            const string markerFilename = ".daggerpad-streaming-assets-version";
            string markerPath = Path.Combine(destinationPath, markerFilename);
            string installedVersion = File.Exists(markerPath) ? File.ReadAllText(markerPath).Trim() : string.Empty;
            if (Directory.Exists(destinationPath) && installedVersion == Application.version)
                return;

            try
            {
                CopyDirectory(sourcePath, destinationPath);
                File.WriteAllText(markerPath, Application.version);
            }
            catch (Exception ex)
            {
                Debug.LogError("DaggerPad could not prepare writable StreamingAssets: " + ex.Message);
                throw;
            }
        }

        private static void CopyDirectory(string sourcePath, string destinationPath)
        {
            Directory.CreateDirectory(destinationPath);

            foreach (string sourceFile in Directory.GetFiles(sourcePath))
            {
                string destinationFile = Path.Combine(destinationPath, Path.GetFileName(sourceFile));
                if (!File.Exists(destinationFile))
                    File.Copy(sourceFile, destinationFile);
            }

            foreach (string sourceDirectory in Directory.GetDirectories(sourcePath))
            {
                string destinationDirectory = Path.Combine(destinationPath, Path.GetFileName(sourceDirectory));
                CopyDirectory(sourceDirectory, destinationDirectory);
            }
        }
    }
}
