# Unity Editor'da yig'ish qo'llanmasi

## 0. Tezkor start (5 daqiqa)

1. Unity Hub → **Add project from disk** → repozitoriy papkasi. Versiya: **2022.3 LTS** (Unity 6 ham ishlaydi — Package Manager paketlarni avtomatik yangilaydi).
2. `Window → TextMeshPro → Import TMP Essential Resources`.
3. `Edit → Project Settings → Player → Active Input Handling = Input System Package (New)` (yoki *Both*). Unity qayta ishga tushadi.
4. URP: `Assets → Create → Rendering → URP Asset (with Universal Renderer)` → `Project Settings → Graphics` va `Quality` ga biriktiring.
5. **`Tools → Circuit Lab → Build Starter Content`** — layer, ScriptableObject'lar, materiallar, komponent prefablari va `Prefabs/CircuitLabBoard.prefab` avtomatik yaratiladi.
6. `Window → General → Test Runner → EditMode → Run All` — solver testlari yashil bo'lishi kerak.

## 1. Layer va Physics

| Sozlama | Qiymat |
|---|---|
| User Layer 8 | `CircuitInteractable` (wizard o'zi qo'shadi; band bo'lsa — birinchi bo'sh layer, Console'da indeks yoziladi) |
| Terminallar, Kalit/Lampochka tanasi | Layer = `CircuitInteractable` |
| Stend yuzasi, korpuslar, simlar | Default (raycast mask'ga kirmaydi → terminallarni to'smaydi) |
| `Interactable Mask` (AR/VR/Mouse kontrollerlar) | Faqat `CircuitInteractable` |
| Physics → Layer Collision Matrix | `CircuitInteractable` × hamma = o'chiq (faqat raycast kerak, to'qnashuv emas) |
| Physics → Auto Sync Transforms | o'chiq |

**Collider qoidalari:**
- Terminal: `SphereCollider`, radius vizual sferadan ~2× katta (barmoq/nigoh uchun). Rigidbody **kerak emas**.
- Kalit va Lampochka: komponent ildizida `BoxCollider` (bosish → kalitni almashtirish / kuygan lampani tiklash).
- Boshqa komponent korpuslarida collider yo'q yoki Default layer'da.
- VR'da terminal radiusini 1.5× oshiring (gaze aniqligi pastroq).

## 2. Prefab iyerarxiyasi

```
P_Battery                       [CircuitComponent(data=CD_Battery_4_5V)]
├── Body                        [MeshRenderer]                       Layer: Default
├── Terminal_A (+)              [SphereCollider, CircuitTerminal(Positive), TerminalHighlighter]  Layer: CircuitInteractable
└── Terminal_B (-)              [SphereCollider, CircuitTerminal(Negative), TerminalHighlighter]  Layer: CircuitInteractable

P_Bulb                          [CircuitComponent(CD_Bulb), BoxCollider, BulbVisualizer]  Layer: CircuitInteractable
├── Body
├── Glass_Filament              [MeshRenderer: M_BulbFilament (URP Lit, Emission ✔)]
│   └── PointLight              [Light: Point, Range 0.5, Shadows: None]
├── Terminal_A (+)
└── Terminal_B (-)

P_Switch                        [CircuitComponent(CD_Switch), BoxCollider, SwitchVisualizer]  Layer: CircuitInteractable
├── Body
├── LeverPivot → Lever
├── Terminal_A / Terminal_B

P_Ammeter / P_Voltmeter         [CircuitComponent, MeterDisplay]
├── Body
├── Display                     [TextMeshPro (3D) — World Space]
├── Terminal_A / Terminal_B

P_Wire                          [LineRenderer: M_Wire, Texture Mode = Tile, Alignment = View, Shadows Off]

CircuitLabBoard                 [CircuitBoard, CircuitWiringController]
├── BoardSurface                [MeshRenderer]   (ildiz pivoti = stend yuzasi, Y = 0)
├── Components                  (yuqoridagi prefablar nested holda)
├── Wires                       [WireVisualizer(board, wirePrefab=P_Wire)]
└── ShortCircuitFeedback        [ShortCircuitFeedback(board, alarm AudioSource loop, spark ParticleSystem, warning CanvasGroup)]
    └── WarningPanel            [Canvas (World Space), CanvasGroup, TMP "QISQA TUTASHUV!"]
```

> `ShortCircuitFeedback` komponentini `WarningPanel` ning **o'zida emas**, ota obyektda saqlang — panel o'chirilganda skript ishlashda davom etadi.

**Yangi komponent qo'shish:** `Create → Circuit Lab → Component Data` → turini va parametrlarini kiriting → mavjud prefabni nusxalab `data` maydoniga yangi aktivni qo'ying. Kod o'zgarmaydi.

## 3. Scene_AR

```
Scene_AR
├── AR Session                  [ARSession, ARInputManager]
├── XR Origin (AR)              [XROrigin, ARRaycastManager, ARPlaneManager, ARAnchorManager, ARCircuitController]
│   └── Camera Offset
│       └── Main Camera         [Camera, ARCameraManager, ARCameraBackground, TrackedPoseDriver (Input System)]
├── PlacementIndicator          [Quad/halqa, collider'siz]
├── CircuitLabBoard             (masshtab 1.0; boshida faol bo'lishi mumkin — kontroller o'zi yashiradi)
├── HUD Canvas (Screen Space)   ["Qayta joylashtirish" tugmasi → ARCircuitController.ResetPlacement()]
├── EventSystem                 [InputSystemUIInputModule]
└── Directional Light
```

- `ARPlaneManager.Detection Mode = Horizontal`, Plane Prefab — yarim shaffof AR Default Plane.
- `ARCircuitController`: `arCamera = Main Camera`, `labRoot = CircuitLabBoard`, `placementIndicator`, `interactableMask = CircuitInteractable`.
- URP Renderer Data → **Add Renderer Feature → AR Background Renderer Feature** (aks holda kamera foni qora).
- Kamera: `Clear Flags/Background Type = Solid Color` (ARCameraBackground o'zi chizadi), `Near = 0.05`.

## 4. Scene_VR

```
Scene_VR
├── XR Origin (VR)              [XROrigin, VRGazeController(rayOrigin = Main Camera)]
│   └── Camera Offset (Y = 1.4 — o'tirgan holat)
│       └── Main Camera         [Camera, TrackedPoseDriver (centerEyePosition/Rotation)]
│           └── GazeReticle     [Canvas World Space, 0.001 scale]
│               ├── Ring        [Image — doira]
│               └── Fill        [Image: Type = Filled, Radial 360, Origin Top, fillAmount 0]
├── Environment                 (stol, xona — statik, baked lighting)
├── CircuitLabBoard             (pozitsiya (0, 0.75, 0.55), masshtab 1.5 — nigoh bilan tanlash osonroq)
├── EventSystem                 [InputSystemUIInputModule]  (ixtiyoriy)
└── Directional Light
```

- `VRGazeController`: `reticle = GazeReticle`, `reticleFill = Fill`, `dwellTime = 1.2 s`, `interactableMask = CircuitInteractable`.
- `selectAction` (ixtiyoriy): Input Action bindinglari — `<Touchscreen>/primaryTouch/tap` (Cardboard tugmasi), `<XRController>{RightHand}/triggerPressed`, `<Keyboard>/space`.
- XR Interaction Toolkit kontrolleri ishlatilsa: `rayOrigin` sifatida kontroller Transform'ini bering — kod o'zgarmaydi.
- Reticle'ni boshqa collider'lar to'smasligi uchun Canvas'da `GraphicRaycaster` kerak emas.

## 5. Scene_Sandbox (qurilmasiz sinash)

`Main Camera` (stendga 45° qarab) + `MouseInteractionController` + `CircuitLabBoard`. Play → terminallarni sichqoncha bilan bosib simlarni ulang.

## 6. Scene_AR va Scene_VR farqlari

| | Scene_AR | Scene_VR |
|---|---|---|
| Input | Touch (EnhancedTouch) | Gaze dwell / tugma / kontroller |
| Kontroller | `ARCircuitController` | `VRGazeController` |
| Stend joylashuvi | Real stol, `ARAnchor` | Virtual stol, statik |
| Masshtab | 1.0 (real o'lcham) | 1.5 |
| Kamera | AR Camera + Background | Stereo (XR plugin) |
| XR Plug-in | ARCore / ARKit | Cardboard XR Plugin |
| Yorug'lik | Light Estimation (ixtiyoriy) | Baked + 1 realtime |
| Umumiy | `CircuitLabBoard.prefab`, barcha Core/Visual skriptlar, SO'lar | ← xuddi shu |

## 7. Foydalanish (ikkala rejimda)

1. Terminalni tanlang → yashil rangga kiradi (Pending).
2. Ikkinchi terminalni tanlang → Bezye sim paydo bo'ladi, zanjir darhol hisoblanadi.
3. Xuddi shu ikki terminalni qayta tanlash → sim uziladi. Bir terminalni ikki marta tanlash → bekor qilish.
4. Kalitni bosing → ochiladi/yopiladi. Kuygan lampochkani bosing → almashtiriladi.
5. Batareya qutblarini to'g'ridan-to'g'ri ulang → qizil simlar, signal, uchqunlar ("Qisqa tutashuv").
