import sys
sys.dont_write_bytecode = True
import runpy
import unittest
from pathlib import Path
m = runpy.run_path(str(Path(__file__).with_name('explore-smoothing-controls.py')))


class Tests(unittest.TestCase):
    def test_negative_extrapolation_increases_original_wiggle(self):
        rows = m['experiment']()['rows']
        self.assertGreater(rows[0]['length'], rows[2]['length'])
        self.assertAlmostEqual(rows[0]['maximumLateralExcursion'], 2*rows[2]['maximumLateralExcursion'])
        self.assertAlmostEqual(rows[-1]['length'], 100)

    def test_split_ranges_cover_each_edge_once(self):
        ranges = m['split_ranges'](7, [4, 2])
        self.assertEqual([i for start, end in ranges for i in range(start, end)], list(range(6)))
        self.assertEqual(ranges, [(0, 2), (2, 4), (4, 6)])
        for splits in ([0], [6], [2, 2], [-1]):
            with self.assertRaises(ValueError): m['split_ranges'](7, splits)


if __name__ == '__main__': unittest.main()
