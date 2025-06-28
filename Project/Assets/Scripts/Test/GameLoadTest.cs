using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using UnityEngine.UI;
using YooAsset;

namespace Test
{
    public class GameLoadTest : MonoBehaviour
    {
        private void Awake()
        {
            ResManager.Instance.Initialize();
        }

        private void Start()
        {
            GameUpdateManager.Instance.OnCompleteUpdate += () =>
            {
                StartCoroutine(StartTest());
            };
        }

        private IEnumerator StartTest()
        {
            yield return ChangeScene();
            // 3. 示例：加载并实例化预制体
            yield return LoadAndInstantiatePrefab("Assets/Res/UI/window");

            
            //yield return LoadSprite();
            
            StartTest2();
        }

        private async void StartTest2()
        {
            await LoadSpriteAsync();
        }
        
        /// <summary>
        /// 切换场景
        /// </summary>
        /// <param name="sceneName">目标场景名称</param>
        public IEnumerator ChangeScene()
        {
            Debug.Log($"开始加载场景");
            string location = "Assets/Res/Scene/TestLoadScene";
            
            var sceneMode = LoadSceneMode.Single;
            var physicsMode = LocalPhysicsMode.None;
            bool suspendLoad = false;
            
            
            
            SceneHandle handle = YooAssets.LoadSceneAsync(location, sceneMode, physicsMode, suspendLoad);
            yield return handle;
            // 检查加载结果
            if (handle.Status == EOperationStatus.Succeed)
            {
                Debug.Log($"场景加载成功:");
                handle.Release();
            }
            else
            {
                Debug.LogError($"场景加载失败, 错误: {handle.LastError}");
            }
        }

        private IEnumerator LoadSprite()
        {
            string location = "Assets/Res/Atlas/test.spriteatlas";


            ResManager.Instance.GetSpriteAsync(location);
            
            AssetHandle handle = YooAssets.LoadAssetAsync<SpriteAtlas>(location);
            yield return handle;
        
            // 检查加载结果
            if (handle.Status == EOperationStatus.Succeed)
            {
                Debug.Log($"图集加载成功:");
                var spriteAtals = handle.AssetObject as SpriteAtlas;
                Sprite sprite = spriteAtals.GetSprite("主角 拆分 拷贝");
                var img = GameObject.Find("Canvas/window/Image").GetComponent<Image>();
                img.sprite = sprite;
                img.SetNativeSize();
            }
            else
            {
                Debug.LogError($"图集加载失败, 错误: {handle.LastError}");
            }
        }
        
        private async UniTask LoadSpriteAsync()
        {
            string location = "Assets/Res/Atlas/test.spriteatlas";

            var sprite = await ResManager.Instance.GetSpriteFromAtlasAsync(location,"主角 拆分 拷贝");
            
            if(sprite == null)
                return;
            
            var img = GameObject.Find("Canvas/window/Image").GetComponent<Image>();
            
            img.sprite = sprite;
            img.SetNativeSize();
            
            // if (handle.Status == EOperationStatus.Succeed)
            // {
            //     Debug.Log($"图集加载成功:");
            //     var spriteAtals = handle.AssetObject as SpriteAtlas;
            //     Sprite sprite = spriteAtals.GetSprite("主角 拆分 拷贝");
            //     var img = GameObject.Find("Canvas/window/Image").GetComponent<Image>();
            //     img.sprite = sprite;
            //     img.SetNativeSize();
            // }
            // else
            // {
            //     Debug.LogError($"图集加载失败, 错误: {handle.LastError}");
            // }
        }
        
        /// <summary>
        /// 加载并实例化预制体
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        public IEnumerator LoadAndInstantiatePrefab(string assetPath)
        {
            Debug.Log($"开始加载预制体: {assetPath}");
        
            // 创建资源加载任务
            var assetHandle = YooAssets.LoadAssetAsync<GameObject>(assetPath);
        
            // 等待预制体加载完成
            yield return assetHandle;
        
            if (assetHandle.Status == EOperationStatus.Succeed)
            {
                var root = GameObject.Find("Canvas").transform;
                // 实例化游戏对象
                GameObject prefab = assetHandle.AssetObject as GameObject;
                if (prefab != null)
                {
                    var go = Instantiate(prefab,root);
                    go.name = "window";
                    Debug.Log($"预制体实例化成功: {assetPath}");
                }
            }
            else
            {
                Debug.LogError($"预制体加载失败: {assetPath}, {assetHandle.LastError}");
            }
        
            // 释放资源句柄（实例化后不再需要）
            assetHandle.Release();
        }
    }
}