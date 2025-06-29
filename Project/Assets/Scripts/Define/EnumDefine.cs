public enum GameStateId
{
    None = 0,
    CheckUpdate = 1,        // 检查更新
    UpdateResource = 2,     // 更新资源
    Initialize = 3,         // 初始化Manager和Mod
    PreloadResource = 4,    // 预加载资源
    InGame = 5,            // 游戏中
    Exit = 6               // 退出
}



public enum UILayer
{
    Normal = 0,
    NormalTop,
    NormalTop2,
    Popup,
    Loading,
    Tips,
    Max,
}