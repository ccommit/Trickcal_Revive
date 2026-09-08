#!/usr/bin/env bash
# 계층 참조 방향 검사. Unity를 켜지 않고 asmdef 규칙과 같은 것을 확인한다.
# 같은 규칙의 NUnit 판은 Assets/Tests/EditMode/Architecture/LayerDependencyTests.cs.
#
# 사용: bash tools/check-layers.sh
set -u

cd "$(dirname "$0")/.." || exit 2

declare -A ALLOWED=(
  [Core]=""
  [Data]="Core"
  [Domain]="Core Data"
  [Infra]="Core Data Domain"
  [MainUI]=""
  [Presentation]="Core Data Domain Infra MainUI"
  [App]="Core Data Domain Infra Presentation MainUI"
)

fail=0
checked=0

for layer in "${!ALLOWED[@]}"; do
  root="Assets/Game/$layer"
  [ -d "$root" ] || continue
  allowed="${ALLOWED[$layer]} $layer"

  while IFS= read -r file; do
    # Editor 전용 툴은 별도 asmdef로 분리돼 있어 규칙 밖이다.
    case "$file" in */Editor/*) continue ;; esac
    checked=$((checked + 1))

    while IFS= read -r ref; do
      [ -n "$ref" ] || continue
      # TrickcalRevive.MainUI 처럼 계층이 아닌 것은 건너뛴다.
      case " ${!ALLOWED[*]} " in *" $ref "*) ;; *) continue ;; esac
      case " $allowed " in *" $ref "*) continue ;; esac
      echo "VIOLATION: ${file#Assets/Game/} -> $ref"
      fail=1
    done < <(sed -n 's/^[[:space:]]*using[[:space:]]\{1,\}TrickcalRevive\.\([A-Za-z0-9_]*\).*/\1/p' "$file" | sort -u)
  done < <(find "$root" -name '*.cs')
done

if [ "$checked" -eq 0 ]; then
  echo "ERROR: 검사한 파일이 없다. 경로를 확인하라."
  exit 2
fi

if [ "$fail" -eq 0 ]; then
  echo "OK: $checked개 파일, 계층 참조 위반 없음"
fi
exit "$fail"
