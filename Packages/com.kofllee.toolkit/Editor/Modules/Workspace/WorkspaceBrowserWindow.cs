using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kofllee.Toolkit.Workspace
{
    internal class WorkspaceBrowserWindow : EditorWindow
    {
        [SerializeField] private List<WorkspaceTab> _tabs = new List<WorkspaceTab>();
        [SerializeField] private int _activeTabIndex;

        private bool _isEditingAddress;
        private bool _focusAddressField;

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

                if (GUILayout.Toggle(isActive, tab.Name, EditorStyles.toolbarButton, GUILayout.Width(80f)) && !isActive)
                    SetActiveTab(i);

                if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(22f)))
                {
                    CloseTab(i);
                    GUIUtility.ExitGUI();
                }
            }

            if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(28f)))
                AddTab();

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            using (new EditorGUI.DisabledScope(ActiveTab.BackHistory.Count == 0))
            {
                if (GUILayout.Button("←", EditorStyles.toolbarButton, GUILayout.Width(28f)))
                    NavigateBack();
            }

            using (new EditorGUI.DisabledScope(ActiveTab.ForwardHistory.Count == 0))
            {
                if (GUILayout.Button("→", EditorStyles.toolbarButton, GUILayout.Width(28f)))
                    NavigateForward();
            }

            using (new EditorGUI.DisabledScope(WorkspaceUtility.GetParent(ActiveTab.Path) == null))
            {
                if (GUILayout.Button("↑", EditorStyles.toolbarButton, GUILayout.Width(28f)))
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
            if (WorkspaceUtility.IsUnityPath(ActiveTab.Path))
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

            if (string.IsNullOrEmpty(root))
                return;

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

        private void DrawContent()
        {
            ActiveTab.ScrollPosition = EditorGUILayout.BeginScrollView(ActiveTab.ScrollPosition);

            try
            {
                List<WorkspaceItem> items = WorkspaceUtility.GetItems(ActiveTab.Path);

                foreach (WorkspaceItem item in items)
                    DrawItem(item);
            }
            catch (UnauthorizedAccessException)
            {
                EditorGUILayout.HelpBox("Access denied", MessageType.Warning);
            }
            catch (DirectoryNotFoundException)
            {
                EditorGUILayout.HelpBox("Folder not found", MessageType.Warning);
            }
            catch (IOException exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Warning);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawItem(WorkspaceItem item)
        {
            Texture icon = GetItemIcon(item);
            bool isSelected = string.Equals(ActiveTab.SelectedPath, item.Path, StringComparison.OrdinalIgnoreCase);
            GUIStyle style = isSelected ? EditorStyles.selectionRect : EditorStyles.label;
            
            Rect rect = EditorGUILayout.GetControlRect(false, 22f);

            if (Event.current.type == EventType.Repaint)
                style.Draw(rect, new GUIContent(item.Name, icon), false, false, isSelected, false);

            HandleItemInput(rect, item);
        }
        
        private void HandleItemInput(Rect rect, WorkspaceItem item)
        {
            Event currentEvent = Event.current;

            if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0 || !rect.Contains(currentEvent.mousePosition))
                return;

            SelectItem(item);

            if (currentEvent.clickCount >= 2)
                OpenItem(item);

            currentEvent.Use();
            Repaint();
        }
        
        private void SelectItem(WorkspaceItem item)
        {
            ActiveTab.SelectedPath = item.Path;

            if (!item.IsUnityAsset)
                return;

            Object asset = AssetDatabase.LoadMainAssetAtPath(item.Path);

            if (asset)
                Selection.activeObject = asset;
        }
        
        private void OpenItem(WorkspaceItem item)
        {
            if (item.IsFolder)
            {
                NavigateTo(item.Path);
                return;
            }

            if (item.IsUnityAsset)
            {
                Object asset = AssetDatabase.LoadMainAssetAtPath(item.Path);

                if (asset)
                    AssetDatabase.OpenAsset(asset);

                return;
            }

            EditorUtility.OpenWithDefaultApp(item.Path);
        }

        private Texture GetItemIcon(WorkspaceItem item)
        {
            if (item.IsFolder)
                return EditorGUIUtility.IconContent("Folder Icon").image;

            if (item.IsUnityAsset)
            {
                Texture icon = AssetDatabase.GetCachedIcon(item.Path);

                if (icon)
                    return icon;
            }

            return EditorGUIUtility.IconContent("DefaultAsset Icon").image;
        }

        private void SelectUnityAsset(WorkspaceItem item)
        {
            Object asset = AssetDatabase.LoadMainAssetAtPath(item.Path);

            if (!asset)
                return;

            Selection.activeObject = asset;

            if (Event.current.clickCount >= 2)
                AssetDatabase.OpenAsset(asset);
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
            _focusAddressField = false;
            GUI.FocusControl(null);
            Repaint();
        }

        private void SetActiveTab(int index)
        {
            _activeTabIndex = index;
            _isEditingAddress = false;
            _focusAddressField = false;
            GUI.FocusControl(null);
            Repaint();
        }

        private void AddTab()
        {
            _tabs.Add(new WorkspaceTab());
            _activeTabIndex = _tabs.Count - 1;
            _isEditingAddress = false;
            _focusAddressField = false;
        }

        private void CloseTab(int index)
        {
            _tabs.RemoveAt(index);

            if (_tabs.Count == 0)
                AddTab();

            if (_activeTabIndex >= _tabs.Count)
                _activeTabIndex = _tabs.Count - 1;
            else if (index < _activeTabIndex)
                _activeTabIndex--;

            _isEditingAddress = false;
            _focusAddressField = false;
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
            if (!WorkspaceUtility.TryResolvePath(ActiveTab.AddressInput, out string path))
            {
                ShowNotification(new GUIContent("Folder not found"));
                return;
            }

            NavigateTo(path);

            _isEditingAddress = false;
            _focusAddressField = false;

            GUI.FocusControl(null);
            Repaint();
        }

        private void OpenParent()
        {
            string parentPath = WorkspaceUtility.GetParent(ActiveTab.Path);

            if (parentPath != null)
                NavigateTo(parentPath);
        }

        private void SetPath(string path)
        {
            ActiveTab.Path = path;
            ActiveTab.AddressInput = path;
            ActiveTab.ScrollPosition = Vector2.zero;
            ActiveTab.Name = WorkspaceUtility.GetDisplayName(path);
            ActiveTab.SelectedPath = null;
            Repaint();
        }
    }
}