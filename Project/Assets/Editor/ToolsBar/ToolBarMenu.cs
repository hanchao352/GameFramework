using System;
using System.Diagnostics;
using System.IO;
using Paps.UnityToolbarExtenderUIToolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using Debug = UnityEngine.Debug;

[InitializeOnLoad]
public class ToolBarMenu
{  
    static ToolBarMenu()
    {
        MainToolbar.OnInitialized += OnToolbarInitialized;
       

    }

    private static void OnToolbarInitialized()
    {
        //proto按钮
        Button protobutton = new Button();
        protobutton.text = "GenProto";
        protobutton.style.color = Color.white;
        protobutton.style.backgroundColor = Color.black;
        protobutton.AddToClassList("unity-toolbar-button");
        protobutton.clicked += ExecuteProto2CS;
        protobutton.style.width = 150;
        protobutton.style.height = 30;
        MainToolbar.LeftContainer.Add(protobutton);
        //打开proto文件夹按钮
        Button openProtoFolderButton = new Button();
        openProtoFolderButton.text = "OpenProtoFolder";
        openProtoFolderButton.style.color = Color.white;
        openProtoFolderButton.style.backgroundColor = Color.black;
        openProtoFolderButton.AddToClassList("unity-toolbar-button");
        openProtoFolderButton.clicked += OpenProtoFolder;
        openProtoFolderButton.style.width = 150;
        openProtoFolderButton.style.height = 30;
        MainToolbar.LeftContainer.Add(openProtoFolderButton);
        
        
    }

public static void ExecuteProto2CS()
    {
        string path = Application.dataPath + "/../../Tools/proto2csforClient.bat";
        
        if (!File.Exists(path))
        {
            Debug.LogError("批处理文件不存在: " + path);
            EditorUtility.DisplayDialog("Error", "批处理文件不存在: " + path, "OK");
            return;
        }

        // 获取批处理文件所在目录
        string workingDirectory = Path.GetDirectoryName(path);
        
        // 获取批处理文件名（不带路径）
        string batchFileName = Path.GetFileName(path);
        
        // 获取 cmd.exe 的完整路径
        string cmdPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = cmdPath,                 // 使用 cmd.exe
                Arguments = $"/C \"{batchFileName}\"", // /C 参数执行后关闭窗口
                WorkingDirectory = workingDirectory, // 设置工作目录
                UseShellExecute = true,             // 必须为 true 才能显示窗口
                CreateNoWindow = false,              // 显示窗口
                WindowStyle = ProcessWindowStyle.Normal // 正常窗口样式
            };

            Debug.Log($"准备执行: {path}");
            Debug.Log($"工作目录: {workingDirectory}");
            
            // 启动进程
            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                
                // 启动进程
                if (process.Start())
                {
                    // 等待进程完成
                    process.WaitForExit();
                    Debug.Log("批处理执行完成");
                }
                else
                {
                    Debug.LogError("无法启动批处理进程");
                    EditorUtility.DisplayDialog("错误", "无法启动命令行进程", "OK");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("异常", $"执行出错: {ex.Message}", "OK");
        }
    }

    public static void OpenProtoFolder()
    {
        string protoFolderPath = Application.dataPath + "/../../PtotoFiles/";
        
        if (Directory.Exists(protoFolderPath))
        {
            // 打开文件夹
            EditorUtility.RevealInFinder(protoFolderPath);
        }
        else
        {
            Debug.LogError("Proto文件夹不存在: " + protoFolderPath);
            EditorUtility.DisplayDialog("Error", "Proto文件夹不存在: " + protoFolderPath, "OK");
        }
    }
}

