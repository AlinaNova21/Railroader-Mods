using System.Reflection;
using Arch.Core;

namespace AlinasRailTools.Editor.ECS.DI;

/// <summary>
/// Lightweight IoC container for ECS systems
/// Handles dependency injection, initialization ordering via topological sort,
/// and automatic detection of system dependencies through reflection
/// </summary>
public class SystemContainer
{
    private readonly Dictionary<Type, object> _singletons = new();
    private readonly Dictionary<Type, Type> _systemRegistrations = new();
    private readonly Dictionary<Type, HashSet<Type>> _dependencies = new();
    private readonly World _world;

    public SystemContainer(World world)
    {
        _world = world;
        // Register world as a singleton service
        RegisterSingleton(world);
    }

    /// <summary>
    /// Register a singleton service (non-system)
    /// Supports both reference types and value types (structs)
    /// </summary>
    public void RegisterSingleton<T>(T instance)
    {
        _singletons[typeof(T)] = instance!;
    }

    /// <summary>
    /// Register a system with its interface
    /// Automatically detects dependencies through reflection
    /// </summary>
    public void RegisterSystem<TInterface, TImplementation>()
        where TInterface : ISystem
        where TImplementation : TInterface
    {
        var interfaceType = typeof(TInterface);
        var implType = typeof(TImplementation);

        _systemRegistrations[interfaceType] = implType;
        _dependencies[interfaceType] = new HashSet<Type>();

        // Use reflection to detect dependencies in constructor
        var constructors = implType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        if (constructors.Length == 0)
        {
            throw new InvalidOperationException($"System {implType.Name} must have at least one public constructor");
        }

        // Use the first public constructor
        var constructor = constructors[0];
        foreach (var param in constructor.GetParameters())
        {
            var paramType = param.ParameterType;

            // Check if parameter is an ISystem-derived interface (system dependency)
            if (typeof(ISystem).IsAssignableFrom(paramType) && paramType.IsInterface)
            {
                _dependencies[interfaceType].Add(paramType);
                Console.WriteLine($"[SystemContainer] Detected dependency: {interfaceType.Name} -> {paramType.Name}");
            }
        }
    }

    /// <summary>
    /// Build the SystemManager with all systems instantiated in correct order
    /// Uses topological sort to determine initialization order
    /// </summary>
    public SystemManager Build()
    {
        Console.WriteLine("[SystemContainer] Building system dependency graph...");

        // Perform topological sort to get initialization order
        var sortedSystems = TopologicalSort();

        Console.WriteLine($"[SystemContainer] System initialization order:");
        for (int i = 0; i < sortedSystems.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. {sortedSystems[i].Name}");
        }

        // Instantiate systems in dependency order
        var systems = new List<ISystem>();
        foreach (var systemInterface in sortedSystems)
        {
            var system = CreateSystemInstance(systemInterface);
            systems.Add(system);

            // Register as singleton for dependency injection
            _singletons[systemInterface] = system;
        }

        Console.WriteLine($"[SystemContainer] Successfully initialized {systems.Count} systems");
        return new SystemManager(_world, systems);
    }

    /// <summary>
    /// Create an instance of a system, injecting all dependencies
    /// </summary>
    private ISystem CreateSystemInstance(Type systemInterface)
    {
        if (!_systemRegistrations.TryGetValue(systemInterface, out var implType))
        {
            throw new InvalidOperationException($"System interface {systemInterface.Name} is not registered");
        }

        var constructor = implType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0];
        var parameters = constructor.GetParameters();
        var args = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;

            // Try to resolve from singletons
            if (_singletons.TryGetValue(paramType, out var service))
            {
                args[i] = service;
            }
            else if (parameters[i].HasDefaultValue)
            {
                // Use default value if parameter is optional
                args[i] = parameters[i].DefaultValue!;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot resolve dependency {paramType.Name} for system {implType.Name}. " +
                    $"Make sure it's registered as a service or has a default value.");
            }
        }

        var instance = (ISystem)constructor.Invoke(args);
        Console.WriteLine($"[SystemContainer] Instantiated {systemInterface.Name} ({implType.Name})");
        return instance;
    }

    /// <summary>
    /// Perform topological sort on the dependency graph
    /// Returns systems in initialization order (dependencies before dependents)
    /// Throws if circular dependencies are detected
    /// </summary>
    private List<Type> TopologicalSort()
    {
        var sorted = new List<Type>();
        var visited = new HashSet<Type>();
        var visiting = new HashSet<Type>();

        foreach (var system in _systemRegistrations.Keys)
        {
            if (!visited.Contains(system))
            {
                TopologicalSortVisit(system, visited, visiting, sorted);
            }
        }

        return sorted;
    }

    /// <summary>
    /// DFS visit for topological sort with cycle detection
    /// </summary>
    private void TopologicalSortVisit(Type system, HashSet<Type> visited, HashSet<Type> visiting, List<Type> sorted)
    {
        if (visiting.Contains(system))
        {
            throw new InvalidOperationException(
                $"Circular dependency detected involving system {system.Name}");
        }

        if (visited.Contains(system))
        {
            return;
        }

        visiting.Add(system);

        // Visit all dependencies first (DFS)
        if (_dependencies.TryGetValue(system, out var deps))
        {
            foreach (var dependency in deps)
            {
                // Only visit if it's a registered system
                if (_systemRegistrations.ContainsKey(dependency))
                {
                    TopologicalSortVisit(dependency, visited, visiting, sorted);
                }
            }
        }

        visiting.Remove(system);
        visited.Add(system);
        sorted.Add(system);
    }

    /// <summary>
    /// Get a registered service (for debugging/testing)
    /// </summary>
    public T GetService<T>()
    {
        if (_singletons.TryGetValue(typeof(T), out var service))
        {
            return (T)service;
        }
        throw new InvalidOperationException($"Service {typeof(T).Name} is not registered");
    }
}
