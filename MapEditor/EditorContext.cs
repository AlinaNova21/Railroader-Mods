using System;
using Core;
using HarmonyLib;
using MapEditor.StateTracker;
using MapEditor.Tools;
using StrangeCustoms.Tracks;
using Track;

namespace MapEditor
{
    public class EditorContext
    {
        public static EditorContext Instance { get; private set; }
        public PatchEditor PatchEditor { get; private set; }

        public ChangeManager ChangeManager { get; private set; } = new ChangeManager();

        public TrackNode SelectedNode { get; set; }
        public TrackSegment SelectedSegment { get; set; }

        public TrackNode HoveredNode { get; set; }
        public TrackSegment HoveredSegment { get; set; }

        private BaseTool activeTool;
        public BaseTool ActiveTool => activeTool;

        private string _prefix = "Custom_";
        public string Prefix
        {
            get => _prefix;
            set {
                _prefix = value;
                TrackNodeIdGenerator = NewIdGenerator($"N{value}", 4);
                TrackSegmentIdGenerator = NewIdGenerator($"S{value}", 4);
            }
        }
        public IdGenerator TrackNodeIdGenerator = NewIdGenerator($"NCustom_", 4);
        public IdGenerator TrackSegmentIdGenerator = NewIdGenerator($"SCustom_", 4);

        private static IdGenerator NewIdGenerator(string prefix, int digits = 4)
        {
            var cons = AccessTools.Constructor(typeof(IdGenerator), [typeof(string), typeof(int)]);
            return cons.Invoke([prefix, digits]) as IdGenerator;
        }

        #region Events
        public static event Action<EditorContext> EditorContextChanged;
        public static event Action<TrackNode> NodeSelectedChanged;
        public static event Action<TrackSegment> SegmentSelectedChanged;
        #endregion

        public EditorContext(string filename)
        {
            Instance = this;
            // Hack since constructor is private
            var cons = AccessTools.Constructor(typeof(PatchEditor), [typeof(string)], false);
            PatchEditor = cons.Invoke(new object[] { filename }) as PatchEditor;
            EditorContextChanged?.Invoke(this);
        }

        public void SetActiveTool(BaseTool tool)
        {
            activeTool = tool;
            EditorContextChanged?.Invoke(this);
        }

        internal void Save()
        {
            PatchEditor.Save();
        }

        internal void SelectNode(TrackNode newNode)
        {
            SelectedNode = newNode;
            NodeSelectedChanged?.Invoke(newNode);
        }
    }
}