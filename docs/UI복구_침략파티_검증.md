# 침략·파티 편성 UI 복구 검증

- 검증일: 2026-08-07
- 작업 브랜치: `issue10`
- 기준 화면: `스크린샷 2026-06-11 011810.png`
- 원본 증거: `Trickcal_Reference`의 `WindowDeckEdit.prefab`, `Deck`/`CommonUI` SpriteAtlas 직렬화 자료

## 확인됨

- 원본 `WindowDeckEdit.prefab`의 `Position`, `Unit1..9`, `DeckEditListContainer`, `FilterAndSort`, `ButtonGroup` 구조를 확인했다.
- 실제 프리팹이 참조하는 `Deck` 6개, `CommonUI` 7개를 아틀라스 render key로 찾아 생성 복사본으로 복구했다.
- 우측 사도 목록은 고정 3열, 세로형 카드, 스크롤 구조다.
- 진형은 3×3이며 `x=1 후열`, `x=2 중열`, `x=3 전열`이다. 각 열은 `y=2 가운데 → y=1 위 → y=3 아래` 순서로 배치한다.
- 자기 열이 꽉 찬 사도는 다른 열로 넘어가지 않는다. `All` 타입만 전열→중열→후열 순서로 검사한다.
- 같은 사도 카드를 다시 누르면 배치에서 제거하며 최대 편성 인원은 6명이다.
- 30명 전원의 InGame SkeletonData에서 `Normal` skin과 `Idle` animation이 실제로 존재함을 EditMode 테스트로 확인했다.
- 배치 슬롯은 정적 초상화가 아니라 `SkeletonGraphic`으로 `Normal` skin의 `Idle`을 표시한다.
- 대여, 빠른 전투, 자동 전투는 보이되 현재 범위 안내만 표시한다. 출발은 BattleContext 조립 결과만 표시하고 전투 씬으로 이동하지 않는다.

## 추론

- 사도별 전·중·후열은 이전 프로젝트의 30명 선정안과 사도 목록을 대조한 참고 복구값이다. 원본 master table을 직접 복호화한 확정값은 아니다.
- 레벨, 별, 전투력, 통화 숫자는 화면 동작 검증용 recovery fixture다.
- 진형의 세부 좌표와 색조는 기준 스크린샷 및 원본 sprite 비율에 맞춘 복원값이다.
- 왼쪽 배경은 현재 확보된 `Bg_BattleList`를 사용한 검증용 배경이다. 기준 스크린샷의 실제 1-1 전투 배경과 동일하다고 확정하지 않는다.

## 미확인·다음 범위

- 원작의 드래그 앤 드롭 입력과 교체 애니메이션은 현재 증거가 부족해 포함하지 않았다.
- 대여, 빠른 전투, 자동 전투의 실제 게임 규칙과 저장 동작은 포함하지 않았다.
- 출발 이후 실제 전투 씬 전환은 다음 이슈 범위다.
- Unity Editor Play 버튼을 사용한 사람의 수동 입력 검증은 별도로 남아 있다.

## 재현 및 증거

- 복구 스크립트: `Tools/Recovery/Recover-PartySetupUI.py`
- 해시 manifest: `Recovery/Manifests/party-setup-ui.json`
- 생성 리소스: `Assets/Game/Content/UI/PartySetup/`
- UI builder: `Assets/Game/App/Editor/PartySetupUIBuilder.cs`
- 최종 16:9 캡처: `Artifacts/SpineRecovery/StageSelect/Captures/partysetup-16x9.png`
- UI 구조 회귀 테스트: 5/5 통과
- 파티 배치 흐름 테스트: 6/6 통과
- 전체 EditMode 테스트: 73/73 통과
- 계층 검사: 97개 파일, 위반 없음
