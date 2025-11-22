using System;
using UnityEngine;

namespace AlinasMapMod.Validation;

/// <summary>
/// Interface for builder transactions that track objects for cleanup on failure
/// </summary>
public interface IBuilderTransaction
{
    /// <summary>
    /// Sets the root GameObject to track for cleanup if the transaction fails
    /// </summary>
    void SetRootGameObject(GameObject gameObject);

    /// <summary>
    /// Adds a custom cleanup action to be executed if the transaction fails
    /// </summary>
    void SetCleanupAction(Action cleanupAction);

    /// <summary>
    /// Commits the transaction, preventing cleanup of tracked objects
    /// </summary>
    void Commit();

    /// <summary>
    /// Rolls back the transaction, destroying tracked objects and executing cleanup actions
    /// </summary>
    void Rollback();
}