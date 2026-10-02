"""Generate local-only source replay from hash-pinned decompile. No game contact.

Algorithm bodies remain from local source; storage types and chunk entry signature
are adapted. Generated proprietary source goes only under ignored obj, not Git.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path


def block(text, marker):
    start = text.index(marker)
    opening = text.index('{', start)
    depth = 1
    end = opening + 1
    while depth:
        depth += (text[end] == '{') - (text[end] == '}')
        end += 1
    return text[start:end]


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--decompile', type=Path, required=True)
    p.add_argument('--project', type=Path, default=Path('NetworkTools.NativeReplay'))
    a = p.parse_args()
    expected = {'GeometrySystem.cs': '4D35623D0AAE5023348E218DC0078483FE1F100907911C2F4DE1FF259892C689',
                'EdgeIterator.cs': 'D99AC105FF6127A13B015CE1369AB4B8DCCDF5A3F2C482A2D4A6CBB28E106B31'}
    expected['NetCompositionHelpers.cs'] = '0D9ECED94C6ADBB0E6DF14F0A1715A5B78AC97F43EDCF393AAA2E7BC9951EDD3'
    sources = {}
    for name, checksum in expected.items():
        path = a.decompile / ('src/Game/Game.Prefabs' if name == 'NetCompositionHelpers.cs' else 'src/Game/Game.Net') / name
        data = path.read_bytes()
        if hashlib.sha256(data).hexdigest().upper() != checksum:
            raise ValueError('Native source changed; review adaptations before regeneration: ' + name)
        sources[name] = data.decode('utf-8-sig')
    geometry = sources['GeometrySystem.cs']
    iterator = block(sources['EdgeIterator.cs'], 'public struct EdgeIterator')
    # Sorting is not called by either selected stage; do not emulate its dependencies.
    iterator = iterator.replace(block(iterator, 'public void AddSorted('), '')
    pieces = [iterator]
    for name in ('InitializeNodeGeometryJob', 'FlattenNodeGeometryJob'):
        original = block(geometry, 'private struct ' + name)
        original = original.replace(block(original, 'void IJobChunk.Execute('), '')
        original = original.replace('private struct', 'public struct', 1).replace(' : IJobChunk', '')
        original = original.replace('in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask', 'in ReplayChunk chunk')
        pieces.append(original)
    for name in ('CalculateEdgeGeometryJob', 'FinishEdgeGeometryJob'):
        edge = block(geometry, 'private struct ' + name)
        edge = edge.replace('private struct', 'public struct', 1).replace(' : IJobParallelForDefer', '')
        pieces.append(edge)
    pieces.append(block(geometry, 'private struct EdgeData').replace('private struct', 'public struct', 1))
    pieces.append('public static class ReplayCompositionHelpers {\n' + block(sources['NetCompositionHelpers.cs'], 'public static float2 CalculateRoundaboutSize(') + '\n}')
    text = '\n'.join(pieces)
    # The raw stage decoder excludes native archetype handles. Fail regeneration
    # if this fixed stage closure starts reading them after a reviewed source change.
    for excluded in ('m_EdgeCompositionArchetype', 'm_NodeCompositionArchetype'):
        if excluded in text:
            raise ValueError('Stage now requires excluded native archetype: ' + excluded)
    text = re.sub(r'\[(?:ReadOnly|WriteOnly|NativeDisableParallelForRestriction)\]\s*', '', text)
    replacements = {'EdgeIterator': 'ReplayEdgeIterator', 'ComponentLookup': 'ReplayLookup',
        'BufferLookup': 'ReplayBufferLookup', 'DynamicBuffer': 'ReplayBuffer',
        'NativeArray': 'ReplayArray', 'EntityTypeHandle': 'ReplayEntityType',
        'ComponentTypeHandle': 'ReplayComponentType', 'NativeList': 'ReplayList',
        'NativeParallelHashMap': 'ReplayMap', 'Allocator.Temp': '0',
        'TerrainHeightData': 'ReplayTerrainData', 'TerrainUtils.SampleHeight': 'ReplayTerrain.SampleHeight',
        'NetCompositionHelpers.CalculateRoundaboutSize': 'ReplayCompositionHelpers.CalculateRoundaboutSize'}
    for old, new in replacements.items():
        text = re.sub(r'\b' + re.escape(old) + r'\b', new, text)
    header = '''// Local generated source. Do not commit or distribute.
using System;
using Game.Net;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Colossal.Mathematics;
using Unity.Entities;
using Unity.Mathematics;
using OutsideConnection = Game.Net.OutsideConnection;
using SubNet = Game.Net.SubNet;
namespace NativeReplay;
'''
    destination = a.project / 'obj/native-generated'
    destination.mkdir(parents=True, exist_ok=True)
    output = destination / 'WorldStages.g.cs'
    output.write_text(header + text, encoding='utf-8')
    ledger = dict(sourceHashes=expected, stages=['InitializeNodeGeometryJob', 'CalculateEdgeGeometryJob', 'FlattenNodeGeometryJob', 'FinishEdgeGeometryJob'],
        adaptations=['Rename storage/lookup types', 'Single explicit replay chunk entry point',
                     'Remove job scheduling/read-only attributes and interface forwarding',
                     'Exclude unused EdgeIterator.AddSorted', 'Replace temporary allocator argument with inert marker',
                     'Alias OutsideConnection/SubNet to original Game.Net namespace resolution',
                     'Source-adapt CalculateRoundaboutSize buffer helper; other game value helpers remain binary calls',
                     'Terrain sampling explicitly throws: terrain-dependent finishing is unsupported'],
        storageSemantics='See ReplayStorage.cs; no ECS scheduler or native allocation reproduced',
        generatedSha256=hashlib.sha256(output.read_bytes()).hexdigest().upper())
    (destination / 'adaptations.json').write_text(json.dumps(ledger, indent=2), encoding='utf-8')
    print('Generated local source replay and adaptation ledger: ' + str(destination))


if __name__ == '__main__': main()
