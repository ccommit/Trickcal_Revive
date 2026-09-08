# 현재 페이지 텍스트 스타일 복구

## 범위

- 로비
- 스테이지 선택
- 스테이지 상세 팝업
- 파티 구성

이 작업은 현재 구현된 네 화면에 실제로 보이는 텍스트만 대상으로 한다. 원본 폰트·머티리얼 전체를 가져오거나, 아직 구현되지 않은 화면의 프리셋을 미리 추가하지 않는다.

## 확인됨

- 현재 프로젝트의 `ONE Mobile POP.ttf`는 추출 원본과 SHA-256이 일치한다.
- 원본 `SystemFont_Outlined`, `SystemFont_Outlined_Black`, `SystemFont_Z_BattleList`는 `UNDERLAY_ON`을 사용한다.
- 선택한 세 머티리얼은 `_FaceTex`를 사용하지 않으므로 `rawsprites/fonttexture`를 가져올 필요가 없다.
- Unity 6의 현재 TMP 폰트 에셋을 유지한 채, 그 아틀라스를 공유하는 머티리얼 프리셋 3개를 재구성했다.
- EditMode 78/78, PlayMode 3/3, 계층 검사 99개 파일이 통과했다.

## 적용 기준

| 대상 | 글자 면 | 외곽 효과 | 근거 |
|---|---|---|---|
| 로비 하단 `모집`, `사도`, `모험` | 어두운색 | 흰색 | `SystemFont_Outlined` 수치와 로비 기준 이미지 |
| 로비 외 페이지의 공통 페이지 제목 | 어두운색 | 흰색 | `SystemFont_Outlined` 수치와 스테이지·파티 기준 이미지 |
| 스테이지 번호 | 흰색 | 보라색 | `SystemFont_Z_BattleList`의 확인된 Underlay 색 |
| 파티 상태와 진형 열 이름 | 흰색 | 검은색 | `SystemFont_Outlined_Black` 수치와 파티 기준 이미지 |
| 스테이지 상세의 회색·노란색 버튼 | 어두운색 | 없음 | 상세 팝업 기준 이미지 |
| 스테이지 상세 닫기 `X` | 흰색 | 없음 | 상세 팝업 기준 이미지 |
| 스테이지 난이도 탭 | 어두운색 | 없음 | 스테이지 선택 기준 이미지 |

## 생성 및 검증 위치

- 공용 적용 코드: `Assets/Game/App/Editor/RecoveredTextStyles.cs`
- 생성 머티리얼: `Assets/Game/Content/UI/Fonts/Materials/`
- 출처·해시 manifest: `Recovery/Manifests/current-page-text-styles.json`
- EditMode 결과: `Artifacts/SpineRecovery/TextStyles/editmode-results.xml`
- PlayMode 결과: `Artifacts/SpineRecovery/TextStyles/playmode-results.xml`
- 캡처: `Artifacts/SpineRecovery/LoginLobby/Captures/main-16x9.png`
- 캡처: `Artifacts/SpineRecovery/StageSelect/Captures/stageselect-16x9.png`
- 캡처: `Artifacts/SpineRecovery/StageSelect/Captures/stageinfo-16x9.png`
- 캡처: `Artifacts/SpineRecovery/StageSelect/Captures/partysetup-16x9.png`

## 추론

- 페이지 제목과 로비 버튼은 기준 이미지의 외형과 공통 원본 프리셋의 용도에 따라 `SystemFont_Outlined`를 대응시켰다. 화면별 런타임 직렬화 데이터에서 해당 머티리얼 이름까지 직접 확인한 것은 아니다.
- 파티 상태·진형 열 이름은 어두운 배경에서의 가독성과 기준 이미지에 따라 검은 외곽 프리셋을 대응시켰다.

## 미확인 및 남은 위험

- 원작의 해상도별 글자 크기, 자간, 외곽선의 최종 픽셀 두께는 실제 기기 영상과의 프레임 단위 비교가 필요하다.
- `ONE Mobile POP SDF.asset`에는 Unity 배치 임포트 시 비결정적 결과 경고가 남아 있다. 테스트와 렌더에는 이상이 없지만, 폰트 아틀라스 생성 절차를 별도 이슈로 고정할 필요가 있다.
- 프로필 이름과 통화 숫자처럼 이번 기준 이미지에서 전용 프리셋을 확정할 수 없는 텍스트는 기존 일반 스타일을 유지했다.
