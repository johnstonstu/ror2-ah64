"""Offline request identity checks only; no window enumeration/capture or game launch."""
import copy
import datetime
import importlib.util
import pathlib
import sys
import unittest

sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location('owned_capture', pathlib.Path(__file__).with_name('Capture-OwnedWindow.py'))
bridge = importlib.util.module_from_spec(spec)
spec.loader.exec_module(bridge)


class IdentityChecks(unittest.TestCase):
    def setUp(self):
        self.run = pathlib.Path('C:/fixture/execution-fixture')
        self.stage = dict(gameDirectory='C:/fixture/game', owner='fixture', reservation='fixture',
                          sourceSha='source', runtimeLockPath='C:/fixture/lease')
        self.lease = dict(owner='fixture', runToken='fixture', sourceSha='source', processIds=[17])
        self.request = dict(schema=1, runId=self.run.name, name='roll-entered', token='a'*32,
                            pid=17, executable=str(pathlib.Path(self.stage['gameDirectory'])/'Risk of Rain 2.exe'),
                            owner='fixture', reservation='fixture', sourceSha='source', lockPath='C:/fixture/lease',
                            bodyName='AH64Body(Clone)', bodyState='ServoDash', weaponState='Idle', phase='roll',
                            requestedUtc=bridge.utc())

    def test_valid_and_rejected_identities(self):
        bridge.validate_identity(self.stage, self.request, self.lease, self.run)
        for field, value in dict(runId='old', token='../wrong', name='other', pid=18, executable='other.exe',
                                 owner='other', reservation='other', sourceSha='other', lockPath='other',
                                 bodyName='other', phase='backflip').items():
            with self.subTest(field=field), self.assertRaises(RuntimeError):
                changed=copy.deepcopy(self.request); changed[field]=value
                bridge.validate_identity(self.stage, changed, self.lease, self.run)

    def test_expired_request(self):
        self.request['requestedUtc']=(datetime.datetime.now(datetime.timezone.utc)-datetime.timedelta(seconds=11)).isoformat()
        with self.assertRaises(RuntimeError):
            bridge.validate_identity(self.stage, self.request, self.lease, self.run)

    def test_changed_or_multiple_process_lease(self):
        for processes in ([18], [17,18], []):
            self.lease['processIds']=processes
            with self.subTest(processes=processes), self.assertRaises(RuntimeError):
                bridge.validate_identity(self.stage, self.request, self.lease, self.run)

    def test_identical_later_window_pixels_rejected(self):
        bridge.require_fresh_pixels('new', {'old'})
        with self.assertRaises(RuntimeError):
            bridge.require_fresh_pixels('old', {'old'})


if __name__ == '__main__':
    unittest.main()
