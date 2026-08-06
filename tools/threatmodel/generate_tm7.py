"""
Generate OpcPublisher.tm7 for the Microsoft Threat Modeling Tool.

Hand-maintaining a .tm7 is error prone because every element is referenced by
GUID from several places (borders, connectors, threat instances). This script
is the source of truth: edit the model description below and re-run it.

    python tools/threatmodel/generate_tm7.py

The generated file is committed so that reviewers who do not run Python can
still open it.
"""
import uuid
import xml.etree.ElementTree as ET
from pathlib import Path

NS = "http://schemas.microsoft.com/sdl/2014/12/ThreatModel"
XSI = "http://www.w3.org/2001/XMLSchema-instance"
XSD = "http://www.w3.org/2001/XMLSchema"

ET.register_namespace("", NS)
ET.register_namespace("i", XSI)

# Deterministic GUIDs so regenerating does not churn the diff.
_NAMESPACE = uuid.UUID("6f9619ff-8b86-d011-b42d-00c04fc964ff")


def guid(name):
    return str(uuid.uuid5(_NAMESPACE, name)).lower()


def q(tag):
    return f"{{{NS}}}{tag}"


def itype(el, value):
    el.set(f"{{{XSI}}}type", value)


# ---------------------------------------------------------------------------
# Model definition
# ---------------------------------------------------------------------------

# (key, stencil, label, left, top, width, height, properties)
ELEMENTS = [
    # External interactors
    ("opcua_server", "GE.EI", "OPC UA Server (field device)", 40, 300, 160, 90, {
        "Code Type": "Unmanaged",
        "Running As": "Not Selected",
        "Accepts Input From": "Untrusted OT network",
    }),
    ("operator", "GE.EI", "Operator / Engineer", 40, 60, 160, 90, {
        "Code Type": "Not Applicable",
        "Running As": "Not Selected",
    }),
    ("registry", "GE.EI", "Container Registry", 1020, 620, 160, 90, {
        "Code Type": "Not Applicable",
    }),
    ("adr", "GE.EI", "Azure Device Registry", 1020, 500, 160, 90, {
        "Code Type": "Not Applicable",
    }),

    # Processes inside the module
    ("stack", "GE.P", "OPC UA Client Stack", 330, 300, 170, 100, {
        "Code Type": "Managed",
        "Running As": "Standard User",
        "Isolation Level": "Sandbox",
        "Accepts Input From": "Untrusted OT network",
    }),
    ("engine", "GE.P", "Publisher Engine (encode / batch / publish)", 570, 300, 190, 100, {
        "Code Type": "Managed",
        "Running As": "Standard User",
        "Isolation Level": "Sandbox",
    }),
    ("api", "GE.P", "REST API Host (ASP.NET Core)", 330, 60, 170, 100, {
        "Code Type": "Managed",
        "Running As": "Standard User",
        "Authentication Scheme": "API key",
        "Isolation Level": "Sandbox",
    }),
    ("config", "GE.P", "Configuration Service", 570, 60, 170, 100, {
        "Code Type": "Managed",
        "Running As": "Standard User",
    }),
    ("edgehub", "GE.P", "edgeHub (IoT Edge runtime)", 820, 180, 170, 100, {
        "Code Type": "Managed",
        "Running As": "Standard User",
    }),

    # Data stores
    ("pnjson", "GE.DS", "published_nodes.json", 570, 620, 170, 90, {
        "Store Type": "File",
        "Encrypted": "No",
        "Signed": "No",
        "Write Access": "Yes",
    }),
    ("pki", "GE.DS", "PKI stores (own / trusted / issuer / rejected)", 330, 620, 170, 90, {
        "Store Type": "File",
        "Encrypted": "No",
        "Signed": "Yes",
        "Write Access": "Yes",
    }),

    # Cloud sinks
    ("iothub", "GE.P", "Azure IoT Hub", 1020, 180, 160, 90, {
        "Code Type": "Not Applicable",
        "Running As": "Not Selected",
    }),
    ("broker", "GE.P", "MQTT broker / HTTP / Dapr sink", 1020, 320, 160, 90, {
        "Code Type": "Not Applicable",
    }),
]

# (key, source, target, label, properties)
FLOWS = [
    ("f1", "opcua_server", "stack",
     "(1) DataChange / Event notifications (opc.tcp)", {
         "Authentication Mechanism": "Certificate / anonymous",
         "Provides Confidentiality": "Depends on SecurityMode",
         "Provides Integrity": "Depends on SecurityMode",
     }),
    ("f2", "stack", "opcua_server",
     "(2) Browse / Read / Write / Call", {
         "Authentication Mechanism": "Certificate / username / anonymous",
     }),
    ("f3", "operator", "api",
     "(3) REST API over HTTPS", {
         "Authentication Mechanism": "API key",
         "Provides Confidentiality": "Yes",
     }),
    ("f4", "iothub", "edgehub",
     "(4) Direct methods / twin desired properties", {
         "Authentication Mechanism": "IoT Hub module identity",
         "Provides Confidentiality": "Yes",
     }),
    ("f4b", "edgehub", "api", "(4) Method invocation", {}),
    ("f5", "engine", "edgehub", "(5) Network messages (telemetry)", {}),
    ("f5b", "edgehub", "iothub", "(5) D2C telemetry", {
        "Provides Confidentiality": "Yes",
    }),
    ("f6", "engine", "broker", "(6) Telemetry via direct transport", {
        "Authentication Mechanism": "Per transport",
    }),
    ("f7", "config", "pnjson", "(7) Read / write published nodes", {}),
    ("f7b", "pnjson", "config", "(7) Load configuration on start / change", {}),
    ("f8", "stack", "pki", "(8) Store rejected / own certificates", {}),
    ("f8b", "pki", "stack", "(8) Load trust list, CRLs", {}),
    ("f9", "adr", "config", "(9) Asset / device definitions", {}),
    ("f10", "registry", "engine", "(10) Module image pull", {}),
    ("f11", "engine", "edgehub", "(11) Diagnostics / runtime state / logs", {}),
    ("f12", "config", "stack", "(12) Applied subscription configuration", {}),
    ("f13", "stack", "engine", "(13) Notifications to writer groups", {}),
    ("f14", "api", "config", "(14) Configuration API calls", {}),
]

# (key, label, left, top, width, height)
BOUNDARIES = [
    ("tb_ot", "OT / plant network boundary", 20, 240, 290, 220),
    ("tb_module", "opcpublisher container boundary", 310, 20, 470, 420),
    ("tb_fs", "Mounted volume (host filesystem) boundary", 310, 570, 460, 170),
    ("tb_cloud", "Cloud boundary", 1000, 140, 200, 600),
]


def make_properties(props, label):
    """Build the <Properties> block. The first entry is always the display name."""
    container = ET.Element(q("Properties"))
    header = ET.SubElement(container, q("anyType"))
    itype(header, "HeaderDisplayAttribute")
    ET.SubElement(header, q("DisplayName")).text = "Name"
    ET.SubElement(header, q("Name")).text = "Name"
    value = ET.SubElement(header, q("Value"))
    value.set(f"{{{XSI}}}type", "x:string")
    value.set("xmlns:x", XSD)
    value.text = label

    for name, val in props.items():
        attr = ET.SubElement(container, q("anyType"))
        itype(attr, "StringDisplayAttribute")
        ET.SubElement(attr, q("DisplayName")).text = name
        ET.SubElement(attr, q("Name")).text = name.replace(" ", "")
        v = ET.SubElement(attr, q("Value"))
        v.set(f"{{{XSI}}}type", "x:string")
        v.set("xmlns:x", XSD)
        v.text = val
    return container


def add_border(parent, key, stencil, label, left, top, width, height, props,
               is_boundary=False):
    kv = ET.SubElement(parent, q("KeyValueOfguidanyType"))
    ET.SubElement(kv, q("Key")).text = guid(key)
    val = ET.SubElement(kv, q("Value"))
    itype(val, "BorderBoundary" if is_boundary else "Stencil")

    ET.SubElement(val, q("GenericTypeId")).text = stencil
    ET.SubElement(val, q("Guid")).text = guid(key)
    ET.SubElement(val, q("Height")).text = str(height)
    ET.SubElement(val, q("Left")).text = str(left)
    val.append(make_properties(props, label))
    ET.SubElement(val, q("StrokeDashArray")).set(f"{{{XSI}}}nil", "true")
    ET.SubElement(val, q("StrokeThickness")).text = "1"
    ET.SubElement(val, q("Top")).text = str(top)
    ET.SubElement(val, q("TypeId")).text = stencil
    ET.SubElement(val, q("Width")).text = str(width)


def add_flow(parent, key, source, target, label, props):
    kv = ET.SubElement(parent, q("KeyValueOfguidanyType"))
    ET.SubElement(kv, q("Key")).text = guid(key)
    val = ET.SubElement(kv, q("Value"))
    itype(val, "Connector")

    ET.SubElement(val, q("GenericTypeId")).text = "GE.DF"
    ET.SubElement(val, q("Guid")).text = guid(key)
    val.append(make_properties(props, label))
    ET.SubElement(val, q("SourceGuid")).text = guid(source)
    ET.SubElement(val, q("SourceX")).text = "0"
    ET.SubElement(val, q("SourceY")).text = "0"
    ET.SubElement(val, q("StrokeDashArray")).set(f"{{{XSI}}}nil", "true")
    ET.SubElement(val, q("StrokeThickness")).text = "1"
    ET.SubElement(val, q("TargetGuid")).text = guid(target)
    ET.SubElement(val, q("TargetX")).text = "0"
    ET.SubElement(val, q("TargetY")).text = "0"
    ET.SubElement(val, q("TypeId")).text = "GE.DF"


HIGH_LEVEL = (
    "OPC Publisher is an Azure IoT Edge module that connects to OPC UA servers "
    "on an operational technology network, subscribes to data changes and "
    "events, and publishes the resulting telemetry to Azure IoT Hub, an MQTT "
    "broker, or an HTTP/Dapr endpoint. It is configured through a mounted "
    "published_nodes.json file, an authenticated REST API, and IoT Hub direct "
    "methods. It maintains an OPC UA PKI store on a mounted volume."
)

ASSUMPTIONS = (
    "1. The OT network is untrusted: an OPC UA server may be malicious, "
    "compromised or faulty and can return hostile or malformed data. "
    "2. The IoT Edge gateway host is trusted; an attacker with root on the host "
    "can already read the module's mounted volume, PKI store and module identity. "
    "3. published_nodes.json and the PKI directory live on a host-shared mounted "
    "volume; their confidentiality and integrity are the host's responsibility. "
    "4. IoT Hub is trusted to authenticate the caller of a direct method; the "
    "module does not independently authenticate that caller. "
    "5. The REST API is reachable only from the edge network and is protected by "
    "the configured API key; it is not intended for internet exposure."
)

EXTERNAL_DEPENDENCIES = (
    "OPC UA .NET Standard stack (OPCFoundation.NetStandard.Opc.Ua); "
    "Azure IoT Edge runtime (edgeAgent, edgeHub); Azure IoT Hub; "
    "Furly transport abstractions (MQTT, HTTP, Dapr, Event Hubs); "
    "ASP.NET Core; the host container runtime."
)


def build():
    root = ET.Element(q("ThreatModel"))

    surfaces = ET.SubElement(root, q("DrawingSurfaceList"))
    surface = ET.SubElement(surfaces, q("DrawingSurfaceModel"))
    ET.SubElement(surface, q("GenericTypeId")).text = "DFD"
    ET.SubElement(surface, q("Guid")).text = guid("diagram")
    ET.SubElement(surface, q("Header")).text = "OPC Publisher - Level 1"

    borders = ET.SubElement(surface, q("Borders"))
    for key, label, left, top, width, height in BOUNDARIES:
        add_border(borders, key, "GE.TB.B", label, left, top, width, height,
                   {}, is_boundary=True)
    for key, stencil, label, left, top, width, height, props in ELEMENTS:
        add_border(borders, key, stencil, label, left, top, width, height, props)

    lines = ET.SubElement(surface, q("Lines"))
    for key, source, target, label, props in FLOWS:
        add_flow(lines, key, source, target, label, props)

    ET.SubElement(surface, q("Zoom")).text = "1"

    meta = ET.SubElement(root, q("MetaInformation"))
    ET.SubElement(meta, q("Assumptions")).text = ASSUMPTIONS
    ET.SubElement(meta, q("Contributors")).text = "Azure Industrial IoT"
    ET.SubElement(meta, q("ExternalDependencies")).text = EXTERNAL_DEPENDENCIES
    ET.SubElement(meta, q("HighLevelSystemDescription")).text = HIGH_LEVEL
    ET.SubElement(meta, q("Owner")).text = "Azure Industrial IoT"
    ET.SubElement(meta, q("Reviewer")).text = ""
    ET.SubElement(meta, q("ThreatModelName")).text = "Azure IIoT OPC Publisher"

    ET.SubElement(root, q("Notes"))
    ET.SubElement(root, q("ThreatInstances"))
    ET.SubElement(root, q("ThreatGenerationEnabled")).text = "true"
    ET.SubElement(root, q("Validations"))
    ET.SubElement(root, q("Version")).text = "4.1"

    return root


def main():
    root = build()
    tree = ET.ElementTree(root)
    ET.indent(tree, space="  ")
    out = Path(__file__).resolve().parents[2] / \
        "docs" / "opc-publisher" / "threatmodel" / "OpcPublisher.tm7"
    tree.write(out, encoding="utf-8", xml_declaration=True)
    print(f"wrote {out}")


if __name__ == "__main__":
    main()
