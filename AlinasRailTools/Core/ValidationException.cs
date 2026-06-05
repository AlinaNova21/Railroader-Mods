using System;
using System.Collections.Generic;

namespace AlinasRailTools.Core;

/// <summary>
/// Exception thrown when resource validation fails during patch application
/// </summary>
public class ValidationException : Exception
{
    public List<string> ValidationErrors { get; }
    
    public ValidationException(string message, List<string> errors) : base(message)
    {
        ValidationErrors = errors ?? new List<string>();
    }
}