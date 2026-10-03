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


def validate(result, identity, records):
    problems = []
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
        phases = {r["phase"] for r in rows}
        required = {"Brake", "Turn"} if name == "interrupt" else {"Brake", "Turn", "Exit"}
        if not required.issubset(phases) or not any(r["lease"] and r["appliedSteps"] > 0 for r in rows):
            problems.append(name + " missing actual native lease/phase")
        if not any(r["phase"] == "inactive" and not r["lease"] and not r["visualOwner"] and not r["visualRecovery"] for r in rows):
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
    for label in ("missing-check", "duplicate-id", "native-y-corruption", "render-alarm"):
        mutated, telemetry = copy.deepcopy(result), copy.deepcopy(records)
        if label == "missing-check":
            mutated["checks"].pop()
        elif label == "duplicate-id":
            mutated["checks"][-1]["id"] = mutated["checks"][0]["id"]
        elif label == "native-y-corruption":
            row = next(r for r in telemetry if r.get("recordType") == "braking-sample" and r["appliedSteps"] > 0)
            row["appliedVelocity"]["y"] += 1.0
        else:
            mutated["renderViolations"] = 1
        reasons = validate(mutated, identity, telemetry)
        controls.append({"name": label, "rejected": bool(reasons), "reasons": reasons})
    if not all(c["rejected"] for c in controls):
        problems.append("negative control escaped")
    unchanged = before == {name: digest(run / name) for name in names}
    if not unchanged:
        raise RuntimeError("Raw evidence changed during audit")
    report = {"schema": 1, "suite": "braking-solo-v1", "status": "passed" if not problems else "failed", "runDirectory": str(run),
              "sourceSha": result["sourceSha"], "dllSha256": result["dllSha256"], "problems": problems,
              "checks": len(result["checks"]), "renderSamples": result["renderSamples"], "negativeControls": controls,
              "rawHashes": before, "rawUnchanged": unchanged, "strictRuntime": read("result.json"),
              "limit": "Audits native braking contract and four in-memory evidence mutants. Does not classify or erase raw strict errors/warnings; no gameplay, media, physical inputs, peers or damage acceptance."}
    destination.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps({k: report[k] for k in ("status", "problems", "checks", "renderSamples", "sourceSha", "dllSha256")}))
    return 0 if not problems else 1


if __name__ == "__main__":
    raise SystemExit(main())
