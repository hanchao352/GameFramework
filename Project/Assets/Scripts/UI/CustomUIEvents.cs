using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// 自定义UI事件定义
/// </summary>
[System.Serializable]
public class PointerEvent : UnityEvent<PointerEventData> { }

[System.Serializable]
public class CustomUIEvents
{
    [Header("基础事件")]
    public PointerEvent onPointerDown = new PointerEvent();
    public PointerEvent onPointerUp = new PointerEvent();
    public PointerEvent onPointerClick = new PointerEvent();
    public PointerEvent onPointerEnter = new PointerEvent();
    public PointerEvent onPointerExit = new PointerEvent();
    
    [Header("扩展事件")]
    public PointerEvent onLongPress = new PointerEvent();
    public PointerEvent onPointerHold = new PointerEvent();
    
    [Header("拖拽事件")]
    public PointerEvent onBeginDrag = new PointerEvent();
    public PointerEvent onDrag = new PointerEvent();
    public PointerEvent onEndDrag = new PointerEvent();
    
    public void ClearAllListeners()
    {
        onPointerDown.RemoveAllListeners();
        onPointerUp.RemoveAllListeners();
        onPointerClick.RemoveAllListeners();
        onPointerEnter.RemoveAllListeners();
        onPointerExit.RemoveAllListeners();
        onLongPress.RemoveAllListeners();
        onPointerHold.RemoveAllListeners();
        onBeginDrag.RemoveAllListeners();
        onDrag.RemoveAllListeners();
        onEndDrag.RemoveAllListeners();
    }
}
