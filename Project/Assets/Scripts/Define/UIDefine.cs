
using System;
using System.Collections.Generic;

public class ViewInfo
{
        public int    ViewId;
        public Type   ViewType;
        public bool   IsMultiInstance;
        public UILayer Layer;          // 新增：指定该界面的Layer
}

public static class UIName
{
        public const int UILogin = 1001;
        public const int UITips = 1002;
}
public static class UIDefine
{
        public static readonly Dictionary<int, ViewInfo> _uiViews = new Dictionary<int, ViewInfo>()
        {
                // 登录界面，单例，Normal层
                { UIName.UILogin, new ViewInfo {
                        ViewId          = UIName.UILogin,
                        ViewType        = typeof(UILoginView),
                        IsMultiInstance = false,
                        Layer           = UILayer.Normal
                }},
                // 提示界面，多例，Tips层
                { UIName.UITips, new ViewInfo {
                        ViewId          = UIName.UITips,
                        ViewType        = typeof(UITipsView),
                        IsMultiInstance = true,
                        Layer           = UILayer.Tips
                }},
            
        };
        
}
