public enum  EventID
{
  
        LoginEvent_Fail = 10000,
        LoginEvent_LoginSuccess = 10001,
        LoginEvent_RegisterSuccess = 10002,
        LoginEvent_UpdateRoleList = 10003,
        LoginEvent_CreateRoleSuccess = 10004,
        LoginEvent_DeleteRoleSuccess = 10005,
        LoginEvent_SelectRoleSuccess = 10006,
        //玩家进入场景成功事件
        PlayerEvent_EnterSceneSuccess = 10007,
        //玩家进入场景失败
        PlayerEvent_EnterSceneFail = 10008,
        //玩家离开场景成功
        PlayerEvent_LeaveSceneSuccess = 10009,
        //玩家离开场景失败
        PlayerEvent_LeaveSceneFail = 10010,
        
}
