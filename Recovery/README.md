# Recovery workspace

`Recovery`에는 복구 대상을 설명하고 다시 검증할 수 있는 작은 텍스트 자료만 둔다.
실제 Unity 런타임 리소스는 `Assets/Game/Content`, 테스트 전용 장면과 코드는
`Assets/Recovery/Harness`, 복구용 Editor 코드는 `Assets/Recovery/Editor`에 둔다.

## 추적하는 것

- `Catalogs`: 복구 대상과 사용 범위
- `Manifests`: 원본/생성 파일의 상대경로, 크기, SHA-256, 근거 상태
- `Schemas`: manifest 형식
- `Reports`: 사람이 읽는 최종 요약과 검증 결과

## 추적하지 않는 것

- 원본 저장소의 절대경로
- 전체 추출 원본
- 원본 캡처, Unity 로그, 테스트 XML 등 큰 실행 산출물

원본 위치는 `TRICKCAL_REFERENCE_ROOT` 환경 변수 또는 무시되는
`UserSettings/TrickcalRecoverySettings.json`에서만 읽는다. 복구 명령은 자동으로
실행되지 않으며 메뉴나 명령줄에서 명시적으로 호출해야 한다.
