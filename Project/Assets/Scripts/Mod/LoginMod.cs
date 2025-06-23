using Google.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class LoginMod : SingletonMod<LoginMod>,IMod
{
    
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void RegisterMessageHandler()
    {
        RegisterCallback<LoginResponse>(OnLoginrResponse);
        RegisterCallback<RegisterResponse>(OnRegisterResponse);
        RegisterCallback<EnterGameResponse>(OnEnterGameResponse);
        
    }

    private async UniTask OnEnterGameResponse(EnterGameResponse arg)
    {
        //UIManager.Instance.ShowTips($"进入游戏：{arg.Result}");
       

        await UniTask.CompletedTask;
    }


    private void OnSceneLoadDone(SceneInstance obj)
    {
        
            Debug.Log(obj.Scene.name+"加载成功");
           
    }

    public override void UnregisterMessageHandler()
    {
        UnregisterCallback<LoginResponse>();
        UnregisterCallback<RegisterResponse>();
        UnregisterCallback<EnterGameResponse>();
    }

    private async UniTask OnRegisterResponse(RegisterResponse arg)
    {
        RegisterResponse response = arg as RegisterResponse;
        if (response != null)
        {
            if (response.Result == NResult.Success)
            {
                //UIManager.Instance.ShowTips($" {response.Msg} ");
            }
            else
            {
                //UIManager.Instance.ShowTips($" 附带消息:{response.Msg}");
            }
            
        }
        await UniTask.CompletedTask;
    }


    private async UniTask OnLoginrResponse(LoginResponse arg)
    {
        
        await UniTask.CompletedTask;
  
    }

   

    public override void Dispose()
    {
        base.Dispose();
        
    }

    public async void SendLoginRequest(string username,string password)
    {
        LoginRequest loginRequest = new LoginRequest();
        loginRequest.Username = username;
        loginRequest.Password = password;
         loginRequest.ToSend(); //SendMessage(loginRequest);
    }
    
    public  void SendRegisterRequest(string username,string password)
    {
        RegisterRequest loginRequest = new RegisterRequest();
        loginRequest.Username = username;
        loginRequest.Password = password;
         SendMessage(loginRequest);
    }

 
}
