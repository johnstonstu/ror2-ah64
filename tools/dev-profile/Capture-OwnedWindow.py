"""Five SOLO baseline checkpoints only; never capture a desktop or unrelated window."""
import ctypes
import datetime
import hashlib
import json
import os
import pathlib
import sys
import time
from ctypes import wintypes
import win32api
import win32gui
import win32process
from OwnedScreenPixels import SOURCE, grab_client

NAMES = {'roll-entered', 'roll-cleanup', 'backflip-entered', 'backflip-cleanup', 'hellfire-launched'}


def require_fresh_pixels(png_hash, previous_hashes):
    if png_hash in previous_hashes:
        raise RuntimeError('Repeated window pixels across distinct moving baseline phases; cached/stale presentation cannot qualify')


def utc():
    return datetime.datetime.now(datetime.timezone.utc).isoformat()


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def validate_identity(stage, request, lease, run):
    expected = str(pathlib.Path(stage['gameDirectory']) / 'Risk of Rain 2.exe')
    if (request['schema'] != 1 or request['runId'] != run.name or request['name'] not in NAMES
            or len(request['token']) != 32 or any(c not in '0123456789abcdef' for c in request['token'])):
        raise RuntimeError('Wrong run/checkpoint/token request')
    for request_key, stage_key, lease_key in (
            ('owner', 'owner', 'owner'), ('reservation', 'reservation', 'runToken'),
            ('sourceSha', 'sourceSha', 'sourceSha'), ('lockPath', 'runtimeLockPath', None)):
        if request[request_key] != stage[stage_key] or (lease_key and request[request_key] != lease[lease_key]):
            raise RuntimeError('Canonical lease/stage/request mismatch: ' + request_key)
    if lease['processIds'] != [request['pid']] or request['pid'] <= 0:
        raise RuntimeError('Wrong owned process request')
    if os.path.normcase(request['executable']) != os.path.normcase(expected):
        raise RuntimeError('Wrong executable request')
    if (not request['bodyName'].startswith('AH64Body') or not request['bodyState'] or not request['weaponState']
            or request['phase'] != request['name'].split('-')[0]):
        raise RuntimeError('Wrong AH64 body/phase request')
    age = (datetime.datetime.now(datetime.timezone.utc) - datetime.datetime.fromisoformat(request['requestedUtc'])).total_seconds()
    if age < 0 or age > 10:
        raise RuntimeError('Stale/future request')


def process_image(pid):
    handle = win32api.OpenProcess(0x1000, False, pid)
    query = ctypes.windll.kernel32.QueryFullProcessImageNameW
    query.argtypes = [wintypes.HANDLE, wintypes.DWORD, wintypes.LPWSTR, ctypes.POINTER(wintypes.DWORD)]
    query.restype = wintypes.BOOL
    image_path = ctypes.create_unicode_buffer(32768)
    size = wintypes.DWORD(len(image_path))
    try:
        if not query(int(handle), 0, image_path, ctypes.byref(size)):
            raise ctypes.WinError()
        return image_path.value
    finally:
        handle.Close()


def owned_window(pid, executable):
    if os.path.normcase(process_image(pid)) != os.path.normcase(executable):
        raise RuntimeError('Owned PID executable changed')
    windows = []

    def collect(hwnd, unused):
        if (win32process.GetWindowThreadProcessId(hwnd)[1] == pid
                and win32gui.IsWindowVisible(hwnd) and not win32gui.IsIconic(hwnd)):
            windows.append(hwnd)

    win32gui.EnumWindows(collect, None)
    if len(windows) != 1:
        raise RuntimeError('Expected exactly one visible owned game window: ' + str(len(windows)))
    return windows[0]


def capture(stage, request, run, request_path, previous_hashes):
    lease_path = pathlib.Path(stage['runtimeLockPath'])
    validate_identity(stage, request, read(lease_path), run)
    if request_path.name != request['token'] + '.request.json':
        raise RuntimeError('Request filename/token mismatch')
    hwnd = owned_window(request['pid'], request['executable'])
    client = win32gui.GetClientRect(hwnd)
    image, screen = grab_client(hwnd, request['pid'], utc)
    # Recheck the lease, executable and unique HWND after reading real pixels.
    validate_identity(stage, request, read(lease_path), run)
    if owned_window(request['pid'], request['executable']) != hwnd or win32gui.GetClientRect(hwnd) != client:
        raise RuntimeError('Owned game window/dimensions changed during capture')
    if (image.width, image.height) != (client[2] - client[0], client[3] - client[1]):
        raise RuntimeError('Capture does not match original game client dimensions')
    extrema = image.convert('RGB').getextrema()
    if not any(low != high for low, high in extrema):
        raise RuntimeError('Flat game-window pixels; visual evidence unavailable')
    png = run / (request['name'] + '.png')
    if png.exists():
        raise RuntimeError('Refusing an existing checkpoint PNG')
    temporary = png.with_suffix('.png.tmp')
    image.save(temporary, format='PNG', compress_level=1)
    png_hash = hashlib.sha256(temporary.read_bytes()).hexdigest().upper()
    try:
        require_fresh_pixels(png_hash, previous_hashes)
    except RuntimeError:
        # Preserve rejected real pixels without labelling them as an accepted phase.
        temporary.replace(request_path.with_name(request['token'] + '.rejected.png'))
        raise
    temporary.replace(png)
    evidence = {key: request[key] for key in ('runId', 'token', 'name', 'pid', 'executable', 'sourceSha',
                                             'owner', 'reservation', 'phase', 'bodyId', 'bodyState', 'weaponState')}
    evidence.update(screen)
    evidence.update(schema=1, status='captured', source=SOURCE, hwnd=hwnd,
                    windowTitle=win32gui.GetWindowText(hwnd), clientRect=client,
                    foregroundIsOwned=win32gui.GetForegroundWindow() == hwnd,
                    request=request, acknowledgedUtc=utc(),
                    width=image.width, height=image.height, rgbExtrema=extrema, nonflat=True,
                    png=png.name, pngSha256=png_hash,
                    limitation='Actual unobstructed owned-client screen pixels; request phase and capture interval only, no exact rendered-frame identity')
    ack = request_path.with_name(request['token'] + '.ack.json')
    temporary = ack.with_suffix('.json.tmp')
    temporary.write_text(json.dumps(evidence, indent=2), encoding='utf-8')
    temporary.replace(ack)
    previous_hashes.add(png_hash)
    print(json.dumps(evidence), flush=True)


def main():
    stage = read(pathlib.Path(sys.argv[1]))
    run = pathlib.Path(sys.argv[2])
    if run.parent != pathlib.Path(stage['runDirectory']) or not run.name.startswith('execution-'):
        raise RuntimeError('Run directory does not belong to the supplied stage')
    seen = set()
    previous_hashes = set()
    deadline = time.monotonic() + 300
    while time.monotonic() < deadline:
        if (run / 'result.json').exists():
            terminal = dict(schema=1, status='completed', runId=run.name, helperPid=os.getpid(),
                            checkpointNames=sorted(name for name in seen if not name.endswith('.json')), finishedUtc=utc())
            (run / 'window-helper-result.json').write_text(json.dumps(terminal, indent=2), encoding='utf-8')
            return
        for path in (run / 'window-captures').glob('*.request.json'):
            if path.name in seen:
                continue
            request = read(path)
            if request['name'] in {name for name in seen if not name.endswith('.json')}:
                raise RuntimeError('Repeated checkpoint request')
            capture(stage, request, run, path, previous_hashes)
            seen.update((path.name, request['name']))
        time.sleep(.005)
    raise RuntimeError('Bounded owned-window helper deadline')


if __name__ == '__main__':
    main()
