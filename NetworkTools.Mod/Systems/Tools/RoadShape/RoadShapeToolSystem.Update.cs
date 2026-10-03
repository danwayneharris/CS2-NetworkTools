namespace NetworkTools.Systems.Tools.RoadShape {
    using Game.Common;
    using Game.Notifications;
    using Game.Net;
    using Game.Prefabs;
    using Game.Tools;

    using NetworkTools.Components.Handles;
    using NetworkTools.Components;
    using NetworkTools.Components.Tools;

    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Jobs;
    using Unity.Collections;

    public partial class NT_RoadShapeToolSystem {
        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            // Selection processing may clear/resize native lists held by the previous job.
            m_LastShapeJob.Complete();
            UpdateActions();

            // ═══════════════════════════════════════════════════════════════════════════
            // HANDLE INTERACTION PIPELINE 
            // ═══════════════════════════════════════════════════════════════════════════

            if (Phase == OperationPhase.Ready && ProcessHandleInput(inputDeps)) {
                // Handle consumed input this frame:
                // - OnHandleDragging() may have updated parameters
                // - m_UpdateNeeded was set to true
                // - Skip node selection, go straight to output
                return HandleTempEntities(inputDeps);
            }

            // ═══════════════════════════════════════════════════════════════════════════
            // NODE SELECTION: Input Detection 
            // ═══════════════════════════════════════════════════════════════════════════

            var rightClickPressed = m_SecondaryApplyAction.WasPressedThisFrame();
            var leftClickPressed = m_ApplyAction.WasPressedThisFrame();
            var raycastHit = false;
            var hoveredEntity = Entity.Null;
            var hitPosition = float3.zero;
            ControlPoint controlPoint = default;

            raycastHit = GetRaycastResult(out controlPoint);
            if (raycastHit) {
                hoveredEntity = controlPoint.m_OriginalEntity;
                hitPosition = controlPoint.m_HitPosition;
            }

            // ═══════════════════════════════════════════════════════════════════════════
            // NODE SELECTION: State Mutation
            // Phase is automatically updated by HandleAddNode/HandleRemoveNode
            // ═══════════════════════════════════════════════════════════════════════════

            // Right-click: cancel/back (skips all raycast processing)
            if (rightClickPressed) {
                HandleRemoveNode();
                m_UpdateNeeded = true;
            }
            // Raycast-based interactions
            else if (raycastHit) {
                // Update hover state first (so path preview is ready if user clicks)
                var newEntityHovered = (hoveredEntity != m_LastHoveredEntity.Value);
                if (newEntityHovered) {
                    HandlePathUpdate(controlPoint);
                    HandleHover(hoveredEntity);
                    m_UpdateNeeded = true;
                }
                m_LastHoveredEntity.Value = hoveredEntity;
                m_LastHitPosition = hitPosition;

                // Left-click: add node (after hover update, same frame OK)
                if (leftClickPressed && hoveredEntity != Entity.Null) {
                    HandleAddNode(hoveredEntity);
                    m_UpdateNeeded = true;
                }
            }
            // No raycast hit
            else {
                HandleNoHover();
            }

            // ═══════════════════════════════════════════════════════════════════════════
            // OUTPUT
            // ═══════════════════════════════════════════════════════════════════════════

            return HandleTempEntities(inputDeps);
        }

        /// <summary>
        ///     Runs various jobs depending on whether we need to Update, Apply, or Cancel temp entities.
        /// </summary>
        /// <param name="inputDeps">Input job dependencies.</param>
        /// <returns>Output job handle.</returns>
        private JobHandle HandleTempEntities(JobHandle inputDeps) {
            return Phase switch {
                // Preview temp entities
                OperationPhase.Ready => Update(inputDeps),
                // Apply real entities
                OperationPhase.Applying => Apply(inputDeps),
                // Clear otherwise
                OperationPhase.Idle or OperationPhase.Configuring => Clear(inputDeps),
                _ => Clear(inputDeps)
            };
        }

        /// <inheritdoc />
        public int ApplyMinNodeCount => 2;

        /// <inheritdoc />
        public bool CanApply => Phase == OperationPhase.Ready && CandidateAllowsApply();

        private bool CandidateAllowsApply() {
            if (Template.Value == ShapeTransformTemplate.Preserve || m_UpdateNeeded || !m_PathDataValid
                || !m_LastShapeJob.IsCompleted || m_SubmittedPreviewRevision != m_PreviewInputRevision)
                return false;
            m_LastShapeJob.Complete();
            if (NetworkTools.Geometry.OriginalInputComparison.CandidateStatus(m_PreviewInputRevision, m_SubmittedPreviewRevision,
                m_CachedOriginalInputs, m_SubmittedOriginalInputs, CaptureOriginalProbeInputs()) != "matches") return false;
#if !IS_DEBUG
            if (UnsupportedReleaseJunction) return false;
#else
            // Same applicable evidence for mouse/UI and provider Apply. Straighten
            // has no native observation contract; do not imply that it does.
            if ((Template.Value == ShapeTransformTemplate.CurveSmooth || AutomationSlope)
                && (m_AutomationVerifiedSubmission != m_SmoothTraceId || m_SmoothTraceId <= 0)) return false;
#endif
            return (Template.Value != ShapeTransformTemplate.CurveSmooth && Template.Value != ShapeTransformTemplate.SlopeLinear)
                || SmoothPreviewResult == 1;
        }

        private bool UnsupportedReleaseJunction {
            get {
#if !IS_DEBUG
                if (Template.Value == ShapeTransformTemplate.CurveSmooth && m_PathDataValid) {
                    foreach (var state in m_NodeStates) {
                        if (!EntityManager.HasBuffer<ConnectedEdge>(state.Entity)
                            || EntityManager.GetBuffer<ConnectedEdge>(state.Entity, true).Length > 2) return true;
                    }
                }
#endif
                return false;
            }
        }

        public string ApplyRestrictionKey => UnsupportedReleaseJunction
            ? "NetworkTools.UI.Curve.ReleaseJunctionUnsupported"
            : Phase == OperationPhase.Ready && !m_PathDataValid
                ? "NetworkTools.UI.Shape.SelectionUnavailable" : CombinedRestrictionKey;

        private string CombinedRestrictionKey {
            get {
#if IS_DEBUG
                if (!CombinedMode || m_UpdateNeeded || !m_LastShapeJob.IsCompleted || !m_SmoothResult.IsCreated) return "";
                m_LastShapeJob.Complete();
                if (m_SmoothResult.Value == -100 - (int)CombinedLinearProfileTransform.Failure.SplitGradeConflict)
                    return "NetworkTools.UI.Curve.CombinedSplitConflict";
                if (m_SmoothResult.Value <= -100) return "NetworkTools.UI.Curve.CombinedProfileInvalid";
                if (m_SurfaceFailed) return "NetworkTools.UI.Curve.CombinedSurfaceFailed";
#endif
                return "";
            }
        }

        private int SmoothPreviewResult {
            get {
                if (m_UpdateNeeded || !m_PathDataValid || !m_LastShapeJob.IsCompleted) { return 0; }
                m_LastShapeJob.Complete();
#if IS_DEBUG
                if (CombinedMode && !SurfacePreviewAllowsApply()) { return 0; }
                if (Template.Value == ShapeTransformTemplate.CurveSmooth && !JunctionSearchAllowsApply()) { return 0; }
                if (Template.Value == ShapeTransformTemplate.SlopeLinear && !SurfacePreviewAllowsApply()) { return 0; }
#endif
                return m_SmoothResult.IsCreated ? m_SmoothResult.Value : 0;
            }
        }

        /// <summary>
        ///     Requests the tool to apply the current transformation.
        /// </summary>
        public void RequestApply() => TryRequestApply();

        private bool TryRequestApply() {
            if (!CanApply) return false;
            Phase = OperationPhase.Applying;
            return true;
        }

        protected override bool GetRaycastResult(out ControlPoint controlPoint) =>
            TryGetNodeRaycast(out controlPoint, requireWithinMaxDistance: false);

        /// <summary>
        ///     Resets the tool to idle state, clearing all selection.
        /// </summary>
        public void ResetToIdle() {
            InvalidatePreviewObservation();
            m_LastShapeJob.Complete();
            // Clear state to completely blank
            Phase = OperationPhase.Idle;

            // Destroy any active handles
            DestroyAllHandles();

            // Use base class method to clear selection state
            ClearSelectionState();
        }
    }
}
