public class MainUIView : UIBase
{
    public JoystickComponent joystickComponent;
    public override void Initialize()
    {
        base.Initialize();
        joystickComponent = MakeComponent<JoystickComponent>(Root.transform.Find("JoystickRoot").gameObject);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        joystickComponent.Visible = true;
        
    }

    public override void UpdateView(params object[] args)
    {
        base.UpdateView(args);
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
}