using System;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
public class GameSceneManager : SingletonManager<GameSceneManager>, IGeneric
{

    private AsyncOperationHandle<SceneInstance> _currentSceneHandle;



    public override void Initialize()
    {
        base.Initialize();
        
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

    public override void Dispose()
    {
        base.Dispose();
    }
        
    public void LoadScene(string resname,Action<SceneInstance> callback)
    {
        //ResManager.Instance.LoadScene(resname, callback);
         
    }

 
}
