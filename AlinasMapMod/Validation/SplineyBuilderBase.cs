using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Serilog;
using StrangeCustoms;
using StrangeCustoms.Tracks;
using UnityEngine;
using AlinasMapMod.MapEditor;
using AlinasMapMod.Caches;
using AlinasMapMod.Definitions;
using AlinasMapMod.Stations;
using AlinasMapMod.Loaders;

namespace AlinasMapMod.Validation;

/// <summary>
/// Abstract base class for spliney builders that provides standardized patterns for
/// logging, error handling, validation, and object creation
/// </summary>
public abstract class SplineyBuilderBase : StrangeCustoms.ISplineyBuilder
{
    /// <summary>
    /// Standardized logger instance for all builders
    /// </summary>
    protected static readonly Serilog.ILogger Logger = Log.ForContext<SplineyBuilderBase>();
        
        
        
    /// <summary>
    /// Main spliney building method - safely executes the build process with standardized error handling
    /// </summary>
    public GameObject BuildSpliney(string id, Transform parentTransform, JObject data)
    {
        return SafeBuildSpliney(id, parentTransform, data, () => BuildSplineyInternal(id, parentTransform, data));
    }
        
        
    /// <summary>
    /// Internal build method to be implemented by derived classes
    /// </summary>
    protected abstract GameObject BuildSplineyInternal(string id, Transform parentTransform, JObject data);
        
    /// <summary>
    /// Validates input parameters before building
    /// </summary>
    protected virtual void ValidateInput(string id, JObject data)
    {
        if (string.IsNullOrEmpty(id))
            throw new ValidationException("ID cannot be null or empty");
                
        if (data == null)
            throw new ValidationException("Data cannot be null");
    }
        
    /// <summary>
    /// Creates a GameObject with standardized naming and parent management
    /// </summary>
    protected virtual GameObject CreateGameObject(string id, Transform parentTransform, string objectTypeName = null)
    {
        var gameObject = new GameObject(id);
            
        if (parentTransform != null)
        {
            gameObject.transform.SetParent(parentTransform);
        }
        else
        {
            // Use standard parent management from Utils if available
            var parent = Utils.GetParent(objectTypeName ?? "Unknown");
            if (parent != null)
                gameObject.transform.SetParent(parent.transform);
        }
            
        return gameObject;
    }
        
    /// <summary>
    /// Safely executes the build process with standardized error handling and logging
    /// </summary>
    protected GameObject SafeBuildSpliney(string id, Transform parentTransform, JObject data, Func<GameObject> buildAction)
    {
        try
        {
            ValidateInput(id, data);
                
            Logger.Information("Building {BuilderType} with ID {Id}", GetType().Name, id);
                
            var result = buildAction();
                
            Logger.Information("Successfully built {BuilderType} with ID {Id}", GetType().Name, id);
            return result;
        }
        catch (ValidationException ex)
        {
            Logger.Error(ex, "Validation failed for {BuilderType} {Id}", GetType().Name, id);
            throw new ValidationException($"Validation failed for {GetType().Name} {id}: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to create {BuilderType} {Id}", GetType().Name, id);
            throw new InvalidOperationException($"Failed to create {GetType().Name} {id}", ex);
        }
    }

    /// <summary>
    /// Safely executes the build process with cleanup on failure
    /// </summary>
    protected GameObject SafeBuildSplineyWithCleanup(string id, Transform parentTransform, JObject data, Func<IBuilderTransaction, GameObject> buildAction)
    {
        var transaction = new BuilderTransaction();
        try
        {
            ValidateInput(id, data);
                
            Logger.Information("Building {BuilderType} with ID {Id}", GetType().Name, id);
                
            var result = buildAction(transaction);
            transaction.Commit();
                
            Logger.Information("Successfully built {BuilderType} with ID {Id}", GetType().Name, id);
            return result;
        }
        catch (ValidationException ex)
        {
            transaction.Rollback();
            Logger.Error(ex, "Validation failed for {BuilderType} {Id}", GetType().Name, id);
            throw new ValidationException($"Validation failed for {GetType().Name} {id}: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Logger.Error(ex, "Failed to create {BuilderType} {Id}", GetType().Name, id);
            throw new InvalidOperationException($"Failed to create {GetType().Name} {id}", ex);
        }
    }
        
    /// <summary>
    /// Helper method to deserialize and validate data objects
    /// </summary>
    protected TData DeserializeAndValidate<TData>(JObject data) where TData : IValidatable
    {
        var deserializedData = data.ToObject<TData>();
        if (deserializedData == null)
            throw new ValidationException($"Failed to deserialize data as {typeof(TData).Name}");
                
        // Use rich validation if available for better error reporting
        var validationResult = deserializedData.ValidateWithDetails();
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                Logger.Error("Validation error in {Field}: {Message}", error.Field, error.Message);
            }
        }
            
        // Log warnings but continue
        foreach (var warning in validationResult.Warnings)
        {
            Logger.Warning("Validation warning in {Field}: {Message}", warning.Field, warning.Message);
        }
            
        // Throw if validation failed
        validationResult.ThrowIfInvalid();
            
        return deserializedData;
    }
        
    /// <summary>
    /// Finds an existing component by ID using caches for efficiency
    /// </summary>
    protected virtual TComponent FindExistingComponent<TComponent>(string id) where TComponent : Component
    {
        // Try known caches first for efficiency
        if (typeof(TComponent) == typeof(PaxStationAgent))
        {
            StationAgentCache.Instance.TryGetValue(id, out var stationAgent);
            return stationAgent as TComponent;
        }
            
        if (typeof(TComponent) == typeof(LoaderInstance))
        {
            LoaderCache.Instance.TryGetValue(id, out var loader);
            return loader as TComponent;
        }
            
        // Fallback to GameObject.FindObjectsOfType for types without caches
        return GameObject.FindObjectsOfType<TComponent>(true)
            .FirstOrDefault(c => GetComponentId(c) == id);
    }
        
    /// <summary>
    /// Gets the ID from a component using common patterns
    /// </summary>
    protected virtual string GetComponentId(Component component)
    {
        // Try common identifier patterns
        var identifiableProperty = component.GetType().GetProperty("identifier");
        if (identifiableProperty != null)
            return identifiableProperty.GetValue(component)?.ToString();
                
        var idProperty = component.GetType().GetProperty("Id");
        if (idProperty != null)
            return idProperty.GetValue(component)?.ToString();
                
        // Some components might use different patterns
        return component.name;
    }
        
    /// <summary>
    /// Builds a GameObject from a serialized component that implements ICreatableComponent
    /// </summary>
    protected virtual GameObject BuildFromCreatableComponent<TComponent, TSerialized>(string id, JObject data)
        where TComponent : Component
        where TSerialized : ICreatableComponent<TComponent>, IValidatable
    {
        var serialized = DeserializeAndValidate<TSerialized>(data);
        Logger.Information("Creating {ObjectType} {Id} using serialized component", typeof(TComponent).Name, id);
        return serialized.Create(id).gameObject;
    }
        
    /// <summary>
    /// Helper method to get or add a component to a GameObject
    /// </summary>
    protected virtual TComponent GetOrAddComponent<TComponent>(GameObject gameObject) where TComponent : Component
    {
        return gameObject.GetComponent<TComponent>() ?? gameObject.AddComponent<TComponent>();
    }
        
    /// <summary>
    /// Safely configures a GameObject by disabling it during configuration
    /// </summary>
    protected virtual void ConfigureWithActivation(GameObject gameObject, System.Action configureAction)
    {
        var wasActive = gameObject.activeSelf;
        gameObject.SetActive(false);
        try
        {
            configureAction();
        }
        finally
        {
            gameObject.SetActive(wasActive);
        }
    }
}