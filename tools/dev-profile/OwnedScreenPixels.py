"""Capture only verified unobstructed owned-game client pixels on the screen."""
import ctypes
import json
from ctypes import wintypes
import win32api, win32gui, win32process
from PIL import ImageGrab

SOURCE = 'Pillow.ImageGrab.grab(bbox=verified-owned-client-screen-bounds,all_screens=True)'


def intersects(a, b):
    return max(a[0], b[0]) < min(a[2], b[2]) and max(a[1], b[1]) < min(a[3], b[3])


def verified_bounds(hwnd, pid):
    if (not win32gui.IsWindowVisible(hwnd) or win32gui.IsIconic(hwnd)
            or win32gui.GetForegroundWindow() != hwnd
            or win32process.GetWindowThreadProcessId(hwnd)[1] != pid):
        raise RuntimeError('Owned game must be foreground, visible and non-minimized')
    client = win32gui.GetClientRect(hwnd)
    top_left = win32gui.ClientToScreen(hwnd, (client[0], client[1]))
    bottom_right = win32gui.ClientToScreen(hwnd, (client[2], client[3]))
    bounds = (*top_left, *bottom_right)
    virtual = (win32api.GetSystemMetrics(76), win32api.GetSystemMetrics(77),
               win32api.GetSystemMetrics(78), win32api.GetSystemMetrics(79))
    if (bounds[2] <= bounds[0] or bounds[3] <= bounds[1] or bounds[0] < virtual[0]
            or bounds[1] < virtual[1] or bounds[2] > virtual[0]+virtual[2] or bounds[3] > virtual[1]+virtual[3]):
        raise RuntimeError('Client bounds outside physical virtual-screen bounds')
    windows = []
    win32gui.EnumWindows(lambda window, unused: windows.append(window), None)
    if hwnd not in windows:
        raise RuntimeError('Owned game absent from current window order')
    dwm = ctypes.WinDLL('dwmapi')
    for other in windows[:windows.index(hwnd)]:
        if not win32gui.IsWindowVisible(other) or win32gui.IsIconic(other):
            continue
        cloaked = wintypes.DWORD()
        result = dwm.DwmGetWindowAttribute(wintypes.HWND(other), 14, ctypes.byref(cloaked), ctypes.sizeof(cloaked))
        if result == 0 and cloaked.value:
            continue
        if intersects(bounds, win32gui.GetWindowRect(other)):
            try: opacity=win32gui.GetLayeredWindowAttributes(other)
            except Exception as error: opacity={'unavailable':str(error)}
            raise RuntimeError('Visible window overlays the game client; no pixels captured or saved; ' + json.dumps(dict(
                ownedHwnd=hwnd, ownedPid=pid, clientScreenBounds=bounds, virtualScreen=virtual,
                overlayHwnd=other, overlayPid=win32process.GetWindowThreadProcessId(other)[1],
                overlayClass=win32gui.GetClassName(other), overlayBounds=win32gui.GetWindowRect(other),
                overlayStyle=win32gui.GetWindowLong(other,-16), overlayExStyle=win32gui.GetWindowLong(other,-20),
                overlayCloakedResult=result, overlayCloaked=cloaked.value, overlayLayeredAttributes=opacity,
                overlayCaption=bool(win32gui.GetWindowLong(other,-16)&0x00c00000),
                overlayOwner=win32gui.GetWindow(other,4),overlayPlacement=win32gui.GetWindowPlacement(other))))
    user32 = ctypes.WinDLL('user32')
    user32.GetAncestor.argtypes = [wintypes.HWND, wintypes.UINT]
    user32.GetAncestor.restype = wintypes.HWND
    # Confirm the client interior belongs to the target, including corners and center.
    for x in (bounds[0]+1, (bounds[0]+bounds[2])//2, bounds[2]-2):
        for y in (bounds[1]+1, (bounds[1]+bounds[3])//2, bounds[3]-2):
            hit = win32gui.WindowFromPoint((x, y))
            if user32.GetAncestor(hit, 2) != hwnd:
                raise RuntimeError('Game client is obstructed at a verified screen point')
    monitor = win32api.GetMonitorInfo(win32api.MonitorFromWindow(hwnd, 2))['Monitor']
    return bounds, dict(clientRect=client, screenBounds=bounds, virtualScreen=virtual,
                        monitorBounds=monitor, monitorOffset=(bounds[0]-monitor[0], bounds[1]-monitor[1]),
                        foregroundHwnd=hwnd, foregroundPid=pid, foregroundIsOwned=True, unobstructed=True)


def grab_client(hwnd, pid, utc):
    user32 = ctypes.WinDLL('user32', use_last_error=True)
    set_context = user32.SetThreadDpiAwarenessContext
    set_context.argtypes = [wintypes.HANDLE]
    set_context.restype = wintypes.HANDLE
    previous = set_context(ctypes.c_void_p(-4))  # Only this capture thread; never an OS/display setting.
    if not previous:
        raise ctypes.WinError(ctypes.get_last_error())
    try:
        bounds, before = verified_bounds(hwnd, pid)
        started = utc()
        image = ImageGrab.grab(bbox=bounds, all_screens=True)
        finished = utc()
        after_bounds, after = verified_bounds(hwnd, pid)
        if bounds != after_bounds or before != after:
            raise RuntimeError('Owned client identity/bounds changed; discard captured pixels')
        if (image.width, image.height) != (bounds[2]-bounds[0], bounds[3]-bounds[1]):
            raise RuntimeError('Original screen client dimensions mismatch')
        return image, dict(before, source=SOURCE, startedUtc=started, finishedUtc=finished,
                           dpiContext='capture-thread-per-monitor-v2; original context restored in finally')
    finally:
        if not set_context(previous):
            raise RuntimeError('Capture thread DPI context restoration failed')
