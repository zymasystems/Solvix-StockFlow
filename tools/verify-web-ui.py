"""Verify the website presentation against the user's uploaded web folder."""
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
baseline = json.loads((root / "web/ui-baseline.json").read_text())
errors = []
for name, expected in baseline["files"].items():
    path = root / name
    if not path.is_file():
        errors.append(name + " is missing")
        continue
    value = path.read_bytes()
    try:
        text = value.decode("utf-8-sig").replace("\r\n", "\n")
        if name == "web/Views/_ViewImports.cshtml":
            text = text.replace("\n@addTagHelper *, Solvix.Web\n", "")
        value = text.encode()
    except UnicodeDecodeError:
        pass
    if hashlib.sha256(value).hexdigest() != expected:
        errors.append(name + " differs from the uploaded UI")
if errors:
    raise SystemExit("\n".join(errors))
print(f"Uploaded website UI verified: {len(baseline['files'])} views/assets.")
