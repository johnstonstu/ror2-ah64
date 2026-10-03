"""Independent raw native smoke audit. Never modifies input evidence.

Synthetic controls prove rejection paths, not gameplay. Preserve strict result.json
independently even when this narrower feature audit passes.
"""
import argparse
import copy
import hashlib
import json
import math
from pathlib import Path

NAMES = ("stationary", "bleed", "curve", "interrupt", "invalid")
IDS = ["catalog.order", "catalog.definition"] + [f"{n}.{c}" for n in NAMES for c in ("activation", "opportunities", "motion", "payload", "cleanup")]


def xyz(v):
    return [v[k] for k in ("x", "y", "z")]


def distance(a, b):
    return math.sqrt(sum((x-y)**2 for x, y in zip(a, b)))


def audit(result, rows):
    try:
        return audit_impl(result, rows)
    except (KeyError, TypeError, ValueError, AttributeError, IndexError, OverflowError) as error:
        return [f"malformed native evidence: {type(error).__name__}: {error}"]


def audit_impl(result, rows):
    errors = []
    def need(ok, message):
        if not ok:
            errors.append(message)
    need(result.get("suite") == "bombing-solo-v1" and result.get("complete") is True and result.get("status") == "passed", "feature result incomplete or failed")
    need(result.get("hooksReleased") is True, "probe hooks not released")
    need(result.get("schema") == 1, "feature schema mismatch")
    need(len(result.get("sourceSha", "")) == 40 and len(result.get("dllSha256", "")) == 64, "invalid source/DLL identity")
    if result.get("runId"):
        need(all(r.get("runId") == result["runId"] for r in rows), "raw run identity mismatch")
    need(all(r.get("schemaVersion") == 1 for r in rows), "raw schema mismatch")
    checks = result.get("checks", [])
    need(len(checks) == len(IDS) and sorted(c.get("id", "") for c in checks) == sorted(IDS) and all(c.get("passed") is True for c in checks), "exact 27 check IDs/results missing")
    need(result.get("expectedChecks") == 27 and result.get("expectedIds") == IDS, "result expected IDs/count mismatch")
    summaries = [r for r in rows if r.get("recordType") == "summary"]
    need(len(summaries) == 1, "terminal raw summary missing or duplicated")
    header = [r for r in rows if r.get("recordType") == "header"]
    need(len(header) == 1 and header[0].get("sourceSha") == result.get("sourceSha") and header[0].get("dllSha256") == result.get("dllSha256"), "raw source/DLL identity mismatch")
    cast_ids = []
    for name in NAMES:
        case = [r for r in rows if r.get("scenario") == "bombing-" + name]
        obs = [r for r in case if r.get("recordType") == "bombing-observation"]
        damage = [r for r in case if r.get("recordType") == "bombing-damage"]
        need(all(math.isfinite(d.get("damageDealt", float("nan"))) and math.isfinite(d.get("rawDamage", float("nan")))
                 and math.isfinite(d.get("procCoefficient", float("nan"))) for d in damage), name + " nonfinite native damage/proc evidence")
        begins, ends = [[o for o in obs if o.get("kind") == kind] for kind in ("begin", "end")]
        need(len(begins) == 1 and len(ends) == 1, name + " unique cast begin/end missing")
        if len(begins) != 1 or len(ends) != 1:
            continue
        cid, owner = begins[0]["castId"], begins[0]["ownerId"]
        cast_ids.append(cid)
        need(cid != 0 and owner != 0 and all(o.get("castId") == cid and o.get("ownerId") == owner for o in obs), name + " cast/owner mismatch")
        release = [o for o in obs if o.get("kind") == "release"]
        terminal = [o for o in obs if o.get("kind") in ("impact", "despawn")]
        due = [o for o in obs if o.get("kind") == "release" or o.get("kind") == "suppressed" and o.get("reason") == "invalid-or-obstructed-origin"]
        count = len(due) if name == "interrupt" else 6
        need(0 < count < 6 if name == "interrupt" else count == 6, name + " interrupted opportunity count outside bounds")
        need(len(due) == count and sorted(o["dropIndex"] for o in due) == list(range(count)), name + " due opportunities missing or duplicate")
        need(ends[0]["reason"] == ("interrupted" if name == "interrupt" else "complete"), name + " wrong terminal reason")
        need(all(o["time"] <= ends[0]["time"] and abs(o["scheduledTime"] - o["dropIndex"] * .3) < .001
                 and -.001 <= o["actualTime"] - o["scheduledTime"] < .2 for o in due), name + " scheduling/post-exit release violation")
        if name == "invalid":
            need(not release and not damage and all(o["kind"] == "suppressed" and o["reason"] == "invalid-or-obstructed-origin" for o in due), "invalid origin spawned/damaged or wrong suppression")
        else:
            need(len(release) == count and len({o["projectileId"] for o in release}) == count and all(o["projectileId"] != 0 for o in release), name + " actual projectile attribution missing or duplicate")
        need(len(terminal) == len(release) and len({o["dropIndex"] for o in terminal}) == len(release)
             and all(any(t["dropIndex"] == r["dropIndex"] and 0 <= t["time"]-r["time"] <= 6.2 for r in release) for t in terminal), name + " native projectile terminal/lifetime evidence missing")
        cleanup = [r for r in case if r.get("recordType") == "bombing-cleanup"]
        need(len(cleanup) == 1 and cleanup[0]["castId"] == cid and cleanup[0]["time"] >= ends[0]["time"] and cleanup[0]["bombs"] == 0
             and all(cleanup[0][k] is False for k in ("targetAlive","targetMasterAlive","obstructionAlive"))
             and all(cleanup[0][k] is True for k in ("noRefund","originValid","overrideReferencesReleased"))
             and cleanup[0]["bleedBefore"] == cleanup[0]["bleedAfter"] and cleanup[0]["icbmBefore"] == cleanup[0]["icbmAfter"]
             and cleanup[0]["stockBefore"] > 0 and cleanup[0]["stockBeforeRestore"] == cleanup[0]["stockBefore"] - 1
             and cleanup[0]["weapon2State"] != "BombingRun", name + " post-activation native fixture cleanup missing")
        for o in release:
            origin = xyz(o["nativeCore"]); origin[1] -= .5
            velocity = xyz(o["nativeVelocity"]); velocity[1] = 0
            size = math.sqrt(sum(v*v for v in velocity))
            velocity = [v * min(1, 25/size) for v in velocity] if size else velocity
            velocity[1] = -4
            need(all(math.isfinite(v) for v in xyz(o["position"]) + xyz(o["velocity"])) and distance(origin, xyz(o["position"])) < .001 and distance(velocity, xyz(o["velocity"])) < .001, name + " actual origin/native velocity mismatch")
        if name == "curve":
            if len(release) >= 4:
                a, b, c = [xyz(release[i]["position"]) for i in (0, 2, -1)]
                early, late = [b[i]-a[i] for i in (0, 2)], [c[i]-b[i] for i in (0, 2)]
                e, l = math.hypot(*early), math.hypot(*late)
                angle = math.degrees(math.acos(max(-1, min(1, sum(x*y for x,y in zip(early,late))/(e*l))))) if e > .1 and l > .1 else 0
                need(angle > 5, "curve actual release path did not bend")
            overlaps = [r for r in case if r.get("eventName") == "bombing-native-overlap"]
            need(len(overlaps) == 1 and "actual=BrakingTurn/FireChaingun/FireRocketPods" in overlaps[0].get("reason", "")
                 and "primary=True hydra=True brake=True" in overlaps[0].get("reason", ""), "curve native simultaneous states missing")
            need(all(math.isfinite(o["airtime"]) and math.isfinite(o["targetHeight"]) and o["airtime"] >= 0 and o["jumpHeld"] is True for o in release)
                 and any(o["ascending"] for o in release), "curve native collective/hover resource evidence missing")
            need(all(o["icbmCount"] == 1 for o in release), "curve actual one-ICBM inventory missing")
        if name in ("stationary", "bleed"):
            fixtures = [r for r in case if r.get("recordType") == "bombing-fixture"]
            need(len(fixtures) == 1 and fixtures[0]["godMode"] is False and fixtures[0]["invulnerable"] is False and fixtures[0]["health"] > 0, name + " native target is missing/protected/dead")
            if len(fixtures) != 1:
                continue
            target = fixtures[0]["targetId"]
            hits = [o for o in obs if o.get("kind") == "hit" and o.get("targetId") == target]
            direct = [d for d in damage if d.get("bomb") and d.get("targetId") == target and d["damageDealt"] > 0 and not d["rejected"]]
            need(len(hits) == len(direct) == 3 and len({h["dropIndex"] for h in hits}) == len({d["dropIndex"] for d in direct}) == 3
                 and sorted(h["acceptedHits"] for h in hits) == [1,2,3], name + " actual native three-hit budget missing")
            release_by_drop = {o["dropIndex"]: o for o in release}
            need(all(d["castId"] == cid and d["ownerId"] == owner and d["dropIndex"] in release_by_drop
                     and d["projectileId"] == release_by_drop[d["dropIndex"]]["projectileId"] and abs(d["procCoefficient"] - .5) < .001 for d in direct), name + " native damage attribution/proc coefficient mismatch")
            need(sorted(d["dropIndex"] for d in direct) == sorted(h["dropIndex"] for h in hits)
                 and all(h["damage"] > 0 and abs(h["procCoefficient"] - .5) < .001 for h in hits), name + " native report/trace hits mismatch")
            need(any(o.get("kind") == "hit-suppressed" and o.get("targetId") == target and o.get("reason") == "overlap-budget" for o in obs), name + " actual overlap-budget suppression missing")
            for event in ("bombing-native-OnHitEnemy", "bombing-native-OnHitAll"):
                need(sum(r.get("eventName") == event for r in case) == 3, name + " separate " + event + " opportunities missing")
            if name == "bleed":
                need(fixtures[0]["pilotBleedChance"] >= 200 and any(d.get("dot") == "Bleed" and d["targetId"] == target
                     and d["ownerId"] == owner and d["damageDealt"] > 0 and not d["rejected"]
                     and direct and d["time"] >= min(x["time"] for x in direct) for d in damage), "bleed downstream positive native Bleed report missing")
    need(len(cast_ids) == len(set(cast_ids)) == 5, "cross-case cast reuse or incomplete cases")
    return errors


def synthetic():
    result = {"schema":1,"runId":"synthetic-only","suite":"bombing-solo-v1", "complete":True, "status":"passed", "hooksReleased":True, "expectedChecks":27,
              "expectedIds":IDS, "checks":[{"id":i,"passed":True} for i in IDS], "sourceSha":"a"*40, "dllSha256":"b"*64}
    rows = [{"recordType":"header","sourceSha":result["sourceSha"],"dllSha256":result["dllSha256"]},{"recordType":"summary","status":"failed"}]
    for ci,name in enumerate(NAMES,1):
        scenario = "bombing-"+name
        def observation(kind,drop=-1,**kw):
            value = dict(recordType="bombing-observation",scenario=scenario,kind=kind,reason="",castId=ci,ownerId=7,targetId=0,
                         dropIndex=drop,time=ci*10+max(drop,0)*.3,scheduledTime=max(drop,0)*.3,actualTime=max(drop,0)*.3,
                         nativeCore={"x":0,"y":2,"z":0},nativeVelocity={"x":0,"y":0,"z":0},position={"x":0,"y":1.5,"z":0},
                         velocity={"x":0,"y":-4,"z":0},projectileId=ci*100+drop+1,airtime=5,targetHeight=3,jumpHeld=name=="curve",ascending=name=="curve",icbmCount=1 if name=="curve" else 0,damage=39,procCoefficient=.5,acceptedHits=0)
            value.update(kw); rows.append(value)
        observation("begin")
        for drop in range(2 if name=="interrupt" else 6):
            core = {"x": max(0,drop-2)*.3 if name=="curve" else 0,"y":2,"z":min(drop,2)*.3 if name=="curve" else 0}
            origin = dict(core); origin["y"] -= .5
            observation("suppressed" if name=="invalid" else "release",drop,reason="invalid-or-obstructed-origin" if name=="invalid" else "",nativeCore=core,position=origin)
            if name!="invalid": observation("impact",drop,time=ci*10+drop*.3+.5)
        observation("end",reason="interrupted" if name=="interrupt" else "complete",time=ci*10+2)
        rows.append(dict(recordType="bombing-cleanup",scenario=scenario,castId=ci,time=ci*10+4,bombs=0,targetAlive=False,targetMasterAlive=False,obstructionAlive=False,
                         noRefund=True,originValid=True,overrideReferencesReleased=True,bleedBefore=0,bleedAfter=0,icbmBefore=0,icbmAfter=0,stockBefore=1,stockBeforeRestore=0,weapon2State="Idle"))
        if name=="curve": rows.append(dict(recordType="event",scenario=scenario,eventName="bombing-native-overlap",reason="primary=True hydra=True brake=True actual=BrakingTurn/FireChaingun/FireRocketPods"))
        if name in ("stationary","bleed"):
            rows.append(dict(recordType="bombing-fixture",scenario=scenario,targetId=9,health=480,godMode=False,invulnerable=False,pilotBleedChance=200 if name=="bleed" else 0))
            for drop in range(3):
                observation("hit",drop,targetId=9,acceptedHits=drop+1)
                rows.append(dict(recordType="bombing-damage",scenario=scenario,castId=ci,ownerId=7,targetId=9,bomb=True,dropIndex=drop,projectileId=ci*100+drop+1,damageDealt=39,rawDamage=39,procCoefficient=.5,rejected=False,dot="None",time=ci*10+drop*.3))
                for event in ("bombing-native-OnHitEnemy","bombing-native-OnHitAll"): rows.append(dict(recordType="event",scenario=scenario,eventName=event))
            observation("hit-suppressed",3,targetId=9,reason="overlap-budget")
            if name=="bleed": rows.append(dict(recordType="bombing-damage",scenario=scenario,ownerId=7,targetId=9,bomb=False,damageDealt=8,rawDamage=8,procCoefficient=0,rejected=False,dot="Bleed",time=ci*10+1))
    for row in rows:
        row["runId"] = result["runId"]; row["schemaVersion"] = 1
    return result,rows


def controls(result, rows):
    variants = []
    def add(name, reason, mutation):
        rr,tt = copy.deepcopy(result),copy.deepcopy(rows); mutation(rr,tt)
        failures = audit(rr,tt)
        variants.append(dict(name=name,expectedReason=reason,rejected=any(reason in f for f in failures),failures=failures))
    add("missing-check","exact 27 check", lambda r,t:r["checks"].pop())
    add("duplicate-drop","due opportunities", lambda r,t:next(x for x in t if x.get("scenario")=="bombing-stationary" and x.get("kind")=="release" and x["dropIndex"]==1).update(dropIndex=0))
    add("reported-hit-without-native-damage","actual native three-hit budget",lambda r,t:t.__setitem__(slice(None),[x for x in t if not(x.get("scenario")=="bombing-stationary" and x.get("recordType")=="bombing-damage")]))
    add("OnHit-without-downstream-Bleed","downstream positive native Bleed",lambda r,t:t.__setitem__(slice(None),[x for x in t if not(x.get("scenario")=="bombing-bleed" and x.get("dot")=="Bleed")]))
    add("wrong-native-proc","damage attribution/proc",lambda r,t:next(x for x in t if x.get("recordType")=="bombing-damage" and x.get("bomb")).update(procCoefficient=0))
    add("wrong-target","native three-hit budget",lambda r,t:next(x for x in t if x.get("recordType")=="bombing-damage" and x.get("bomb")).update(targetId=999))
    add("no-curve","actual release path did not bend",lambda r,t:[x.update(position={"x":0,"y":1.5,"z":0},nativeCore={"x":0,"y":2,"z":0}) for x in t if x.get("scenario")=="bombing-curve" and x.get("kind")=="release"])
    add("post-exit-drop","post-exit release",lambda r,t:next(x for x in t if x.get("scenario")=="bombing-interrupt" and x.get("kind")=="release").update(time=999))
    add("unclean-hooks","probe hooks not released",lambda r,t:r.update(hooksReleased=False))
    add("missing-native-overlap","native simultaneous states",lambda r,t:t.__setitem__(slice(None),[x for x in t if x.get("eventName")!="bombing-native-overlap"]))
    add("protected-target","target is missing/protected",lambda r,t:next(x for x in t if x.get("recordType")=="bombing-fixture").update(godMode=True))
    add("infinite-native-damage","nonfinite native damage/proc",lambda r,t:next(x for x in t if x.get("recordType")=="bombing-damage").update(damageDealt=float("inf")))
    add("NaN-native-rawDamage","nonfinite native damage/proc",lambda r,t:next(x for x in t if x.get("recordType")=="bombing-damage").update(rawDamage=float("nan")))
    add("missing-projectile-terminal","native projectile terminal/lifetime",lambda r,t:t.__setitem__(slice(None),[x for x in t if not(x.get("scenario")=="bombing-stationary" and x.get("kind")=="impact" and x["dropIndex"]==0)]))
    add("stale-prefix-cleanup","post-activation native fixture cleanup",lambda r,t:next(x for x in t if x.get("scenario")=="bombing-stationary" and x.get("recordType")=="bombing-cleanup").update(time=0))
    add("no-actual-ICBM","actual one-ICBM inventory",lambda r,t:[x.update(icbmCount=0) for x in t if x.get("scenario")=="bombing-curve" and x.get("kind")=="release"])
    add("no-native-collective","native collective/hover resource",lambda r,t:[x.update(jumpHeld=False,ascending=False) for x in t if x.get("scenario")=="bombing-curve" and x.get("kind")=="release"])
    return variants


def main():
    parser = argparse.ArgumentParser(); parser.add_argument("run",nargs="?",type=Path); parser.add_argument("--self-test",action="store_true"); parser.add_argument("--output",type=Path)
    args = parser.parse_args()
    if args.self_test:
        result,rows=synthetic(); errors=audit(result,rows); negative=controls(result,rows)
        early = audit({},rows[:1]); malformed = audit(result,[None])
        regressions = [dict(name="early-aborted",rejected=bool(early),failures=early),dict(name="malformed-row",rejected=bool(malformed),failures=malformed)]
        report=dict(status="passed" if not errors and all(c["rejected"] for c in negative+regressions) else "failed",syntheticOnly=True,positiveErrors=errors,
                    negativeControls=negative,partialRunRegressions=regressions,assertions=1+len(negative)+len(regressions))
    else:
        if not args.run: parser.error("run directory required")
        files=[args.run / n for n in ("telemetry.jsonl","bombing-result.json","result.json","identity.json")]
        if args.output and args.output.resolve() in [p.resolve() for p in files]: parser.error("audit output must not overwrite a raw input")
        errors=[]
        def raw_hashes():
            values={}
            for path in files:
                try: values[path.name]=hashlib.sha256(path.read_bytes()).hexdigest()
                except OSError: values[path.name]=None
            return values
        def read_json(path):
            try:
                value=json.loads(path.read_text(encoding="utf-8-sig"))
                if not isinstance(value,dict): raise ValueError("expected JSON object")
                return value
            except (OSError,ValueError) as error:
                errors.append(f"missing/invalid {path.name}: {type(error).__name__}: {error}"); return {}
        before=raw_hashes(); result=read_json(files[1]); strict=read_json(files[2]); identity=read_json(files[3]); rows=[]
        try:
            for number,line in enumerate(files[0].read_text(encoding="utf-8-sig").splitlines(),1):
                try:
                    row=json.loads(line)
                    if not isinstance(row,dict): raise ValueError("expected JSON row object")
                    rows.append(row)
                except ValueError as error: errors.append(f"invalid telemetry row {number}: {error}")
        except OSError as error: errors.append(f"missing/unreadable telemetry.jsonl: {error}")
        errors.extend(audit(result,rows))
        synthetic_result,synthetic_rows=synthetic(); negative=controls(synthetic_result,synthetic_rows)
        if identity.get("schema")!=1 or identity.get("status")!="staged" or identity.get("sourceSha")!=result.get("sourceSha"): errors.append("staged identity schema/status/source mismatch")
        replacements=identity.get("replacements",[])
        if not isinstance(replacements,list): errors.append("malformed staged replacement list"); replacements=[]
        installed=[r for r in replacements if isinstance(r,dict) and str(r.get("target","")).replace("\\","/").split("/")[-1]=="AH64.dll"]
        installed_value=installed[0].get("installed") if len(installed)==1 else None
        if not isinstance(installed_value,dict) or str(installed_value.get("sha256","")).upper()!=str(result.get("dllSha256","")).upper(): errors.append("staged installed AH64.dll identity mismatch")
        if result.get("runId")!=args.run.name or strict.get("runId")!=args.run.name or strict.get("schema")!=1: errors.append("execution runId/strict schema mismatch")
        if not all(c["rejected"] for c in negative): errors.append("synthetic negative control escaped")
        after=raw_hashes()
        if before!=after: errors.append("raw evidence changed while auditing")
        report=dict(status="passed" if not errors else "failed",run=str(args.run),errors=errors,rawHashes=before,rawUnchanged=before==after,negativeControls=negative,negativeControlsSyntheticOnly=True,
                    strictRuntime=dict(status=strict.get("status"),errors=strict.get("errors"),warnings=strict.get("warnings"),visualFlags=strict.get("visualFlags")),
                    limitation="Feature audit does not waive strict failures or establish controls/feel/peers/audio/media/full terrain acceptance")
    output=json.dumps(report,indent=2)
    if args.output:
        with args.output.open("x",encoding="utf-8") as stream: stream.write(output+"\n")
    print(output); return 0 if report["status"]=="passed" else 1


if __name__=="__main__": raise SystemExit(main())
