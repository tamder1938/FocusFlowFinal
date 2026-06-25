using System;

namespace FocusFlowFinal.Services;

public interface ICurrentWorkspace
{
    string CurrentOwnerKey { get; }
    event EventHandler? WorkspaceChanged;
    void SetOwner(string ownerKey);
}
