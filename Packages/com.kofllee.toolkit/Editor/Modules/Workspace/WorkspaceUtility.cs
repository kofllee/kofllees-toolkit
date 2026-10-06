using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Kofllee.Toolkit.Workspace
{
    internal static class WorkspaceUtility
    {
        internal static bool IsUnityPath(string path)
        {
            return string.Equals(path, "Assets", StringComparison.OrdinalIgnoreCase) || path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) || string.Equals(path, "Packages", StringComparison.OrdinalIgnoreCase) || path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool TryResolvePath(string path, out string resolvedPath)
        {
            path = NormalizeAddressPath(path);

            if (IsUnityPath(path))
                return TryResolveUnityPath(path, out resolvedPath);

            return TryResolveFilesystemPath(path, out resolvedPath);
        }

        internal static bool IsDriveRoot(string path)
        {
            return path.Length == 3 && char.IsLetter(path[0]) && path[1] == ':' && path[2] == '/';
        }

        internal static string NormalizeAddressPath(string path)
        {
            path = path.Trim().Trim('"').Replace('\\', '/');

            if (!IsDriveRoot(path))
                path = path.TrimEnd('/');

            return path;
        }

        internal static string GetParent(string path)
        {
            if (IsUnityPath(path))
            {
                if (string.Equals(path, "Assets", StringComparison.OrdinalIgnoreCase) || string.Equals(path, "Packages", StringComparison.OrdinalIgnoreCase))
                    return null;

                int separatorIndex = path.LastIndexOf('/');

                return separatorIndex > 0 ? path.Substring(0, separatorIndex) : null;
            }

            DirectoryInfo parent = Directory.GetParent(path);

            return parent?.FullName.Replace('\\', '/');
        }

        internal static string GetDisplayName(string path)
        {
            if (string.Equals(path, "Assets", StringComparison.OrdinalIgnoreCase) || string.Equals(path, "Packages", StringComparison.OrdinalIgnoreCase))
                return path;

            string normalizedPath = path.TrimEnd('/', '\\');
            string name = Path.GetFileName(normalizedPath);

            return string.IsNullOrEmpty(name) ? path : name;
        }

        internal static List<WorkspaceItem> GetItems(string path)
        {
            List<WorkspaceItem> items = IsUnityPath(path) ? GetUnityItems(path) : GetFilesystemItems(path);

            items.Sort(CompareItems);

            return items;
        }

        private static bool TryResolveUnityPath(string path, out string resolvedPath)
        {
            string[] parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
            {
                resolvedPath = null;
                return false;
            }

            if (string.Equals(parts[0], "Assets", StringComparison.OrdinalIgnoreCase))
                resolvedPath = "Assets";
            else if (string.Equals(parts[0], "Packages", StringComparison.OrdinalIgnoreCase))
                resolvedPath = "Packages";
            else
            {
                resolvedPath = null;
                return false;
            }

            for (int i = 1; i < parts.Length; i++)
            {
                if (resolvedPath == "Packages")
                {
                    if (!TryResolvePackage(parts[i], out resolvedPath))
                        return false;

                    continue;
                }

                string[] folders = AssetDatabase.GetSubFolders(resolvedPath);
                string matchingPath = null;

                foreach (string folderPath in folders)
                {
                    string folderName = Path.GetFileName(folderPath);

                    if (!string.Equals(folderName, parts[i], StringComparison.OrdinalIgnoreCase))
                        continue;

                    matchingPath = folderPath;
                    break;
                }

                if (matchingPath == null)
                    return false;

                resolvedPath = matchingPath;
            }

            return true;
        }

        private static bool TryResolvePackage(string packageName, out string resolvedPath)
        {
            PackageInfo[] packages = PackageInfo.GetAllRegisteredPackages();

            foreach (PackageInfo package in packages)
            {
                if (string.IsNullOrEmpty(package.assetPath))
                    continue;

                string name = Path.GetFileName(package.assetPath);

                if (!string.Equals(name, packageName, StringComparison.OrdinalIgnoreCase))
                    continue;

                resolvedPath = package.assetPath;
                return true;
            }

            resolvedPath = null;
            return false;
        }

        private static bool TryResolveFilesystemPath(string path, out string resolvedPath)
        {
            resolvedPath = null;

            try
            {
                string fullPath = Path.GetFullPath(path);
                string rootPath = Path.GetPathRoot(fullPath);

                if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
                    return false;

                DirectoryInfo currentDirectory = new DirectoryInfo(rootPath);
                string relativePath = fullPath.Substring(rootPath.Length);
                string[] parts = relativePath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string part in parts)
                {
                    DirectoryInfo matchingDirectory = FindDirectory(currentDirectory, part);

                    if (matchingDirectory == null)
                        return false;

                    currentDirectory = matchingDirectory;
                }

                resolvedPath = currentDirectory.FullName.Replace('\\', '/');

                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                return false;
            }
        }

        private static DirectoryInfo FindDirectory(DirectoryInfo parent, string name)
        {
            foreach (DirectoryInfo directory in parent.EnumerateDirectories())
            {
                if (string.Equals(directory.Name, name, StringComparison.OrdinalIgnoreCase))
                    return directory;
            }

            return null;
        }

        private static List<WorkspaceItem> GetUnityItems(string path)
        {
            if (string.Equals(path, "Packages", StringComparison.OrdinalIgnoreCase))
                return GetPackages();

            List<WorkspaceItem> items = new List<WorkspaceItem>();
            string[] folders = AssetDatabase.GetSubFolders(path);

            foreach (string folderPath in folders)
                items.Add(new WorkspaceItem(Path.GetFileName(folderPath), folderPath, true, true));

            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { path });

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath))
                    continue;

                string parentPath = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');

                if (!string.Equals(parentPath, path, StringComparison.OrdinalIgnoreCase))
                    continue;

                items.Add(new WorkspaceItem(Path.GetFileName(assetPath), assetPath, false, true));
            }

            return items;
        }

        private static List<WorkspaceItem> GetPackages()
        {
            List<WorkspaceItem> items = new List<WorkspaceItem>();
            PackageInfo[] packages = PackageInfo.GetAllRegisteredPackages();

            foreach (PackageInfo package in packages)
            {
                if (string.IsNullOrEmpty(package.assetPath))
                    continue;

                items.Add(new WorkspaceItem(package.displayName, package.assetPath, true, true));
            }

            return items;
        }

        private static List<WorkspaceItem> GetFilesystemItems(string path)
        {
            List<WorkspaceItem> items = new List<WorkspaceItem>();

            foreach (string folderPath in Directory.EnumerateDirectories(path))
                items.Add(new WorkspaceItem(Path.GetFileName(folderPath), folderPath.Replace('\\', '/'), true, false));

            foreach (string filePath in Directory.EnumerateFiles(path))
            {
                if (Path.GetExtension(filePath).Equals(".meta", StringComparison.OrdinalIgnoreCase))
                    continue;

                items.Add(new WorkspaceItem(Path.GetFileName(filePath), filePath.Replace('\\', '/'), false, false));
            }

            return items;
        }

        private static int CompareItems(WorkspaceItem first, WorkspaceItem second)
        {
            if (first.IsFolder != second.IsFolder)
                return first.IsFolder ? -1 : 1;

            return string.Compare(first.Name, second.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}