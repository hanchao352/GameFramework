using System;
using System.Collections;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using YooAsset;

namespace Test
{
    public class GameUpdateManager : MonoSingleton<GameUpdateManager>
    {
        [Header("更新配置")]
        [SerializeField] private string packageName = "DefaultPackage";
        [SerializeField] private EPlayMode playMode = EPlayMode.EditorSimulateMode;
        private string hostServerURL = "http://localhost:3000/files";
        
        
        [Header("更新选项")]
        [SerializeField] private int downloadingMaxNum = 10;
        [SerializeField] private int failedTryAgain = 3;
        [SerializeField] private int timeout = 60;
        
        private ResourcePackage _package;
        private ResourceDownloaderOperation _downloader;
        
        public ResourcePackage Package => _package;
        
        public event Action<UpdateEventArgs> OnUpdateEvent;
        
        public Action OnCompleteUpdate;
        private string _serverVersion;

        protected override void Start()
        {
            Initialize();
            //StartCoroutine(UpdateCoroutine());
        }

        public IEnumerator UpdateCoroutine()
        {
            InitializeYooAsset();
            
            // 初始化资源包
            yield return InitializePackage();
            
            // 请求资源版本
            yield return RequestPackageVersion();
            
            // 更新资源清单
            yield return UpdatePackageManifest();
            
            // 创建资源下载器
            yield return CreateDownloader();
            
            // 下载资源文件
            yield return DownloadFiles();
            
            // 完成更新
            CompleteUpdate();
            
            OnCompleteUpdate?.Invoke();
        }
        
        private void InitializeYooAsset()
        {
            YooAssets.Initialize();
            Debug.Log("YooAsset系统初始化完成");
        }
        
         /// <summary>
        /// 初始化资源包
        /// </summary>
        private IEnumerator InitializePackage()
        {
            Debug.Log("正在初始化资源包...");
            _package = YooAssets.TryGetPackage(packageName);
            if (_package == null)
                _package = YooAssets.CreatePackage(packageName);
            
            InitializationOperation initOperation = null;
            
            switch (playMode)
            {
                case EPlayMode.EditorSimulateMode:
                    var buildResult = EditorSimulateModeHelper.SimulateBuild(packageName);
                    var parameters = new EditorSimulateModeParameters();
                    parameters.EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(buildResult.PackageRootDirectory);
                    initOperation = _package.InitializeAsync(parameters);
                    break;
                    
                case EPlayMode.OfflinePlayMode:
                    var offlineParameters = new OfflinePlayModeParameters();
                    offlineParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                    initOperation = _package.InitializeAsync(offlineParameters);
                    break;
                    
                case EPlayMode.HostPlayMode:
                    var hostParameters = new HostPlayModeParameters();
                    hostParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                    hostParameters.CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(new RemoteServices(GetPlatformURL()));
                    initOperation = _package.InitializeAsync(hostParameters);
                    break;
                    
                case EPlayMode.WebPlayMode:
                    var webParameters = new WebPlayModeParameters();
                    webParameters.WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
                    initOperation = _package.InitializeAsync(webParameters);
                    break;
            }

            yield return initOperation;
            
            if (initOperation.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"资源包初始化失败：{initOperation.Error}");
                yield break;
            }
            
            Debug.Log($"资源包 [{packageName}] 初始化完成");
        }
         
        /// <summary>
        /// 请求资源版本
        /// </summary>
        private IEnumerator RequestPackageVersion()
        {
            var operation = _package.RequestPackageVersionAsync(true, timeout);
            yield return operation;
            
            if (operation.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"请求资源版本失败：{operation.Error}");
                yield break;
            }
            
            Debug.Log($"请求到的资源版本：{operation.PackageVersion}");
        }
        
        /// <summary>
        /// 更新资源清单
        /// </summary>
        private IEnumerator UpdatePackageManifest()
        {
            Debug.Log( "正在更新资源清单...");
            var versionOperation = _package.RequestPackageVersionAsync(true, timeout);
            yield return versionOperation;
            
            if (versionOperation.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"请求版本失败：{versionOperation.Error}");
                yield break;
            }
            
            var updateOperation = _package.UpdatePackageManifestAsync(versionOperation.PackageVersion, timeout);
            yield return updateOperation;
            
            if (updateOperation.Status != EOperationStatus.Succeed)
            {
                Debug.Log($"更新资源清单失败：{updateOperation.Error}");
                yield break;
            }
            
            Debug.Log("资源清单更新完成");
        }
        
        /// <summary>
        /// 创建资源下载器
        /// </summary>
        private IEnumerator CreateDownloader()
        {
            Debug.Log("正在创建资源下载器...");
            try
            {
                _downloader = _package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"创建下载器失败：{ex.Message}");
                yield break;
            }
            
            if (_downloader.TotalDownloadCount == 0)
            {
                Debug.Log("没有发现需要下载的资源文件");
               
                yield break;
            }
            
            long totalBytes = _downloader.TotalDownloadBytes;
            int totalCount = _downloader.TotalDownloadCount;
            float totalSizeMB = totalBytes / 1048576f;
            
            string message = $"发现 {totalCount} 个更新文件，总大小 {totalSizeMB:F1}MB";
            Debug.Log(message);
            
     
            yield return null;
        }
        
        /// <summary>
        /// 下载资源文件
        /// </summary>
        private IEnumerator DownloadFiles()
        {
            if (_downloader == null || _downloader.TotalDownloadCount == 0)
            {
                yield break;
            }
            
            Debug.Log("正在下载资源文件...");
            
            // 开始下载
            _downloader.BeginDownload();
            
            while (!_downloader.IsDone)
            {
                var eventArgs = new UpdateEventArgs
                {
                    currentCount = _downloader.CurrentDownloadCount,
                    totalCount = _downloader.TotalDownloadCount,
                    currentBytes = _downloader.CurrentDownloadBytes,
                    totalBytes = _downloader.TotalDownloadBytes,
                    progress = (float)_downloader.CurrentDownloadCount / _downloader.TotalDownloadCount
                };
                
                float currentMB = eventArgs.currentBytes / 1048576f;
                float totalMB = eventArgs.totalBytes / 1048576f;
                eventArgs.message = $"下载进度：{eventArgs.currentCount}/{eventArgs.totalCount} ({currentMB:F1}MB/{totalMB:F1}MB)";
                
                OnUpdateEvent?.Invoke(eventArgs);
                yield return null;
            }
            
            if (_downloader.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"资源下载失败：{_downloader.Error}");
                yield break;
            }
            
            Debug.Log("资源文件下载完成");
        }
        
        private void CompleteUpdate()
        {
            Debug.Log("游戏资源更新完成！");
            
            // 设置默认包
            YooAssets.SetDefaultPackage(_package);
            
            Debug.Log("游戏更新流程全部完成！");
        }

         
        /// <summary>
        /// 获取平台对应的URL
        /// </summary>
        private string GetPlatformURL()
        {
            string platform = GetPlatformName();
            return $"{hostServerURL}/{platform}/{packageName}/{_serverVersion}";
        }
        
        /// <summary>
        /// 获取平台名称
        /// </summary>
        private string GetPlatformName()
        {
#if UNITY_EDITOR
            UnityEditor.BuildTarget buildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            switch (buildTarget)
            {
                case UnityEditor.BuildTarget.Android: return "Android";
                case UnityEditor.BuildTarget.iOS: return "IPhone";
                case UnityEditor.BuildTarget.WebGL: return "WebGL";
                default: return "StandaloneWindows64";
            }
#else
            switch (Application.platform)
            {
                case RuntimePlatform.Android: return "Android";
                case RuntimePlatform.IPhonePlayer: return "IPhone";
                case RuntimePlatform.WebGLPlayer: return "WebGL";
                default: return "PC";
            }
#endif
        }
         
        /// <summary>
        /// 远程服务实现
        /// </summary>
        private class RemoteServices : IRemoteServices
        {
            private readonly string _defaultHostServer;
            private readonly string _fallbackHostServer;
            
            public RemoteServices(string hostServer)
            {
                _defaultHostServer = hostServer;
                _fallbackHostServer = hostServer; // 可以设置不同的备用服务器
            }
            
            string IRemoteServices.GetRemoteMainURL(string fileName)
            {
                return $"{_defaultHostServer}/{fileName}";
            }
            
            string IRemoteServices.GetRemoteFallbackURL(string fileName)
            {
                return $"{_fallbackHostServer}/{fileName}";
            }
        }
    }
}