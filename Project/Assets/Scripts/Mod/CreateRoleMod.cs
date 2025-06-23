//Des:
//Author:韩超
//Time:2023年04月06日 星期四 15:01

using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

public class CreateRoleMod : SingletonMod<CreateRoleMod>,IMod
{

  
    public string CreatName=string.Empty;


    public override void Initialize()
    {
        base.Initialize();
    }

    public override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
    }

    public override void Dispose()
    {
        base.Dispose();
    }

    public override void OnApplicationFocus(bool hasFocus)
    {
        base.OnApplicationFocus(hasFocus);
    }

    public override void OnApplicationPause(bool pauseStatus)
    {
        base.OnApplicationPause(pauseStatus);
    }



    public override void RegisterMessageHandler()
    {
        RegisterCallback<CreateRoleResponse>(OnCreateRoleResponse);
    }
    private UniTask OnCreateRoleResponse(CreateRoleResponse arg)
    {
        CreateRoleResponse response = arg as CreateRoleResponse;
        if (response != null)
        {
           // UIManager.Instance.ShowTips($" {response.Result.ToString()} : {response.Msg}");
        }
        return UniTask.CompletedTask;
    }


    public override void Update(float time)
    {
        base.Update(time);
    }

   
    
    
    
    public void CreareRoleRquest( )
    {
        if (string.IsNullOrEmpty(CreatName))
        {
            //UIManager.Instance.ShowTips("角色名不能为空");
            return;
        }
        CreateRoleRequest createRoleRequest = new CreateRoleRequest();
        createRoleRequest.Rolename = CreatName;
        createRoleRequest.Race = GameRace.Human;
         SendMessage(createRoleRequest);//SendMessage(createRoleRequest);
    }

   

    public override void UnregisterMessageHandler()
    {
        UnregisterCallback<CreateRoleResponse>();
    }
}