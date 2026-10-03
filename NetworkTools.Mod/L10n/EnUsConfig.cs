// <copyright file="EnUsConfig.cs" company="Luca Rager">
// Copyright (c) Luca Rager. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace NetworkTools.L10n {
    using System.Collections.Generic;

    using Colossal;

    using NetworkTools;
    using NetworkTools.Settings;

    /// <summary>
    /// Configures the English (US) localization for NetworkTools Mod.
    /// </summary>
    public class EnUsConfig : IDictionarySource {
        private readonly Dictionary<string, string> m_Localization;
        private readonly NT_Settings                m_Setting;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnUsConfig"/> class.
        /// </summary>
        /// <param name="setting">NetworkToolsModSettings.</param>
        public EnUsConfig(NT_Settings setting) {
            m_Setting = setting;

            m_Localization = new Dictionary<string, string> {
                { m_Setting.GetSettingsLocaleID(), "Network Tools" },

                // Actions
                { m_Setting.GetOptionLabelLocaleID(NT_Settings.ToggleToolPanelStr), "Toggle Network Tools Panel" },
                { m_Setting.GetBindingKeyLocaleID(NT_Settings.ToggleToolPanelStr), "Toggle Network Tools Panel" },
                { m_Setting.GetOptionDescLocaleID(NT_Settings.ToggleToolPanelStr), "Opens the NT panel" },
                { m_Setting.GetOptionLabelLocaleID(NT_Settings.OpenTool1Str), "Open Add Node" },
                { m_Setting.GetBindingKeyLocaleID(NT_Settings.OpenTool1Str), "Open Add Node" },
                { m_Setting.GetOptionDescLocaleID(NT_Settings.OpenTool1Str), "Shortcut to open a specific Network Tools tool, use it after opening the NT panel" },
                { m_Setting.GetOptionLabelLocaleID(NT_Settings.OpenTool2Str), "Open Remove Node" },
                { m_Setting.GetBindingKeyLocaleID(NT_Settings.OpenTool2Str), "Open Remove Node" },
                { m_Setting.GetOptionDescLocaleID(NT_Settings.OpenTool2Str), "Shortcut to open a specific Network Tools tool, use it after opening the NT panel" },
                { m_Setting.GetOptionLabelLocaleID(NT_Settings.OpenTool3Str), "Open Slide Node" },
                { m_Setting.GetBindingKeyLocaleID(NT_Settings.OpenTool3Str), "Open Slide Node" },
                { m_Setting.GetOptionDescLocaleID(NT_Settings.OpenTool3Str), "Shortcut to open a specific Network Tools tool, use it after opening the NT panel" },
                { m_Setting.GetOptionLabelLocaleID(NT_Settings.OpenTool4Str), "Open Super Node" },
                { m_Setting.GetBindingKeyLocaleID(NT_Settings.OpenTool4Str), "Open Super Node" },
                { m_Setting.GetOptionDescLocaleID(NT_Settings.OpenTool4Str), "Shortcut to open a specific Network Tools tool, use it after opening the NT panel" },
                { m_Setting.GetOptionLabelLocaleID(NT_Settings.OpenTool5Str), "Open Slope Tools" },
                { m_Setting.GetBindingKeyLocaleID(NT_Settings.OpenTool5Str), "Open Slope Tools" },
                { m_Setting.GetOptionDescLocaleID(NT_Settings.OpenTool5Str), "Shortcut to open a specific Network Tools tool, use it after opening the NT panel" },
                { m_Setting.GetOptionLabelLocaleID(NT_Settings.OpenTool6Str), "Open Curve Tools" },
                { m_Setting.GetBindingKeyLocaleID(NT_Settings.OpenTool6Str), "Open Curve Tools" },
                { m_Setting.GetOptionDescLocaleID(NT_Settings.OpenTool6Str), "Shortcut to open a specific Network Tools tool, use it after opening the NT panel" },
                { m_Setting.GetOptionLabelLocaleID(NT_Settings.OpenTool7Str), "Open Connect Tools" },
                { m_Setting.GetBindingKeyLocaleID(NT_Settings.OpenTool7Str), "Open Connect Tools" },
                { m_Setting.GetOptionDescLocaleID(NT_Settings.OpenTool7Str), "Shortcut to open a specific Network Tools tool, use it after opening the NT panel" },
                { m_Setting.GetOptionLabelLocaleID(NT_Settings.OpenTool8Str), "Open Parallel Tool" },
                { m_Setting.GetBindingKeyLocaleID(NT_Settings.OpenTool8Str), "Open Parallel Tool" },
                { m_Setting.GetOptionDescLocaleID(NT_Settings.OpenTool8Str), "Shortcut to open a specific Network Tools tool, use it after opening the NT panel" },
                { m_Setting.GetOptionLabelLocaleID(NT_Settings.OpenTool9Str), "Open Generate Tool" },
                { m_Setting.GetBindingKeyLocaleID(NT_Settings.OpenTool9Str), "Open Generate Tool" },
                { m_Setting.GetOptionDescLocaleID(NT_Settings.OpenTool9Str), "Shortcut to open a specific Network Tools tool, use it after opening the NT panel" },
                { m_Setting.GetOptionLabelLocaleID(NT_Settings.ApplyTransformationStr), "Apply Tool" },
                { m_Setting.GetBindingKeyLocaleID(NT_Settings.ApplyTransformationStr), "Apply Tool" },
                { m_Setting.GetOptionDescLocaleID(NT_Settings.ApplyTransformationStr), "Applies the current tool's action" },

                // Sections

                // Groups
                { m_Setting.GetOptionGroupLocaleID(NT_Settings.GeneralGroupStr), "General" },
                { m_Setting.GetOptionGroupLocaleID(NT_Settings.KeybindingsGroupStr), "Key Bindings" },
                { m_Setting.GetOptionGroupLocaleID(NT_Settings.AboutGroupStr), "About" },

                // General
                { m_Setting.GetOptionLabelLocaleID(nameof(NT_Settings.DistanceUnit)), "Distance Unit" },
                { m_Setting.GetOptionDescLocaleID(nameof(NT_Settings.DistanceUnit)), "Choose whether distance parameters display in meters or units (1 unit = 8 meters)" },
                { m_Setting.GetOptionLabelLocaleID(NT_Settings.DistanceUnitMeters), "Meters" },
                { m_Setting.GetOptionLabelLocaleID(NT_Settings.DistanceUnitUnits), "Units (8m)" },

                // About
                { m_Setting.GetOptionLabelLocaleID(nameof(NT_Settings.Version)), "Version" },
                { m_Setting.GetOptionLabelLocaleID(nameof(NT_Settings.InformationalVersion)), "Informational Version" },
                { m_Setting.GetOptionLabelLocaleID(nameof(NT_Settings.Credits)), string.Empty },
                { m_Setting.GetOptionLabelLocaleID(nameof(NT_Settings.Github)), "GitHub" }, {
                    m_Setting.GetOptionDescLocaleID(nameof(NT_Settings.Github)),
                    "Opens a browser window to https://github.com/lucarager/CS2-NetworkTools"
                },
                { m_Setting.GetOptionLabelLocaleID(nameof(NT_Settings.Discord)), "Discord" },
                { m_Setting.GetOptionDescLocaleID(nameof(NT_Settings.Discord)), "Opens link to join the CS:2 Modding Discord" },

                // Hint Tooltips - Common
                { "Common.ACTION[NetworkTools.HintTooltip.Common.Exit]", "Exit Tool" },

                // # Tool Strings
                // ## AddNode
                // ### AddNode - Metadata
                { "NetworkTools.Tools.AddNode.Name", "Add Node" },
                { "NetworkTools.Tools.AddNode.Description", "Allows adding a node to a segment, dividing the segment into two." },
                // ### AddNode - Hint Tooltips
                { "Common.ACTION[NetworkTools.HintTooltip.AddNode.Hover]", "Hover over a network segment" },
                { "Common.ACTION[NetworkTools.HintTooltip.AddNode.Apply]", "Add node" },
                { "Common.ACTION[NetworkTools.HintTooltip.AddNode.Cancel]", "Cancel" },

                // ## RemoveNode
                // ### RemoveNode - Metadata
                { "NetworkTools.Tools.RemoveNode.Name", "Remove Node" },
                { "NetworkTools.Tools.RemoveNode.Description", "Allows removing a node from a network." },
                // ### RemoveNode - Hint Tooltips
                { "Common.ACTION[NetworkTools.HintTooltip.RemoveNode.Select]", "Select a node to remove" },
                { "Common.ACTION[NetworkTools.HintTooltip.RemoveNode.Apply]", "Remove node" },
                { "Common.ACTION[NetworkTools.HintTooltip.RemoveNode.Cancel]", "Cancel" },

                // ## ShapeSlope
                // ### ShapeSlope - Metadata
                { "NetworkTools.Tools.ShapeSlope.Name", "Slope Tools" },
                { "NetworkTools.Tools.ShapeSlope.Description", "Allows editing the slope of a contiguous path." },
                // ### ShapeSlope - Hint Tooltips
                { "Common.ACTION[NetworkTools.HintTooltip.ShapeSlope.SelectStart]", "Select a starting node" },
                { "Common.ACTION[NetworkTools.HintTooltip.ShapeSlope.SelectSecond]", "Select a second node" },
                { "Common.ACTION[NetworkTools.HintTooltip.ShapeSlope.RemoveLast]", "Remove last node" },
                { "Common.ACTION[NetworkTools.HintTooltip.ShapeSlope.ExtendPath]", "Select a new end node to extend the path" },

                // ## ShapeCurve
                // ### ShapeCurve - Metadata
                { "NetworkTools.Tools.ShapeCurve.Name", "Curve Tools" },
                { "NetworkTools.Tools.ShapeCurve.Description", "Allows editing the curve of a contiguous path." },
                // ### ShapeCurve - Hint Tooltips
                { "Common.ACTION[NetworkTools.HintTooltip.ShapeCurve.SelectStart]", "Select a starting node" },
                { "Common.ACTION[NetworkTools.HintTooltip.ShapeCurve.SelectSecond]", "Select a second node" },
                { "Common.ACTION[NetworkTools.HintTooltip.ShapeCurve.RemoveLast]", "Remove last node" },
                { "Common.ACTION[NetworkTools.HintTooltip.ShapeCurve.ExtendPath]", "Select a new end node to extend the path" },
                
                // ## SlideNode
                // ### SlideNode - Metadata
                { "NetworkTools.Tools.SlideNode.Name", "Slide Node" },
                { "NetworkTools.Tools.SlideNode.Description", "Allows sliding nodes along existing edges." },

                // ## SuperNode
                // ### SuperNode - Metadata
                { "NetworkTools.Tools.SuperNode.Name", "Super Node" },
                { "NetworkTools.Tools.SuperNode.Description", "Allows combining multiple nodes into one large intersection." },
                // ### SuperNode - Hint Tooltips
                { "Common.ACTION[NetworkTools.HintTooltip.SuperNode.SelectStart]", "Select a node" },
                { "Common.ACTION[NetworkTools.HintTooltip.SuperNode.SelectSecond]", "Add another node" },
                { "Common.ACTION[NetworkTools.HintTooltip.SuperNode.RemoveLast]", "Remove last node" },

                // ## Connect
                // ### Connect - Metadata
                { "NetworkTools.Tools.Connect.Name", "Connect Tools" },
                { "NetworkTools.Tools.Connect.Description", "Allows creating a new connection between two nodes in a number of ways." },

                // ## Parallel
                // ### Parallel - Metadata
                { "NetworkTools.Tools.Parallel.Name", "Parallel Tool" },
                { "NetworkTools.Tools.Parallel.Description", "Allows creating perfect parallel networks from a source network." },
                // ### Parallel - Hint Tooltips
                { "Common.ACTION[NetworkTools.HintTooltip.Parallel.SelectStart]", "Select a starting node" },
                { "Common.ACTION[NetworkTools.HintTooltip.Parallel.SelectSecond]", "Select a second node" },
                { "Common.ACTION[NetworkTools.HintTooltip.Parallel.RemoveLast]", "Remove last node" },
                { "Common.ACTION[NetworkTools.HintTooltip.Parallel.ExtendPath]", "Select a new end node to extend the path" },

                // ## Generate
                // ### Generate - Metadata
                { "NetworkTools.Tools.Generate.Name", "Generate Tool" },
                { "NetworkTools.Tools.Generate.Description", "Allows generating a variety of networks such as perfect road grids and circles." },
                // ### Generate - Hint Tooltips
                { "Common.ACTION[NetworkTools.HintTooltip.Generate.Place]", "Place network origin" },
                { "Common.ACTION[NetworkTools.HintTooltip.Generate.Rotate]", "Rotate" },
                { "Common.ACTION[NetworkTools.HintTooltip.Generate.SetDirection]", "Set direction" },
                { "Common.ACTION[NetworkTools.HintTooltip.Generate.RemovePlacement]", "Remove placement" },

                // # UI Strings
                // ## Common
                { "NetworkTools.UI.Common.NetworkTools", "Network Tools" },
                { "NetworkTools.UI.Common.Close", "Close" },
                { "NetworkTools.UI.Common.Mode", "Mode" },
                { "NetworkTools.UI.Common.ToggleAll", "Toggle All" },
                { "NetworkTools.UI.Common.SelectAtLeastTwoNodes", "Select at least two nodes." },
                { "NetworkTools.UI.Common.HowToUse", "How to use" },
                { "NetworkTools.UI.Common.Tutorial", "Select the tool to configure it. Adjust snapping, target selection, and view mode using the options in the panel. Each tool provides its own specific parameters below." },
                { "NetworkTools.UI.Common.ComingSoon", "Coming Soon!" },
                { "NetworkTools.UI.Common.Advanced", "Advanced" },
                { "NetworkTools.UI.Common.Decrease", "Decrease" },
                { "NetworkTools.UI.Common.Increase", "Increase" },

                // ## Prefab Search
                { "NetworkTools.UI.PrefabSearch.Title", "Select Asset" },
                { "NetworkTools.UI.PrefabSearch.Placeholder", "Search assets..." },
                { "NetworkTools.UI.PrefabSearch.Empty", "No assets found." },
                { "NetworkTools.UI.PrefabSearch.NetworkPrefab", "Asset" },
                { "NetworkTools.UI.PrefabSearch.RecentlyUsed", "Recently Used" },
                { "NetworkTools.UI.PrefabSearch.All", "All" },

                // ## Prefab Tabs
                { "NetworkTools.UI.PrefabTab.Road", "Road" },
                { "NetworkTools.UI.PrefabTab.Path", "Path" },
                { "NetworkTools.UI.PrefabTab.Rail", "Rail" },
                { "NetworkTools.UI.PrefabTab.Waterway", "Waterway" },
                { "NetworkTools.UI.PrefabTab.NetLane", "NetLane" },

                // ## View Options
                { "NetworkTools.UI.View.Label", "View" },
                { "NetworkTools.UI.View.Underground", "Underground" },
                { "NetworkTools.UI.View.ZoneGrid", "Zone Grid" },
                { "NetworkTools.UI.View.InvisibleNetworks", "Invisible Networks" },

                // ## Target Options
                { "NetworkTools.UI.Target.Label", "Targets" },
                { "NetworkTools.UI.Target.Road", "Road" },
                { "NetworkTools.UI.Target.Path", "Path" },
                { "NetworkTools.UI.Target.Rail", "Rail" },
                { "NetworkTools.UI.Target.Waterway", "Waterway" },
                { "NetworkTools.UI.Target.InvisiblePath", "InvisiblePath" },

                // ## Anarchy
                { "NetworkTools.UI.Anarchy.Label", "Anarchy" },
                { "NetworkTools.UI.Anarchy.Toggle", "Disable validation" },

                // ## Snap Options
                { "NetworkTools.UI.Snap.Label", "Snapping" },
                { "NetworkTools.UI.Snap.ZoneGrid", "Snap to zone grid" },
                { "NetworkTools.UI.Snap.MidPoint", "Snap to segment mid point" },
                { "NetworkTools.UI.Snap.ExistingGeometry", "Snap to existing Geometry" },
                { "NetworkTools.UI.Snap.ObjectSide", "Snap to the side of a building" },
                { "NetworkTools.UI.Snap.GuideLines", "Snap to guide lines" },

                // ## Slope Tool
                { "NetworkTools.UI.Slope.Preserve", "Preserve" },
                { "NetworkTools.UI.Slope.ConstantSlope", "Constant Slope" },
                { "NetworkTools.UI.Slope.EaseInOutSlope", "Ease-In-Out Slope" },
                { "NetworkTools.UI.Slope.ArchSlope", "Arched Slope" },
                { "NetworkTools.UI.Slope.StartingFlatness", "Starting Flatness" },
                { "NetworkTools.UI.Slope.EndingFlatness", "Ending Flatness" },
                { "NetworkTools.UI.Slope.ArchHeight", "Arch Height" },
                { "NetworkTools.UI.Slope.ArchPosition", "Arch Position" },
                { "NetworkTools.UI.Slope.SmoothStart", "Smooth start connection" },
                { "NetworkTools.UI.Slope.SmoothEnd", "Smooth end connection" },

                // ## Curve Tool
                { "NetworkTools.UI.Curve.Preserve", "Preserve" },
                { "NetworkTools.UI.Curve.StraightenCurve", "Straighten Curve" },
                { "NetworkTools.UI.Curve.SmoothCurve", "Smooth Curve" },
                { "NetworkTools.UI.Curve.SmoothingFactor", "Smoothing Factor" },
                { "NetworkTools.UI.Curve.AllowJunctionElevation", "Allow interior junction elevation modification" },
                { "NetworkTools.UI.Curve.JunctionElevationUnlimited", "Unlimited" },
                { "NetworkTools.UI.Curve.JunctionElevationLimit", "Maximum junction elevation change (m)" },
                { "NetworkTools.UI.Curve.JunctionElevationPrecise", "Precise maximum change (m)" },
                { "NetworkTools.UI.Curve.JunctionElevationExplanation", "Limit movement above or below the original junction height for this operation. Disabled or zero keeps the original height. Bounds may prevent a constant grade; endpoints and split pins remain fixed. Each new Apply starts a new movement allowance." },
                { "NetworkTools.UI.Curve.CombinedExplanation", "Fit slope along the smoothed path. Endpoints and split points stay fixed; interior junction elevation follows the permission and limit below. Conflicting split grades cannot be applied." },
                { "NetworkTools.HintTooltip.ShapeCurve.Invalid", "Cannot smooth this path. Reduce smoothing or change the selection." },

                // ## Connect Tool
                { "NetworkTools.UI.Connect.ProfileDisabled", "Smooth elevation profile is off." },
                { "NetworkTools.UI.Connect.ProfileAccepted", "Current preview accepted for Apply." },
                { "NetworkTools.UI.Connect.ProfileOffset", "The approach curve and node have different endpoint heights. This attachment is not supported." },
                { "NetworkTools.UI.Connect.ProfileMoved", "Profile endpoints must stay at the selected nodes. Restore the endpoint handles." },
                { "NetworkTools.UI.Connect.ProfileNativePending", "Waiting for native profile validation. Apply is unavailable." },
                { "NetworkTools.UI.Connect.ProfileNativeError", "The game rejected the current preview. Apply is unavailable." },
                { "NetworkTools.UI.Connect.ProfileNativeMismatch", "The native preview does not preserve the requested profile. Apply is unavailable." },
                { "NetworkTools.UI.Connect.ProfilePending", "Waiting for a current, stable preview. Apply is unavailable." },
                { "NetworkTools.UI.Connect.ProfileRejected", "The requested profile cannot be applied. Review the endpoint choices and curve handles." },
                { "NetworkTools.UI.Connect.SmoothElevationProfile", "Smooth elevation profile" },
                { "NetworkTools.UI.Connect.ProfileExplanation", "Match endpoint heights and approach grades. Choose the existing approach at each junction. Native validation may reject profiles it cannot preserve." },
                { "NetworkTools.UI.Connect.ProfileLoop", "Smooth elevation profile is unavailable for Loop." },
                { "NetworkTools.UI.Connect.StartApproach", "Start approach" },
                { "NetworkTools.UI.Connect.EndApproach", "End approach" },
                { "NetworkTools.UI.Connect.ProfileChoose", "Choose an approach edge." },
                { "NetworkTools.UI.Connect.ProfileUnavailable", "Approach context is unavailable or changed. Reselect the endpoint or approach." },
                { "NetworkTools.UI.Connect.None", "None" },
                { "NetworkTools.UI.Connect.SimpleCurve", "Simple Curve" },
                { "NetworkTools.UI.Connect.ComplexCurve", "Complex Curve" },
                { "NetworkTools.UI.Connect.Loop", "Loop" },
                { "NetworkTools.UI.Connect.LoopRadius", "Loop Radius" },
                { "NetworkTools.UI.Connect.LoopArcSide", "Arc Side" },
                { "NetworkTools.UI.Connect.LoopOuterArc", "Outer" },
                { "NetworkTools.UI.Connect.LoopInnerArc", "Inner" },

                // ## Parallel Tool
                { "NetworkTools.UI.Parallel.HorizontalOffset", "Horizontal Offset" },
                { "NetworkTools.UI.Parallel.VerticalOffset", "Vertical Offset" },
                { "NetworkTools.UI.Parallel.Direction", "Direction" },
                { "NetworkTools.UI.Parallel.Same", "Same" },
                { "NetworkTools.UI.Parallel.Reverse", "Reverse" },
                { "NetworkTools.UI.Parallel.Origin", "Origin" },
                { "NetworkTools.UI.Parallel.LeftEdge", "Left Edge" },
                { "NetworkTools.UI.Parallel.Center", "Center" },
                { "NetworkTools.UI.Parallel.RightEdge", "Right Edge" },

                // ## Generate Tool
                { "NetworkTools.UI.Generate.Grid", "Grid" },
                { "NetworkTools.UI.Generate.Circle", "Circle" },
                { "NetworkTools.UI.Generate.XSpacing", "Column Spacing" },
                { "NetworkTools.UI.Generate.ZSpacing", "Row Spacing" },
                { "NetworkTools.UI.Generate.XCount", "Columns" },
                { "NetworkTools.UI.Generate.ZCount", "Rows" },
                { "NetworkTools.UI.Generate.Radius", "Radius" },
                { "NetworkTools.UI.Generate.Oval", "Oval" },
                { "NetworkTools.UI.Generate.RadiusX", "Width Radius" },
                { "NetworkTools.UI.Generate.RadiusZ", "Depth Radius" },
                { "NetworkTools.UI.Generate.Elevation", "Elevation" },
                { "NetworkTools.UI.Generate.FollowTerrain", "Follow Terrain" },
                { "NetworkTools.UI.Generate.AltPrefabX", "Column - Use alternating asset" },
                { "NetworkTools.UI.Generate.AltNetPrefabX", "Asset" },
                { "NetworkTools.UI.Generate.AltEveryX", "Alternate every" },
                { "NetworkTools.UI.Generate.AltPrefabZ", "Row - Use alternating asset" },
                { "NetworkTools.UI.Generate.AltNetPrefabZ", "Asset" },
                { "NetworkTools.UI.Generate.AltEveryZ", "Alternate every" },
                { "NetworkTools.UI.PrefabSearch.None", "None" },
                { "NetworkTools.UI.PrefabSearch.SameAsSelected", "Same as selected" },

                // ## Apply Buttons
                { "NetworkTools.UI.Apply.ShapeSlope", "Apply Slope" },
                { "NetworkTools.UI.Apply.ShapeCurve", "Apply Transformation" },
                { "NetworkTools.UI.Apply.Connect", "Create Connection" },
                { "NetworkTools.UI.Apply.SuperNode", "Create Supernode" },
                { "NetworkTools.UI.Apply.Parallel", "Create parallel network" },
                { "NetworkTools.UI.Apply.Generate", "Generate" },
            };
        }

        /// <inheritdoc/>
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts) {
            return m_Localization;
        }

        /// <inheritdoc/>
        public void Unload() { }
    }
}
