# Arxitektura

## 1. Papkalar tuzilmasi

```
Assets/_Project/
├── Scripts/
│   ├── Core/                        # CircuitLab.Core.asmdef — XR'ga bog'liq EMAS
│   │   ├── Data/ComponentDataSO.cs          # ScriptableObject: U, R, Imax, P
│   │   ├── Interaction/ICircuitInteractable.cs
│   │   ├── Model/
│   │   │   ├── CircuitBoard.cs              # Graf egasi, solve rejalashtirish, hodisalar
│   │   │   ├── CircuitComponent.cs          # Ikki qutbli komponent (graf qirrasi)
│   │   │   ├── CircuitTerminal.cs           # Ulanish qisqichi (graf tuguni)
│   │   │   ├── CircuitWiringController.cs   # "A → B" ulash holat mashinasi
│   │   │   └── WireConnection.cs            # Sim (pool qilinadigan qirra)
│   │   └── Solver/                          # Sof C# (UnityEngine'siz)
│   │       ├── CircuitElement.cs            # Norton elementi: G + I_manba
│   │       └── CircuitSolver.cs             # Nodal Analysis + Gauss
│   ├── Interaction/                 # CircuitLab.Interaction.asmdef (umumiy hover/raycast)
│   │   ├── InteractableHoverTracker.cs
│   │   └── RaycastUtility.cs
│   ├── Visual/                      # CircuitLab.Visual.asmdef (Core + TMP)
│   │   ├── WireVisualizer.cs                # Bezye + ObjectPool<LineRenderer>
│   │   ├── BulbVisualizer.cs                # Emission + Point Light
│   │   ├── MeterDisplay.cs                  # Ampermetr/Voltmetr (World Space TMP)
│   │   ├── TerminalHighlighter.cs
│   │   ├── SwitchVisualizer.cs
│   │   └── ShortCircuitFeedback.cs          # Audio + panel + uchqun pool
│   ├── XR/
│   │   ├── AR/ARCircuitController.cs        # CircuitLab.XR.AR.asmdef (AR Foundation)
│   │   ├── VR/VRGazeController.cs           # CircuitLab.XR.VR.asmdef (Input System)
│   │   └── Desktop/MouseInteractionController.cs  # Editor'da sinash uchun
│   └── Editor/CircuitLabSetupWizard.cs      # Tools → Circuit Lab → Build Starter Content
├── Prefabs/
│   ├── Components/   P_Battery, P_Bulb, P_Resistor, P_Switch, P_Ammeter, P_Voltmeter
│   ├── Wires/        P_Wire (LineRenderer)
│   ├── XR/           P_AR_Session, P_VR_Rig, P_GazeReticle
│   ├── UI/           P_ShortCircuitPanel, P_AR_HUD
│   └── CircuitLabBoard.prefab               # Ikkala sahnada umumiy stend
├── ScriptableObjects/Components/  CD_Battery_4_5V, CD_Bulb_3_5V, CD_Resistor_10R, ...
├── Materials/    M_Wire (URP Unlit + T_WireFlow), M_BulbFilament (URP Lit + Emission), ...
├── Audio/        SFX_ShortAlarm, SFX_Spark, SFX_Click
├── Scenes/       Scene_AR.unity, Scene_VR.unity, Scene_Sandbox.unity (sichqoncha bilan)
└── Tests/EditMode/CircuitSolverTests.cs
```

## 2. Modullar o'rtasidagi bog'lanish

```
                    ┌───────────────────────────────────────────────┐
                    │          XR / INPUT LAYER (almashtiriladi)    │
                    │  ARCircuitController   VRGazeController       │
                    │  (ARRaycast, Anchor,   (Gaze ray, Dwell,      │
                    │   Touch)                Reticle fill)         │
                    │            MouseInteractionController         │
                    └───────────────┬───────────────────────────────┘
                                    │ faqat ICircuitInteractable
                                    ▼
                    ┌───────────────────────────────────────────────┐
                    │   CircuitLab.Interaction                      │
                    │   InteractableHoverTracker, RaycastUtility    │
                    └───────────────┬───────────────────────────────┘
                                    │ OnHoverEnter / OnHoverExit / OnSelect
                                    ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                         CORE (CircuitLab.Core)                           │
│                                                                          │
│  CircuitTerminal ──OnSelect──► CircuitWiringController ──Connect()──┐    │
│        ▲  (tugun)                (A→B holat mashinasi)              ▼    │
│        │                                                     CircuitBoard│
│  CircuitComponent ──ConfigurationChanged──────────────────────►  (graf)  │
│   (qirra) ◄── ComponentDataSO (U, R, Imax)                        │      │
│                                                     LateUpdate: dirty?   │
│                                                          ▼               │
│                                         CircuitSolver (G·V = I, Gauss)   │
│                                                          │               │
│     events: StateChanged · Solved · WireAdded/Removed · ShortCircuitChanged
└───────────────────────────────┬──────────────────────────────────────────┘
                                │ C# Action (obuna, qattiq bog'liqliksiz)
                                ▼
                    ┌───────────────────────────────────────────────┐
                    │           VISUAL & FEEDBACK (CircuitLab.Visual)│
                    │  WireVisualizer  BulbVisualizer  MeterDisplay │
                    │  TerminalHighlighter SwitchVisualizer         │
                    │  ShortCircuitFeedback                         │
                    └───────────────────────────────────────────────┘
```

Bog'liqlik yo'nalishi faqat **pastdan yuqoriga**: Core hech narsaga bog'liq emas; Visual va XR Core'ga bog'liq, lekin bir-birini bilmaydi. AR va VR sahnalar bir xil `CircuitLabBoard.prefab` ni ishlatadi, farq faqat input kontrollerida.

## 3. Ma'lumot oqimi (bitta sim ulash)

1. Foydalanuvchi terminalga tegadi / qaraydi → XR kontroller `RaycastNonAlloc` → `InteractableHoverTracker.UpdateTarget(collider)`.
2. Tap / dwell tugadi → `ICircuitInteractable.OnSelect()` → `CircuitTerminal` → `CircuitWiringController.Select()`.
3. Ikkinchi terminal → `CircuitBoard.Connect()` → `WireAdded` hodisasi (WireVisualizer pool'dan LineRenderer oladi) va `_solveDirty = true`.
4. `CircuitBoard.LateUpdate()` → `CircuitSolver.Solve()` → har bir terminalga potensial, komponentga tok yoziladi.
5. `CircuitComponent.StateChanged` → BulbVisualizer, MeterDisplay yangilanadi. Batareya toki `MaxCurrent` dan oshsa → `ShortCircuitChanged(true)`.

## 4. Fizik model

| Komponent | Solver elementi | Izoh |
|---|---|---|
| Battery | Norton: `G = 1/r`, `I = E/r` | Ichki qarshilik qisqa tutashuv tokini cheklaydi: `I_qt = E / (r + R_sim)` |
| Resistor, Bulb | `G = 1/R` | Lampochka yorqinligi `P = I²R / P_nom` |
| Switch | yopiq: `G = 1/R_yopiq`, ochiq: qirra yo'q | |
| Ammeter | `G = 1/R_shunt` (0.01 Om) | Ketma-ket ulanadi |
| Voltmeter | `G = 1/R_kirish` (1 MOm) | Parallel ulanadi |
| Sim | `G = 1/R_sim` (0.01 Om) | Sim toki ham hisoblanadi → oqim animatsiyasi |

Har bir terminal alohida tugun. Kirxgofning tok qonuni → `G·V = I` (n−1 noma'lum, yer tuguni — birinchi batareyaning "−" qutbi). Ulanmagan tugunlar uchun har bir diagonalga `GMIN = 1e-9 S` qo'shiladi (SPICE usuli), shuning uchun matritsa hech qachon singulyar bo'lmaydi. Ketma-ket va parallel ulanishlar alohida qoidalar bilan emas, umumiy graf orqali avtomatik hisoblanadi (testlar: `Tests/EditMode/CircuitSolverTests.cs`).

**Qisqa tutashuv:** batareya toki `ComponentDataSO.MaxCurrent` dan oshsa. `BreaksOnOverload = true` bo'lgan komponentlar (lampochka) kuyadi va zanjirni uzadi; ularni bosish orqali almashtirish mumkin.

## 5. SOLID qisqacha

- **S** — Solver faqat matematika, Board faqat graf va rejalashtirish, Visualizer'lar faqat ko'rinish.
- **O** — Yangi input usuli (masalan, Hand Tracking) = yangi kontroller, Core o'zgarmaydi.
- **L** — Har qanday `ICircuitInteractable` (terminal, kalit, lampochka) kontrollerlar uchun bir xil.
- **I** — `ICircuitInteractable` faqat 3 ta metod + 1 xossa.
- **D** — Kontrollerlar konkret klasslarni emas, interfeysni biladi; vizual qatlam hodisalarga obuna bo'ladi.

## 6. Ishlash samaradorligi (60 FPS, GC = 0 B/kadr)

- Solver faqat graf o'zgarganda ishlaydi (dirty flag), har kadr emas. Massivlar faqat tugunlar soni oshganda qayta ajratiladi.
- `Physics.RaycastNonAlloc` + oldindan ajratilgan `RaycastHit[8]`; `ARRaycastManager.Raycast` statik `List<ARRaycastHit>` bilan.
- Interfeys qidiruvi (`TryGetComponent<ICircuitInteractable>`) faqat collider o'zgarganda.
- `MaterialPropertyBlock` (material nusxalari yo'q), `Shader.PropertyToID` keshlangan.
- `TMP_Text.SetText(format, float)` — satr ajratmaydi, faqat qiymat o'zgarganda chaqiriladi.
- Simlar: `ObjectPool<WireView>` (LineRenderer + `Vector3[]`), egri chiziq faqat uchlar siljiganda qayta hisoblanadi.
- Uchqunlar: oldindan yaratilgan `ParticleSystem` halqa-pool'i.
- Lampochka Point Light'lari soyasiz; URP'da `Per Object Light Limit` ≥ 4.
