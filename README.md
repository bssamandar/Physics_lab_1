# Physics Lab 1 — Elektr zanjirini yig'ish laboratoriyasi (AR / VR)

Unity (2022.3 LTS / Unity 6, URP) uchun interaktiv fizika laboratoriyasi. Bitta umumiy fizika yadrosi (**Shared Core**) ustida ikki interfeys:

- **Mobile AR** — AR Foundation (ARCore / ARKit): stol yuzasini aniqlash, stendni `ARAnchor` ga joylashtirish, touch raycast orqali simlarni ulash.
- **Mobile VR** — Cardboard / XR Interaction Toolkit: nigoh (gaze) + radial reticle dwell yoki kontroller trigger orqali zanjir yig'ish.

## Imkoniyatlar

- Zanjir graf ko'rinishida: terminal = tugun, komponent/sim = qirra.
- **Nodal Analysis** (Kirxgof tok qonuni + Om qonuni), qisman pivotli Gauss — ketma-ket, parallel va aralash zanjirlar.
- Batareya ichki qarshiligi, ampermetr shunti, voltmetr kirish qarshiligi — real o'lchov xatti-harakati.
- Qisqa tutashuvni aniqlash → qizil simlar, signal, uchqunlar, World Space ogohlantirish. Ortiqcha tokda lampochka kuyadi.
- Kvadratik **Bezye** bo'yicha salqiydigan simlar (`LineRenderer` + `ObjectPool`), tok yo'nalishi va kuchiga mos oqim animatsiyasi.
- Lampochka yorqinligi `P = I²R` ga proporsional (Emission + Point Light), raqamli ampermetr/voltmetr.
- Komponent parametrlari — `ScriptableObject` (`ComponentDataSO`).
- Barcha bog'lanishlar — C# `event Action` (qattiq bog'liqliksiz).
- `Update()` da GC ajratish yo'q (`RaycastNonAlloc`, keshlangan komponentlar, `MaterialPropertyBlock`, `TMP.SetText`).

## Tez boshlash

1. Unity Hub → *Add project from disk* → shu papka.
2. `Window → TextMeshPro → Import TMP Essential Resources`.
3. Player Settings → Active Input Handling = **Input System Package**.
4. **`Tools → Circuit Lab → Build Starter Content`** — prefablar, SO'lar, materiallar va `CircuitLabBoard.prefab` avtomatik yaratiladi.
5. Sahnalarni [Docs/EDITOR_SETUP.md](Docs/EDITOR_SETUP.md) bo'yicha yig'ing, build uchun [Docs/BUILD_CHECKLIST.md](Docs/BUILD_CHECKLIST.md).

## Hujjatlar

| Hujjat | Mazmuni |
|---|---|
| [Docs/ARCHITECTURE.md](Docs/ARCHITECTURE.md) | Papkalar tuzilmasi, modullar diagrammasi, ma'lumot oqimi, fizik model, optimizatsiya |
| [Docs/EDITOR_SETUP.md](Docs/EDITOR_SETUP.md) | Prefab iyerarxiyasi, Collider/Layer, Scene_AR va Scene_VR farqlari |
| [Docs/BUILD_CHECKLIST.md](Docs/BUILD_CHECKLIST.md) | Android/iOS: XR Plug-in Management, Player Settings, ruxsatnomalar |

## Asosiy skriptlar

| Qatlam | Fayl |
|---|---|
| Core / Data | [`ComponentDataSO.cs`](Assets/_Project/Scripts/Core/Data/ComponentDataSO.cs) |
| Core / Interaction | [`ICircuitInteractable.cs`](Assets/_Project/Scripts/Core/Interaction/ICircuitInteractable.cs) |
| Core / Model | [`CircuitTerminal.cs`](Assets/_Project/Scripts/Core/Model/CircuitTerminal.cs), [`CircuitComponent.cs`](Assets/_Project/Scripts/Core/Model/CircuitComponent.cs), [`CircuitBoard.cs`](Assets/_Project/Scripts/Core/Model/CircuitBoard.cs), [`CircuitWiringController.cs`](Assets/_Project/Scripts/Core/Model/CircuitWiringController.cs) |
| Core / Solver | [`CircuitSolver.cs`](Assets/_Project/Scripts/Core/Solver/CircuitSolver.cs), [`CircuitElement.cs`](Assets/_Project/Scripts/Core/Solver/CircuitElement.cs) |
| Visual | [`WireVisualizer.cs`](Assets/_Project/Scripts/Visual/WireVisualizer.cs), [`BulbVisualizer.cs`](Assets/_Project/Scripts/Visual/BulbVisualizer.cs), [`MeterDisplay.cs`](Assets/_Project/Scripts/Visual/MeterDisplay.cs), [`ShortCircuitFeedback.cs`](Assets/_Project/Scripts/Visual/ShortCircuitFeedback.cs) |
| XR / AR | [`ARCircuitController.cs`](Assets/_Project/Scripts/XR/AR/ARCircuitController.cs) |
| XR / VR | [`VRGazeController.cs`](Assets/_Project/Scripts/XR/VR/VRGazeController.cs) |
| Testlar | [`CircuitSolverTests.cs`](Assets/_Project/Tests/EditMode/CircuitSolverTests.cs) |

## Paketlar (2022.3 LTS)

URP 14, AR Foundation 5.1 (+ ARCore, ARKit), Input System 1.7, XR Management 4.4, XR Interaction Toolkit 2.5, TextMeshPro. Cardboard XR Plugin git URL orqali qo'shiladi (qarang: build checklist). Unity 6'da ochilganda Package Manager versiyalarni mosiga yangilaydi.
