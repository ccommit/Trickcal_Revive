# Recovery Harness

`LobbyResourceHarness`와 `CharacterResourceHarness`는 issue2 리소스 import 및 렌더링 검증만을 위한 테스트 전용 구성이다.

- 실제 플레이 씬 또는 빌드 씬으로 사용하지 않는다.
- `CharacterResourceCatalog.asset`은 사도 30명과 몬스터 표시 변형 20개의 검증 참조를 보관한다.
- 캐릭터 씬은 InGame/Standing Spine, 이미지, 전투 음성/SFX의 연결만 확인한다.
- 원본 동작이 확인되지 않은 FX와 전투 상태 전이는 포함하지 않는다.

이 폴더는 리소스 가져오기와 표시 동작을 검증하는 테스트 전용 장면, prefab, 코드만 둔다.
실제 게임 장면이나 프로덕션 흐름으로 간주하지 않으며 Player 빌드 장면 목록에도 넣지 않는다.
