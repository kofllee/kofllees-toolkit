using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kofllee.Toolkit.Editor.Modules.Workspace
{
    internal class WorkspaceBrowserWindow : EditorWindow
    {
        [SerializeField] private List<WorkspaceTab> _tabs = new List<WorkspaceTab>();
        [SerializeField] private int _activeTabIndex;
        
        private WorkspaceTab ActiveTab => _tabs[_activeTabIndex];

        [MenuItem("Window/kofllee's Toolkit/Workspace Browser")]
        private static void Open()
        {
            WorkspaceBrowserWindow window = GetWindow<WorkspaceBrowserWindow>();
            window.titleContent = new GUIContent("Workspace");
            window.minSize = new Vector2(300f, 200f);
            window.Repaint();
        }

        private void OnEnable()
        {
            if (_tabs.Count == 0)
                AddTab();
        }

        private void OnGUI()
        {
            DrawTabs();
            DrawToolbar();
            DrawContent();
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            for (int i = 0; i < _tabs.Count; i++)
            {
                WorkspaceTab tab = _tabs[i];
                bool isActive = i == _activeTabIndex;
                
                if(GUILayout.Toggle(isActive, tab.Name, EditorStyles.toolbarButton, GUILayout.Width(60f)))
                    _activeTabIndex = i;

                if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(22f)))
                {
                    CloseTab(i);
                    GUIUtility.ExitGUI();
                }
            }
            
            if(GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(28f)))
                AddTab();
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            using (new EditorGUI.DisabledScope(ActiveTab.Path == "Assets"))
            {
                if(GUILayout.Button("←", EditorStyles.toolbarButton, GUILayout.Width(28f)))
                    OpenParent();
            }
            
            GUILayout.Label(ActiveTab.Path, EditorStyles.miniLabel);
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawContent()
        {
            ActiveTab.ScrollPosition = EditorGUILayout.BeginScrollView(ActiveTab.ScrollPosition);

            DrawFolders();
            DrawAssets();
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawFolders()
        {
            string[] folders = AssetDatabase.GetSubFolders(ActiveTab.Path);

            foreach (string folderPath in folders)
            {
                string folderName = Path.GetFileName(folderPath);
                Texture icon = EditorGUIUtility.IconContent("Folder Icon").image;
                
                if(GUILayout.Button(new GUIContent(folderName, icon), EditorStyles.label, GUILayout.Height(22f)))
                    OpenFolder(folderPath);
            }
        }

        private void DrawAssets()
        {
            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { ActiveTab.Path });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                
                if(AssetDatabase.IsValidFolder(assetPath))
                    continue;
                
                string parentPath = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
                
                if(parentPath != ActiveTab.Path)
                    continue;

                DrawAsset(assetPath);
            }
        }

        private void DrawAsset(string assetPath)
        {
            string assetName = Path.GetFileName(assetPath);
            Texture icon = AssetDatabase.GetCachedIcon(assetPath);

            if (!GUILayout.Button(new GUIContent(assetName, icon), EditorStyles.label, GUILayout.Height(22f)))
                return;
            
            Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);

            if (!asset)
                return;
            
            Selection.activeObject = asset;
            
            if(Event.current.clickCount >= 2)
                AssetDatabase.OpenAsset(asset);
        }

        private void AddTab()
        {
            _tabs.Add(new WorkspaceTab());
            _activeTabIndex = _tabs.Count - 1;
        }

        private void CloseTab(int index)
        {
            _tabs.RemoveAt(index);
            
            if(_tabs.Count == 0)
                AddTab();
            
            if (_activeTabIndex >= _tabs.Count)
                _activeTabIndex = _tabs.Count - 1;
            else if (index < _activeTabIndex)
                _activeTabIndex--;
        }

        private void OpenFolder(string path)
        {
            ActiveTab.Path = path;
            ActiveTab.ScrollPosition = Vector2.zero;
            ActiveTab.Name = Path.GetFileName(path);
            Repaint();
        }

        private void OpenParent()
        {
            int separatorIndex = ActiveTab.Path.LastIndexOf('/');
            
            if(separatorIndex <= 0)
                return;
            
            OpenFolder(ActiveTab.Path.Substring(0, separatorIndex));
        }

        [Serializable]
        private class WorkspaceTab
        {
            [SerializeField] private string _name = "Assets";
            [SerializeField] private string _path = "Assets";
            [SerializeField] private Vector2 _scrollPosition;

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

            internal Vector2 ScrollPosition
            {
                get => _scrollPosition;
                set => _scrollPosition = value;
            }
        }
    }
}