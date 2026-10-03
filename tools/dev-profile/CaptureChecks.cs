// Compile the real bridge validator; these are protocol fixtures, never game evidence.
using System;
using System.IO;
using AH64;
internal static class CaptureChecks
{
    private static int checks;
    private static readonly DateTime Now=new DateTime(2026,10,3,12,0,2,DateTimeKind.Utc);
    private static readonly byte[] Png={137,80,78,71,13,10,26,10,0,0,0,13,73,72,68,82,0,0,10,0,0,0,5,160};
    private static WindowCaptureRequest Request() { return new WindowCaptureRequest {
        runId="execution-test",token="0123456789abcdef0123456789abcdef",name="roll-entered",pid=17,bodyId=42,
        phase="roll",bodyState="ServoDash",weaponState="Idle",executable="C:/Game/Risk of Rain 2.exe",
        sourceSha="source",owner="owner",reservation="lease",requestedUtc=Now.AddSeconds(-1).ToString("o")
    }; }
    private static WindowCaptureAck Ack() { var request=Request(); return new WindowCaptureAck {
        schema=1,status="captured",source="Pillow.ImageGrab.grab(bbox=verified-owned-client-screen-bounds,all_screens=True)",
        runId=request.runId,token=request.token,name=request.name,pid=request.pid,bodyId=request.bodyId,phase=request.phase,
        bodyState=request.bodyState,weaponState=request.weaponState,executable=request.executable,sourceSha=request.sourceSha,
        owner=request.owner,reservation=request.reservation,hwnd=99,width=2560,height=1440,nonflat=true,
        foregroundIsOwned=true,unobstructed=true,foregroundHwnd=99,foregroundPid=request.pid,
        screenBounds=new[]{0,0,2560,1440},virtualScreen=new[]{0,0,5120,1440},
        startedUtc=Now.AddSeconds(-.7).ToString("o"),finishedUtc=Now.AddSeconds(-.4).ToString("o"),png="roll-entered.png",pngSha256="hash"
    }; }
    private static void Validate(WindowCaptureAck ack,bool expired=false,string hash="hash",byte[] png=null) {
        WindowCaptureContract.Validate(Request(),ack,expired,hash,png??Png,Now);
    }
    private static void Reject(Action<WindowCaptureAck> change,string reason) {
        var ack=Ack(); change(ack);
        try { Validate(ack); } catch (InvalidDataException) { checks++; return; }
        throw new Exception("Accepted "+reason);
    }
    private static void RejectCall(Action operation,string reason) {
        try { operation(); } catch (InvalidDataException) { checks++; return; }
        throw new Exception("Accepted "+reason);
    }
    public static int Main() {
        Validate(Ack()); checks++;
        var negativeOrigin=Ack(); negativeOrigin.screenBounds=new[]{-2560,0,0,1440}; negativeOrigin.virtualScreen=new[]{-2560,0,5120,1440}; Validate(negativeOrigin); checks++;
        Reject(a=>a.source="Pillow.ImageGrab.grab(window=observed-owned-HWND)","cached HWND capture provenance");
        Reject(a=>a.foregroundIsOwned=false,"background capture"); Reject(a=>a.unobstructed=false,"obstructed capture");
        Reject(a=>a.foregroundPid=18,"wrong foreground process"); Reject(a=>a.foregroundHwnd=100,"wrong foreground window");
        Reject(a=>a.screenBounds=null,"missing client screen bounds"); Reject(a=>a.screenBounds=new[]{0,0,2560},"truncated client bounds");
        Reject(a=>a.virtualScreen=new[]{0,0,0,1440},"invalid virtual screen"); Reject(a=>a.screenBounds[2]=5120,"resized client bounds");
        Reject(a=>a.screenBounds=new[]{-2560,0,0,1440},"client outside physical screen");
        Reject(a=>a.runId="old","wrong run"); Reject(a=>a.token="old","stale token");
        Reject(a=>a.pid=18,"wrong PID"); Reject(a=>a.hwnd=0,"missing window");
        Reject(a=>a.executable="other.exe","wrong executable"); Reject(a=>a.reservation="other","wrong lease");
        Reject(a=>a.sourceSha="other","wrong candidate"); Reject(a=>a.bodyId=43,"wrong body");
        Reject(a=>a.bodyState="AH64Main","later state"); Reject(a=>a.phase="backflip","wrong phase");
        Reject(a=>a.weaponState="FireHellfire","wrong weapon state"); RejectCall(()=>Validate(Ack(),true),"expired phase");
        Reject(a=>a.startedUtc=Now.AddSeconds(-2).ToString("o"),"capture preceding request");
        Reject(a=>a.finishedUtc=Now.AddSeconds(-.9).ToString("o"),"reversed interval");
        Reject(a=>a.finishedUtc=Now.AddSeconds(1).ToString("o"),"future response");
        Reject(a=>a.nonflat=false,"flat pixels"); Reject(a=>a.width=512,"resized PNG");
        Reject(a=>a.png="other.png","wrong PNG"); RejectCall(()=>Validate(Ack(),false,"other"),"wrong hash");
        RejectCall(()=>Validate(Ack(),false,"hash",new byte[0]),"empty PNG");
        Console.WriteLine("CAPTURE_CHECK_PASS checks="+checks+" game=not-run images=not-produced"); return 0;
    }
}
