
using System.Xml.Linq;
using UnityEngine;

namespace AlinasMapMod.Extensions;

public static class Vector3Extensions
{
    public static XElement[] ToXElement(this Vector3 vector3)
    {
        return [
            new XElement("x", vector3.x),
            new XElement("y", vector3.y),
            new XElement("z", vector3.z)
        ];
    }

    public static Vector3 ToVector3(this XElement element)
    {
        return new Vector3(
            float.Parse(element.Element("x")?.Value ?? "0"),
            float.Parse(element.Element("y")?.Value ?? "0"),
            float.Parse(element.Element("z")?.Value ?? "0")
        );
    }
}