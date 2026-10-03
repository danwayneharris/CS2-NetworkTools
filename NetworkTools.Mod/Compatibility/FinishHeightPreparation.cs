using System;
using Game.Net;
using Unity.Mathematics;

// Shared offline/runtime preparation. GeometrySystem.FinishEdgeGeometryJob consumes these
// exact producer fields before limiting slopes and calculating lengths/bounds.
// Prefilling them makes a missed native hash lookup harmless while preserving
// the rest of the original finishing calculation. No runtime hook is installed.
namespace NetworkTools.Compatibility {
internal static class FinishHeightPreparation {
    public static void Apply(ref EdgeGeometry geometry,int endpoint,float4 heights) {
        if(endpoint==0) {
            geometry.m_Start.m_Right.b.y=heights.x;
            geometry.m_Start.m_Right.a.y=heights.y;
            geometry.m_Start.m_Left.b.y=heights.z;
            geometry.m_Start.m_Left.a.y=heights.w;
        } else if(endpoint==1) {
            geometry.m_End.m_Left.c.y=heights.x;
            geometry.m_End.m_Left.d.y=heights.y;
            geometry.m_End.m_Right.c.y=heights.z;
            geometry.m_End.m_Right.d.y=heights.w;
        } else throw new ArgumentOutOfRangeException(nameof(endpoint));
    }
}

}
