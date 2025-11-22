using System;
using UnityEngine;

namespace AlinasMapMod.Validation;

/// <summary>
/// Helper class for tracking a root GameObject during building and providing cleanup on failure
/// </summary>
public class BuilderTransaction : IBuilderTransaction
{
    private GameObject _rootGameObject;
    private Action _cleanupAction;
    private bool _committed = false;

    /// <summary>
    /// Sets the root GameObject to track for cleanup if the transaction fails
    /// </summary>
    public void SetRootGameObject(GameObject gameObject)
    {
        _rootGameObject = gameObject;
    }

    /// <summary>
    /// Adds a custom cleanup action to be executed if the transaction fails
    /// </summary>
    public void SetCleanupAction(Action cleanupAction)
    {
        _cleanupAction = cleanupAction;
    }

    /// <summary>
    /// Commits the transaction, preventing cleanup of the root object
    /// </summary>
    public void Commit()
    {
        _committed = true;
    }

    /// <summary>
    /// Rolls back the transaction, destroying the root GameObject and executing cleanup action
    /// </summary>
    public void Rollback()
    {
        if (_committed) return;

        // Execute custom cleanup action first
        if (_cleanupAction != null)
        {
            try
            {
                _cleanupAction();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during cleanup action: {ex.Message}");
            }
        }

        // Destroy root GameObject (children will be destroyed automatically)
        if (_rootGameObject != null)
        {
            try
            {
                UnityEngine.Object.DestroyImmediate(_rootGameObject);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error destroying root GameObject {_rootGameObject.name}: {ex.Message}");
            }
        }
    }
}