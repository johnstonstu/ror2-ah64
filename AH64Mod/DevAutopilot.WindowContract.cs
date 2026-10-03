using System;
using System.Globalization;
using System.IO;

namespace AH64
{
    // These DTO fields are populated by Unity's JSON serializer, not assignments visible to the compiler.
#pragma warning disable CS0649
    [Serializable] internal sealed class WindowCaptureRequest {
        public int schema=1, pid, bodyId, requestFrame;
        public float requestFixedTime, requestRealtime;
        public bool requiresProjectile;
        public string runId, token, name, phase, bodyName, bodyState, weaponState, executable, sourceSha,
            owner, reservation, lockPath, requestedUtc;
    }
    [Serializable] internal sealed class WindowCaptureAck {
        public int schema, pid, bodyId, width, height, foregroundPid;
        public long hwnd, foregroundHwnd;
        public int[] screenBounds, virtualScreen;
        public bool nonflat, foregroundIsOwned, unobstructed;
        public string status, runId, token, name, phase, bodyState, weaponState, executable, sourceSha,
            owner, reservation, startedUtc, finishedUtc, png, pngSha256, source;
    }
#pragma warning restore CS0649
    internal static class WindowCaptureContract
    {
        internal static void Validate(WindowCaptureRequest request, WindowCaptureAck ack, bool phaseInvalid,
            string actualHash, byte[] png, DateTime now)
        {
            if (ack==null || ack.schema!=1 || ack.status!="captured" ||
                ack.source!="Pillow.ImageGrab.grab(bbox=verified-owned-client-screen-bounds,all_screens=True)")
                throw new InvalidDataException("Missing/invalid window capture acknowledgement.");
            if (!ack.foregroundIsOwned || !ack.unobstructed || ack.foregroundHwnd!=ack.hwnd || ack.foregroundPid!=ack.pid ||
                ack.screenBounds==null || ack.screenBounds.Length!=4 || ack.virtualScreen==null || ack.virtualScreen.Length!=4 ||
                ack.virtualScreen[2]<=0 || ack.virtualScreen[3]<=0 || ack.width<=0 || ack.height<=0 ||
                (long)ack.screenBounds[2]-ack.screenBounds[0]!=ack.width || (long)ack.screenBounds[3]-ack.screenBounds[1]!=ack.height ||
                ack.screenBounds[0]<ack.virtualScreen[0] || ack.screenBounds[1]<ack.virtualScreen[1] ||
                ack.screenBounds[2]>(long)ack.virtualScreen[0]+ack.virtualScreen[2] ||
                ack.screenBounds[3]>(long)ack.virtualScreen[1]+ack.virtualScreen[3])
                throw new InvalidDataException("Missing/wrong owned foreground or unobstructed physical client bounds.");
            if (ack.runId!=request.runId || ack.token!=request.token || ack.name!=request.name || ack.pid!=request.pid ||
                ack.hwnd<=0 || !string.Equals(ack.executable,request.executable,StringComparison.OrdinalIgnoreCase) ||
                ack.sourceSha!=request.sourceSha || ack.owner!=request.owner || ack.reservation!=request.reservation)
                throw new InvalidDataException("Stale/wrong run, token, executable, PID/window or lease acknowledgement.");
            if (phaseInvalid || ack.bodyId!=request.bodyId || ack.phase!=request.phase ||
                ack.bodyState!=request.bodyState || ack.weaponState!=request.weaponState)
                throw new InvalidDataException("Requested body/state/phase did not survive the capture interval.");
            DateTime requested=DateTime.Parse(request.requestedUtc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind).ToUniversalTime();
            DateTime start=DateTime.Parse(ack.startedUtc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind).ToUniversalTime();
            DateTime end=DateTime.Parse(ack.finishedUtc,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind).ToUniversalTime();
            if (start<requested || end<start || end>now || (end-requested).TotalSeconds>10)
                throw new InvalidDataException("Stale/invalid capture interval.");
            if (!ack.nonflat || ack.png!=request.name+".png" || !string.Equals(ack.pngSha256,actualHash,StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Flat or wrong PNG/hash acknowledgement.");
            byte[] signature={137,80,78,71,13,10,26,10};
            if (png==null || png.Length<24) throw new InvalidDataException("Empty/truncated PNG.");
            for (int i=0;i<signature.Length;i++) if (png[i]!=signature[i]) throw new InvalidDataException("Invalid PNG signature.");
            if (png[12]!=73 || png[13]!=72 || png[14]!=68 || png[15]!=82 || ack.width<=0 || ack.height<=0 ||
                Dimension(png,16)!=ack.width || Dimension(png,20)!=ack.height)
                throw new InvalidDataException("PNG original dimensions do not match acknowledgement.");
        }
        private static long Dimension(byte[] png,int offset) {
            return ((long)png[offset]<<24)|((long)png[offset+1]<<16)|((long)png[offset+2]<<8)|png[offset+3];
        }
    }
}
