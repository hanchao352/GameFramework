using Google.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class SceneMod : SingletonMod<SceneMod>, IMod
{
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void RegisterMessageHandler()
    {
        RegisterCallback<EnterSceneResponse>(OnEnterSceneResponse);
        RegisterCallback<EntityLeaveSceneResponse>(OnEntityLeaveSceneResponse);
    }

    private async UniTask OnEntityLeaveSceneResponse(EntityLeaveSceneResponse arg)
    {
        await UniTask.CompletedTask;
    }

    private async UniTask OnEnterSceneResponse(EnterSceneResponse response)
    {
        if (response.Result == NResult.Success)
        {
            
        }
        else
        {
            Debug.LogError("Enter scene failed");
        }
        await UniTask.CompletedTask;
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

    

    public override void UnregisterMessageHandler()
    {

    }

    public override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
    }


    public override void Dispose()
    {
        base.Dispose();

    }



}