using System;

namespace FocusFlowFinal.Services;

public sealed class CurrentWorkspaceService : ICurrentWorkspace
{
    public const string LocalOwner = "local";

    private string _ownerKey = LocalOwner;
    public string CurrentOwnerKey => _ownerKey;
    public event EventHandler? WorkspaceChanged;

    public void SetOwner(string ownerKey)
    {
        if (_ownerKey == ownerKey) return;
        _ownerKey = ownerKey;
        WorkspaceChanged?.Invoke(this, EventArgs.Empty);
    }
}
