using AlinasMapMod.Definitions.Converters;
using Newtonsoft.Json;
using UnityEngine;

namespace AlinasMapMod.Definitions;

[JsonConverter(typeof(InlineJsonConverter))]
public class SerializedVector2
{
    public float x { get; set; }
    public float y { get; set; }

    public static implicit operator Vector2(SerializedVector2 vector)
    {
        return new Vector2(vector.x, vector.y);
    }

    public static implicit operator SerializedVector2(Vector2 vector)
    {
        return new SerializedVector2
        {
            x = vector.x,
            y = vector.y
        };
    }
}