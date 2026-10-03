namespace NetworkTools.Systems.Tools.RoadShape {
    using System.Collections.Generic;

    using Game.Input;
    using NetworkTools.Systems.Tools;
    using NetworkTools.Systems.Tools.Handles;
    using NetworkTools.Systems.Tools.Parameters;

    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    ///     Tool system for reshaping road segments.
    ///     Allows selecting a contiguous path of road nodes and applying transformations.
    /// </summary>
    public partial class NT_RoadShapeToolSystem : NT_PathSelectionToolSystem, IManualApplyProvider {
        /// <inheritdoc />
        public override string toolID => "RoadShapeTool";

        // ── Parameters 

        public EnumParameter<ShapeTransformTemplate> Template        = new("roadShape.template", ShapeTransformTemplate.Preserve, label: "NetworkTools.UI.Common.Mode");
        public FloatParameter                        EaseInLength    = new("roadShape.easeInLength",    0.1f, 0f, 0.5f, modes: (int)ShapeTransformTemplate.SlopeEaseInOut, label: "NetworkTools.UI.Slope.StartingFlatness", fractionDigits: 0, numberType: NumberType.Percentage, displayScale: 200f) {
            Handles = new IHandleSpec<float>[] {
                new AxisHandle {
                    StartPoint = tool => ((NT_RoadShapeToolSystem)tool).m_ShapeTransformContext.StartPosition,
                    EndPoint   = tool => ((NT_RoadShapeToolSystem)tool).m_ShapeTransformContext.EndPosition,
                    YOffset    = 1f
                }
            }
        };
        public FloatParameter                        EaseOutLength   = new("roadShape.easeOutLength",   0.1f, 0f, 0.5f, modes: (int)ShapeTransformTemplate.SlopeEaseInOut, label: "NetworkTools.UI.Slope.EndingFlatness", fractionDigits: 0, numberType: NumberType.Percentage, displayScale: 200f) {
            Handles = new IHandleSpec<float>[] {
                new AxisHandle {
                    StartPoint = tool => ((NT_RoadShapeToolSystem)tool).m_ShapeTransformContext.StartPosition,
                    EndPoint   = tool => ((NT_RoadShapeToolSystem)tool).m_ShapeTransformContext.EndPosition,
                    YOffset    = 1f,
                    Reverse    = true
                }
            }
        };
        public FloatParameter                        ArchHeight      = new("roadShape.archHeight",      10f, -80f, 80f, modes: (int)ShapeTransformTemplate.SlopeArch, label: "NetworkTools.UI.Slope.ArchHeight", fractionDigits: 0, numberType: NumberType.Distance);
        public FloatParameter                        ArchPosition    = new("roadShape.archPosition",    0.5f, 0.1f, 0.9f, modes: (int)ShapeTransformTemplate.SlopeArch, label: "NetworkTools.UI.Slope.ArchPosition", fractionDigits: 0, numberType: NumberType.Percentage, displayScale: 100f);
        public FloatParameter                        SmoothingFactor = new("roadShape.smoothingFactor", 0.5f, 0f, 1f,   modes: (int)ShapeTransformTemplate.CurveSmooth, label: "NetworkTools.UI.Curve.SmoothingFactor", fractionDigits: 2);
        public BoolParameter                         SmoothStart     = new("roadShape.smoothStart", false, modes: (int)ShapeTransformTemplate.SlopeLinear | (int)ShapeTransformTemplate.SlopeEaseInOut | (int)ShapeTransformTemplate.SlopeArch, label: "NetworkTools.UI.Slope.SmoothStart");
        public BoolParameter                         SmoothEnd       = new("roadShape.smoothEnd",   false, modes: (int)ShapeTransformTemplate.SlopeLinear | (int)ShapeTransformTemplate.SlopeEaseInOut | (int)ShapeTransformTemplate.SlopeArch, label: "NetworkTools.UI.Slope.SmoothEnd");

        public BoolParameter CombinedSlope = new("roadShape.combinedSlope", false, modes: (int)ShapeTransformTemplate.CurveSmooth, label: "NetworkTools.UI.Curve.CombinedSlope");
        // Session preferences are deliberately excluded from on-disk persistence.
        public BoolParameter AllowInteriorJunctionElevation = new("roadShape.allowInteriorJunctionElevation", true, modes: (int)ShapeTransformTemplate.CurveSmooth, label: "NetworkTools.UI.Curve.AllowJunctionElevation", persist: false);
        public BoolParameter JunctionElevationUnlimited = new("roadShape.junctionElevationUnlimited", true, modes: (int)ShapeTransformTemplate.CurveSmooth, label: "NetworkTools.UI.Curve.JunctionElevationUnlimited", persist: false);
        public FloatParameter JunctionElevationLimit = new("roadShape.junctionElevationLimit", 5f, 0f, 20f, modes: (int)ShapeTransformTemplate.CurveSmooth, label: "NetworkTools.UI.Curve.JunctionElevationLimit", fractionDigits: 3, numberType: NumberType.Distance, persist: false) {
            ValidateValue = value => IsValidJunctionElevationLimit(value)
        };
        private static bool s_AllowInteriorJunctionElevation = true;
        private static bool s_JunctionElevationUnlimited = true;
        private static float s_JunctionElevationLimit = 5f;

        internal static bool IsValidJunctionElevationLimit(double value) =>
            !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0 && value <= 20;

        private bool CombinedMode {
            get {
#if IS_DEBUG
                return Template.Value == ShapeTransformTemplate.CurveSmooth && CombinedSlope.Value;
#else
                return false;
#endif
            }
        }

        /// <inheritdoc />
        protected override int GetActiveModeFlag() => (int)Template.Value;

        // ── Non-parameter state

        /// <summary>
        ///     Caches the last hit position for tool-specific use.
        /// </summary>
        private float3 m_LastHitPosition;

        #region Template Method Implementations

        /// <inheritdoc />
        protected override void OnPathReady() {
            RefreshPathData();
            RefreshTransformHandles();
        }

        /// <inheritdoc />
        protected override void OnSelectionCleared() {
            m_SplitNodes.Clear();
            DestroyAllHandles();
            InvalidatePathData();
        }

        /// <inheritdoc />
        protected override void OnPathExtended(Entity newEndNode) {
            m_SplitNodes.Clear();
            RefreshPathData();
            RefreshTransformHandles();
        }

        /// <inheritdoc />
        protected override void OnPathTrimmed(Entity newEndNode) {
            m_SplitNodes.Clear();
            RefreshPathData();
            RefreshTransformHandles();
        }

        #endregion

        /// <summary>
        ///     Builds a Burst-compatible snapshot from the current parameter values.
        /// </summary>
        internal ShapeJobConfig BuildJobConfig() {
            return new ShapeJobConfig {
                Template        = Template.Value,
                EaseInLength    = EaseInLength.Value,
                EaseOutLength   = EaseOutLength.Value,
                ArchHeight      = ArchHeight.Value,
                ArchPosition    = ArchPosition.Value,
                SmoothingFactor = SmoothingFactor.Value,
                CombinedSlope   = CombinedMode,
                ConstrainJunctionElevation = CombinedMode && (!AllowInteriorJunctionElevation.Value || !JunctionElevationUnlimited.Value),
                JunctionElevationLimit = AllowInteriorJunctionElevation.Value ? JunctionElevationLimit.Value : 0,

                SmoothStart     = SmoothStart.Value,
                SmoothEnd       = SmoothEnd.Value,
            };
        }

        public override IReadOnlyList<HintTooltipEntry> GetHintTooltips(
    OperationPhase phase,
    ProxyAction applyAction,
    ProxyAction secondaryApplyAction) {
            if (phase == OperationPhase.Ready && Template.Value == ShapeTransformTemplate.CurveSmooth
                && SmoothPreviewResult < 0) {
                return new HintTooltipEntry[] {
                    new("NetworkTools.HintTooltip.ShapeCurve.Invalid", secondaryApplyAction)
                };
            }
            return phase switch {
                OperationPhase.Idle => new HintTooltipEntry[] {
                    new("NetworkTools.HintTooltip.ShapeSlope.SelectStart", applyAction),
                    new("NetworkTools.HintTooltip.Common.Exit", secondaryApplyAction)
                },
                OperationPhase.Configuring => new HintTooltipEntry[] {
                    new("NetworkTools.HintTooltip.ShapeSlope.SelectSecond", applyAction),
                    new("NetworkTools.HintTooltip.ShapeSlope.RemoveLast", secondaryApplyAction)
                },
                OperationPhase.Ready => new HintTooltipEntry[] {
                    new("NetworkTools.HintTooltip.ShapeSlope.ExtendPath", applyAction),
                    new("NetworkTools.HintTooltip.ShapeSlope.RemoveLast", secondaryApplyAction)
                },
                _ => System.Array.Empty<HintTooltipEntry>()
            };
        }
    }
}
