using Google.Protobuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class HeroMod : SingletonMod<HeroMod>, IMod
{
   
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void AllModInitialize()
    {
        base.AllModInitialize();
    }

    public override void RegisterMessageHandler()
    {


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