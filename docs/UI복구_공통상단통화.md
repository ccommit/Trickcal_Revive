# 공통 상단 패널 복구

- 작업 브랜치: `issue10`
- 기준 이미지: `로비_배낭1.png`, `사도_스킬_기본공격.png`
- 공통 프리팹: `Assets/Game/MainUI/Prefabs/TopCurrencyPanel.prefab`

## 확인됨

- `로비_배낭1.png`의 좌→우 순서는 마카롱, 캔디, 골드, 엘리프다.
- `사도_스킬_기본공격.png`처럼 화면에 따라 필요한 통화만 표시된다.
- `TopCurrencyPanelView.Show(TopCurrencyVisibility, isLobby, pageTitle)`가 좌측 Profile/Back+Title과 우측 통화/Menu/Home을 한 상태로 함께 전환한다.
- 공유 패널은 `Main` 씬 Canvas에 한 번만 존재하며 Lobby/StageSelect/PartySetup 프리팹에는 프로필·뒤로가기·제목·통화 슬롯이 중복되지 않는다.
- 로비는 4종 전체를, 현재 StageSelect와 PartySetup은 캔디·골드·엘리프를 표시한다.
- 기존 로비의 `SettingsButton`은 제거하고, 공통 패널 내부의 `MenuButton`으로 이름과 위치를 통합했다.
- `MenuButton`과 `HomeButton`은 같은 96×96 크기와 같은 위치를 공유하며 패널 높이도 96이다.
- `MenuButton`은 Lobby에서만, `HomeButton`은 Lobby 이외 화면에서만 표시된다.
- `HomeButton`은 화면 방문 기록의 뒤로가기를 호출하지 않고 `IScreenNavigator.Reset("Lobby")`로 항상 로비에 복귀한다.
- `TopMenu_IconHome`은 기존 메뉴 아이콘과 같은 원본 `99.common/commonicons` 아틀라스에서 복구했으며 생성 복사본과 해시는 `lobby-resources.json`에 기록했다.
- `ProfilePanel`은 Lobby에서만 표시되고 계정 닉네임·레벨을 공통 패널의 `TopCurrencyController`가 갱신한다.
- Lobby 이외 화면은 같은 좌측 자리에 공통 `BackButton`과 `PageTitle`을 표시한다.
- `BackButton`은 `IScreenNavigator.GoBack()`으로 PartySetup→StageSelect→Lobby의 Main 씬 내부 방문 기록을 따른다.
- StageSelect 제목은 `스테이지 리스트`, PartySetup 제목은 현재 선택된 스테이지 데이터의 `StageId + Name + 시작!` 형식이다.
- 화면 전환 PlayMode 테스트에서 Lobby→StageSelect→PartySetup→Back→StageSelect→Home→Lobby의 좌우 상단 상태를 확인한다.

## 인터페이스와 책임

- `TopCurrencyPanelView.Render(...)`: 네 통화 값을 한 번에 갱신한다.
- `TopCurrencyPanelView.RenderProfile(...)`: 계정 프로필 값을 갱신한다.
- `TopCurrencyPanelView.Show(mask, isLobby, pageTitle)`: 좌우 상단 표시 상태를 원자적으로 전환한다.
- `TopCurrencyController`: 프로필과 통화 값을 공통 View 하나에 전달하고 Menu/Home/Back 클릭을 설치 시 주입한 화면 동작에 연결한다.
- `MainSceneInstaller`: 화면 ID를 통화 표시 조합과 페이지 제목으로 변환한다. 개별 화면 Controller는 공통 상단 UI를 알지 않는다.

## 추론

- 현재 2560×1440 좌표와 슬롯 사이 10px 간격은 기준 이미지 및 기존 `TopCurrencySlot` 크기를 대조한 복원값이다.
- 통화 슬롯과 Menu/Home 사이 16px 간격은 기준 이미지 비율을 따른 현재 복원값이다.
- 공통 BackButton의 좌표와 PageTitle 간격은 제공된 StageSelect/PartySetup 기준 이미지 비율을 대조한 복원값이다.
- UI 오브젝트명은 사용자 합의안에 따라 `MacaroonCurrency`, 데이터 키는 기존 저장 형식인 `Macaron`을 유지한다.

## 미확인

- 통화 증감 애니메이션, 플러스 버튼의 구매 팝업 연결, 아직 구현되지 않은 화면의 최종 제목·통화 표시 정책은 후속 화면 복구 범위다.

## 검증

- 공통 패널 EditMode 구조 테스트: 10/10 통과
- Lobby→StageSelect→PartySetup→Back→StageSelect→Home→Lobby PlayMode 전환 테스트: 1/1 통과
- 전체 EditMode 테스트: 74/74 통과
- 계층 참조 검사: 99개 파일, 위반 없음
- 캡처:
  - `Artifacts/SpineRecovery/LoginLobby/Captures/main-16x9.png`
  - `Artifacts/SpineRecovery/StageSelect/Captures/stageselect-16x9.png`
  - `Artifacts/SpineRecovery/StageSelect/Captures/partysetup-16x9.png`
