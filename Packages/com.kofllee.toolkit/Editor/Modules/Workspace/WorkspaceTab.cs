using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kofllee.Toolkit.Workspace
{
    internal enum WorkspaceViewMode
    {
        List,
        Grid
    }
    
    internal enum WorkspaceSortMode
    {
        NameAscending,
        NameDescending,
        Type
    }
    
    [Serializable]
    internal class WorkspaceTab
    {
        [SerializeField] private string _name = "Assets";
        [SerializeField] private string _path = "Assets";
        [SerializeField] private string _addressInput = "Assets";
        [SerializeField] private Vector2 _scrollPosition;
        [SerializeField] private List<string> _backHistory = new List<string>();
        [SerializeField] private List<string> _forwardHistory = new List<string>();

        [SerializeField] private string _selectedPath;
        [SerializeField] private WorkspaceViewMode _viewMode = WorkspaceViewMode.Grid;
        
        [SerializeField] private string _searchQuery;
        [SerializeField] private WorkspaceSortMode _sortMode = WorkspaceSortMode.NameAscending;

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

        internal string SelectedPath
        {
            get => _selectedPath;
            set => _selectedPath = value;
        }

        internal WorkspaceViewMode ViewMode
        {
            get => _viewMode;
            set => _viewMode = value;
        }
        
        internal string SearchQuery
        {
            get => _searchQuery;
            set => _searchQuery = value;
        }

        internal WorkspaceSortMode SortMode
        {
            get => _sortMode;
            set => _sortMode = value;
        }
        

        internal List<string> BackHistory => _backHistory;
        internal List<string> ForwardHistory => _forwardHistory;
    }
}