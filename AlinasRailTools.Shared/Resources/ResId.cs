using System;

namespace AlinasRailTools.Shared.Resources;

/// <summary>
/// Strongly-typed resource identifier for type-safe resource lookups
/// </summary>
public struct ResId<T> : IEquatable<ResId<T>>
{
  public string Id { get; }

  public ResId(string id)
  {
    Id = id;
  }

  public static implicit operator string(ResId<T> resId) => resId.Id;
  public static implicit operator ResId<T>(string id) => new ResId<T>(id);

  public override string ToString() => Id;
  public override int GetHashCode() => Id?.GetHashCode() ?? 0;
  public bool Equals(ResId<T> other) => Id == other.Id;
  public override bool Equals(object obj) => obj is ResId<T> other && Equals(other);

  public static bool operator ==(ResId<T> left, ResId<T> right) => left.Equals(right);
  public static bool operator !=(ResId<T> left, ResId<T> right) => !left.Equals(right);
}
