namespace BlazorBasics.Opfs.Models;

/// <summary>How a lock is shared.</summary>
public enum OpfsLockMode
{
    /// <summary>Only one holder.</summary>
    Exclusive,

    /// <summary>Many holders, and no exclusive one.</summary>
    Shared
}
