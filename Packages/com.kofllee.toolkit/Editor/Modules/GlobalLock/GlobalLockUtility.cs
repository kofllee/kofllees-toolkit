using System;
using System.Reflection;
using UnityEditor;

namespace Kofllee.Toolkit.GlobalLock
{
    internal static class GlobalLockUtility
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;
        
        private const BindingFlags StaticFlags =
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        internal static void ToggleFocusedWindowLock()
        {
            EditorWindow window = EditorWindow.focusedWindow;

            if (!window)
                return;

            if (!TryToggleDirectLock(window) && !TryToggleHierarchyLock(window) && !TryToggleAnimationLock(window) && !TryToggleParticleSystemLock(window))
                return;

            window.Repaint();
        }

        private static bool TryToggleDirectLock(EditorWindow window)
        {
            return TryToggleLockObject(window);
        }

        private static bool TryToggleHierarchyLock(EditorWindow window)
        {
            PropertyInfo sceneHierarchyProperty = window.GetType().GetProperty("sceneHierarchy", InstanceFlags);

            if (sceneHierarchyProperty == null)
                return false;

            object sceneHierarchy = sceneHierarchyProperty.GetValue(window);

            return TryToggleLockObject(sceneHierarchy);
        }
        
        private static bool TryToggleAnimationLock(EditorWindow window)
        {
            if (window.GetType().Name != "AnimationWindow")
                return false;

            FieldInfo trackerField = window.GetType().GetField("m_LockTracker", InstanceFlags);

            if (trackerField == null)
                return false;

            object tracker = trackerField.GetValue(window);

            if (!TryToggleLockObject(tracker))
                return false;

            MethodInfo selectionChangedMethod = window.GetType().GetMethod(
                    "OnSelectionChangeInternal",
                    InstanceFlags,
                    null,
                    new[] { typeof(bool) },
                    null);

            selectionChangedMethod?.Invoke(window, new object[] { false });

            return true;
        }
        
        private static bool TryToggleParticleSystemLock(EditorWindow window)
        {
            if (window.GetType().Name != "ParticleSystemWindow")
                return false;

            Type utilsType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ParticleSystemEditorUtils");

            if (utilsType == null)
                return false;

            PropertyInfo lockedProperty = utilsType.GetProperty("lockedParticleSystem", StaticFlags);

            FieldInfo targetField =
                window.GetType().GetField("m_Target", InstanceFlags);

            if (lockedProperty == null || targetField == null)
                return false;

            object locked = lockedProperty.GetValue(null);

            if (locked != null)
            {
                lockedProperty.SetValue(null, null);
            }
            else
            {
                object target = targetField.GetValue(window);

                if (target == null)
                    return false;

                lockedProperty.SetValue(null, target);
            }

            return true;
        }
        
        private static bool TryToggleLockObject(object target)
        {
            if (target == null)
                return false;

            PropertyInfo isLockedProperty = target.GetType().GetProperty("isLocked", InstanceFlags);

            if (!IsValidLockProperty(isLockedProperty))
                return false;

            bool currentValue = (bool)isLockedProperty.GetValue(target);
            isLockedProperty.SetValue(target, !currentValue);

            return true;
        }
        
        private static bool IsValidLockProperty(PropertyInfo property)
        {
            return property != null &&
                   property.PropertyType == typeof(bool) &&
                   property.CanRead &&
                   property.CanWrite;
        }
    }
}