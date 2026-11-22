#!/usr/bin/env bash
# Runs every check that works without Unity. Needs the .NET 8 SDK.
#   core:   builds Bnhon.Core + EditMode tests, runs them, checks map reachability
#   game:   type-checks Bnhon.Game against Unity stubs (compiles only, nothing runs)
#   editor: type-checks the Editor scripts
# When game/editor fail only because a real Unity API is missing from the stubs,
# add that API to UnityStubs.cs / S.cs; do not change the game code to suit the stubs.
set -u
here="$(cd "$(dirname "$0")" && pwd)"
export BNHON_ROOT="$(cd "$here/../.." && pwd)"
status=0
echo "== core";   (cd "$here/core"   && dotnet run 2>&1 | grep -E " error |passed|FAIL|^map|^  ") || status=1
echo "== game";   (cd "$here/game"   && dotnet build 2>&1 | grep -E " error |Error\(s\)" | sort -u)
echo "== editor"; (cd "$here/editor" && dotnet build 2>&1 | grep -E " error |Error\(s\)" | sort -u)
exit $status
