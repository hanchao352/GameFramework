using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IAssetPool<T> : IResourcePool where T : Object
{
    UniTask<T> GetAsync(string path);
}