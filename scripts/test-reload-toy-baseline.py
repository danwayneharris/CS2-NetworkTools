"""Save guards only; does not launch or close CS2."""
import hashlib
import importlib.util
from pathlib import Path
import tempfile
import unittest
import zipfile

spec=importlib.util.spec_from_file_location('reload',Path(__file__).with_name('reload-toy-baseline.py'))
reload=importlib.util.module_from_spec(spec)
spec.loader.exec_module(reload)


class SaveGuardTests(unittest.TestCase):
    def test_exact_package_and_checksum_required(self):
        with tempfile.TemporaryDirectory() as root:
            p=Path(root)/'toy.cok'
            with zipfile.ZipFile(p,'w') as z:
                z.writestr('test.SaveGameMetadata.cid','fixture')
            checksum=hashlib.sha256(p.read_bytes()).hexdigest()
            self.assertEqual(reload.verified_package(root,p.name,checksum),p.resolve())
            with self.assertRaises(ValueError): reload.verified_package(root,p.name,'0'*64)
            with self.assertRaises(ValueError): reload.verified_package(root,'absent.cok')
            nested=Path(root)/'duplicate'; nested.mkdir()
            (nested/p.name).write_bytes(p.read_bytes())
            with self.assertRaises(ValueError): reload.verified_package(root,p.name,checksum)

    def test_zip_without_metadata_rejected(self):
        with tempfile.TemporaryDirectory() as root:
            p=Path(root)/'toy.cok'
            with zipfile.ZipFile(p,'w') as z: z.writestr('unrelated','data')
            with self.assertRaises(ValueError): reload.verified_package(root,p.name)


if __name__=='__main__': unittest.main()
