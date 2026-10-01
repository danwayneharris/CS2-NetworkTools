"""Analytic checks for diagnostic grade and sampled horizontal length."""
import runpy,unittest
from pathlib import Path
m=runpy.run_path(str(Path(__file__).with_name('terrain-regression.py')))
class Tests(unittest.TestCase):
    def edge(self,points):return {'index':1,'version':1,'prefab':'test','curve':[dict(x=x,y=y,z=z) for x,y,z in points]}
    def test_linear_grade(self):
        row=m['metrics']([self.edge([(0,0,0),(10,1,0),(20,2,0),(30,3,0)])])[0]
        self.assertAlmostEqual(row['sampledMaxAbsGradePercent'],10)
        self.assertAlmostEqual(row['sampledHorizontalLength'],30)
    def test_reversed_grade(self):
        row=m['metrics']([self.edge([(30,3,0),(20,2,0),(10,1,0),(0,0,0)])])[0]
        self.assertAlmostEqual(row['sampledGradeRange'][0],-10)
    def test_vertical_is_unknown(self):
        row=m['metrics']([self.edge([(0,0,0),(0,1,0),(0,2,0),(0,3,0)])])[0]
        self.assertIsNone(row['sampledMaxAbsGradePercent']);self.assertEqual(row['degenerateHorizontalTangents'],101)
if __name__=='__main__':unittest.main()
