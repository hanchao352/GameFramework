using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

public class SkillToolWindow:OdinMenuEditorWindow
{
    private OdinMenuTree odintree;
    private SkillEditorWindow SkillEditorWindow = new SkillEditorWindow();
    
    [MenuItem("工具/技能编辑器")]
    public static void OpenWindow()
    {
        var window = GetWindow<SkillToolWindow>();
        window.titleContent.text = "技能编辑器";
    }
    
    protected override OdinMenuTree BuildMenuTree()
    {
        OdinMenuStyle odinMenuStyle = new OdinMenuStyle();
        odinMenuStyle.SetBorders(true);
        odinMenuStyle.SetSelectedColorDarkSkin(Color.blue);
        OdinMenuTree tree = new OdinMenuTree(true,odinMenuStyle);
        tree.Add("技能编辑器/编辑工具",SkillEditorWindow);
        tree.Selection.SelectionChanged += OnSelcectChange;
        odintree = tree;
        return tree;
    }

    private void OnSelcectChange(SelectionChangedType obj)
    {
        if (obj==SelectionChangedType.SelectionCleared)
        {
            if (odintree==null)
                return;
            for (int i = 0; i < odintree.MenuItems.Count; i++)
            {
                odintree.MenuItems[i].Toggled = false;
            }
            
        }
        else if (obj==SelectionChangedType.ItemRemoved)
        {
            if (odintree==null)
                return;
            for (int i = 0; i < odintree.MenuItems.Count; i++)
            {
                odintree.MenuItems[i].Toggled = false;
            }
        }
        else if (obj==SelectionChangedType.ItemAdded)
        {
            if (odintree==null)
                return;
            if (odintree.Selection!=null)
            {
                odintree.Selection[0].Toggled = true;
            }
        }
    }
}
