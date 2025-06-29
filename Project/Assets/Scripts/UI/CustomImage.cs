using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
/// <summary>
/// 自定义Image - 继承自Image，添加交互事件和置灰控制
/// </summary>
[AddComponentMenu("UI/Custom/Custom Image")]
public class CustomImage : Image, IPointerClickHandler, 
    IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    #region 字段
    [Header("扩展事件")]
    [SerializeField] private CustomUIEvents customEvents = new CustomUIEvents();
    
    [Header("事件配置")]
    [SerializeField] private CustomUIEventHandler eventHandler = new CustomUIEventHandler();
    
    [Header("拖拽设置")]
    [SerializeField] private bool enableDrag = false;
    [SerializeField] private float dragThreshold = 5f;
    
    [Header("置灰控制")]
    [SerializeField] private bool enableGrayscale = false;
    [SerializeField] [Range(0f, 1f)] private float grayscaleAmount = 0.5f;
    
    [Header("交互效果")]
    [SerializeField] private bool enableHoverEffect = true;
    [SerializeField] private Color hoverTintColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    [SerializeField] private bool enablePressEffect = true;
    [SerializeField] private Color pressTintColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    
    private Material instanceMaterial;
    private Material originalMaterial;
    private static Material grayscaleMaterial;
    private static readonly int GrayAmountProperty = Shader.PropertyToID("_GrayAmount");
    
    private bool isDragging = false;
    private Vector2 dragStartPosition;
    #endregion

    #region 事件属性 - 便捷访问（Image没有原生点击事件）
    /// <summary>
    /// 点击事件（Image原生没有）
    /// </summary>
    public PointerEvent onClick => customEvents.onPointerClick;
    
    /// <summary>
    /// 指针按下事件
    /// </summary>
    public PointerEvent onPointerDown => customEvents.onPointerDown;
    
    /// <summary>
    /// 指针抬起事件
    /// </summary>
    public PointerEvent onPointerUp => customEvents.onPointerUp;
    
    /// <summary>
    /// 指针进入事件
    /// </summary>
    public PointerEvent onPointerEnter => customEvents.onPointerEnter;
    
    /// <summary>
    /// 指针离开事件
    /// </summary>
    public PointerEvent onPointerExit => customEvents.onPointerExit;
    
    /// <summary>
    /// 长按事件
    /// </summary>
    public PointerEvent onLongPress => customEvents.onLongPress;
    
    /// <summary>
    /// 按住事件（持续触发）
    /// </summary>
    public PointerEvent onPointerHold => customEvents.onPointerHold;
    
    /// <summary>
    /// 开始拖拽事件
    /// </summary>
    public PointerEvent onBeginDrag => customEvents.onBeginDrag;
    
    /// <summary>
    /// 拖拽中事件
    /// </summary>
    public PointerEvent onDrag => customEvents.onDrag;
    
    /// <summary>
    /// 结束拖拽事件
    /// </summary>
    public PointerEvent onEndDrag => customEvents.onEndDrag;
    #endregion

    #region 属性
    public CustomUIEvents CustomEvents => customEvents;
    
    public bool EnableDrag
    {
        get => enableDrag;
        set => enableDrag = value;
    }
    
    public float DragThreshold
    {
        get => dragThreshold;
        set => dragThreshold = value;
    }
    
    public bool IsDragging => isDragging;
    
    public float LongPressThreshold
    {
        get => eventHandler.LongPressThreshold;
        set => eventHandler.LongPressThreshold = value;
    }

    public float HoldInterval
    {
        get => eventHandler.HoldInterval;
        set => eventHandler.HoldInterval = value;
    }

    public bool EnableLongPress
    {
        get => eventHandler.EnableLongPress;
        set => eventHandler.EnableLongPress = value;
    }

    public bool EnableHold
    {
        get => eventHandler.EnableHold;
        set => eventHandler.EnableHold = value;
    }

    public bool EnableGrayscale
    {
        get => enableGrayscale;
        set
        {
            if (enableGrayscale != value)
            {
                enableGrayscale = value;
                UpdateGrayscaleEffect();
            }
        }
    }

    public float GrayscaleAmount
    {
        get => grayscaleAmount;
        set
        {
            grayscaleAmount = Mathf.Clamp01(value);
            if (enableGrayscale)
            {
                UpdateGrayscaleEffect();
            }
        }
    }
    #endregion

    #region Unity生命周期
    protected override void Awake()
    {
        base.Awake();
        originalMaterial = material;
        InitializeGrayscaleMaterial();
        
        // 默认启用raycastTarget以接收事件
        if (!raycastTarget)
        {
            raycastTarget = true;
        }
    }

    protected override void Start()
    {
        base.Start();
        // 确保初始状态正确
        UpdateGrayscaleEffect();
    }

    protected virtual void Update()
    {
        // 只有raycastTarget为true时才处理事件
        if (raycastTarget)
        {
            eventHandler.Update(customEvents);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (instanceMaterial != null)
        {
            DestroyImmediate(instanceMaterial);
        }
        RemoveAllListeners();
    }

    #if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (Application.isPlaying)
        {
            UpdateGrayscaleEffect();
        }
    }
    #endif
    #endregion

    #region 重写Graphic方法处理交互效果
    public override void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha)
    {
        // 应用交互效果
        Color finalColor = ApplyInteractionEffect(targetColor);
        base.CrossFadeColor(finalColor, duration, ignoreTimeScale, useAlpha);
    }

    public override void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha, bool useRGB)
    {
        // 应用交互效果
        Color finalColor = ApplyInteractionEffect(targetColor);
        base.CrossFadeColor(finalColor, duration, ignoreTimeScale, useAlpha, useRGB);
    }

    private Color ApplyInteractionEffect(Color baseColor)
    {
        if (!raycastTarget) return baseColor;
        
        if (eventHandler.IsPointerDown && enablePressEffect)
        {
            return pressTintColor * baseColor;
        }
        else if (eventHandler.IsPointerInside && enableHoverEffect)
        {
            return hoverTintColor * baseColor;
        }
        
        return baseColor;
    }

    // 在交互状态改变时更新颜色
    private void UpdateInteractionVisual()
    {
        if (!Application.isPlaying) return;
        
        // 使用CanvasRenderer直接设置颜色，保持原有透明度
        var currentColor = canvasRenderer.GetColor();
        var targetColor = ApplyInteractionEffect(color);
        
        if (currentColor != targetColor)
        {
            CrossFadeColor(targetColor, 0.1f, true, true);
        }
    }
    #endregion

    #region EventSystem接口实现
    public virtual void OnPointerClick(PointerEventData eventData)
    {
        eventHandler.HandlePointerClick(eventData);
        
        // 如果没有触发长按或拖拽，则触发点击
        if (!eventHandler.ShouldBlockClick() && !isDragging)
        {
            customEvents.onPointerClick?.Invoke(eventData);
        }
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        dragStartPosition = eventData.position;
        eventHandler.HandlePointerDown(eventData);
        customEvents.onPointerDown?.Invoke(eventData);
        UpdateInteractionVisual();
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        eventHandler.HandlePointerUp(eventData);
        customEvents.onPointerUp?.Invoke(eventData);
        UpdateInteractionVisual();
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        eventHandler.HandlePointerEnter(eventData);
        customEvents.onPointerEnter?.Invoke(eventData);
        UpdateInteractionVisual();
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        eventHandler.HandlePointerExit(eventData);
        customEvents.onPointerExit?.Invoke(eventData);
        UpdateInteractionVisual();
    }
    #endregion

    #region 拖拽接口实现
    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        if (!enableDrag || !raycastTarget) return;
        
        // 检查拖拽阈值
        if (Vector2.Distance(eventData.position, dragStartPosition) >= dragThreshold)
        {
            isDragging = true;
            customEvents.onBeginDrag?.Invoke(eventData);
        }
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        if (!enableDrag || !raycastTarget || !isDragging) return;
        
        customEvents.onDrag?.Invoke(eventData);
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        if (!enableDrag || !raycastTarget || !isDragging) return;
        
        isDragging = false;
        customEvents.onEndDrag?.Invoke(eventData);
        UpdateInteractionVisual();
    }
    #endregion

    #region 事件管理方法
    /// <summary>
    /// 移除所有事件监听器
    /// </summary>
    public void RemoveAllListeners()
    {
        customEvents.ClearAllListeners();
    }
    #endregion

    #region 私有方法
    private static void InitializeGrayscaleMaterial()
    {
        if (grayscaleMaterial == null)
        {
            Shader shader = Shader.Find("UI/Grayscale");
            if (shader != null)
            {
                grayscaleMaterial = new Material(shader);
            }
        }
    }

    private void UpdateGrayscaleEffect()
    {
        if (enableGrayscale && grayscaleAmount > 0)
        {
            if (instanceMaterial == null && grayscaleMaterial != null)
            {
                instanceMaterial = new Material(grayscaleMaterial);
            }
            
            if (instanceMaterial != null)
            {
                instanceMaterial.SetFloat(GrayAmountProperty, grayscaleAmount);
                material = instanceMaterial;
            }
        }
        else
        {
            material = originalMaterial;
            
            if (instanceMaterial != null)
            {
                DestroyImmediate(instanceMaterial);
                instanceMaterial = null;
            }
        }
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 设置颜色（使用继承的color属性）
    /// </summary>
    public void SetColor(Color newColor)
    {
        color = newColor;
    }

    /// <summary>
    /// 设置透明度
    /// </summary>
    public void SetAlpha(float alpha)
    {
        var c = color;
        c.a = Mathf.Clamp01(alpha);
        color = c;
    }

    /// <summary>
    /// 获取当前透明度
    /// </summary>
    public float GetAlpha()
    {
        return color.a;
    }

    /// <summary>
    /// 设置置灰状态和射线检测状态
    /// </summary>
    public void SetState(bool enableGray, bool raycastEnabled)
    {
        EnableGrayscale = enableGray;
        raycastTarget = raycastEnabled;
    }

    /// <summary>
    /// 快速设置为禁用状态（置灰且不可交互）
    /// </summary>
    public void SetDisabled(bool disabled)
    {
        EnableGrayscale = disabled;
        raycastTarget = !disabled;
    }

    /// <summary>
    /// 设置是否可点击
    /// </summary>
    public void SetClickable(bool clickable)
    {
        raycastTarget = clickable;
    }
    #endregion
}
