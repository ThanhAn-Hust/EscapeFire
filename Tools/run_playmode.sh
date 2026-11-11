#!/usr/bin/env bash
# Run selected PlayMode tests in Unity batchmode WITH a GPU (screenshots work) and print a short result.
# Usage: bash Tools/run_playmode.sh <testFilter> [logName]
# Close the Unity Editor first (the project must not be open).
set -u
FILTER="${1:?test filter (regex on full test name)}"
NAME="${2:-run}"
D="$(cd "$(dirname "$0")" && pwd)"; P="$(cd "$D/.." && pwd)"
UNITY="${UNITY_EXE:-/c/Program Files/Unity/Hub/Editor/6000.3.15f1/Editor/Unity.exe}"
WP="$(cygpath -w "$P" 2>/dev/null || echo "$P")"
mkdir -p "$P/Logs/verify"
WL="$(cygpath -w "$P/Logs/verify" 2>/dev/null || echo "$P/Logs/verify")"
"$UNITY" -batchmode -projectPath "$WP" -runTests -testPlatform PlayMode -assemblyNames EscapeFire.Tests.PlayMode \
  -testFilter "$FILTER" -testResults "$WL\\$NAME-results.xml" -logFile "$WL\\$NAME.log"
echo "unity_exit=$?"
PYTHONIOENCODING=utf-8 python - "$P/Logs/verify/$NAME-results.xml" <<'PYEOF'
import sys, xml.etree.ElementTree as ET
try:
    r = ET.parse(sys.argv[1]).getroot()
except Exception as e:
    print("no results:", e); sys.exit(0)
print({k: r.get(k) for k in ("total", "passed", "failed", "skipped")})
for tc in r.iter("test-case"):
    m = tc.find(".//message")
    print(tc.get("result"), tc.get("name"), ((m.text or "").strip()[:600] if m is not None and tc.get("result") != "Passed" else ""))
PYEOF
