using System;
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
        .ForEach(v => {
            try {
                var id = GetIdentifier(v);
                this[id] = v;
            } catch (Exception e) {
                logger.Error(e, $"Error adding component to {typeof(IType).Name}: {v.name}");
            }
        });
  }
}
