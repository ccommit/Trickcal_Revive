# Lobby resource validation

상태: `ready-for-integration` — 실제 게임 장면의 완료를 뜻하지 않는다.

## 확인됨

- 승인된 로비 리소스 15개를 `Assets/Game/Content/UI/Lobby`에 생성했다.
- 리소스 15개와 대응 `.meta` 15개의 크기 및 SHA-256이 manifest와 일치한다.
- 15개 PNG는 이전 프로젝트의 검증된 생성본과 SHA-256이 모두 일치한다.
- Unity 6000.3.16f1에서 C# 컴파일 오류가 없다.
- EditMode 3/3, PlayMode 1/1 테스트가 통과했다.
- Harness prefab과 테스트 전용 scene에서 Missing Script, Texture, Material이 없다.
- Harness scene은 Player Build Settings에 포함되지 않는다.
- 16:9, 19.5:9, 4:3, 9:16 캡처를 육안 검토했고 빈 화면이나 글자 잘림이 없다.

## 추론

- `Currency_Elif`는 이전 조사와 사용자 승인에 따라 Elif 표시용으로 채택했다.
  manifest와 Harness에서 `inferred`로 계속 표시한다.

## 제외 및 미확인

- `Currency_Elif_Alternate`는 `unconfirmed`이며 승인된 로비에서 사용되지 않아 가져오지 않았다.
- 실제 Main scene, 게임플레이 흐름, Windows/Android Player 빌드는 issue2 범위가 아니다.
- 로비 외 캐릭터·음성·SFX·FX 검증은 다음 리소스 단계에서 진행한다.

원본 캡처, Unity 로그, 테스트 XML은 `Artifacts/SpineRecovery/LobbyUI`에 로컬 보관하며 Git에는 포함하지 않는다.
