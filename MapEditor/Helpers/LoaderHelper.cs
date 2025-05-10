using System.Linq;
using System.Text;
using Helpers;
using JetBrains.Annotations;
using MapEditor.Dialogs;
using MapEditor.Managers;
using RLD;
using Serilog;
using Track;
using UI;
using UI.ContextMenu;
using UnityEngine;

namespace MapEditor.Helpers
{
  public sealed class LoaderHelper : MonoBehaviour, IPickable
  {
    private static readonly Material _Material = new(Shader.Find("Universal Render Pipeline/Lit"));

    private AlinasMapMod.LoaderBuilder.CustomLoader _Loader = null!;

    private GameObject? _Sphere;

    public float MaxPickDistance => 200f;

    public int Priority => 1;

    public TooltipInfo TooltipInfo => BuildTooltipInfo();
    public PickableActivationFilter ActivationFilter => PickableActivationFilter.Any;

    private TooltipInfo BuildTooltipInfo()
    {
      var sb = new StringBuilder();
      sb.AppendLine($"ID: {_Loader.id}");
      sb.AppendLine($"Pos: {_Loader.transform.localPosition}");
      sb.AppendLine($"Rot: {_Loader.transform.localEulerAngles}");
      sb.AppendLine($"Prefab: {_Loader.config.Prefab}");
      sb.AppendLine($"Industry: {_Loader.config.Industry}");

      return new TooltipInfo($"Loader {_Loader.id}", sb.ToString());
    }

    [UsedImplicitly]
    public void Start()
    {
      _Loader = transform.parent.GetComponent<AlinasMapMod.LoaderBuilder.CustomLoader>()!;

      transform.localPosition = Vector3.zero;
      transform.localEulerAngles = Vector3.zero;

      gameObject.layer = Layers.Clickable;

      _Sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
      _Sphere.transform.parent = transform;
      _Sphere.transform.localPosition = Vector3.zero;
      _Sphere.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
      _Sphere.transform.localEulerAngles = Vector3.zero;
      _Sphere.GetMeshRenderer().material = _Material;

      var boxCollider = gameObject.AddComponent<BoxCollider>();
      boxCollider.size = new Vector3(0.4f, 0.4f, 0.8f);
    }

    [UsedImplicitly]
    public void Update()
    {
      var mr = _Sphere.GetMeshRenderer();
      mr.material.color = EditorContext.SelectedLoader?.id == _Loader.id ? Color.magenta : Color.cyan;
      mr.enabled = EditorContext.PatchEditor != null;
    }

    public void Activate(PickableActivateEvent evt)
    {
      if (evt.Activation == PickableActivation.Secondary)
      {
        ShowContextMenu();
        return;
      }
      EditorContext.SelectedLoader = _Loader;
    }

    public void ShowContextMenu()
    {
      //var node = transform.parent.GetComponent<TrackNode>();
      //var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
      //var shared = UI.ContextMenu.ContextMenu.Shared;
      //if (UI.ContextMenu.ContextMenu.IsShown)
      //{
      //  shared.Hide();
      //}
      //shared.Clear();
      //shared.AddButton(ContextMenuQuadrant.General, (EditorContext.SelectedNode == node) ? "Deselect" : "Select", SpriteName.Select, () => EditorContext.SelectedNode = (EditorContext.SelectedNode == node) ? null : node);
      //shared.AddButton(ContextMenuQuadrant.Brakes, "Delete", sprite, () => NodeManager.RemoveNode(node));
      //shared.AddButton(ContextMenuQuadrant.Unused1, "Flip Switch Stand", sprite, () => NodeManager.FlipSwitchStand(!node.flipSwitchStand, node));
      //shared.AddButton(ContextMenuQuadrant.Unused2, "Split", sprite, () => NodeManager.SplitNode(node));

      //shared.Show($"Node {node.id}");
    }

    public void Deactivate()
    {
    }

  }
}
