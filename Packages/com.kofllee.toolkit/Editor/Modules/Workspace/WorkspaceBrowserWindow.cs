using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;
using Object = UnityEngine.Object;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Kofllee.Toolkit.Editor.Modules.Workspace
{
    internal class WorkspaceBrowserWindow : EditorWindow
    {
        [SerializeField] private List<WorkspaceTab> _tabs = new List<WorkspaceTab>();
        [SerializeField] private int _activeTabIndex;
        
        private WorkspaceTab ActiveTab => _tabs[_activeTabIndex];
        
        private bool _isEditingAddress;
        private bool _focusAddressField;

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
            
            _activeTabIndex = Mathf.Clamp(_activeTabIndex, 0, _tabs.Count - 1);

            foreach (WorkspaceTab tab in _tabs)
                tab.AddressInput = tab.Path;
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

            using (new EditorGUI.DisabledScope(ActiveTab.BackHistory.Count == 0))
            {
                if(GUILayout.Button("←", EditorStyles.toolbarButton, GUILayout.Width(28f)))
                    NavigateBack();
            }
            
            using (new EditorGUI.DisabledScope(ActiveTab.ForwardHistory.Count == 0))
            {
                if(GUILayout.Button("→", EditorStyles.toolbarButton, GUILayout.Width(28f)))
                    NavigateForward();
            }
            
            using (new EditorGUI.DisabledScope(!CanOpenParent()))
            {
                if(GUILayout.Button("↑", EditorStyles.toolbarButton, GUILayout.Width(28f)))
                    OpenParent();
            }
            
            if (_isEditingAddress)
                DrawAddressInput();
            else
                DrawBreadcrumbs();
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAddressInput()
        {
            Event currentEvent = Event.current;

            bool submit = currentEvent.type == EventType.KeyDown && currentEvent.keyCode is KeyCode.Return or KeyCode.KeypadEnter;
            bool cancel = currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Escape;

            GUI.SetNextControlName("WorkspaceAddress");

            ActiveTab.AddressInput = EditorGUILayout.TextField(ActiveTab.AddressInput, EditorStyles.toolbarTextField, GUILayout.ExpandWidth(true));

            if (_focusAddressField)
            {
                EditorGUI.FocusTextInControl("WorkspaceAddress");
                _focusAddressField = false;
            }

            if (submit)
            {
                NavigateFromAddress();
                currentEvent.Use();
                return;
            }

            if (cancel)
            {
                EndAddressEditing();
                currentEvent.Use();
            }
        }

        private void DrawBreadcrumbs()
        {
            if (IsUnityPath(ActiveTab.Path))
                DrawUnityBreadcrumbs();
            else
                DrawFilesystemBreadcrumbs();

            if (GUILayout.Button(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(18f)))
                BeginAddressEditing();
        }
        
        private void DrawUnityBreadcrumbs()
        {
            string[] parts = ActiveTab.Path.Split('/');
            string path = string.Empty;

            for (int i = 0; i < parts.Length; i++)
            {
                path = i == 0 ? parts[i] : path + "/" + parts[i];
                DrawBreadcrumb(parts[i], path, i > 0);
            }
        }

        private void DrawFilesystemBreadcrumbs()
        {
            string normalizedPath = ActiveTab.Path.Replace('\\', '/');
            string root = Path.GetPathRoot(normalizedPath)?.Replace('\\', '/');
            string relativePath = normalizedPath.Substring(root.Length);
            string[] parts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            string path = root.TrimEnd('/');

            DrawBreadcrumb(root, root, false);

            foreach (string part in parts)
            {
                path += "/" + part;
                DrawBreadcrumb(part, path, true);
            }
        }

        private void DrawBreadcrumb(string label, string path, bool showSeparator)
        {
            if (showSeparator)
                GUILayout.Label(">", EditorStyles.miniLabel, GUILayout.Width(10f));

            if (GUILayout.Button(label, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                NavigateTo(path);
        }
        
        private void BeginAddressEditing()
        {
            ActiveTab.AddressInput = ActiveTab.Path;
            _isEditingAddress = true;
            _focusAddressField = true;
            Repaint();
        }

        private void EndAddressEditing()
        {
            ActiveTab.AddressInput = ActiveTab.Path;
            _isEditingAddress = false;
            GUI.FocusControl(null);
            Repaint();
        }
        
        private void DrawContent()
        {
            ActiveTab.ScrollPosition = EditorGUILayout.BeginScrollView(ActiveTab.ScrollPosition);

            if (IsUnityPath(ActiveTab.Path))
                DrawUnityContent();
            else
                DrawFilesystemContent();

            EditorGUILayout.EndScrollView();
        }
        
        private void DrawUnityContent()
        {
            DrawFolders();
            DrawAssets();
        }

        private void DrawFilesystemContent()
        {
            try
            {
                DrawFilesystemFolders();
                DrawFilesystemFiles();
            }
            catch (UnauthorizedAccessException)
            {
                EditorGUILayout.HelpBox("Access denied", MessageType.Warning);
            }
            catch (DirectoryNotFoundException)
            {
                EditorGUILayout.HelpBox("Folder not found", MessageType.Warning);
            }
        }
        
        private void DrawFilesystemFolders()
        {
            foreach (string folderPath in Directory.EnumerateDirectories(ActiveTab.Path))
            {
                string folderName = Path.GetFileName(folderPath);
                Texture icon = EditorGUIUtility.IconContent("Folder Icon").image;

                if (GUILayout.Button(new GUIContent(folderName, icon), EditorStyles.label, GUILayout.Height(22f)))
                    NavigateTo(folderPath.Replace('\\', '/'));
            }
        }
        
        private void DrawFilesystemFiles()
        {
            foreach (string filePath in Directory.EnumerateFiles(ActiveTab.Path))
            {
                if (Path.GetExtension(filePath).Equals(".meta", StringComparison.OrdinalIgnoreCase))
                    continue;

                string fileName = Path.GetFileName(filePath);
                Texture icon = EditorGUIUtility.IconContent("DefaultAsset Icon").image;

                if (!GUILayout.Button(new GUIContent(fileName, icon), EditorStyles.label, GUILayout.Height(22f)))
                    continue;

                if (Event.current.clickCount >= 2)
                    EditorUtility.OpenWithDefaultApp(filePath);
            }
        }

        private void DrawFolders()
        {
            if (ActiveTab.Path == "Packages")
            {
                DrawPackages();
                return;
            }
            
            string[] folders = AssetDatabase.GetSubFolders(ActiveTab.Path);

            foreach (string folderPath in folders)
            {
                string folderName = Path.GetFileName(folderPath);
                Texture icon = EditorGUIUtility.IconContent("Folder Icon").image;
                
                if(GUILayout.Button(new GUIContent(folderName, icon), EditorStyles.label, GUILayout.Height(22f)))
                    NavigateTo(folderPath);
            }
        }

        private void DrawPackages()
        {
            PackageInfo[] packages = PackageInfo.GetAllRegisteredPackages();

            foreach (PackageInfo package in packages)
            {
                if(string.IsNullOrEmpty(package.assetPath))
                    continue;
                
                Texture icon = EditorGUIUtility.IconContent("Folder Icon").image;
                
                if (GUILayout.Button(new GUIContent(package.displayName, icon), EditorStyles.label, GUILayout.Height(22f)))
                    NavigateTo(package.assetPath);
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

        private void NavigateTo(string path)
        {
            if (path == ActiveTab.Path)
                return;
            
            ActiveTab.BackHistory.Add(ActiveTab.Path);
            ActiveTab.ForwardHistory.Clear();
            
            SetPath(path);
        }

        private void NavigateBack()
        {
            if (ActiveTab.BackHistory.Count == 0)
                return;
            
            int lastIndex = ActiveTab.BackHistory.Count - 1;
            string path = ActiveTab.BackHistory[lastIndex];
            
            ActiveTab.BackHistory.RemoveAt(lastIndex);
            ActiveTab.ForwardHistory.Add(ActiveTab.Path);
            
            SetPath(path);
        }

        private void NavigateForward()
        {
            if (ActiveTab.ForwardHistory.Count == 0)
                return;
            
            int lastIndex = ActiveTab.ForwardHistory.Count - 1;
            string path = ActiveTab.ForwardHistory[lastIndex];
            
            ActiveTab.ForwardHistory.RemoveAt(lastIndex);
            ActiveTab.BackHistory.Add(ActiveTab.Path);
            
            SetPath(path);
        }

        private void NavigateFromAddress()
        {
            string path = ActiveTab.AddressInput.Trim().Replace('\\', '/');

            if (!IsDriveRoot(path))
                path = path.TrimEnd('/');

            if (!IsValidPath(path))
            {
                ShowNotification(new GUIContent("Folder not found"));
                return;
            }

            NavigateTo(path);
            _isEditingAddress = false;
            GUI.FocusControl(null);
            Repaint();
        }
        
        private bool IsDriveRoot(string path)
        {
            return path.Length == 3 && char.IsLetter(path[0]) && path[1] == ':' && path[2] == '/';
        }
        
        private bool IsValidPath(string path)
        {
            if (IsUnityPath(path))
                return path is "Assets" or "Packages" || AssetDatabase.IsValidFolder(path);

            return Directory.Exists(path);
        }
        
        private bool IsUnityPath(string path)
        {
            return path == "Assets" || path.StartsWith("Assets/") || path == "Packages" || path.StartsWith("Packages/");
        }
        
        private bool CanOpenParent()
        {
            if (IsUnityPath(ActiveTab.Path))
                return ActiveTab.Path is not "Assets" and not "Packages";

            return Directory.GetParent(ActiveTab.Path) != null;
        }
        
        private void OpenParent()
        {
            if (IsUnityPath(ActiveTab.Path))
            {
                int separatorIndex = ActiveTab.Path.LastIndexOf('/');

                if (separatorIndex <= 0)
                    return;

                NavigateTo(ActiveTab.Path.Substring(0, separatorIndex));
                return;
            }

            DirectoryInfo parent = Directory.GetParent(ActiveTab.Path);

            if (parent != null)
                NavigateTo(parent.FullName.Replace('\\', '/'));
        }

        private void SetPath(string path)
        {
            ActiveTab.Path = path;
            ActiveTab.AddressInput = path;
            ActiveTab.ScrollPosition = Vector2.zero;
            ActiveTab.Name = Path.GetFileName(path);
            Repaint();
        }

        [Serializable]
        private class WorkspaceTab
        {
            [SerializeField] private string _name = "Assets";
            [SerializeField] private string _path = "Assets";
            [SerializeField] private Vector2 _scrollPosition;
            [SerializeField] private string _addressInput = "Assets";
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

            internal Vector2 ScrollPosition
            {
                get => _scrollPosition;
                set => _scrollPosition = value;
            }

            internal string AddressInput
            {
                get => _addressInput;
                set => _addressInput = value;
            }
            
            internal List<string> BackHistory => _backHistory;
            internal List<string> ForwardHistory => _forwardHistory;
        }
    }
}