# UI 인계 — 로그인→로비 화면

> 대상: `AI팀/UI복구_GPT`(이 저장소 `issue8` 브랜치 기준)
> 이 문서는 개발_클로드가 만든 구조를 그대로 두고 화면(Canvas 하위)만 채우기 위한 참고 자료다.
> 구조(씬 계층, 컨트롤러, DI 배선)는 이미 완성돼 있고 실제로 동작한다 — 계정 생성/로그인/로그아웃은
> 로컬 파일 기반으로 진짜 저장·조회된다. 여기서는 **비주얼만 얹으면 된다.**

## 1. 씬 위치

- `Assets/Scenes/Flow/Login.unity` — 로그인/회원가입 화면
- `Assets/Scenes/Flow/Main.unity` — 로비 화면

두 씬 모두 이미 있는 것:
- `Main Camera`(2D, orthographic, 배경 검정)
- `Canvas`(Screen Space - Overlay, `CanvasScaler` 1080x1920 기준 Scale With Screen Size) + `GraphicRaycaster`
- `EventSystem`(`InputSystemUIInputModule` — 이 프로젝트는 새 Input System 전용이라 이거 아니면 버튼이 반응 안 함)
- TextMeshPro Essential Resources 임포트 완료(`Assets/TextMesh Pro/`)

**UI 배치는 각 씬의 `Canvas` GameObject 밑에 자식으로 만들면 된다.** `Canvas` 바깥 계층(GameApplication, SceneFlowController, LoginSceneInstaller 등)은 건드리지 않는다 — 특히 `GameApplication`은 계층 맨 위에 있어야 하는 이유가 있다(Awake 순서, 아래 5번 참고).

## 2. Login 씬 — 붙어있는 컨트롤러

`AuthController` GameObject에 `TrickcalRevive.Presentation.AuthController` 컴포넌트가 붙어있다.

```csharp
AuthResult Login(string loginId, string password)
AuthResult SignUp(string loginId, string password, string nickname)
```

`AuthResult`: `Success` / `NotFound`(존재하지 않는 아이디 — 신규 가입 유도) / `WrongPassword` / `LoginIdTaken`.

**흐름**: 아이디 입력 후 `Login()` 먼저 호출 → `NotFound`면 닉네임 입력 팝업 띄우고 `SignUp()` 호출 → `Success`면 `Main` 씬으로 전환.
씬 전환은 컨트롤러가 직접 하지 않는다 — UI 쪽에서 버튼 핸들러가 결과 보고 `SceneManager.LoadScene("Main")`을 부르거나, `INavigationService`(`Canvas` 밖 `SceneFlowController`가 구현체)를 DI로 받아 `Go("Main")`을 부르면 된다. 지금은 UI 컴포넌트가 DI 컨테이너에 접근하는 경로가 없으니, 간단히는 `SceneManager.LoadScene("Main")` 직접 호출로 충분하다.

## 3. Main 씬 — 붙어있는 컨트롤러

`LobbyController` GameObject:

```csharp
AccountData RenderProfile()              // Nickname, PlayerLevel, Exp 등
List<PlayerCurrencyData> RenderCurrencies()
SettingsController OpenSettings()        // 팝업 열림 표시 + SettingsController 반환
```

`SettingsController` GameObject:

```csharp
void Logout()   // 세션 해제. 호출 후 UI가 Login 씬으로 전환해야 한다.
```

`RenderCurrencies()`가 돌려주는 `PlayerCurrencyData.CurrencyType`은 열거형이 아니라 문자열이다 — 값은 정확히 `"Gold"` / `"Elleaf"` / `"Macaron"` / `"Stamina"` 4가지. 아이콘은 이미 복구돼 있다: `Assets/Game/Content/UI/Lobby/Sprites/Currency_Gold.png`, `Currency_Elif.png`(주의: "Elleaf"가 아니라 "Elif"), `Currency_Macaron.png`, `Currency_Stamina.png`. 로비 배경은 `Assets/Game/Content/UI/Lobby/Background/Lobby_Default.png`.

**주의**: `Main.unity`를 로그인 없이 바로 Play하면 `RenderProfile()`이 `null`을 반환한다(세션 없음). UI 다듬을 때는 `Login` 씬부터 실제로 가입/로그인해서 들어오는 걸 권장한다 — 더미 계정 자동 생성 같은 편의 기능은 일부러 안 넣었다.

## 4. 아직 없는 것 (건드리지 말 것)

다음은 각자의 후속 이슈에서 채워질 예정이라 이번 스코프에 없다. 버튼을 만들어도 연결할 메서드가 없다:

- `LobbyController.OpenAdventure()` — 스테이지 진입(03 파티편성 이슈)
- `SettingsController.ResetAccount()` — 계정 초기화
- `SceneFlowController.ShowLoading()` — 로딩 오버레이 실동작(`NotImplementedException` 상태)
- `LoadingController.Show/Complete/Fail` — 비주얼 없는 no-op 상태(나중에 실제 오버레이 붙이면 됨, 지금 건드려도 안전은 함)

## 5. 저장 데이터 위치

`Application.persistentDataPath` 밑에 `account_{아이디}.json`, `session.json` 파일로 저장된다. 테스트 계정 초기화하려면 이 파일들을 지우면 된다. Windows 에디터 기준 보통:
`%userprofile%\AppData\LocalLow\<CompanyName>\Trickcal_Revive\`

## 6. 계층 규칙 참고

`GameApplication`이 씬 맨 위(계층 순서 첫 번째)에 있어야 하는 이유: 같은 프레임에 여러 GameObject의 `Awake()`가 도는데, `LoginSceneInstaller.Awake()`가 `GameApplication.RootContainer`를 참조하기 때문에 `GameApplication`이 먼저 실행돼야 한다. Unity가 이 순서를 공식 보장하진 않지만 계층 순서로 실질적으로는 보장된다 — **`GameApplication`을 계층에서 다른 오브젝트보다 아래로 옮기지 말 것.**
