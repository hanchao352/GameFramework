using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf;
using UnityEngine;

public  class ModManager :SingletonManager<ModManager>, IGeneric
{
     private  readonly Dictionary<Type, IMod> _mods = new Dictionary<Type, IMod>();
     private List<IMod> _modList = new List<IMod>();
    
    public override void Initialize()
    {
        
    }


    public  void RegisterMod(IMod mod)
    {
        var modType = mod.GetType();
        if (_mods.ContainsKey(modType))
        {
            throw new InvalidOperationException($"Mod {modType} has already been registered.");
            return;
        }
        Debug.Log($"Registering mod {modType}.");
        _mods.Add(modType, mod);
        _modList.Add(mod);
        
       
    }

    public override void Update(float time)
    {
        base.Update(time);
        // foreach (var mod in _mods.Values)
        // {
        //     mod.Update(time);
        // }

        for (int i = 0; i < _modList.Count; i++)
        {
            _modList[i].Update(time);
        }
    }

    public override void Dispose ()
    {
        base.Dispose();
        // foreach (var mod in _mods.Values)
        // {
        //     mod.Dispose();
        // }
        for (int i = 0; i < _modList.Count; i++)
        {
            _modList[i].Dispose();
        }
    
        _mods.Clear();
        _modList.Clear();
    }
   
    


   




}
