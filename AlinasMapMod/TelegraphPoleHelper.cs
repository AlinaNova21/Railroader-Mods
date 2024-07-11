using System.Linq;
using Serilog;
using UnityEngine;

namespace AlinasMapMod
{
  class TelegraphPoleHelper : MonoBehaviour, IPickable
  {
    public float MaxPickDistance => 300;

    public int Priority => 0;

    private string id { get => name.Split(' ').Last(); }

    public TooltipInfo TooltipInfo => new TooltipInfo($"Telegraph Pole {id}", $"This is a telegraph pole. :D\n{transform.localPosition}");

    public PickableActivationFilter ActivationFilter { get; }

    private Serilog.ILogger logger = Log.ForContext<TelegraphPoleHelper>();
    public void OnEnable()
    {
      logger.Debug("OnEnable()");
      logger.Debug(name);
      var go = gameObject;
      if (go.GetComponent<CapsuleCollider>() == null)
      {
        var cc = go.AddComponent<CapsuleCollider>();
        cc.height = 15;
        cc.radius = 0.5f;
        go.layer |= LayerMask.NameToLayer("Clickable");
      }
    }

    public void Activate(PickableActivateEvent evt)
    {
      throw new System.NotImplementedException();
    }

    public void Deactivate()
    {
    }
  }
}
