"""Author the initial rotor bank via local Wwise 2023.1.4 WAAPI on port 8097.

Run with AH64Audio.wproj open in a dedicated WwiseConsole waapi-server.
The project is saved and closed before adding RTPC curves to its XML.
Subsequent builds use WwiseConsole generate-soundbank; this is setup tooling.
"""
import json
from pathlib import Path
import urllib.request
import uuid
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent
PROJECT = ROOT / "Art/Wwise/AH64Audio"
SOUND = r"\Actor-Mixer Hierarchy\Default Work Unit\AH64Rotor"


def call(uri, args=None, options=None):
    data = json.dumps({"uri": "ak.wwise." + uri, "args": args or {}, "options": options or {}}).encode()
    request = urllib.request.Request("http://127.0.0.1:8097/waapi", data, {"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(request) as response:
            return json.load(response)
    except urllib.error.HTTPError as error:
        raise RuntimeError(uri + ": " + error.read().decode()) from error


def create(parent, kind, name, **properties):
    return call("core.object.create", dict(parent=parent, type=kind, name=name,
                onNameConflict="merge", **properties))


def prop(obj, name, value):
    call("core.object.setProperty", dict(object=obj, property=name, value=value))


def guid():
    return "{" + str(uuid.uuid4()).upper() + "}"


def add_curve(sound, parameter, property_name, points, work_unit):
    lists = sound.find("ObjectLists")
    if lists is None:
        lists = ET.SubElement(sound, "ObjectLists")
    rtpcs = lists.find("ObjectList[@Name='RTPC']")
    if rtpcs is None:
        rtpcs = ET.SubElement(lists, "ObjectList", Name="RTPC")
    rtpc = ET.SubElement(ET.SubElement(ET.SubElement(rtpcs, "Reference"), "Local"), "RTPC", Name="", ID=guid())
    ET.SubElement(ET.SubElement(rtpc, "PropertyList"), "Property", Name="PropertyName", Type="string", Value=property_name)
    refs = ET.SubElement(rtpc, "ReferenceList")
    control = ET.SubElement(refs, "Reference", Name="ControlInput")
    ET.SubElement(control, "ObjectRef", Name=parameter["name"], ID=parameter["id"], WorkUnitID=work_unit)
    curve = ET.SubElement(ET.SubElement(ET.SubElement(refs, "Reference", Name="Curve"), "Custom"), "Curve", Name="", ID=guid())
    ET.SubElement(ET.SubElement(curve, "PropertyList"), "Property", Name="Flags", Type="int32", Value="65537")
    point_list = ET.SubElement(curve, "PointList")
    for index, (x, y) in enumerate(points):
        point = ET.SubElement(point_list, "Point")
        ET.SubElement(point, "XPos").text = str(x)
        ET.SubElement(point, "YPos").text = str(y)
        ET.SubElement(point, "Flags").text = "37" if index == len(points) - 1 else "5"


def main():
    existing_project = PROJECT / "Actor-Mixer Hierarchy/Default Work Unit.wwu"
    if existing_project.exists() and ET.parse(existing_project).find(".//ObjectList[@Name='RTPC']") is not None:
        raise RuntimeError("Project already authored. Use build-rotor-bank.ps1; setup must not mutate it again.")
    info = call("core.getInfo")
    assert info["version"]["build"] == 8496, info["version"]
    bus = create(r"\Master-Mixer Hierarchy\Default Work Unit\Master Audio Bus", "Bus", "SFX_BUS")
    sound = create(r"\Actor-Mixer Hierarchy\Default Work Unit", "Sound", "AH64Rotor")
    for key, value in {"IsLoopingEnabled": True, "IsLoopingInfinite": True,
                       "OverrideOutput": True, "OverridePositioning": True,
                       "ListenerRelativeRouting": True, "3DSpatialization": 1,
                       "EnableAttenuation": False}.items():
        prop(SOUND, key, value)
    call("core.object.setReference", dict(object=SOUND, reference="OutputBus", value=bus["id"]))
    wav = ROOT / "AH64UnityProject/Assets/AH64/Bundle/AH64Audio/sfxAH64RotorHoverGrounded.wav"
    imported = call("core.audio.import", {"importOperation": "useExisting", "imports": [
        {"audioFile": str(wav), "objectPath": SOUND.replace("\\AH64Rotor", "\\<Sound SFX>AH64Rotor"),
         "originalsSubFolder": "AH64"}]})
    if not imported.get("files") or imported.get("log"):
        raise RuntimeError("Audio import did not succeed cleanly: " + json.dumps(imported))
    parameters = []
    specs = [("AH64_RotorPitch", "Pitch", -700, 600, 0),
             ("AH64_RotorLowpass", "Lowpass", 0, 100, 0),
             ("AH64_RotorSpatial", "SpeakerPanning3DSpatializationMix", 0, 100, 0)]
    for name, _, minimum, maximum, default in specs:
        obj = create(r"\Game Parameters\Default Work Unit", "GameParameter", name)
        for key, value in {"Min": minimum, "Max": maximum, "InitialValue": default}.items():
            prop(obj["id"], key, value)
        parameters.append(obj)
    events = []
    for name, action_type in [("Play_AH64_Rotor", 1), ("Stop_AH64_Rotor", 2)]:
        event = create(r"\Events\Default Work Unit", "Event", name,
                       children=[{"type": "Action", "name": "", "@ActionType": action_type,
                                  "@Target": sound["id"]}])
        events.append(event)
    bank = create(r"\SoundBanks\Default Work Unit", "SoundBank", "AH64Rotor")
    call("core.soundbank.setInclusions", {"soundbank": bank["id"], "operation": "replace",
        "inclusions": [{"object": e["id"], "filter": ["events", "structures", "media"]} for e in events]})
    call("core.project.save")
    call("console.project.close")
    path = PROJECT / "Actor-Mixer Hierarchy/Default Work Unit.wwu"
    tree = ET.parse(path)
    sound_xml = tree.find(".//Sound[@Name='AH64Rotor']")
    assert sound_xml is not None
    existing = sound_xml.find("ObjectLists")
    if existing is not None:
        raise RuntimeError("RTPCs already authored; use generate-soundbank for subsequent builds")
    parameter_tree = ET.parse(PROJECT / "Game Parameters/Default Work Unit.wwu")
    unit_id = parameter_tree.find(".//WorkUnit").get("ID")
    for obj, (_, property_name, minimum, maximum, _) in zip(parameters, specs):
        add_curve(sound_xml, obj, property_name, [(minimum, minimum), (maximum, maximum)], unit_id)
    ET.indent(tree, space="\t")
    tree.write(path, encoding="utf-8", xml_declaration=True)
    print("Authored AH64Rotor bank, SFX_BUS route, loop/stop events and three RTPC curves.")


if __name__ == "__main__":
    main()
