# Mygame — 프로젝트 가이드 (CLAUDE.md)

마녀의 샘(Witch's Spring) 스타일의 **2D 싱글 스토리 모바일 게임**.
AI 툴 체인으로 에셋을 생성하고 Unity에서 통합한다.

## 기술 스택

| 영역 | 도구 / 패키지 | 버전 |
|------|----------------|------|
| 엔진 | Unity (2D, URP) | 6000.4.11f1 |
| 렌더 | Universal Render Pipeline (2D Renderer) | 17.4.0 |
| 맵 | LDtkToUnity (com.cammin.ldtkunity) | 6.12.3 (OpenUPM) |
| 스토리 | Ink (com.inkle.ink-unity-integration) | 1.2.2 (OpenUPM) |
| 입력 | Input System | 1.19.0 |
| (legacy) | Spine Runtime | git #4.2 — 현재 파이프라인 미사용 |

> 캐릭터 애니메이션은 **Spine이 아니라 Meshy AI(FBX/GLB)** 로 제작한다.
> Spine 패키지는 남아있지만 이 파이프라인에서는 사용하지 않는다.

## 화면 / 플랫폼
- 기본 해상도: **1080 x 1920 (세로 / Portrait)**, 세로 고정
- 타깃: Android / iOS, Orthographic 카메라 + URP 2D Renderer

---

## 제작 파이프라인 (9단계)

```
1. ComfyUI      캐릭터/배경 이미지 생성        → RawAssets/comfyui
2. Scenario.gg  파츠 분리 자동화               → RawAssets/scenario
3. Meshy AI     자동 리깅 + 애니메이션(FBX/GLB) → RawAssets/meshy → Assets/Models
4. Suno         BGM 생성                       → RawAssets/suno  → Assets/Audio/BGM
5. ElevenLabs   보이스 생성                    → RawAssets/elevenlabs → Assets/Audio/Voice
6. Inky         스토리/대화(.ink → .json)      → RawAssets/ink   → Assets/Ink
7. LDtk         맵 제작(.ldtk)                 → RawAssets/ldtk  → Assets/Maps
8. Unity        최종 통합 (씬/프리팹/스크립트)  → Assets/Scripts, Assets/Scenes
9. GitHub       버전관리                       → origin/main
```

**원칙**: 툴에서 export한 **원본**은 `RawAssets/<툴>/` 에 보관(빌드 미포함),
Unity가 실제로 쓰는 **변환/임포트 결과**만 `Assets/` 에 둔다.

---

## 폴더 ↔ 툴 매핑 (export 파일을 어디에 넣는가)

### 1. ComfyUI — 이미지 생성
| 결과물 | 원본 위치 | Unity 위치 |
|--------|-----------|------------|
| 캐릭터 컨셉(png) | `RawAssets/comfyui/characters/` | (Scenario로 전달) |
| 배경(png) | `RawAssets/comfyui/backgrounds/` | `Assets/Sprites/Backgrounds/` |
| UI 소스(png) | `RawAssets/comfyui/ui/` | `Assets/UI/` |

> 배경/UI는 그대로 스프라이트로 쓰므로 Assets로 복사. PNG는 sRGB, 단일 스프라이트.

### 2. Scenario.gg — 파츠 분리
| 결과물 | 원본 위치 | Unity 위치 |
|--------|-----------|------------|
| 분리된 파츠(psd/png 레이어) | `RawAssets/scenario/<캐릭터>/` | (Meshy로 전달) |

> 파츠는 Meshy 리깅 입력으로만 사용. 2D 스프라이트로 직접 쓸 경우만 `Assets/Sprites/`로.

### 3. Meshy AI — 리깅 + 애니메이션 ⭐
| 결과물 | 원본 위치 | Unity 위치 |
|--------|-----------|------------|
| 리깅된 모델 + 애니(fbx/glb) | `RawAssets/meshy/<캐릭터>/` | `Assets/Models/<캐릭터>/` |
| 텍스처(png) | 〃 (fbx와 동봉) | `Assets/Models/<캐릭터>/` |

> **`Assets/Models/` 아래에 넣으면 `MeshyModelPostprocessor`가 자동 임포트 설정 +
> AnimatorController + 프리팹을 `Assets/Models/Prefabs/`에 생성한다.** (아래 4번 항목)
> 파일명 규칙: `<캐릭터>@<애니메이션>.fbx` (예: `witch@idle.fbx`, `witch@walk.fbx`).
> `@` 앞이 같은 모델들은 하나의 캐릭터로 묶여 AnimatorController에 모인다.

### 4. Suno — BGM
| 결과물 | 원본 위치 | Unity 위치 |
|--------|-----------|------------|
| BGM(wav/mp3) | `RawAssets/suno/` | `Assets/Audio/BGM/` |

> 임포트 설정: Load Type = Streaming, Background music는 보통 Compressed/Vorbis.

### 5. ElevenLabs — 보이스
| 결과물 | 원본 위치 | Unity 위치 |
|--------|-----------|------------|
| 보이스(mp3/wav) | `RawAssets/elevenlabs/` | `Assets/Audio/Voice/` |

> 파일명을 Ink 태그와 맞춘다: `#voice:witch_001` → `Assets/Audio/Voice/witch_001.wav`.
> 대사 음성은 `DialogueManager`의 voice 태그로 재생(아래 6번).

### 6. Inky — 스토리/대화
| 결과물 | 원본 위치 | Unity 위치 |
|--------|-----------|------------|
| `.ink` 소스 | `RawAssets/ink/` | `Assets/Ink/` (선택) |
| 컴파일 `.json` | — | `Assets/Ink/` |

> `.ink`를 `Assets/Ink/`에 두면 ink-unity-integration이 `.json`을 자동 컴파일.
> 런타임 재생은 `Assets/Scripts/Dialogue/DialogueManager.cs`가 담당.

### 7. LDtk — 맵
| 결과물 | 원본 위치 | Unity 위치 |
|--------|-----------|------------|
| `.ldtk` + 타일셋(png) | `RawAssets/ldtk/` | `Assets/Maps/` |

> `.ldtk`를 `Assets/Maps/`에 두면 LDtkToUnity가 프리팹으로 임포트.
> 런타임 로딩/레벨 전환은 `Assets/Scripts/Map/MapLoader.cs`가 담당.

---

## Assets 폴더 구조

```
Assets/
├── Sprites/        # ComfyUI 배경, 스프라이트
│   └── Backgrounds/
├── Models/         # Meshy FBX/GLB (자동 임포트 대상)
│   └── Prefabs/    # MeshyModelPostprocessor가 생성
├── Animations/     # 추가 애니 클립/컨트롤러
├── Maps/           # LDtk .ldtk 임포트
├── Ink/            # .ink + 컴파일 .json
├── Audio/
│   ├── BGM/        # Suno
│   └── Voice/      # ElevenLabs
├── UI/             # UI 스프라이트/프리팹
├── Scenes/         # Main.unity
├── Settings/       # URP_2D.asset, Renderer2D.asset
├── Editor/         # MeshyModelPostprocessor.cs 등 에디터 전용
└── Scripts/
    ├── Dialogue/   # DialogueManager.cs (Ink)
    ├── Map/        # MapLoader.cs (LDtk)
    ├── Character/  # 캐릭터 컨트롤 / Animator 제어
    └── System/     # 게임매니저, 세이브/로드, 씬 흐름
```

---

## 코딩 규칙
- 네임스페이스 루트 `Mygame`, 폴더와 일치 (`Mygame.Dialogue`, `Mygame.Map`, `Mygame.Character`, `Mygame.System`).
- 런타임 스크립트는 `Assets/Scripts/<영역>/`, 에디터 전용은 `Assets/Editor/`.
- 1파일 1클래스, 파일명 = 클래스명. 매직넘버 대신 SerializeField/상수.

## 네이밍 규칙 (중요)
- Meshy FBX: `<캐릭터>@<애니>.fbx` (idle/walk/attack/talk …)
- ElevenLabs 보이스: `<캐릭터>_<번호>.wav`, Ink 태그 `#voice:<파일명>`과 동일
- Ink 노트/knot: 영문 snake_case
- LDtk 레벨 Identifier: `Level_<지역>_<번호>` 권장

## Git 규칙
- `Library/ Temp/ Logs/ Build/ .claude/` 미커밋(.gitignore).
- `Assets/ Packages/ ProjectSettings/ RawAssets/ Skills/ Docs/` 추적, `.meta` 항상 동반 커밋.
- 큰 바이너리(fbx/wav/png)가 많아지면 Git LFS 도입 검토.
- 원격: `origin` = https://github.com/roql47/Storygame.git (main).
