﻿using UnityEngine;

namespace BJSYGameCore.UI
{
    class UIScriptGeneratorPref : ScriptableObject
    {
        [SerializeField]
        string _lastDir = string.Empty;
        public string lastDir
        {
            get { return _lastDir; }
            set { _lastDir = value; }
        }
        [SerializeField]
        string _namespace = "UI";
#pragma warning disable IDE1006 // 命名样式
        public string Namespace
#pragma warning restore IDE1006 // 命名样式
        {
            get { return _namespace; }
        }
    }
}