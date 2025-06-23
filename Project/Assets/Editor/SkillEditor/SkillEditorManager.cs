using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillEditorManager : SingletonManager<SkillEditorManager>,IGeneric
{
    public bool IsInitialized { get; set; }
    public override void Initialize()
    {
        base.Initialize();
    }

    public void Dispose()
    {
        
    }
}
