using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;

public interface IAtlasPool : IAssetPool<SpriteAtlas>
{
    /// <summary>
    /// 从图集中获取指定名称的 Sprite
    /// </summary>
    UniTask<Sprite> GetSpriteAsync(string atlasPath, string spriteName);
    
}