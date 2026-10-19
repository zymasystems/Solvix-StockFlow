"""Guard the mobile presentation restored from the user's mobile.zip.

Business handlers and injected services can change without changing the visible design.
Baseline changes require an intentional design decision; this script never updates it.
"""
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASELINE = ROOT / 'mobile/ui-baseline.json'


def visible_source(text):
    text = text.replace('\r\n', '\n').split('@code', 1)[0]
    text = re.sub(r'^@(inherits|inject) [^\n]+\n?', '', text, flags=re.MULTILINE)
    return '\n'.join(line.rstrip() for line in text.splitlines()).strip()


def digest(data):
    return hashlib.sha256(data).hexdigest()


def asset_bytes(path):
    data = path.read_bytes()
    return data.replace(b'\r\n', b'\n') if path.suffix.lower() in {'.css', '.js', '.svg', '.html', '.json', '.txt'} else data


def main():
    baseline = json.loads(BASELINE.read_text())
    failures = []
    for relative, expected in baseline['razor'].items():
        path = ROOT / 'mobile' / relative
        if not path.exists() or digest(visible_source(path.read_text()).encode()) != expected:
            failures.append(relative)
    for relative, expected in baseline['assets'].items():
        path = ROOT / 'mobile' / relative
        if not path.exists() or digest(asset_bytes(path)) != expected:
            failures.append(relative)
    if failures:
        raise SystemExit('Mobile UI differs from the approved upload: ' + ', '.join(failures))
    print(f"Original mobile presentation verified: {len(baseline['razor'])} components and {len(baseline['assets'])} assets.")


if __name__ == '__main__':
    main()
