# Android / iOS Build Checklist

## Umumiy (ikkala platforma)

- [ ] `Project Settings → XR Plug-in Management` o'rnatilgan (`com.unity.xr.management`).
- [ ] Active Input Handling = **Input System Package (New)** yoki **Both**.
- [ ] Graphics: URP Asset biriktirilgan (Graphics + Quality darajalari).
- [ ] URP Asset (mobil): HDR **off**, MSAA **2x**, Render Scale **1.0** (VR: 0.9), Shadows: Main Light 1 cascade / 15 m, Additional Lights **Per Pixel, limit 4**, Additional Light Shadows **off**, SRP Batcher **on**.
- [ ] `Quality`: VSync **Don't Sync** (XR o'zi boshqaradi), `Application.targetFrameRate = 60` (AR uchun ixtiyoriy).
- [ ] Player → Other Settings: Color Space **Linear**, Scripting Backend **IL2CPP**, API Compatibility **.NET Standard 2.1**, Managed Stripping **Medium**.
- [ ] `Scenes In Build`: AR build uchun faqat `Scene_AR`, VR build uchun faqat `Scene_VR` (ikki xil build profili / alohida bundle id tavsiya etiladi).
- [ ] Profiler (Development Build) bilan tekshirish: `GC Alloc` ustuni Update/LateUpdate'da **0 B**, CPU < 16.6 ms.

## Android

| Bo'lim | Sozlama |
|---|---|
| Build Settings | Platform = Android, Texture Compression = **ASTC** |
| XR Plug-in Management (Android tab) | **AR:** ✔ Google ARCore · **VR:** ✔ Cardboard XR Plugin |
| Player → Other → Rendering | Auto Graphics API **o'chiq**, ro'yxatda faqat **OpenGLES3** (ARCore + Cardboard bilan barqaror). Vulkan ixtiyoriy, faqat sinovdan so'ng |
| Multithreaded Rendering | ✔ |
| Minimum API Level | **Android 7.0 (API 24)** (ARCore talabi) |
| Target API Level | Automatic (highest installed), Google Play uchun ≥ 34 |
| Target Architectures | ✔ **ARM64** (ARMv7 o'chiq) |
| Scripting Backend | IL2CPP |
| Package Name | masalan `uz.physicslab.circuit.ar` / `.vr` |
| Project Settings → XR → ARCore | Requirement = **Required** (faqat AR qurilmalar) yoki Optional; Depth = Optional |
| Publishing | Custom Main Gradle Template — kerak bo'lsa (Cardboard: protobuf-javalite qo'shimchasi) |
| Ruxsatnomalar | `android.permission.CAMERA` — ARCore plagini avtomatik qo'shadi; manifestda `android.hardware.camera.ar` (Required rejimda) |

**Cardboard (VR) qo'shimcha:**
- Package Manager → *Add package from git URL* → `https://github.com/googlevr/cardboard-xr-plugin.git`.
- Player → Resolution: Default Orientation = **Landscape Left**, `Optimized Frame Pacing` ✔.
- Birinchi ishga tushirishda QR kod (viewer profili) ruxsati — kamera ruxsati so'raladi.

## iOS

| Bo'lim | Sozlama |
|---|---|
| XR Plug-in Management (iOS tab) | **AR:** ✔ Apple ARKit · **VR:** ✔ Cardboard XR Plugin |
| Graphics API | **Metal** (iOS'da OpenGLES yo'q) |
| Target minimum iOS Version | **13.0+** (ARKit 5 / AR Foundation 5) |
| Architecture | **ARM64** |
| Camera Usage Description | `NSCameraUsageDescription` = "Laboratoriya stendini stolga joylashtirish uchun kamera kerak" — **majburiy**, bo'sh bo'lsa ilova yopiladi |
| Requires ARKit support | ✔ (Project Settings → XR → ARKit → Requirement = Required) |
| Bundle Identifier / Signing Team | Xcode'da avtomatik imzo |
| Xcode | `Unity-iPhone` → Signing & Capabilities → Team; Build Settings → `ENABLE_BITCODE = NO` |

## Ruxsatnomalarni runtime'da tekshirish

- Android: ARCore `ARSession.state` → `NeedsInstall` / `Unsupported` holatlarini UI'da ko'rsating (`ARSession.CheckAvailability()`).
- Kamera ruxsati rad etilsa: `UnityEngine.Android.Permission.HasUserAuthorizedPermission(Permission.Camera)` → qayta so'rash yoki sozlamalarga yo'naltirish.
- iOS: ruxsat rad etilsa tizim qayta so'ramaydi — foydalanuvchini `Settings` ga yo'naltiruvchi xabar chiqaring.

## Yakuniy tekshiruv

- [ ] Mid-range qurilmada (masalan, Snapdragon 7xx / A13) 60 FPS — `Development Build` + Profiler.
- [ ] Qisqa tutashuv ssenariysi: signal + qizil simlar + uchqun.
- [ ] 20+ sim ulash/uzish — FPS tushmaydi, GC Alloc = 0.
- [ ] AR: stol topilmaganda indikator yashirin; "Qayta joylashtirish" ishlaydi.
- [ ] VR: dwell 1.2 s, nigohni olib qochmasdan qayta tanlanmaydi.
