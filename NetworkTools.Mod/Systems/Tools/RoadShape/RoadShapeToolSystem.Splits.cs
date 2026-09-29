namespace NetworkTools.Systems.Tools.RoadShape {
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;
    using Unity.Entities;
    using Unity.Mathematics;

    public partial class NT_RoadShapeToolSystem {
        private readonly HashSet<Entity> m_SplitNodes = new();

        public string SplitChoicesJson() {
            var choices = new JArray();
            if (!m_PathDataValid || Template.Value != ShapeTransformTemplate.CurveSmooth) return "[]";
            m_LastShapeJob.Complete();
            var distance = 0f;
            for (var i=1;i<m_NodeStates.Length-1;i++) {
                distance += math.distance(m_NodeStates[i-1].OriginalPosition, m_NodeStates[i].OriginalPosition);
                var node = m_NodeStates[i];
                choices.Add(new JObject { ["index"]=node.Entity.Index, ["version"]=node.Entity.Version,
                    ["ordinal"]=i+1, ["distance"]=Math.Round(distance),
                    ["selected"]=m_SplitNodes.Contains(node.Entity), ["eligible"]=!node.SmoothPinned });
            }
            return choices.ToString(Newtonsoft.Json.Formatting.None);
        }

        public bool SetSplitNode(Entity entity, bool enabled) {
            if (m_ToolSystem.activeTool != this || Phase != OperationPhase.Ready
                || Template.Value != ShapeTransformTemplate.CurveSmooth || !m_PathDataValid) return false;
            m_LastShapeJob.Complete();
            for (var i=1;i<m_NodeStates.Length-1;i++) {
                if (m_NodeStates[i].Entity != entity || m_NodeStates[i].SmoothPinned) continue;
                if (enabled) m_SplitNodes.Add(entity); else m_SplitNodes.Remove(entity);
                MarkDirty(); return true;
            }
            return false;
        }

        private void SnapshotSplitNodes() {
            for (var i=0;i<m_NodeStates.Length;i++) {
                var node=m_NodeStates[i];
                node.SmoothSplit=i>0 && i<m_NodeStates.Length-1 && m_SplitNodes.Contains(node.Entity);
                m_NodeStates[i]=node;
            }
        }
    }
}
