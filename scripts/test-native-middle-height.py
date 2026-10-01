"""Analytic checks independent of game capture values."""
import runpy,math
from pathlib import Path
m=runpy.run_path(str(Path(__file__).with_name('replay-native-middle-height.py')));f=m['bounds']
# Incompatible 10 m drop over 20 horizontal m at 20% collapses to halfway.
r=f(10,0,10,10,.2,8);assert r['incompatible'] and r['final']==[5,5]
# Equal heights and symmetric lengths: feasible interval contracts about height.
r=f(10,10,10,10,.2,10);assert not r['incompatible'] and r['final']==[9,11]
# Swap traversal: the middle interval is unchanged.
a=f(17,10,3,23,.2,8);b=f(10,17,23,3,.2,8);assert a==b
# Elevation translation changes bounds by that translation.
a=f(17,10,3,23,.2,8);b=f(117,110,3,23,.2,8);assert all(math.isclose(y-x,100) for x,y in zip(a['final'],b['final']))
c=[{'x':i*10.,'y':i*100.,'z':0.} for i in range(4)];assert math.isclose(m['horizontal_length'](c),30)
print('Five analytic middle-height/length checks passed')
