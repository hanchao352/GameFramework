using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEditor;
using UnityEngine;

public class CreateUIView :EditorWindow
{
   [MenuItem("GameObject/UITools/CreatUIScripts", false, 10)]
   static void CustomMenuFunction(MenuCommand menuCommand)
   {
      GameObject go = menuCommand.context as GameObject;
      
   }
}
