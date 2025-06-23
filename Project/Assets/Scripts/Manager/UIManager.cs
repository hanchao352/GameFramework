using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManager : SingletonManager<UIManager>, IGeneric
{
    // 单例 & 隐藏 & 加载中 管理
    private readonly Dictionary<int, UIBase> _singleShowViews    = new Dictionary<int, UIBase>();
    private readonly Dictionary<int, UIBase> _singleHideViews    = new Dictionary<int, UIBase>();
    private readonly Dictionary<int, UIBase> _singleLoadingViews = new Dictionary<int, UIBase>();

    // 多例 对象池 & 活动 & 加载中 管理
    private readonly Dictionary<int, Stack<UIBase>> _multiInstancePool     = new Dictionary<int, Stack<UIBase>>();
    private readonly Dictionary<int, List<UIBase>>  _multiActiveInstances  = new Dictionary<int, List<UIBase>>();
    private readonly Dictionary<int, List<UIBase>>  _multiLoadingInstances = new Dictionary<int, List<UIBase>>();

    // UI 根节点 & 各层容器
    public Transform UIRoot { get; private set; }
    private readonly Dictionary<UILayer, Transform> _uiLayers        = new Dictionary<UILayer, Transform>();
    private const int LAYER_SPACING = 2000;
    private const int UI_SPACING    = 50;
    private readonly Dictionary<UILayer, int>          _currentMaxOrders = new Dictionary<UILayer, int>();
    private readonly Dictionary<UILayer, List<UIBase>> _activePerLayer   = new Dictionary<UILayer, List<UIBase>>();

    // 延迟销毁
    private readonly Dictionary<UIBase, CancellationTokenSource> _destroyTokens = new Dictionary<UIBase, CancellationTokenSource>();
    private const float DEFAULT_DESTROY_DELAY = 5f;

    public override void Initialize()
    {
        // 1) 创建 UI 根节点
        UIRoot = new GameObject("UIRoot",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        ).transform;

        var rootCanvas = UIRoot.GetComponent<Canvas>();
        var scaler     = UIRoot.GetComponent<CanvasScaler>();

        // 配置 Canvas Scaler
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution= new Vector2(1080, 1920);
        scaler.screenMatchMode    = CanvasScaler.ScreenMatchMode.Expand;

        // 2) 创建 UI 摄像机
        var camGO = new GameObject("UICamera");
        camGO.transform.SetParent(UIRoot, false);
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags          = CameraClearFlags.Depth;
        cam.depth               = 100;
        cam.cullingMask         = 1 << LayerMask.NameToLayer("UI");
        cam.orthographic        = true;
        cam.orthographicSize    = 5;
        cam.nearClipPlane       = 0.3f;
        cam.farClipPlane        = 1000f;
        cam.useOcclusionCulling = false;

        // 3) 事件系统
        UIRoot.gameObject.AddComponent<EventSystem>();
        UIRoot.gameObject.AddComponent<StandaloneInputModule>();
        rootCanvas.renderMode   = RenderMode.ScreenSpaceCamera;
        rootCanvas.worldCamera  = cam;
        UIRoot.gameObject.layer = LayerMask.NameToLayer("UI");
        GameObject.DontDestroyOnLoad(UIRoot);

        // 4) 初始化各层级容器（**不** 添加 Canvas/Raycaster）
        for (int i = 0; i < (int)UILayer.Max; i++)
        {
            var layer = (UILayer)i;
            _currentMaxOrders[layer] = i * LAYER_SPACING;
            _activePerLayer[layer]   = new List<UIBase>();

            var go = new GameObject(layer.ToString());
            go.transform.SetParent(UIRoot, false);
            go.layer = LayerMask.NameToLayer("UI");

            _uiLayers[layer] = go.transform;
        }
        GameObject.DontDestroyOnLoad(UIRoot);
    }


    public async UniTask<UIBase> ShowView(int viewId,
        Action<UIBase> onLoadComplete = null,
        Action<UIBase> onShowComplete = null,
        params object[] args)
    {
        if (!UIDefine._uiViews.TryGetValue(viewId, out var info))
            throw new ArgumentException($"未定义 ViewId={viewId}");

        UIBase view = info.IsMultiInstance
            ? await ShowMulti(viewId, info, onLoadComplete, onShowComplete, args)
            : await ShowSingle(viewId, info, onLoadComplete, onShowComplete, args);

        if (view != null)
            PlaceInLayer(view, info.Layer);

        return view;
    }

    #region 单例流程
    private async UniTask<UIBase> ShowSingle(int viewId, ViewInfo info,
        Action<UIBase> onLoad, Action<UIBase> onShow, params object[] args)
    {
        // 已显示
        if (_singleShowViews.TryGetValue(viewId, out var shown))
        {
            CancelDestroy(shown);
            shown.UpdateView(args);
            onShow?.Invoke(shown);
            return shown;
        }
        // 缓存复用
        if (_singleHideViews.TryGetValue(viewId, out var hidden))
        {
            CancelDestroy(hidden);
            _singleHideViews.Remove(viewId);
            _singleShowViews[viewId] = hidden;
            hidden.Args = args;
            hidden.OnShow(args);
            onShow?.Invoke(hidden);
            return hidden;
        }
        // 加载中
        if (_singleLoadingViews.TryGetValue(viewId, out var loading))
        {
            loading.Args = args;
            return loading;
        }
        // 新建
        var view = ObjectCreator.CreateInstance(info.ViewType) as UIBase;
        view.viewInfo = info;
        view.Args     = args;
        _singleLoadingViews[viewId] = view;

        try
        {
            await view.CreateRoot();
            view.Initialize();
            onLoad?.Invoke(view);
        }
        catch (OperationCanceledException)
        {
            view.Dispose();
            _singleLoadingViews.Remove(viewId);
            return null;
        }

        _singleLoadingViews.Remove(viewId);
        _singleShowViews[viewId] = view;
        view.OnShow(args);
        onShow?.Invoke(view);
        return view;
    }
    #endregion

    #region 多例流程
    private async UniTask<UIBase> ShowMulti(int viewId, ViewInfo info,
        Action<UIBase> onLoad, Action<UIBase> onShow, params object[] args)
    {
        UIBase view;
        if (_multiInstancePool.TryGetValue(viewId, out var pool) && pool.Count > 0)
        {
            view = pool.Pop();
            CancelDestroy(view);
        }
        else
        {
            view = ObjectCreator.CreateInstance(info.ViewType) as UIBase;
            view.viewInfo = info;
            view.Args     = args;

            if (!_multiLoadingInstances.TryGetValue(viewId, out var lst))
            {
                lst = new List<UIBase>();
                _multiLoadingInstances[viewId] = lst;
            }
            lst.Add(view);

            try
            {
                await view.CreateRoot();
                view.Initialize();
                onLoad?.Invoke(view);
            }
            catch (OperationCanceledException)
            {
                view.Dispose();
                lst.Remove(view);
                return null;
            }
            finally
            {
                lst.Remove(view);
                if (lst.Count == 0)
                    _multiLoadingInstances.Remove(viewId);
            }
        }

        if (!_multiActiveInstances.TryGetValue(viewId, out var actList))
        {
            actList = new List<UIBase>();
            _multiActiveInstances[viewId] = actList;
        }
        actList.Add(view);

        view.OnShow(args);
        onShow?.Invoke(view);
        return view;
    }
    #endregion

    #region 隐藏 & 延迟销毁
    public void HideView(int viewId, UIBase specific = null)
    {
        if (!UIDefine._uiViews.TryGetValue(viewId, out var info)) return;
        if (info.IsMultiInstance)
            HideMulti(viewId, specific);
        else
            HideSingle(viewId);
    }
    private void HideSingle(int viewId)
    {
        if (_singleLoadingViews.TryGetValue(viewId, out var loading))
        {
            loading.CancelLoading();
            _singleLoadingViews.Remove(viewId);
        }
        if (_singleShowViews.TryGetValue(viewId, out var view))
        {
            view.OnHide();
            _singleShowViews.Remove(viewId);
            _singleHideViews[viewId] = view;
            view.Root.SetActive(false);
            RemoveActive(view);
            ScheduleDestroy(view, DEFAULT_DESTROY_DELAY);
        }
    }
    private void HideMulti(int viewId, UIBase specific)
    {
        if (_multiLoadingInstances.TryGetValue(viewId, out var lstL))
        {
            UIBase tgt = specific != null ? specific : (lstL.Count > 0 ? lstL[0] : null);
            if (tgt != null)
            {
                tgt.CancelLoading();
                lstL.Remove(tgt);
                if (lstL.Count == 0)
                    _multiLoadingInstances.Remove(viewId);
            }
        }
        if (_multiActiveInstances.TryGetValue(viewId, out var lstA))
        {
            UIBase tgt = specific != null ? specific : (lstA.Count > 0 ? lstA[0] : null);
            if (tgt != null && RemoveFromList(lstA, tgt))
            {
                tgt.OnHide();
                tgt.Root.SetActive(false);
                if (!_multiInstancePool.TryGetValue(viewId, out var pool))
                    _multiInstancePool[viewId] = pool = new Stack<UIBase>();
                pool.Push(tgt);
                ScheduleDestroy(tgt, DEFAULT_DESTROY_DELAY);
            }
        }
    }
    private bool RemoveFromList(List<UIBase> list, UIBase v)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == v)
            {
                list.RemoveAt(i);
                return true;
            }
        }
        return false;
    }
    private void ScheduleDestroy(UIBase v, float delay)
    {
        var cts = new CancellationTokenSource();
        _destroyTokens[v] = cts;
        UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cts.Token)
            .ContinueWith(() =>
            {
                if (!_destroyTokens.Remove(v, out _)) return;
                int vid = v.viewInfo.ViewId;
                if (_singleHideViews.TryGetValue(vid, out var sv) && sv == v)
                    _singleHideViews.Remove(vid);
                if (_multiInstancePool.TryGetValue(vid, out var pool2))
                {
                    var tmp = pool2.ToArray();
                    var newPool = new Stack<UIBase>();
                    for (int i = 0; i < tmp.Length; i++)
                        if (tmp[i] != v)
                            newPool.Push(tmp[i]);
                    _multiInstancePool[vid] = newPool;
                }
                v.Dispose();
                cts.Dispose();
            }).Forget();
    }
    private void CancelDestroy(UIBase v)
    {
        if (_destroyTokens.TryGetValue(v, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
            _destroyTokens.Remove(v);
        }
    }
    #endregion

    #region 排序 & 层级
    /// <summary>
    /// 将 view 挂到对应 UILayer 容器，并在它的根节点上添加 Canvas＋排序＋Raycaster
    /// </summary>
    private void PlaceInLayer(UIBase view, UILayer layer)
    {
        // 1) 父节点设置
        view.Root.transform.SetParent(_uiLayers[layer], false);

        // 2) 计算新的排序值
        int order = _currentMaxOrders[layer] + UI_SPACING;
        _currentMaxOrders[layer] = order;
        view.CurrentOrder        = order;

        // 3) 确保界面根节点有 Canvas，并设置排序
        var cvs = view.Root.GetComponent<Canvas>();
        if (cvs == null)
            cvs = view.Root.AddComponent<Canvas>();
        cvs.overrideSorting = true;
        cvs.sortingOrder    = order;

        // 4) 确保有 GraphicRaycaster
        if (view.Root.GetComponent<GraphicRaycaster>() == null)
            view.Root.AddComponent<GraphicRaycaster>();

        // 5) 递归对子 Canvas 进行排序（若子物体也有 overrideSorting）
        SetChildOrder(view.Root.transform, order);

        // 6) 记录活动列表
        _activePerLayer[layer].Add(view);
    }

    /// <summary>
    /// 递归调整所有后代 Canvas 的 sortingOrder
    /// </summary>
    private void SetChildOrder(Transform parent, int baseOrder)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var ch = parent.GetChild(i);
            var c  = ch.GetComponent<Canvas>();
            if (c != null && c.overrideSorting)
                c.sortingOrder = baseOrder + (c.sortingOrder - baseOrder);

            if (ch.childCount > 0)
                SetChildOrder(ch, baseOrder);
        }
    }


    private void RemoveActive(UIBase v)
    {
        foreach (var kv in _activePerLayer)
        {
            var list = kv.Value;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == v)
                {
                    list.RemoveAt(i);
                    int max = (int)kv.Key * LAYER_SPACING;
                    for (int j = 0; j < list.Count; j++)
                        if (list[j].CurrentOrder > max)
                            max = list[j].CurrentOrder;
                    _currentMaxOrders[kv.Key] = max;
                    return;
                }
            }
        }
    }
    #endregion

    public override void Update(float time)
    {
        foreach (var v in _singleShowViews.Values)
            v.Update(time);
        foreach (var lst in _multiActiveInstances.Values)
            for (int i = 0; i < lst.Count; i++)
                lst[i].Update(time);
    }

    public override void Dispose()
    {
        ClearAllViews();
    }

    private void ClearAllViews()
    {
        // 取消并销毁单例加载中
        var loadKeys = new List<int>(_singleLoadingViews.Keys);
        foreach(var key in loadKeys)
        {
            var v = _singleLoadingViews[key];
            v.CancelLoading();
            v.Dispose();
            _singleLoadingViews.Remove(key);
        }
        // 销毁单例显示中
        var showKeys = new List<int>(_singleShowViews.Keys);
        foreach(var key in showKeys)
        {
            var v = _singleShowViews[key];
            v.Dispose();
            _singleShowViews.Remove(key);
        }
        // 销毁单例隐藏中
        var hideKeys = new List<int>(_singleHideViews.Keys);
        foreach(var key in hideKeys)
        {
            var v = _singleHideViews[key];
            v.Dispose();
            _singleHideViews.Remove(key);
        }
        // 多例加载中
        var multiLoadKeys = new List<int>(_multiLoadingInstances.Keys);
        foreach(var key in multiLoadKeys)
        {
            var lst = _multiLoadingInstances[key];
            for(int i=0; i<lst.Count; i++)
                { lst[i].CancelLoading(); lst[i].Dispose(); }
            _multiLoadingInstances.Remove(key);
        }
        // 多例活动中
        var multiActKeys = new List<int>(_multiActiveInstances.Keys);
        foreach(var key in multiActKeys)
        {
            var lst = _multiActiveInstances[key];
            for(int i=0; i<lst.Count; i++)
                lst[i].Dispose();
            _multiActiveInstances.Remove(key);
        }
        // 多例池中
        var poolKeys = new List<int>(_multiInstancePool.Keys);
        foreach (var key in poolKeys)
        {
            var stack = _multiInstancePool[key];
            while (stack.Count > 0)
            {
                var v = stack.Pop();
                v.Dispose();
            }
            _multiInstancePool.Remove(key);
        }

        // 取消并销毁所有延迟销毁任务
        var destroyKeys = new List<UIBase>(_destroyTokens.Keys);
        foreach (var view in destroyKeys)
        {
            var cts = _destroyTokens[view];
            cts.Cancel();
            cts.Dispose();
            _destroyTokens.Remove(view);
        }

        // 清理层级活动列表
        foreach (var layer in _activePerLayer.Keys)
        {
            var list = _activePerLayer[layer];
            for (int i = 0; i < list.Count; i++)
            {
                // 这里 list 中的 view 应该已经在上面被 Dispose 过了
            }
            list.Clear();
        }

        // 重置排序计数
        foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
        {
            _currentMaxOrders[layer] = (int)layer * LAYER_SPACING;
        }
    }
    // 退出时彻底清理
    public override void OnApplicationQuit()
    {
        ClearAllViews();
    }
}


