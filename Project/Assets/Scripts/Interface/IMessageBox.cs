
    using System;
    using UnityEngine;
    using UnityEngine.PlayerLoop;

    public interface IMessageBox
    {
        public string Resname { get; set; }
        abstract void CreateMessageBox();
        abstract void Initialization(string titlestr,string content,string okstr,Action OKAction,string canclestr,Action CancelActon,Action CloseAction);
        abstract void MessageBoxLoadComplete(GameObject root);
        abstract void ShowMessageBox();
        abstract void HideMessageBox();
    }
