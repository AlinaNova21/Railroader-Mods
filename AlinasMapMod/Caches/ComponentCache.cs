using System.Linq;
using UnityEngine;

namespace AlinasMapMod.Caches;

public abstract class ComponentCache<IType, CType> : BaseCache<IType, CType> where CType : MonoBehaviour
{
  public abstract string GetIdentifier(CType obj);

  public override void Rebuild()
  {
    Clear();
    GameObject.FindObjectsOfType<CType>(true)
        .ToList()
        .ForEach(v => this[GetIdentifier(v)] = v);
  }
}
