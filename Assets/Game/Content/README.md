# Game Content

복구가 끝나 Unity에서 직접 사용하는 리소스만 기능/도메인별로 둔다.
원본 추출 폴더 구조를 그대로 복제하지 않으며, 각 파일의 출처와 변환 이력은
`Recovery/Manifests`에서 상대경로와 SHA-256으로 추적한다.

issue2는 리소스와 복구 검증 Harness까지만 포함한다. 실제 플레이 장면과 게임 흐름은
후속 브랜치에서 `Assets/Game/Scenes` 아래에 구현한다.
