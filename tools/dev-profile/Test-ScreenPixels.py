"""Offline window-policy fixtures; never read pixels, enumerate live windows or launch a game."""
import contextlib
import pathlib
import sys
import unittest
from unittest.mock import MagicMock, patch

sys.dont_write_bytecode = True
sys.path.insert(0, str(pathlib.Path(__file__).parent))
import OwnedScreenPixels as screen


class ScreenChecks(unittest.TestCase):
    def setUp(self):
        self.stack = contextlib.ExitStack()
        self.addCleanup(self.stack.close)
        self.windows = [99]
        self.virtual = [0, 0, 2, 1]
        self.dll = MagicMock()
        self.dll.SetThreadDpiAwarenessContext.return_value = 123
        self.dll.GetAncestor.return_value = 99
        self.dll.DwmGetWindowAttribute.return_value = 0
        values = {
            'IsWindowVisible': True, 'IsIconic': False, 'GetForegroundWindow': 99,
            'GetClientRect': (0, 0, 2, 1), 'GetWindowRect': (0, 0, 2, 1),
            'WindowFromPoint': 99, 'GetWindowLong': 0,
            'GetLayeredWindowAttributes': (0, 0, 0), 'GetClassName': 'fixture',
            'GetWindow': 0, 'GetWindowPlacement': (0, 1, (-1, -1), (-1, -1), (0, 0, 2, 1))
        }
        self.functions = {}
        for name, value in values.items():
            self.functions[name] = self.stack.enter_context(patch.object(screen.win32gui, name, return_value=value))
        self.stack.enter_context(patch.object(screen.win32gui, 'ClientToScreen', side_effect=lambda hwnd, point: point))
        self.stack.enter_context(patch.object(screen.win32gui, 'EnumWindows', side_effect=lambda callback, arg: [callback(w, arg) for w in self.windows]))
        self.stack.enter_context(patch.object(screen.win32process, 'GetWindowThreadProcessId', return_value=(1, 17)))
        self.stack.enter_context(patch.object(screen.win32api, 'GetSystemMetrics', side_effect=lambda index: self.virtual[index-76]))
        self.stack.enter_context(patch.object(screen.win32api, 'MonitorFromWindow', return_value=1))
        self.stack.enter_context(patch.object(screen.win32api, 'GetMonitorInfo', return_value={'Monitor': (0, 0, 2, 1)}))
        self.stack.enter_context(patch.object(screen.ctypes, 'WinDLL', return_value=self.dll))
        self.image = MagicMock(width=2, height=1)
        self.grab = self.stack.enter_context(patch.object(screen.ImageGrab, 'grab', return_value=self.image))

    def read(self):
        return screen.grab_client(99, 17, lambda: 'fixture-time')

    def test_owned_unobstructed_physical_client_only(self):
        image, evidence = self.read()
        self.assertIs(image, self.image)
        self.grab.assert_called_once_with(bbox=(0, 0, 2, 1), all_screens=True)
        self.assertTrue(evidence['foregroundIsOwned'] and evidence['unobstructed'])
        self.assertEqual(self.dll.SetThreadDpiAwarenessContext.call_args.args, (123,))

    def test_background_rejected_before_pixels(self):
        self.functions['GetForegroundWindow'].return_value = 100
        with self.assertRaises(RuntimeError): self.read()
        self.grab.assert_not_called()

    def test_outside_virtual_screen_rejected_before_pixels(self):
        self.virtual[2] = 1
        with self.assertRaises(RuntimeError): self.read()
        self.grab.assert_not_called()

    def test_unknown_layered_obstruction_rejected_before_pixels(self):
        self.windows = [100, 99]
        with self.assertRaisesRegex(RuntimeError, 'Visible window overlays'): self.read()
        self.grab.assert_not_called()

    def test_client_hit_test_obstruction_rejected_before_pixels(self):
        self.dll.GetAncestor.return_value = 100
        with self.assertRaises(RuntimeError): self.read()
        self.grab.assert_not_called()

    def test_changed_post_capture_bounds_discarded_and_context_restored(self):
        self.functions['GetClientRect'].side_effect = [(0, 0, 2, 1), (0, 0, 1, 1)]
        with self.assertRaisesRegex(RuntimeError, 'identity/bounds changed'): self.read()
        self.grab.assert_called_once()
        self.assertEqual(self.dll.SetThreadDpiAwarenessContext.call_args.args, (123,))

    def test_wrong_original_dimensions_rejected(self):
        self.image.width = 1
        with self.assertRaisesRegex(RuntimeError, 'dimensions mismatch'): self.read()


if __name__ == '__main__':
    unittest.main()
