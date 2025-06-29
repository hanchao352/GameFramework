using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;


/// <summary>
/// 自定义按钮 - 继承自Button，扩展交互事件和置灰控制
/// </summary>
[AddComponentMenu("UI/Custom/Custom Button")]
public class CustomButton : Button, IBeginDragHandler, IDragHandler, IEndDragHandler
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
    
    [Header("按钮效果")]
    [SerializeField] private bool enableScaleAnimation = true;
    [SerializeField] private float pressedScale = 0.95f;
    [SerializeField] private float animationDuration = 0.1f;
    
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;
    private Material instanceMaterial;
    private Material originalMaterial;
    private static Material grayscaleMaterial;
    private static readonly int GrayAmountProperty = Shader.PropertyToID("_GrayAmount");
    
    private bool isDragging = false;
    private Vector2 dragStartPosition;
    #endregion

    #region 事件属性 - 便捷访问
    /// <summary>
    /// 指针按下事件
    /// </summary>
    public PointerEvent onPointerDown => customEvents.onPointerDown;
    
    /// <summary>
    /// 指针抬起事件
    /// </summary>
    public PointerEvent onPointerUp => customEvents.onPointerUp;
    
    /// <summary>
    /// 指针点击事件（扩展的，会被长按和拖拽阻止）
    /// </summary>
    public PointerEvent onPointerClick => customEvents.onPointerClick;
    
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
        originalScale = transform.localScale;
        
        if (targetGraphic != null)
        {
            originalMaterial = targetGraphic.material;
        }
        
        InitializeGrayscaleMaterial();
    }

    protected override void Start()
    {
        base.Start();
        // 确保初始状态正确
        UpdateGrayscaleEffect();
    }

    protected virtual void Update()
    {
        // 只有在可交互时才更新事件
        if (IsInteractable())
        {
            eventHandler.Update(customEvents);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
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
        if (Application.isPlaying && targetGraphic != null)
        {
            UpdateGrayscaleEffect();
        }
    }
    #endif
    #endregion

    #region 重写Button方法
    public override void OnPointerClick(PointerEventData eventData)
    {
        eventHandler.HandlePointerClick(eventData);
        
        // 如果触发了长按或正在拖拽，则不触发点击
        if (!eventHandler.ShouldBlockClick() && !isDragging)
        {
            base.OnPointerClick(eventData);
            customEvents.onPointerClick?.Invoke(eventData);
        }
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        
        if (IsInteractable())
        {
            dragStartPosition = eventData.position;
            eventHandler.HandlePointerDown(eventData);
            customEvents.onPointerDown?.Invoke(eventData);
            
            if (enableScaleAnimation && !isDragging)
            {
                AnimateScale(pressedScale);
            }
        }
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        
        if (IsInteractable())
        {
            eventHandler.HandlePointerUp(eventData);
            customEvents.onPointerUp?.Invoke(eventData);
            
            if (enableScaleAnimation && !isDragging)
            {
                AnimateScale(1f);
            }
        }
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        
        if (IsInteractable())
        {
            eventHandler.HandlePointerEnter(eventData);
            customEvents.onPointerEnter?.Invoke(eventData);
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        
        if (IsInteractable())
        {
            eventHandler.HandlePointerExit(eventData);
            customEvents.onPointerExit?.Invoke(eventData);
            
            if (enableScaleAnimation)
            {
                AnimateScale(1f);
            }
        }
    }
    #endregion

    #region 拖拽接口实现
    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        if (!enableDrag || !IsInteractable()) return;
        
        // 检查拖拽阈值
        if (Vector2.Distance(eventData.position, dragStartPosition) >= dragThreshold)
        {
            isDragging = true;
            
            // 恢复缩放
            if (enableScaleAnimation)
            {
                AnimateScale(1f);
            }
            
            customEvents.onBeginDrag?.Invoke(eventData);
        }
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        if (!enableDrag || !IsInteractable() || !isDragging) return;
        
        customEvents.onDrag?.Invoke(eventData);
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        if (!enableDrag || !IsInteractable() || !isDragging) return;
        
        isDragging = false;
        customEvents.onEndDrag?.Invoke(eventData);
    }
    #endregion

    #region 事件管理方法
    /// <summary>
    /// 移除所有自定义事件监听器
    /// </summary>
    public void RemoveAllCustomListeners()
    {
        customEvents.ClearAllListeners();
    }

    /// <summary>
    /// 移除所有事件监听器（包括原生onClick）
    /// </summary>
    public void RemoveAllListeners()
    {
        // 移除原生onClick
        onClick.RemoveAllListeners();
        // 移除自定义事件
        RemoveAllCustomListeners();
    }
    #endregion

    #region 私有方法
    private bool IsInteractable()
    {
        return interactable && targetGraphic != null && targetGraphic.raycastTarget;
    }

    private void AnimateScale(float targetScale)
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        
        scaleCoroutine = StartCoroutine(ScaleAnimation(targetScale));
    }

    private System.Collections.IEnumerator ScaleAnimation(float targetScale)
    {
        float elapsedTime = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = originalScale * targetScale;

        while (elapsedTime < animationDuration)
        {
            transform.localScale = Vector3.Lerp(startScale, endScale, elapsedTime / animationDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = endScale;
        scaleCoroutine = null;
    }

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
        if (targetGraphic == null) return;

        if (enableGrayscale && grayscaleAmount > 0)
        {
            if (instanceMaterial == null && grayscaleMaterial != null)
            {
                instanceMaterial = new Material(grayscaleMaterial);
            }
            
            if (instanceMaterial != null)
            {
                instanceMaterial.SetFloat(GrayAmountProperty, grayscaleAmount);
                targetGraphic.material = instanceMaterial;
            }
        }
        else
        {
            targetGraphic.material = originalMaterial;
            
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
    /// 设置按钮文本
    /// </summary>
    public void SetText(string text)
    {
        var textComponent = GetComponentInChildren<Text>();
        if (textComponent != null)
        {
            textComponent.text = text;
        }

        var tmpText = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = text;
        }
    }

    /// <summary>
    /// 设置颜色（通过targetGraphic）
    /// </summary>
    public void SetColor(Color newColor)
    {
        if (targetGraphic != null)
        {
            targetGraphic.color = newColor;
        }
    }

    /// <summary>
    /// 获取颜色
    /// </summary>
    public Color GetColor()
    {
        return targetGraphic != null ? targetGraphic.color : Color.white;
    }

    /// <summary>
    /// 设置透明度
    /// </summary>
    public void SetAlpha(float alpha)
    {
        if (targetGraphic != null)
        {
            var c = targetGraphic.color;
            c.a = Mathf.Clamp01(alpha);
            targetGraphic.color = c;
        }
    }

    /// <summary>
    /// 获取透明度
    /// </summary>
    public float GetAlpha()
    {
        return targetGraphic != null ? targetGraphic.color.a : 1f;
    }

    /// <summary>
    /// 设置置灰状态（置灰不影响交互性）
    /// </summary>
    public void SetGrayscale(bool enable)
    {
        EnableGrayscale = enable;
    }

    /// <summary>
    /// 设置是否可交互（通过interactable）
    /// </summary>
    public void SetInteractable(bool enable)
    {
        interactable = enable;
    }

    /// <summary>
    /// 设置是否可点击（通过raycastTarget）
    /// </summary>
    public void SetClickable(bool clickable)
    {
        if (targetGraphic != null)
        {
            targetGraphic.raycastTarget = clickable;
        }
    }

    /// <summary>
    /// 快速设置为禁用状态（置灰且不可交互）
    /// </summary>
    public void SetDisabled(bool disabled)
    {
        EnableGrayscale = disabled;
        interactable = !disabled;
    }

    /// <summary>
    /// 设置完整状态
    /// </summary>
    public void SetState(bool enableGray, bool enableInteractable, bool enableRaycast)
    {
        EnableGrayscale = enableGray;
        interactable = enableInteractable;
        if (targetGraphic != null)
        {
            targetGraphic.raycastTarget = enableRaycast;
        }
    }
    #endregion
}
