# cds-window

대항해시대풍 **항해·무역** 게임을 Unity로 만드는 프로젝트.
현재 **Phase 0**(파도 위에 배를 띄우고 조종) 까지 구현됨.

- 엔진: **Unity 6.2** (`6000.2.9f1`)
- 렌더: **URP** (Universal Render Pipeline) — 추후 Crest Ocean System 연동 예정
- 카메라: Cinemachine 패키지 포함 (현재는 자체 `CameraFollow` 사용)

---

## 요구 사항

| 항목 | 버전 |
|------|------|
| Unity Editor | `6000.2.9f1` (Unity 6.2) |
| Unity Hub | 3.x |

> 다른 Unity 버전으로 열면 패키지 재해석이 일어날 수 있습니다. 가능하면 동일 버전 권장.

---

## 프로젝트 열기

**방법 A — Unity Hub**
1. Unity Hub 실행 → **Add** → `Open project from disk`
2. 이 폴더(`cds-window`) 선택
3. 에디터 버전 `6000.2.9f1` 로 열기

**방법 B — 명령줄 (Windows PowerShell)**
```powershell
& "C:\Program Files\Unity Hub\Unity Hub.exe" -- --projectPath "C:\Users\ocean\git\cds-window"
```

> 처음 열 때는 셰이더/패키지 임포트로 몇 분 걸립니다.

---

## 실행 (에디터에서 플레이)

1. Project 창에서 **`Assets/Scenes/Phase0.unity`** 더블클릭해 씬을 엽니다.
2. 상단 **▶ Play** 버튼을 누릅니다.

### 조작
| 키 | 동작 |
|----|------|
| `W` / `↑` | 전진 |
| `S` / `↓` | 후진 |
| `A` / `←` | 왼쪽 선회 |
| `D` / `→` | 오른쪽 선회 |

배가 수면으로 떨어져 파도를 따라 출렁이며 뜨고, 방향키로 조종됩니다.

### 파라미터 튜닝 (선택)
Hierarchy에서 오브젝트 선택 후 Inspector에서 조정:
- **Water** → `WaveField` : 파도 방향/높이/파장/속도
- **Boat** → `BoatController` : `thrust`(추력), `steerTorque`(선회력)
- **Boat** → `Buoyancy` : `displacementAmount`(부력), `waterDrag`(저항)

---

## 씬 다시 만들기

씬은 코드로 생성됩니다. 씬이 꼬이거나 처음부터 다시 만들려면:

**Unity 메뉴 → `Tools ▸ CDS ▸ Build Phase 0 Scene`**

`Assets/Scenes/Phase0.unity` 가 새로 생성됩니다.

---

## 실행 파일로 빌드 (Standalone)

1. 메뉴 **`File ▸ Build Profiles`** (구버전은 `Build Settings`)
2. 플랫폼 **Windows** 선택
3. Scene List에 `Scenes/Phase0` 가 포함돼 있는지 확인 (기본 포함됨)
4. **Build** → 출력 폴더 지정 → `.exe` 생성

명령줄 빌드가 필요하면 (CI 등):
```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.2.9f1\Editor\Unity.exe" `
  -batchmode -quit -projectPath "C:\Users\ocean\git\cds-window" `
  -buildWindows64Player "Build\cds-window.exe" -logFile build.log
```

---

## 프로젝트 구조

```
Assets/
├── Scenes/
│   └── Phase0.unity          # 플레이 가능한 Phase 0 씬 (코드 생성)
├── Scripts/
│   ├── Ocean/
│   │   ├── WaveField.cs       # 파도 높이 계산 (← 추후 Crest로 교체 지점)
│   │   └── WaterPlane.cs      # 파도에 맞춰 출렁이는 바다 메시
│   ├── Boat/
│   │   ├── Buoyancy.cs        # 프로브 기반 부력 + 롤/피치
│   │   └── BoatController.cs  # WASD 조작
│   └── CameraRig/
│       └── CameraFollow.cs    # 배 추적 카메라
├── Editor/
│   ├── URPSetup.cs            # URP 파이프라인 생성/활성화 (초기 세팅용)
│   └── Phase0SceneBuilder.cs  # Phase 0 씬 자동 구성
└── Settings/                  # URP 파이프라인 · 머티리얼 애셋
```

---

## 로드맵

| Phase | 내용 | 상태 |
|-------|------|------|
| 0 | 파도 위 배 조작 (부력 + WASD + 카메라) | ✅ 완료 |
| 1 | 세계지도/도시 배치, 도시 도착 판정 | 예정 |
| 2 | 도시·항구 UI (교역소·숙소·조선소 등) | 예정 |
| 3 | 무역 루프 (매매·시세·소지금/적재) | 예정 |
| 4 | 함선/전투 | 예정 |
| 5 | Crest Ocean System 연동, 세이브/로드, 사운드 | 예정 |
