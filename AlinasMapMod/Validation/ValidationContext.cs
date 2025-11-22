using System.Collections.Generic;

namespace AlinasMapMod.Validation;

/// <summary>
/// Context information passed to validation rules
/// </summary>
public class ValidationContext
{
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    public string FieldName { get; set; }
    public object Owner { get; set; }
    
    public T GetProperty<T>(string key, T defaultValue = default(T))
    {
        return Properties.ContainsKey(key) && Properties[key] is T ? (T)Properties[key] : defaultValue;
    }
    
    public void SetProperty<T>(string key, T value)
    {
        Properties[key] = value;
    }
}