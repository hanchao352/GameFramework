using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JoystickComponent : ComponentBase
{
    public bool freemove = true;
    public RectTransform background;
    public RectTransform handle;
    //private GameImage gameImage;
    private CustomImage rangeImage;
    private Vector2 joystickCenter;
    private Vector2 FixjoystickCenter;
    public float maxDistance =100;
    public override void Initialize()
    {
        base.Initialize();
        //gameImage=Root.GetComponent<GameImage>();
        rangeImage=Root.GetComponent<CustomImage>();
        background = Root.transform.Find("Joystick") as RectTransform;
        handle = Root.transform.Find("Joystick/BackBg/Handle") as RectTransform;
        rangeImage.onClick.AddListener(OnClick);
        rangeImage.onPointerDown.AddListener(OnClickDown);
        rangeImage.onPointerUp.AddListener(OnClickUp);
        rangeImage.onBeginDrag.AddListener(OnDragBegin);
        rangeImage.onDrag.AddListener(OnDrag);
        rangeImage.onEndDrag.AddListener(OnDragEnd);
        joystickCenter = background.position;
        FixjoystickCenter=background.position;
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
    }

    public override void UpdateView(params object[] args)
    {
        base.UpdateView(args);
    }

    private void OnClickUp(PointerEventData eventData)
    {
        handle.anchoredPosition = Vector2.zero;
        handle.localRotation = Quaternion.identity; 
        background.position = FixjoystickCenter;
    }

    private void OnClickDown(PointerEventData eventData)
    {
        UpdateHandlePosition(eventData);
    }

    private void OnDragEnd(PointerEventData eventData)
    {
        Debug.Log("OnDragEnd");
        handle.anchoredPosition = Vector2.zero;
        handle.localRotation = Quaternion.identity; 
        background.position = FixjoystickCenter;
    }

    private void OnDrag(PointerEventData eventData)
    {
        Debug.Log("OnDrag");
        UpdateHandlePosition(eventData);
    }

    private void OnDragBegin(PointerEventData eventData)
    {
        Debug.Log("OnDragBegin");
        UpdateHandlePosition(eventData);
    }

    private void OnClick(PointerEventData eventData)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            if (freemove)
            {
                background.anchoredPosition = localPoint;
            }
         
           
            joystickCenter = background.position; 

           
            UpdateHandlePosition(eventData);
        }
    }
    public override void Update(float time)
    {
        base.Update(time);
    }

    public override void OnApplicationFocus(bool hasFocus)
    {
        base.OnApplicationFocus(hasFocus);
    }

    public override void OnApplicationPause(bool pauseStatus)
    {
        base.OnApplicationPause(pauseStatus);
    }

    public override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
    }

    public override void OnHide()
    {
        base.OnHide();
    }

    public override void OnEnterMutex()
    {
        base.OnEnterMutex();
    }

    public override void OnExitMutex()
    {
        base.OnExitMutex();
    }

    public override void Dispose()
    {
        base.Dispose();
    }
    private void UpdateHandlePosition(PointerEventData eventData)
    {
        
        Vector2 direction = eventData.position - new Vector2(joystickCenter.x, joystickCenter.y);
        
        float distance = Mathf.Min(direction.magnitude, maxDistance);
        
        handle.anchoredPosition = direction.normalized * distance;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float handleAngle =angle - 90;
        float entityAngle = 90-angle;
        handle.localRotation = Quaternion.Euler(0, 0, handleAngle);
       // EntityManager.Instance.Hero.Rotation=Quaternion.Euler(0, entityAngle, 0);
        
    }
}