using System;
using UnityEngine;
using UnityEngine.EventSystems;

[System.Serializable]
public class CustomUIEventHandler
{
    #region 配置
    [SerializeField] private float longPressThreshold = 1.0f;
    [SerializeField] private float holdInterval = 0.1f;
    [SerializeField] private bool enableLongPress = true;
    [SerializeField] private bool enableHold = true;
    #endregion

    #region 内部状态
    private bool isPointerDown = false;
    private float pointerDownTime = 0f;
    private float lastHoldTriggerTime = 0f;
    private bool hasTriggeredLongPress = false;
    private bool isPointerInside = false;
    private PointerEventData currentEventData;
    #endregion

    #region 属性
    public float LongPressThreshold 
    { 
        get => longPressThreshold; 
        set => longPressThreshold = value; 
    }
    
    public float HoldInterval 
    { 
        get => holdInterval; 
        set => holdInterval = value; 
    }

    public bool EnableLongPress
    {
        get => enableLongPress;
        set => enableLongPress = value;
    }

    public bool EnableHold
    {
        get => enableHold;
        set => enableHold = value;
    }

    public bool IsPointerDown => isPointerDown;
    public bool IsPointerInside => isPointerInside;
    public bool HasTriggeredLongPress => hasTriggeredLongPress;
    public PointerEventData CurrentEventData => currentEventData;
    #endregion

    #region 公共方法
    public void Update(CustomUIEvents events)
    {
        if (!isPointerDown || currentEventData == null) return;

        float pressDuration = Time.time - pointerDownTime;

        // 长按和按住互斥处理
        if (enableLongPress && !hasTriggeredLongPress)
        {
            // 长按模式：等待长按触发
            if (pressDuration >= longPressThreshold)
            {
                hasTriggeredLongPress = true;
                events.onLongPress?.Invoke(currentEventData);
            }
        }
        else if (enableHold)
        {
            // 按住模式：立即开始或长按后继续
            if (!enableLongPress || hasTriggeredLongPress)
            {
                if (Time.time - lastHoldTriggerTime >= holdInterval)
                {
                    events.onPointerHold?.Invoke(currentEventData);
                    lastHoldTriggerTime = Time.time;
                }
            }
        }
    }

    public void HandlePointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        pointerDownTime = Time.time;
        lastHoldTriggerTime = Time.time;
        hasTriggeredLongPress = false;
        currentEventData = eventData;
    }

    public void HandlePointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
        currentEventData = null;
    }

    public void HandlePointerClick(PointerEventData eventData)
    {
        // 如果已经触发长按，则不应该触发点击
        if (enableLongPress && hasTriggeredLongPress)
        {
            return;
        }
    }

    public void HandlePointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
    }

    public void HandlePointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        isPointerDown = false;
        currentEventData = null;
    }

    public bool ShouldBlockClick()
    {
        return enableLongPress && hasTriggeredLongPress;
    }

    public void Reset()
    {
        isPointerDown = false;
        hasTriggeredLongPress = false;
        isPointerInside = false;
        currentEventData = null;
    }
    #endregion
}
