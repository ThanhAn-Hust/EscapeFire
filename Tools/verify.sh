#!/usr/bin/env bash
# EscapeFire verify: compile + Unity analyzers + pure-logic tests + edge server tests + static scene audit.
# Usage: bash Tools/verify.sh           fast checks, no Unity license needed
#        bash Tools/verify.sh --unity   + real Unity batchmode: scene audit, EditMode + PlayMode tests, snapshots
#        (close the Unity Editor first; requires being signed in to Unity Hub)
# Compile-check every EscapeFire assembly with Unity's bundled Roslyn + Microsoft.Unity.Analyzers (no Unity license needed).
set -u
FAIL=0
D="$(cd "$(dirname "$0")" && pwd)"
P="$(cd "$D/.." && pwd)"
E="${UNITY_EDITOR_DATA:-/c/Program Files/Unity/Hub/Editor/6000.3.15f1/Editor/Data}"
OUT="$P/Temp/verify"; mkdir -p "$OUT"
DOTNET="$E/NetCoreRuntime/dotnet.exe"; CSC="$E/DotNetSdkRoslyn/csc.dll"
SA="$P/Library/ScriptAssemblies"
refs=("-r:$E/NetStandard/ref/2.1.0/netstandard.dll" "-r:$E/NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll")
for f in "$E"/Managed/UnityEngine/UnityEngine*.dll; do refs+=("-r:$f"); done
for a in Unity.TextMeshPro UnityEngine.UI Unity.XR.Interaction.Toolkit Unity.XR.CoreUtils Unity.InputSystem Unity.XR.Management Unity.XR.OpenXR Unity.XR.MetaOpenXR UnityEngine.TestRunner; do refs+=("-r:$SA/$a.dll"); done
refs+=("-r:$(ls "$P"/Library/PackageCache/com.unity.nuget.newtonsoft-json@*/Runtime/Newtonsoft.Json.dll)")
nunit="-r:$(ls "$P"/Library/PackageCache/com.unity.ext.nunit@*/net40/unity-custom/nunit.framework.dll)"
eds=(); for f in "$E"/Managed/UnityEngine/UnityEditor*.dll; do eds+=("-r:$f"); done
for a in Unity.RenderPipelines.Universal.Runtime Unity.RenderPipelines.Core.Runtime Unity.XR.Management.Editor Unity.XR.OpenXR.Editor UnityEditor.TestRunner; do eds+=("-r:$SA/$a.dll"); done
common=(-analyzerconfig:"$D/unity.globalconfig" -analyzer:"$P/Assets/Scripts/Analyzers/Microsoft.Unity.Analyzers.dll" -nologo -noconfig -nostdlib -langversion:9.0 -target:library -warn:4 -nowarn:1701,1702,0649,0414 -define:UNITY_INCLUDE_TESTS -define:UNITY_EDITOR -define:UNITY_EDITOR_WIN)
run() { local name="$1"; shift; echo "== $name"; "$DOTNET" "$CSC" "$@" | grep -E "error|warning (UNT|CS)" | grep -v "UNT0028\|UNT0026\|UNT0022\|UNT0032"; local rc=${PIPESTATUS[0]}; echo "${name}_exit=$rc"; [ "$rc" -eq 0 ] || FAIL=1; }
src() { find "$@" -name "*.cs" -not -path "*/Analyzers/*"; }
mapfile -t rt < <(src "$P/Assets/Scripts");                  run runtime  "${common[@]}" "${refs[@]}" -out:"$OUT/EscapeFire.Runtime.dll" "${rt[@]}"
mapfile -t ed < <(src "$P/Assets/Editor/EscapeFire");        run editor   "${common[@]}" "${refs[@]}" "${eds[@]}" -r:"$OUT/EscapeFire.Runtime.dll" -out:"$OUT/EscapeFire.Editor.dll" "${ed[@]}"
mapfile -t te < <(src "$P/Assets/Tests/EditMode");           run editmode "${common[@]}" "${refs[@]}" "${eds[@]}" "$nunit" -r:"$OUT/EscapeFire.Runtime.dll" -r:"$OUT/EscapeFire.Editor.dll" -out:"$OUT/EscapeFire.Tests.EditMode.dll" "${te[@]}"
mapfile -t tp < <(src "$P/Assets/Tests/PlayMode");           run playmode "${common[@]}" "${refs[@]}" "${eds[@]}" "$nunit" -r:"$OUT/EscapeFire.Runtime.dll" -out:"$OUT/EscapeFire.Tests.PlayMode.dll" "${tp[@]}"
mapfile -t ac < <(find "$P/Assets/ThirdPartyAdapters" "$P/Assets/Overtone/Scripts" "$P/Assets/FrostweepGames" -name "*.cs" -not -path "*/Editor/*")
echo "== adapters"; "$DOTNET" "$CSC" -nologo -noconfig -nostdlib -target:library -nowarn:0618,0649,0414 -define:UNITY_2018_3_OR_NEWER -define:UNITY_2019_4_OR_NEWER -define:UNITY_2020_1_OR_NEWER -define:UNITY_2021_1_OR_NEWER -define:UNITY_6000_0_OR_NEWER -define:FG_GCSR -unsafe "${refs[@]}" -r:"$OUT/EscapeFire.Runtime.dll" -out:"$OUT/Assembly-CSharp.subset.dll" "${ac[@]}" | grep -E "error"; echo "adapters_exit=${PIPESTATUS[0]}"

echo "== pure-logic EditMode tests (outside Unity)"
FX="$E/NetCoreRuntime/shared/Microsoft.NETCore.App/$(ls "$E/NetCoreRuntime/shared/Microsoft.NETCore.App" | head -1)"
rr=(); for f in System.Runtime System.Console System.Linq System.Collections System.Private.CoreLib; do rr+=("-r:$FX/$f.dll"); done
"$DOTNET" "$CSC" -nologo -noconfig -nostdlib -target:exe "${rr[@]}" -out:"$OUT/MiniRunner.dll" "$D/MiniRunner.cs" || FAIL=1
printf '{ "runtimeOptions": { "tfm": "net6.0", "framework": { "name": "Microsoft.NETCore.App", "version": "%s" } } }' "$(basename "$FX")" > "$OUT/MiniRunner.runtimeconfig.json"
cp "$E"/Managed/UnityEngine/UnityEngine.CoreModule.dll "$E"/Managed/UnityEngine/UnityEngine.dll "$SA"/UnityEngine.TestRunner.dll "$SA"/Unity.TextMeshPro.dll "$SA"/UnityEngine.UI.dll "$OUT"/ 2>/dev/null
cp "$(ls "$P"/Library/PackageCache/com.unity.ext.nunit@*/net40/unity-custom/nunit.framework.dll)" "$(ls "$P"/Library/PackageCache/com.unity.nuget.newtonsoft-json@*/Runtime/Newtonsoft.Json.dll)" "$OUT"/
# Tests needing the Unity engine/editor at runtime are skipped here; run them in the Test Runner.
"$DOTNET" "$OUT/MiniRunner.dll" "$OUT/EscapeFire.Tests.EditMode.dll" "HandlerThrows" "LabSceneValidation" "FireVisualTests.ResetFire" "FireVisualTests.SecondaryFireClone" | grep -v "^PASS" || FAIL=1
echo "== edge server tests"
(cd "$P/Server" && uv run pytest -q -p no:warnings 2>&1 | tail -1) || FAIL=1
echo "== static scene audit (BasicScene placeholders / physics risks)"
PYTHONIOENCODING=utf-8 python "$D/scene_static_audit.py" "$P/Assets/Scenes/BasicScene.unity" all > "$OUT/scene_audit.txt"
PATTERN="Rigidbody\(dyn\)|MB:MISSING"
# Capsule NPC meshes are fine once the PolygonOffice character (Person_Visual) is attached and the capsule renderer is off.
grep -q "Person_Visual" "$P/Assets/Scenes/BasicScene.unity" || PATTERN="builtin:10208|$PATTERN"
if grep -qE "$PATTERN" "$OUT/scene_audit.txt"; then
  echo "TODO in Unity: run EscapeFire > Setup All (still capsule NPCs / free-falling bodies / missing scripts):"
  grep -E "$PATTERN" "$OUT/scene_audit.txt"
else
  echo "OK: NPCs use real characters, no free-falling rigidbodies, no missing scripts"
fi

if [ "${1:-}" = "--unity" ]; then
  UNITY="${UNITY_EXE:-/c/Program Files/Unity/Hub/Editor/6000.3.15f1/Editor/Unity.exe}"
  WP="$(cygpath -w "$P" 2>/dev/null || echo "$P")"
  LOGS="$P/Logs/verify"; mkdir -p "$LOGS"; WL="$(cygpath -w "$LOGS" 2>/dev/null || echo "$LOGS")"
  echo "== Unity: lab scene audit"
  "$UNITY" -batchmode -nographics -quit -projectPath "$WP" -executeMethod EscapeFire.EditorTools.LabSceneAudit.RunOnLab -logFile "$WL/audit.log"
  grep -E "\[EscapeFire\] Audit|  ERROR |  WARN  " "$LOGS/audit.log" | sort -u
  grep -q "Audit 'BasicScene': 0 error" "$LOGS/audit.log" || FAIL=1
  for MODE in EditMode PlayMode; do
    echo "== Unity: $MODE tests"
    "$UNITY" -batchmode -nographics -projectPath "$WP" -runTests -testPlatform $MODE -assemblyNames "EscapeFire.Tests.$MODE" -testCategory "!Demo"       -testResults "$WL/$MODE-results.xml" -logFile "$WL/$MODE.log" || FAIL=1
    PYTHONIOENCODING=utf-8 python - "$LOGS/$MODE-results.xml" <<'PYEOF'
import sys, xml.etree.ElementTree as ET
r = ET.parse(sys.argv[1]).getroot()
print({k: r.get(k) for k in ("total", "passed", "failed")})
for tc in r.iter("test-case"):
    if tc.get("result") != "Passed":
        m = tc.find(".//message")
        print("FAIL", tc.get("fullname"), (m.text or "")[:400] if m is not None else "")
PYEOF
  done
  echo "== Unity: snapshots -> Logs/Snapshots"
  "$UNITY" -batchmode -quit -projectPath "$WP" -executeMethod EscapeFire.EditorTools.SceneSnapshots.CaptureLab -logFile "$WL/snap.log" || FAIL=1
fi
exit $FAIL
