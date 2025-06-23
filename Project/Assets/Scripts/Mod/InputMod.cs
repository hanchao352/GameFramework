
using UnityEngine;
[ModAttribute]
public class InputMod:SingletonMod<InputMod>,IMod
{
    
    public override void Initialize()
    {
        base.Initialize();
    }

   

    public override void Dispose()
    {
        base.Dispose();
    }

    public override void RegisterMessageHandler()
    {
        
    }

    public override void UnregisterMessageHandler()
    {
       
    }
}
