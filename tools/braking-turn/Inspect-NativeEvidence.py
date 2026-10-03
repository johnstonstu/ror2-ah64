"""Audit immutable braking SOLO evidence; keep strict launcher failure separate."""
import copy
import hashlib
import json
import math
import pathlib
import sys

sys.dont_write_bytecode = True
NAMES = ("stationary", "small", "fast", "spare", "collective", "interrupt")
IDS = ["catalog.order", "catalog.definition"] + [n + "." + c for n in NAMES for c in
       ("activation-capture", "midflight", "motion", "exit", "cleanup")] + [
       "guidance.entry", "guidance.turn-weapons", "guidance.release", "guidance.drain"]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def priority_error(result):
    return None if result.get("utilityInterruptPriority") == "PrioritySkill" else "wrong native utility priority"


def observed_turn(capture, velocity, completed):
    x, z = velocity["x"], velocity["z"]
    if x * x + z * z < 0.000001:
        return False
    entry, requested = capture["EntryHeading"], capture["RequestedHeading"]
    signed = math.degrees(math.atan2(entry["z"] * x - entry["x"] * z, entry["x"] * x + entry["z"] * z))
    correction = capture["SignedCorrection"]
    progress = abs(signed) if abs(signed) > 179.9 else signed * (1 if correction > 0 else -1 if correction < 0 else 0)
    alignment = abs(math.degrees(math.atan2(z * requested["x"] - x * requested["z"], x * requested["x"] + z * requested["z"])))
    return progress > 1 and alignment < abs(correction) - 1 and (not completed or alignment <= 3 and progress >= abs(correction) - 3)


def activation_window(rows):
    """Exclude ResetCase preparation; capture marks this native state instance."""
    indices = [i for i, row in enumerate(rows) if row.get("capture") not in (None, "none")]
    if not indices:
        return [], [], None, "missing current activation capture"
    first, last = indices[0], indices[-1]
    if indices != list(range(first, last + 1)):
        return [], [], None, "ambiguous current activation windows"
    active = rows[first:last + 1]
    capture = json.loads(active[0]["capture"])
    if any(json.loads(row["capture"]) != capture for row in active):
        return [], [], None, "current activation capture changed"
    tail = rows[last + 1:]
    # Release preserves the motor's last sample. The first inactive observation
    # can contain its final PreMove step, applied after the last active sample.
    steps = [row for row in active if row["appliedSteps"] > 0]
    if steps and tail and tail[0]["appliedSteps"] >= steps[-1]["appliedSteps"] and tail[0]["appliedAge"] >= steps[-1]["appliedAge"]:
        steps.append(tail[0])
    final = max(enumerate(steps), key=lambda pair: (pair[1]["appliedSteps"], pair[0]), default=(None, None))[1]
    return active, tail, final, None


def settled_cleanup(tail):
    return any(row["phase"] == "inactive" and not row["lease"] and not row["visualOwner"] and not row["visualRecovery"] for row in tail)


def turn_error(rows, name):
    if name not in ("small", "fast"):
        return None
    active, _, final, error = activation_window(rows)
    if error:
        return name + " " + error
    capture = json.loads(active[0]["capture"])
    middle = [r for r in active if r["phase"] == "Turn" and r["appliedAge"] >= 0.35]
    if not any(observed_turn(capture, r["appliedVelocity"], False) for r in middle):
        return name + " missing actual signed turn progress"
    if final is None or final["appliedAge"] < 0.95 or not observed_turn(capture, final["appliedVelocity"], True):
        return name + " actual final velocity misaligned"
    return None


def validate(result, identity, records):
    problems = []
    if priority_error(result):
        problems.append(priority_error(result))
    dll = [r for r in identity["replacements"] if pathlib.Path(r["target"]).name == "AH64.dll"]
    if len(dll) != 1 or result.get("dllSha256") != dll[0]["installed"]["sha256"]:
        problems.append("DLL identity mismatch")
    if result.get("sourceSha") != identity["sourceSha"]:
        problems.append("source identity mismatch")
    if result.get("schema") != 1 or result.get("suite") != "braking-solo-v1" or result.get("status") != "passed" or result.get("complete") is not True:
        problems.append("suite incomplete/failed")
    checks = result.get("checks", [])
    actual = [c.get("id") for c in checks]
    if result.get("expectedChecks") != len(IDS) or result.get("expectedIds") != IDS or sorted(actual) != sorted(IDS) or len(set(actual)) != len(IDS):
        problems.append("explicit IDs/count contract mismatch")
    if any(c.get("passed") is not True for c in checks):
        problems.append("failed check")
    if any(r.get("runId") != result.get("runId") for r in records):
        problems.append("telemetry run identity mismatch")
    samples = [r for r in records if r.get("recordType") == "braking-sample"]
    renders = [r for r in records if r.get("recordType") == "braking-render"]
    if not samples or not renders or result.get("renderSamples") != len(renders) or result.get("renderViolations") != 0:
        problems.append("render/sample evidence incomplete or alarmed")
    for name in NAMES:
        rows = [r for r in samples if r["scenario"] == "braking-" + name]
        active, tail, _, window_error = activation_window(rows)
        if window_error:
            problems.append(name + " " + window_error)
        steering_error = turn_error(rows, name)
        if steering_error and steering_error not in problems:
            problems.append(steering_error)
        phases = {r["phase"] for r in active}
        required = {"Brake", "Turn"} if name == "interrupt" else {"Brake", "Turn", "Exit"}
        if not required.issubset(phases) or not any(r["lease"] and r["appliedSteps"] > 0 for r in active):
            problems.append(name + " missing actual native lease/phase")
        if not settled_cleanup(tail):
            problems.append(name + " missing settled cleanup observation")
    for row in samples:
        if row["appliedSteps"] > 0 and row["nativeVelocity"]["y"] != row["appliedVelocity"]["y"]:
            problems.append("native PreMove Y overwritten")
            break
        for field in ("position", "velocity", "nativeAim", "nativeVelocity", "appliedVelocity"):
            if not all(math.isfinite(v) for v in row[field].values()):
                problems.append("nonfinite " + field)
                break
    return problems


def main():
    if sys.argv[1] == "--self-test":
        for correction in (30, 180, -30, -180):
            requested = {"x": math.sin(math.radians(correction)), "y": 0, "z": math.cos(math.radians(correction))}
            capture = {"EntryHeading": {"x": 0, "y": 0, "z": 1}, "RequestedHeading": requested, "SignedCorrection": correction}
            signed_middle = math.copysign(18, correction)
            middle = {"x": math.sin(math.radians(signed_middle)), "y": 0, "z": math.cos(math.radians(signed_middle))}
            assert observed_turn(capture, middle, False)
            assert observed_turn(capture, requested, True)
            assert not observed_turn(capture, capture["EntryHeading"], False)
            assert not observed_turn(capture, {"x": -middle["x"], "y": 0, "z": middle["z"]}, False)
        assert priority_error({"utilityInterruptPriority": "PrioritySkill"}) is None
        assert priority_error({"utilityInterruptPriority": "Skill"}) == "wrong native utility priority"
        capture = {"EntryHeading": {"x": 0, "y": 0, "z": 1}, "RequestedHeading": {"x": 0.5, "y": 0, "z": math.cos(math.radians(30))}, "SignedCorrection": 30}
        def sample(phase, age, steps, velocity, captured=True, recovery=False):
            return {"phase": phase, "appliedAge": age, "appliedSteps": steps, "appliedVelocity": velocity,
                    "capture": json.dumps(capture) if captured else "none", "lease": captured,
                    "visualOwner": captured, "visualRecovery": recovery}
        stale_zero = sample("inactive", 1, 60, {"x": 0, "y": 0, "z": 0}, False)
        stale_aligned = sample("inactive", 1, 60, capture["RequestedHeading"], False)
        current = [sample("Brake", 0.1, 6, capture["EntryHeading"]),
                   sample("Turn", 0.4, 24, {"x": math.sin(math.radians(18)), "y": 0, "z": math.cos(math.radians(18))}),
                   sample("Exit", 0.98, 59, capture["RequestedHeading"]),
                   sample("inactive", 1, 60, capture["RequestedHeading"], False, True)]
        settled = sample("inactive", 1, 60, capture["RequestedHeading"], False)
        assert turn_error([stale_zero] + current + [settled], "small") is None
        assert activation_window([stale_zero] + current)[2] is current[-1]
        assert not settled_cleanup(activation_window([stale_zero] + current)[1])
        assert settled_cleanup(activation_window([stale_zero] + current + [settled])[1])
        wrong_final = copy.deepcopy(current)
        wrong_final[-1]["appliedVelocity"] = capture["EntryHeading"]
        assert turn_error([stale_aligned] + wrong_final + [settled], "small") == "small actual final velocity misaligned"
        disabled = copy.deepcopy(current)
        for row in disabled:
            row["appliedVelocity"] = capture["EntryHeading"]
        assert turn_error([stale_aligned] + disabled + [settled], "small") == "small missing actual signed turn progress"
        assert activation_window(current + [settled] + current)[3] == "ambiguous current activation windows"
        assert not settled_cleanup(activation_window([stale_zero])[1])
        identity = {"sourceSha": "fixture", "replacements": [{"target": "AH64.dll", "installed": {"sha256": "fixture"}}]}
        result = {"sourceSha": "fixture", "dllSha256": "fixture", "utilityInterruptPriority": "PrioritySkill", "schema": 1,
                  "suite": "braking-solo-v1", "status": "passed", "complete": True, "checks": [{"id": name, "passed": True} for name in IDS],
                  "expectedChecks": len(IDS), "expectedIds": IDS, "runId": "fixture", "renderSamples": 1, "renderViolations": 0}
        records = [{"recordType": "braking-render", "runId": "fixture"}]
        for name in NAMES:
            for row in copy.deepcopy([stale_zero] + current + [settled]):
                row.update(recordType="braking-sample", runId="fixture", scenario="braking-" + name,
                           nativeVelocity=row["appliedVelocity"], position=capture["EntryHeading"],
                           velocity=row["appliedVelocity"], nativeAim=capture["RequestedHeading"])
                records.append(row)
        assert validate(result, identity, records) == []
        bad_cleanup = copy.deepcopy(records)
        for row in bad_cleanup:
            if row.get("scenario") == "braking-small" and row["phase"] == "inactive" and row["appliedVelocity"] != stale_zero["appliedVelocity"]:
                row["visualRecovery"] = True
        assert "small missing settled cleanup observation" in validate(result, identity, bad_cleanup)
        bad_final = copy.deepcopy(records)
        for row in bad_final:
            if row.get("scenario") == "braking-small" and row["phase"] == "inactive":
                row["appliedVelocity"] = capture["RequestedHeading"] if row["appliedVelocity"] == stale_zero["appliedVelocity"] else capture["EntryHeading"]
        assert "small actual final velocity misaligned" in validate(result, identity, bad_final)
        print("PASS 29 isolated auditor acceptance assertions, including complete-fixture stale prefix/current activation/post-exit cleanup and disabled-turn rejection; synthetic, no native execution")
        return 0
    run = pathlib.Path(sys.argv[1]).resolve()
    destination = pathlib.Path(sys.argv[2]).resolve()
    if destination.exists():
        raise RuntimeError("Refuse audit overwrite")
    names = ("identity.json", "braking-result.json", "telemetry.jsonl", "trace.txt", "result.json", "launcher-result.json")
    before = {name: digest(run / name) for name in names}
    read = lambda name: json.loads((run / name).read_text(encoding="utf-8-sig"))
    result, identity = read("braking-result.json"), read("identity.json")
    records = [json.loads(line) for line in (run / "telemetry.jsonl").read_text(encoding="utf-8-sig").splitlines()]
    problems = validate(result, identity, records)
    if result.get("runId") != run.name:
        problems.append("directory identity mismatch")
    controls = []
    expected_reasons = {"missing-check": "explicit IDs/count contract mismatch", "duplicate-id": "explicit IDs/count contract mismatch",
                        "native-y-corruption": "native PreMove Y overwritten", "render-alarm": "render/sample evidence incomplete or alarmed",
                        "wrong-priority": "wrong native utility priority", "turn-disabled": "small missing actual signed turn progress"}
    for label, expected_reason in expected_reasons.items():
        mutated, telemetry = copy.deepcopy(result), copy.deepcopy(records)
        if label == "missing-check":
            mutated["checks"].pop()
        elif label == "duplicate-id":
            mutated["checks"][-1]["id"] = mutated["checks"][0]["id"]
        elif label == "native-y-corruption":
            row = next(r for r in telemetry if r.get("recordType") == "braking-sample" and r["appliedSteps"] > 0)
            row["appliedVelocity"]["y"] += 1.0
        elif label == "render-alarm":
            mutated["renderViolations"] = 1
        elif label == "wrong-priority":
            mutated["utilityInterruptPriority"] = "Skill"
        else:
            captures = {name: json.loads(next(r["capture"] for r in telemetry if r.get("recordType") == "braking-sample"
                        and r["scenario"] == "braking-" + name and r["capture"] != "none")) for name in ("small", "fast")}
            for row in telemetry:
                if row.get("recordType") == "braking-sample" and row["scenario"] in ("braking-small", "braking-fast"):
                    entry = captures[row["scenario"].removeprefix("braking-")]["EntryHeading"]
                    speed = math.hypot(row["appliedVelocity"]["x"], row["appliedVelocity"]["z"])
                    row["appliedVelocity"]["x"], row["appliedVelocity"]["z"] = entry["x"] * speed, entry["z"] * speed
        reasons = validate(mutated, identity, telemetry)
        controls.append({"name": label, "rejected": expected_reason in reasons, "expectedReason": expected_reason, "reasons": reasons})
    if not all(c["rejected"] for c in controls):
        problems.append("negative control escaped")
    unchanged = before == {name: digest(run / name) for name in names}
    if not unchanged:
        raise RuntimeError("Raw evidence changed during audit")
    report = {"schema": 1, "suite": "braking-solo-v1", "status": "passed" if not problems else "failed", "runDirectory": str(run),
              "sourceSha": result["sourceSha"], "dllSha256": result["dllSha256"], "problems": problems,
              "checks": len(result["checks"]), "renderSamples": result["renderSamples"], "negativeControls": controls,
              "rawHashes": before, "rawUnchanged": unchanged, "strictRuntime": read("result.json"),
              "limit": "Audits native braking contract and six in-memory evidence mutants. Does not classify or erase raw strict errors/warnings; no gameplay, media, physical inputs, peers or damage acceptance."}
    destination.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps({k: report[k] for k in ("status", "problems", "checks", "renderSamples", "sourceSha", "dllSha256")}))
    return 0 if not problems else 1


if __name__ == "__main__":
    raise SystemExit(main())
