namespace NetworkTools.Systems.Tools.RoadShape {
    using System.Collections.Generic;

    using Game.Common;
    using NetworkTools.Systems.Tools.Parameters;
    using Game.Input;
    using Game.Notifications;
    using Game.Net;
    using Game.Prefabs;
    using Game.Tools;

    using NetworkTools.Components.Handles;
    using NetworkTools.Components;
    using NetworkTools.Components.Tools;
    using NetworkTools.Systems.Tools;

    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Jobs;
    using Unity.Collections;

    public partial class NT_RoadShapeToolSystem {
        private NativeReference<int> m_SmoothResult;
        private JobHandle m_LastShapeJob;

        /// <inheritdoc />
        public override bool TrySetPrefab(PrefabBase prefab) {
            var hasShapeSlope = m_PrefabSystem.HasComponent<NT_ShapeSlopeTool>(prefab);
            var hasShapeCurve = m_PrefabSystem.HasComponent<NT_ShapeCurveTool>(prefab);
            m_Log.Debug(
                $"TrySetPrefab {prefab is NT_ToolPrefab} hasShapeSlope={hasShapeSlope} hasShapeCurve={hasShapeCurve}");
            var validRequest =
                prefab is NT_ToolPrefab &&
                (hasShapeSlope || hasShapeCurve);

            if (!validRequest)
            {
                return false;
            }

            // Detect variant switch (slope ↔ curve) and reset config/handles
            var wasSlopePrefab = m_Prefab != null && m_PrefabSystem.HasComponent<NT_ShapeSlopeTool>(m_Prefab);
            var wasCurvePrefab = m_Prefab != null && m_PrefabSystem.HasComponent<NT_ShapeCurveTool>(m_Prefab);

            m_Prefab = prefab;

            if (hasShapeSlope && !wasSlopePrefab) {
                Template.Value = ShapeTransformTemplate.SlopeLinear;
            } else if (hasShapeCurve && !wasCurvePrefab) {
                Template.Value = ShapeTransformTemplate.CurveStraighten;
            }

            return true;
        }

        protected override void OnCreate() {
            base.OnCreate();

            Template.OnChanged += _ => InvalidatePreviewObservation();
            SmoothingFactor.OnChanged += _ => InvalidatePreviewObservation();
            CombinedSlope.OnChanged += _ => InvalidatePreviewObservation();
            EaseInLength.OnChanged += _ => InvalidatePreviewObservation();
            EaseOutLength.OnChanged += _ => InvalidatePreviewObservation();
            ArchHeight.OnChanged += _ => InvalidatePreviewObservation();
            ArchPosition.OnChanged += _ => InvalidatePreviewObservation();
            SmoothStart.OnChanged += _ => InvalidatePreviewObservation();
            SmoothEnd.OnChanged += _ => InvalidatePreviewObservation();

            m_Log.Prefix = nameof(NT_RoadShapeToolSystem);

            // Configuration
            RenderHandles            = true;
            DisableVanillaValidation = true;

            // Apply edits existing edges; it never combines adjacent segments. Keep
            // the native preview on the same topology, including unselected branches.
            DisableVanillaNodeReduction = true;

            // Template change additionally applies presets and reinitializes
            Template.OnChanged += _ => {
                ApplyTemplatePreset(Template.Value);
                if (Phase == OperationPhase.Ready) {
                    RefreshTransformHandles();
                }
            };

            // Cached path data for handles and jobs
            m_EdgeStates = new NativeList<EdgeState>(32, Allocator.Persistent);
            m_NodeStates = new NativeList<NodeState>(33, Allocator.Persistent);
            m_SmoothResult = new NativeReference<int>(Allocator.Persistent);
            m_PathDataValid = false;
#if IS_DEBUG
            m_SurfaceReferences = new NativeParallelHashMap<Entity, EdgeGeometry>(512, Allocator.Persistent);
            m_SurfaceCorrected = new NativeReference<int>(Allocator.Persistent);
#endif
        }

        protected override void OnDestroy() {
            m_LastShapeJob.Complete();
            #if IS_DEBUG
            if (m_SurfaceReferences.IsCreated) { m_SurfaceReferences.Dispose(); }
            if (m_SurfaceCorrected.IsCreated) { m_SurfaceCorrected.Dispose(); }
#endif
            if (m_SmoothResult.IsCreated) { m_SmoothResult.Dispose(); }
            // Dispose cached path data
            if (m_EdgeStates.IsCreated) {
                m_EdgeStates.Dispose();
            }

            if (m_NodeStates.IsCreated) {
                m_NodeStates.Dispose();
            }

            base.OnDestroy();
        }

        protected override void OnStartRunning() {
            InvalidatePreviewObservation();
            base.OnStartRunning();
#if IS_DEBUG
            if (m_AutomationActivatePending) {
                m_AutomationActivatePending = false;
                Template.Value = m_AutomationRequestedTemplate;
            }
#endif

            // Reset internal state
            m_LastHitPosition = default;
            Phase = OperationPhase.Idle;
        }

        protected override void OnStopRunning() {
            m_LastShapeJob.Complete();
            base.OnStopRunning();

            // Invalidate cached path data
            InvalidatePathData();
        }

        public void MarkDirty() {
            InvalidatePreviewObservation();
            m_UpdateNeeded = true;
        }

        private void InvalidatePreviewObservation() {
            ++m_PreviewInputRevision;
        }

        /// <summary>
        ///     Applies template-specific defaults when the template changes.
        /// </summary>
        private void ApplyTemplatePreset(ShapeTransformTemplate template) {
            var isSlopeTemplate = template == ShapeTransformTemplate.SlopeLinear ||
                                  template == ShapeTransformTemplate.SlopeEaseInOut ||
                                  template == ShapeTransformTemplate.SlopeArch;

            RenderSlopeTooltips = isSlopeTemplate;
            RenderNodeTooltips  = isSlopeTemplate;

            switch (template) {
                case ShapeTransformTemplate.SlopeEaseInOut:
                    EaseInLength.ResetToDefault();
                    EaseOutLength.ResetToDefault();
                    break;
                case ShapeTransformTemplate.SlopeArch:
                    ArchHeight.ResetToDefault();
                    ArchPosition.ResetToDefault();
                    break;
                case ShapeTransformTemplate.CurveSmooth:
                    SmoothingFactor.ResetToDefault();
                    break;
            }

            m_Log.Debug($"Template preset applied: {template}");
        }
    }
}
