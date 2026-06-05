namespace AlinasRailTools.Core;

/// <summary>
/// Interface for all ART resources
/// </summary>
public interface IResource
{
    /// <summary>
    /// Unique identifier for this resource
    /// </summary>
    string Id { get; set; }
    
    /// <summary>
    /// Validate this resource using the current working snapshot as context
    /// </summary>
    /// <param name="snapshot">Working snapshot for dependency validation</param>
    /// <returns>Validation result</returns>
    ValidationResult Validate(IWorkingSnapshot snapshot);
}