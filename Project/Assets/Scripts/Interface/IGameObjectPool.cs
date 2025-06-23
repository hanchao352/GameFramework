using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IGameObjectPool : IResourcePool
{
    UniTask<GameObject> GetAsync(string path);
    UniTask<GameObject> GetAsync(string path, Transform parent, bool worldPositionStays = false);
    void ReleaseInstance(GameObject instance);
}