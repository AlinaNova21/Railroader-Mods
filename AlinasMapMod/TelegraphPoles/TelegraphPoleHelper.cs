using System.IO;
using System.Linq;
using System.Reflection;
using Serilog;
using UnityEngine;

namespace AlinasMapMod.TelegraphPoles;

class TelegraphPoleHelper : MonoBehaviour, IPickable
{
  public float MaxPickDistance => 300;

  public int Priority => 0;

  private string id { get => name.Split(' ').Last(); }

  public TooltipInfo TooltipInfo => new TooltipInfo($"Telegraph Pole {id}", $"{transform.localPosition}\n{randomText}");

  public PickableActivationFilter ActivationFilter => PickableActivationFilter.Any;

  private static string[] _randomText = [];
  public static string RandomText
  {
    get {
      if (_randomText.Length == 0) {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames().Single(r => r.EndsWith("TelegraphPoleStrings.txt"));
        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        using (StreamReader reader = new StreamReader(stream)) {
          _randomText = reader
            .ReadToEnd()
            .Split('\n')
            .Select(s => s.Trim('\n', '\r', ' ', '\t'))
            .ToArray();
        }
      }
      return _randomText[Random.Range(0, _randomText.Length)];
    }
  }

  private readonly string randomText = RandomText;

  private Serilog.ILogger logger = Log.ForContext<TelegraphPoleHelper>();
  public void OnEnable()
  {
    logger.Debug("OnEnable()");
    logger.Debug(name);
    var go = gameObject;
    if (go.GetComponent<CapsuleCollider>() == null) {
      var cc = go.AddComponent<CapsuleCollider>();
      cc.height = 15;
      cc.radius = 0.5f;
      go.layer |= LayerMask.NameToLayer("Clickable");
    }
  }

  public void Activate(PickableActivateEvent evt)
  {
  }

  public void Deactivate()
  {
  }
}
