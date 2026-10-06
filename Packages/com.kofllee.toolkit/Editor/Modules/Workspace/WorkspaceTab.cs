using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kofllee.Toolkit.Workspace
{
    [Serializable]
    internal class WorkspaceTab
    {
        [SerializeField] private string _name = "Assets";
        [SerializeField] private string _path = "Assets";
        [SerializeField] private string _addressInput = "Assets";
        [SerializeField] private Vector2 _scrollPosition;
        [SerializeField] private List<string> _backHistory = new List<string>();
        [SerializeField] private List<string> _forwardHistory = new List<string>();

        internal string Name
        {
            get => _name;
            set => _name = value;
        }

        internal string Path
        {
            get => _path;
            set => _path = value;
        }
        
        internal string AddressInput
        {
            get => _addressInput;
            set => _addressInput = value;
        }

        internal Vector2 ScrollPosition
        {
            get => _scrollPosition;
            set => _scrollPosition = value;
        }

        internal List<string> BackHistory => _backHistory;
        internal List<string> ForwardHistory => _forwardHistory;
    }
}