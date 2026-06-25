# Mygame — 프로젝트 가이드 (CLAUDE.md)

마녀의 샘(Witch's Spring) 스타일의 **2D 싱글 스토리 모바일 게임**.
Unity로 제작하며, 아트/애니메이션/맵/스토리를 외부 툴에서 만들고 Unity로 통합한다.

## 기술 스택

| 영역 | 도구 / 패키지 | 버전 |
|------|----------------|------|
| 엔진 | Unity (2D, URP) | 6000.4.11f1 |
| 렌더 | Universal Render Pipeline (2D Renderer) | 17.4.0 |
| 2D 기능 | com.unity.feature.2d | 2.0.2 |
| 캐릭터 애니 | Spine Runtime (spine-unity) | git #4.2 |
| 맵 | LDtkToUnity (com.cammin.ldtkunity) | 6.12.3 (OpenUPM) |
| 스토리 | Ink (com.inkle.ink-unity-integration) | 1.2.2 (OpenUPM) |
| 입력 | Input System | 1.19.0 |

> 서드파티 패키지는 `Packages/manifest.json`로 관리한다. LDtk/Ink는 OpenUPM
> scoped registry, Spine은 git URL로 연결되어 있다. 임의로 버전을 바꾸지 말 것.

## 화면 / 플랫폼

- 기본 해상도: **1080 x 1920 (세로 / Portrait)**
- 타깃 플랫폼: 모바일 (Android / iOS). 세로 고정.
- 카메라: Orthographic, URP 2D Renderer 사용.

## 폴더 구조

```
Mygame/
├── Assets/
│   ├── Sprites/        # 정적 스프라이트, 아틀라스
│   ├── Animations/     # Spine export(.json/.atlas/.png), Unity 애니
│   ├── Maps/           # LDtk import 결과(.ldtk → 프리팹/씬)
│   ├── Ink/            # Ink 컴파일 결과(.json) + .ink 소스
│   ├── Audio/          # BGM, SFX
│   ├── UI/             # UI 스프라이트, 프리팹
│   ├── Scenes/         # Main.unity 등 씬
│   ├── Settings/       # URP_2D.asset, Renderer2D.asset
│   ├── Editor/         # 에디터 전용 스크립트(ProjectBootstrap 등)
│   └── Scripts/
│       ├── Dialogue/   # Ink 연동, 대화 UI, 분기 제어
│       ├── Map/        # LDtk 로딩, 맵 전환, 충돌/엔티티
│       ├── Character/  # 캐릭터 컨트롤, Spine 애니 제어
│       └── System/     # 세이브/로드, 게임 매니저, 씬 흐름
├── RawAssets/          # 작업 원본 (git 추적 O, 빌드 미포함)
│   ├── spine/  ldtk/  ink/  audio/  ui/
├── Skills/             # 제작 파이프라인 단계별 작업 폴더 (01~09)
└── Docs/               # 기획서, 설계 문서
```

## 제작 파이프라인 (Skills 01 → 09)

원본은 `RawAssets/`에서 만들고, Unity용 산출물은 `Assets/`로 들어온다.

```
01_GameDirector      게임 방향성 · GDD · 작업 지시
        ↓
02_CharacterConcept  캐릭터 컨셉 아트
        ↓
03_CharacterParts    파츠 분리 (→ RawAssets/spine)
        ↓
04_SpineAnimation    리깅 · 애니 · export  → Assets/Animations, Assets/Sprites
05_BackgroundMap     LDtk 맵 (RawAssets/ldtk)  → Assets/Maps
06_UI                UI 에셋 (RawAssets/ui)   → Assets/UI
07_StoryInk          Ink 스토리 (RawAssets/ink) → Assets/Ink
        ↓
08_UnityIntegration  씬 구성 · 프리팹 · 스크립트 연동 (Assets/Scripts)
        ↓
09_QABuild           테스트 · 빌드 · 배포
```

각 단계의 상세 역할은 해당 `Skills/<단계>/README.md` 참고.

## 코딩 규칙

- 네임스페이스 루트: `Mygame`. 폴더와 네임스페이스를 맞춘다
  (예: `Mygame.Dialogue`, `Mygame.Map`, `Mygame.Character`, `Mygame.System`).
- 런타임 스크립트는 `Assets/Scripts/<영역>/`, 에디터 전용은 `Assets/Editor/`.
- MonoBehaviour 1파일 1클래스, 파일명 = 클래스명.
- 시스템 간 의존은 `System`(매니저)에서 조립. 영역끼리 직접 참조는 최소화.
- 매직 넘버 대신 ScriptableObject/상수 사용.

## 에셋 연동 규칙

- **Spine**: export한 `_SkeletonData`/atlas/png 세트를 `Assets/Animations/<캐릭터>/`에
  통째로 넣는다. 애니 재생은 `Character` 스크립트에서 제어.
- **LDtk**: `.ldtk` 파일은 `Assets/Maps/`에 두고 LDtkToUnity가 임포트. 맵 로딩/전환은
  `Map` 스크립트에서.
- **Ink**: `.ink` 작성 → 컴파일된 `.json`을 `Assets/Ink/`에. 런타임 재생/분기/변수는
  `Dialogue` 스크립트(ink-unity-integration `Story` API)로 제어.

## 빌드 / 실행

- 메인 씬: `Assets/Scenes/Main.unity` (Build Settings 0번에 등록됨).
- 모바일 빌드 시 Android/iOS 모듈 필요. 세로 고정 확인.
- 에디터: Unity 6000.4.11f1로 열 것. 첫 오픈 시 패키지 자동 복원됨.

## Git 규칙

- `Library/`, `Temp/`, `Logs/`, `Build/` 등은 커밋하지 않는다(.gitignore 적용).
- `Assets/` `Packages/` `ProjectSettings/` `RawAssets/` `Skills/` `Docs/`는 추적.
- `.meta` 파일은 반드시 함께 커밋한다.
