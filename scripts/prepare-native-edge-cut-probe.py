"""Read-only native edge-cut probes for the installed CS2 GeometrySystem.
Generate debugger expressions; does not execute them or write ECS components.
Check entity existence/version immediately before evaluation. This diagnostic is
version-sensitive and intentionally separate from production geometry code.
"""
import argparse,json
from pathlib import Path

INIT = "var s=world.GetExistingSystemManaged<Game.Net.GeometrySystem>(); var t=typeof(Game.Net.GeometrySystem).GetNestedType(\"CalculateEdgeGeometryJob\",System.Reflection.BindingFlags.NonPublic); var j=System.Activator.CreateInstance(t); j.m_PrefabRefDataFromEntity=s.GetComponentLookup<Game.Prefabs.PrefabRef>(true); j.m_OwnerData=s.GetComponentLookup<Game.Common.Owner>(true); j.m_EdgeData=s.GetComponentLookup<Game.Net.Edge>(true); j.m_NodeDataFromEntity=s.GetComponentLookup<Game.Net.Node>(true); j.m_CurveDataFromEntity=s.GetComponentLookup<Game.Net.Curve>(true); j.m_ElevationData=s.GetComponentLookup<Game.Net.Elevation>(true); j.m_CompositionDataFromEntity=s.GetComponentLookup<Game.Net.Composition>(true); j.m_OutsideConnectionData=s.GetComponentLookup<Game.Net.OutsideConnection>(true); j.m_NodeGeometryData=s.GetComponentLookup<Game.Net.NodeGeometry>(true); j.m_PrefabGeometryData=s.GetComponentLookup<Game.Prefabs.NetGeometryData>(true); j.m_PrefabCompositionData=s.GetComponentLookup<Game.Prefabs.NetCompositionData>(true); j.m_PlaceableNetData=s.GetComponentLookup<Game.Prefabs.PlaceableNetData>(true); j.m_PlaceableObjectData=s.GetComponentLookup<Game.Prefabs.PlaceableObjectData>(true); j.m_ObjectGeometryData=s.GetComponentLookup<Game.Prefabs.ObjectGeometryData>(true); j.m_NetLaneData=s.GetComponentLookup<Game.Prefabs.NetLaneData>(true); j.m_TempData=s.GetComponentLookup<Game.Tools.Temp>(true); j.m_HiddenData=s.GetComponentLookup<Game.Tools.Hidden>(true); j.m_Edges=s.GetBufferLookup<Game.Net.ConnectedEdge>(true); j.m_SubNets=s.GetBufferLookup<Game.Net.SubNet>(true); j.m_SubObjects=s.GetBufferLookup<Game.Objects.SubObject>(true); j.m_PrefabCompositionLanes=s.GetBufferLookup<Game.Prefabs.NetCompositionLane>(true); j.m_PrefabCompositionCrosswalks=s.GetBufferLookup<Game.Prefabs.NetCompositionCrosswalk>(true); j.m_PrefabCompositionPieces=s.GetBufferLookup<Game.Prefabs.NetCompositionPiece>(true); j.m_EdgeGeometryData=s.GetComponentLookup<Game.Net.EdgeGeometry>(true); j.m_StartNodeGeometryData=s.GetComponentLookup<Game.Net.StartNodeGeometry>(true); j.m_EndNodeGeometryData=s.GetComponentLookup<Game.Net.EndNodeGeometry>(true);"
def expression(index, version):
    code = INIT + f"var a=System.Array.CreateInstance(typeof(object),23);a.SetValue(entity({index},{version}),0);t.GetMethod(\"CalculateOffsets\").Invoke(j,a);"
    code += 'var so=(Unity.Mathematics.float2)a.GetValue(1);var eo=(Unity.Mathematics.float2)a.GetValue(2);var comp=(Game.Prefabs.NetCompositionData)a.GetValue(19);'
    # Cut is a private, pure native helper. Reflection avoids reproducing its math.
    for side, start, end, axis in [('l',9,11,'x'),('r',10,12,'y')]:
        code += f'var {side}s=(Colossal.Mathematics.Bezier4x3)a.GetValue({start});var {side}e=(Colossal.Mathematics.Bezier4x3)a.GetValue({end});'
        code += f'var {side}p=System.Array.CreateInstance(typeof(object),5);{side}p.SetValue({side}s,0);{side}p.SetValue({side}e,1);{side}p.SetValue(so.{axis},2);{side}p.SetValue(eo.{axis},3);{side}p.SetValue(comp.m_Width,4);'
        code += f'var {side}cut=t.GetMethod("CalculateCutOffset",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(j,{side}p);{side}p.SetValue({side}cut,4);'
        code += f'var {side}start=(Colossal.Mathematics.Bezier4x3)t.GetMethod("Cut",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(j,{side}p);'
        code += f'{side}p.SetValue(Colossal.Mathematics.MathUtils.Invert({side}e),0);{side}p.SetValue(Colossal.Mathematics.MathUtils.Invert({side}s),1);{side}p.SetValue(eo.{axis},2);{side}p.SetValue(so.{axis},3);'
        code += f'var {side}end=(Colossal.Mathematics.Bezier4x3)t.GetMethod("Cut",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(j,{side}p);'
    code += '$"offsets={so}/{eo}; startLeft={lstart.a}; startRight={rstart.a}; endLeft={lend.a}; endRight={rend.a}; startNode={((Game.Net.NodeGeometry)a.GetValue(17)).m_Position}; endNode={((Game.Net.NodeGeometry)a.GetValue(18)).m_Position}; flatness={((Game.Net.NodeGeometry)a.GetValue(17)).m_Flatness}/{((Game.Net.NodeGeometry)a.GetValue(18)).m_Flatness}; flags={((Game.Prefabs.NetGeometryData)a.GetValue(22)).m_Flags}; compFlags={comp.m_Flags}; startFlags={((Game.Prefabs.NetCompositionData)a.GetValue(20)).m_Flags}; endFlags={((Game.Prefabs.NetCompositionData)a.GetValue(21)).m_Flags}"'
    return code

if __name__ == '__main__':
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('entity',help='index:version')
    p.add_argument('--output',required=True)
    a=p.parse_args()
    index,version=map(int,a.entity.split(':'))
    Path(a.output).write_text(json.dumps({'entity':a.entity,'assumptions':['SmoothElevation, zero flatness, no FixedNodeSize or SymmetricalEdges','Offsets must not require clamping; check returned offsets against native num3','Does not include endpoint retention or junction flattening'],'code':expression(index,version)},indent=2))
