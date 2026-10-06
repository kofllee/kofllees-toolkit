namespace Kofllee.Toolkit.Workspace
{
    internal readonly struct WorkspaceItem
    {
        internal string Name { get; }
        internal string Path { get; }
        internal bool IsFolder { get; }
        internal bool IsUnityAsset { get; }

        internal WorkspaceItem(string name, string path, bool isFolder, bool isUnityAsset)
        {
            Name = name;
            Path = path;
            IsFolder = isFolder;
            IsUnityAsset = isUnityAsset;
        }
    }
}