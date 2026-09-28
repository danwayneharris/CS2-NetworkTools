import sys
sys.dont_write_bytecode=True
import copy, importlib.util, unittest
from pathlib import Path
spec=importlib.util.spec_from_file_location('composition',Path(__file__).with_name('rail-composition-inputs.py'))
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
def curve(x):return [{'x':x,'y':0,'z':z} for z in (0,3,7,10)]
def fixture():
    node={'index':1,'version':1}; prefab={'netGeometryData':{'mergeLayers':'TrainTrack'}}
    return {'schemaVersion':2,'complete':True,'junction':node,'owners':[
        {**node,'prefab':prefab}, {'index':2,'version':1,'startNode':node,'endNode':{'index':3,'version':1},'prefab':prefab,
        'edgeGeometry':{'start':{'left':curve(-5),'right':curve(5)}},
        'composition':{'edge':{'width':10,'lanes':[{'index':1,'group':0,'flags':'Track',
            'position':{'x':2,'y':1,'z':0},'prefab':{'trackLaneData':{'maxCurviness':.02,'trackTypes':'Train'}}}]}}}]}
class Tests(unittest.TestCase):
    def test_start_endpoint(self):
        p=m.connect_positions(fixture())[0]
        self.assertEqual(p['position'],(2.,1.,0.));self.assertEqual(p['inwardTangent'],(0.,0.,-1.))
        self.assertFalse(p['source']);self.assertTrue(p['target']);self.assertEqual(p['maxCurviness'],.02)
    def test_reversed_edge_with_inverted_lane_is_same_physical_input(self):
        a=fixture();b=copy.deepcopy(a);e=b['owners'][1]
        e['startNode'],e['endNode']=e['endNode'],e['startNode']
        s=e['edgeGeometry'].pop('start')
        e['edgeGeometry']['end']={'left':s['right'][::-1],'right':s['left'][::-1]}
        lane=e['composition']['edge']['lanes'][0];lane['position']['x']=-2;lane['flags']='Track, Invert'
        self.assertEqual(m.connect_positions(a),m.connect_positions(b))
    def test_fail_closed(self):
        for field,value in [('schemaVersion',1),('complete',False)]:
            f=fixture();f[field]=value
            with self.assertRaises(ValueError):m.connect_positions(f)
        f=fixture();f['owners'][1]['composition']['edge']['lanes'][0]['flags']='Track, FindAnchor'
        with self.assertRaises(ValueError):m.connect_positions(f)
    def test_disconnected_and_twoway(self):
        f=fixture();lane=f['owners'][1]['composition']['edge']['lanes'][0]
        lane['flags']='Track, DisconnectedStart';self.assertEqual(m.connect_positions(f),[])
        lane['flags']='Track, Twoway';p=m.connect_positions(f)[0]
        self.assertTrue(p['source'] and p['target'])
if __name__=='__main__':unittest.main()