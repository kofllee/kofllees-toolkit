using System.Reflection;
using UnityEditor;

namespace Kofllee.Toolkit.GlobalLock
{
    public static class GlobalLockUtility
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;
        
        internal static void ToggleFocusedWindowLock()
        {
            EditorWindow window = EditorWindow.focusedWindow;
            
            if(!window)
                return;
            
            if(!TryToggleLock(window))
                return;
            
            window.Repaint();
        }

        private static bool TryToggleLock(EditorWindow window)
        {
            var type = window.GetType();
            
            PropertyInfo isLockedProperty = type.GetProperty("isLocked", InstanceFlags);
            
            if(isLockedProperty == null || isLockedProperty.PropertyType != typeof(bool) || !isLockedProperty.CanRead || !isLockedProperty.CanWrite)
                return false;
            
            bool currentValue = (bool)isLockedProperty.GetValue(window);
            isLockedProperty.SetValue(window, !currentValue);
            
            return true;
        }
    }
}