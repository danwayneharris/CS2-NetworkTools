"""Run standalone generator fixtures; invoked by test-codegen.ps1, no game needed."""
import argparse
import hashlib
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

parser=argparse.ArgumentParser()
parser.add_argument('--dll',required=True)
args,remaining=parser.parse_known_args()
DLL=str(Path(args.dll).resolve())
DOTNET=shutil.which('dotnet') or r'C:/Program Files/dotnet/dotnet.exe'
ROOT=Path(__file__).resolve().parent.parent

class CodegenTests(unittest.TestCase):
    def generate(self, source, config='Debug', expect=True):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);folder=root/'source';folder.mkdir()
            (folder/'Fixture.cs').write_text(source,encoding='utf-8')
            output=root/'output.ts';output.write_text('sentinel',encoding='utf-8')
            result=subprocess.run([DOTNET,DLL,str(folder),str(output),'--configuration',config],text=True,capture_output=True)
            if expect:
                self.assertEqual(0,result.returncode,result.stderr)
                return output.read_text(encoding='utf-8')
            self.assertNotEqual(0,result.returncode)
            self.assertEqual('sentinel',output.read_text())
            self.assertIn('NTGEN001',result.stderr)
            return result.stderr

    def test_current_sources_preserve_prechange_output(self):
        # Pre-change complete output content, line-order independent: dictionary
        # enumeration/source-file traversal order does not affect the metadata.
        hashes={'Debug':'2b273d829ea268de3a1e77e39e1ae5da6aa96cdce1a9a970d59fa47936ded505',
                'Release':'a0ace91f207b46727bae9a40088874fac1f0b11b17ac33a669fb900eed23f359'}
        for config,digest in hashes.items():
            with self.subTest(config=config),tempfile.TemporaryDirectory() as d:
                output=Path(d)/'output.ts'
                result=subprocess.run([DOTNET,DLL,str(ROOT/'NetworkTools.Mod/Systems'),str(output),'--configuration',config],text=True,capture_output=True)
                self.assertEqual(0,result.returncode,result.stderr)
                lines=output.read_text(encoding='utf-8').splitlines()
                # Intentional additive metadata: verify every new line explicitly,
                # then retain the unchanged golden for all pre-existing parameters.
                added={
                    'combinedSlope: "roadShape.combinedSlope",',
                    '"roadShape.combinedSlope": { type: "bool", default: false, modes: 16, label: "NetworkTools.UI.Curve.CombinedSlope" },',
                    'combinedSlope: new TwoWayBinding<boolean>("roadShape.combinedSlope", false),',
                    '"roadShape.combinedSlope": PARAM_BINDINGS.roadShape.combinedSlope,'}
                actual=[line.strip() for line in lines if 'combinedSlope' in line]
                self.assertEqual(len(added),len(actual))
                self.assertEqual(added,set(actual))
                content='\n'.join(sorted(line for line in lines if 'combinedSlope' not in line))
                self.assertEqual(digest,hashlib.sha256(content.encode()).hexdigest())

    def test_golden_styles_and_configuration_options(self):
        source='''enum Mode { [EnumOption("label", "icon", Disabled = true)] A = 1, B = 2 }
enum NumberType { None, Distance }
class C {
 public EnumParameter<Mode> Mode = new("toy.mode", Mode.A);
 public FloatParameter Strength = new("toy.strength", -.5f, -1f, 1f, modes: (int)Mode.A | (int)Mode.B, fractionDigits: 2, numberType: NumberType.Distance, displayScale: 100f);
 public BoolParameter Flag = new("toy.flag", @default: true);
 public IntParameter Count = new IntParameter("toy.count", 2, 1, 5);
 public NetPrefabParameter Prefab = new("toy.prefab", nullable: true);
 public Float3Parameter Position = new("toy.position");
}'''
        for config in ('Debug','Release'):
            out=self.generate(source,config)
            self.assertIn('export enum Mode { A = 1, B = 2 }',out)
            self.assertIn('default: -0.5, min: -1, max: 1, fractionDigits: 2, displayScale: 100, numberType: "distance", modes: 3',out)
            self.assertIn('"toy.flag": { type: "bool", default: true, modes: 0 }',out)
            self.assertIn('"toy.prefab": { type: "netPrefab", modes: 0, nullable: true }',out)
            self.assertEqual(config=='Release','disabled: true' in out)

    def test_configuration_symbols_select_actual_declarations(self):
        source='''class C {
#if IS_DEBUG
 public BoolParameter Flag = new("toy.debug", true);
#else
 public BoolParameter Flag = new("toy.release", false);
#endif
#if EXPORT_EN_US
 public BoolParameter Export = new("toy.export", true);
#endif
}'''
        self.assertIn('toy.debug',self.generate(source,'Debug'))
        self.assertNotIn('toy.release',self.generate(source,'Debug'))
        self.assertIn('toy.release',self.generate(source,'Release'))
        self.assertIn('toy.export',self.generate(source,'I18N'))

    def test_unresolved_expressions_fail_with_location_and_preserve_output(self):
        declarations=[
            'public EnumParameter<Missing> P = new("toy.p", 0);',
            'public EnumParameter<Mode> P = new("toy.p", Mode.Missing);',
            'public EnumParameter<Mode> P = new("toy.p", Mode.A + 1);',
            'public BoolParameter P = new("toy.p", false, modes: (int)Mode.Missing);',
            'public BoolParameter P = new("toy.p", false, modes: (int)Mode.A + 1);',
            'public BoolParameter P = GetParameter();',
            'public FloatParameter P = new("toy.p", 1f + 2f, 0f, 10f);',
            'public FloatParameter P = new("toy.p", 1e99f, 0f, 10f);',
            'public FloatParameter P = new("toy.p", 1f, 0f, 10f, fractionDigits: Calculate());',
            'public FloatParameter P = new("toy.p", 1f, 0f, 10f, numberType: Unknown.Value);',
            'public BoolParameter P = new("toy.p", false, label: Labels.Name);',
            'public BoolParameter P = new("toy.p", false, typo: 2);',
            'public BoolParameter P = new("toy.p", false, 0, "ignored positional label");',
            'public BoolParameter P = new("toy.p");',
        ]
        for declaration in declarations:
            with self.subTest(declaration=declaration):
                error=self.generate('enum Mode { A = 1 }\nclass C {\n'+declaration+'\n}',expect=False)
                self.assertRegex(error,r'Fixture\.cs\(3,\d+\)')

    def test_invalid_enum_key_and_empty_failures(self):
        fixtures=[
            'enum Mode { A = 1 << 2 } class C { public EnumParameter<Mode> P = new("toy.p", Mode.A); }',
            'class C { public BoolParameter A = new("toy.p", true); public BoolParameter B = new("toy.p", false); }',
            'class C { public BoolParameter A = new("toy.x.y", true); public BoolParameter B = new("toy.xY", false); }',
            'class C { public BoolParameter A = new(Keys.P, true); }',
            'class C { public BoolParameter A = new("bad-key", true); }',
            'enum Mode { [EnumOption("x", "y", Visible = Something)] A } class C { public EnumParameter<Mode> P = new("toy.p", Mode.A); }',
            'class C {}',
        ]
        for source in fixtures:
            with self.subTest(source=source):self.generate(source,expect=False)

if __name__=='__main__':
    result=unittest.main(argv=[__file__,*remaining],exit=False).result
    if not result.wasSuccessful():raise SystemExit(1)
    print('Codegen regression checks passed.')
