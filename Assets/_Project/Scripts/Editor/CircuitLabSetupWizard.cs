using System.IO;
using CircuitLab.Core;
using CircuitLab.Visual;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace CircuitLab.EditorTools
{
    /// <summary>
    /// Bir bosishda: "CircuitInteractable" layer, ComponentData aktivlari, materiallar,
    /// komponent prefablari (primitivlardan) va to'liq laboratoriya stendi prefabini yaratadi.
    /// Menyu: Tools → Circuit Lab → Build Starter Content.
    /// </summary>
    public static class CircuitLabSetupWizard
    {
        private const string Root = "Assets/_Project";
        private const string SoDir = Root + "/ScriptableObjects/Components";
        private const string PrefabDir = Root + "/Prefabs/Components";
        private const string WirePrefabDir = Root + "/Prefabs/Wires";
        private const string BoardPrefabPath = Root + "/Prefabs/CircuitLabBoard.prefab";
        private const string MaterialDir = Root + "/Materials";
        private const string LayerName = "CircuitInteractable";

        private static int s_layer = 8;

        [MenuItem("Tools/Circuit Lab/Build Starter Content", priority = 0)]
        public static void BuildAll()
        {
            EnsureFolders();
            s_layer = EnsureLayer(LayerName, 8);

            var battery = EnsureData("CD_Battery_4_5V", "Batareya 4.5 V", ComponentKind.Battery, 4.5f, 0.5f, 3f, 1f, false);
            var bulb = EnsureData("CD_Bulb_3_5V", "Lampochka 3.5 V", ComponentKind.Bulb, 0f, 10f, 0.6f, 1.5f, true);
            var resistor = EnsureData("CD_Resistor_10R", "Rezistor 10 Om", ComponentKind.Resistor, 0f, 10f, 1f, 1f, false);
            var sw = EnsureData("CD_Switch", "Kalit", ComponentKind.Switch, 0f, 0.001f, 10f, 1f, false);
            var ammeter = EnsureData("CD_Ammeter", "Ampermetr", ComponentKind.Ammeter, 0f, 0.01f, 5f, 1f, false);
            var voltmeter = EnsureData("CD_Voltmeter", "Voltmetr", ComponentKind.Voltmeter, 0f, 1e6f, 0.01f, 1f, false);

            Material body = EnsureLit("M_Body", new Color(0.18f, 0.2f, 0.24f), false);
            Material metal = EnsureLit("M_TerminalMetal", new Color(0.75f, 0.6f, 0.2f), false);
            Material glass = EnsureLit("M_BulbFilament", new Color(1f, 0.95f, 0.85f), true);
            Material board = EnsureLit("M_Board", new Color(0.55f, 0.42f, 0.3f), false);
            Material wire = EnsureWireMaterial();

            GameObject batteryPrefab = BuildComponentPrefab("P_Battery", battery, body, metal, new Vector3(0.09f, 0.05f, 0.05f), extra: null);
            GameObject bulbPrefab = BuildComponentPrefab("P_Bulb", bulb, body, metal, new Vector3(0.06f, 0.02f, 0.06f), go => AddBulb(go, glass));
            GameObject resistorPrefab = BuildComponentPrefab("P_Resistor", resistor, body, metal, new Vector3(0.08f, 0.02f, 0.03f), null);
            GameObject switchPrefab = BuildComponentPrefab("P_Switch", sw, body, metal, new Vector3(0.08f, 0.02f, 0.05f), go => AddSwitch(go, metal));
            GameObject ammeterPrefab = BuildComponentPrefab("P_Ammeter", ammeter, body, metal, new Vector3(0.08f, 0.04f, 0.06f), AddMeter);
            GameObject voltmeterPrefab = BuildComponentPrefab("P_Voltmeter", voltmeter, body, metal, new Vector3(0.08f, 0.04f, 0.06f), AddMeter);
            LineRenderer wirePrefab = BuildWirePrefab(wire);

            BuildBoardPrefab(board, wirePrefab, new[]
            {
                (batteryPrefab, new Vector3(-0.2f, 0f, 0.08f)),
                (switchPrefab, new Vector3(0f, 0f, 0.1f)),
                (bulbPrefab, new Vector3(0.2f, 0f, 0.08f)),
                (bulbPrefab, new Vector3(0.2f, 0f, -0.06f)),
                (resistorPrefab, new Vector3(0f, 0f, -0.1f)),
                (ammeterPrefab, new Vector3(-0.2f, 0f, -0.08f)),
                (voltmeterPrefab, new Vector3(0f, 0f, 0f)),
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CircuitLab] Starter content tayyor. Interaktiv layer: {LayerName} (#{s_layer}). " +
                      $"Kontrollerlardagi Interactable Mask shu layer'ga o'rnatilganini tekshiring. Stend: {BoardPrefabPath}");
        }

        #region Assets

        private static void EnsureFolders()
        {
            foreach (string dir in new[] { SoDir, PrefabDir, WirePrefabDir, MaterialDir })
            {
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            }
            AssetDatabase.Refresh();
        }

        private static int EnsureLayer(string layerName, int preferred)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return preferred;

            var tagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            for (int i = 8; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName) return i;

            int index = -1;
            if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(preferred).stringValue)) index = preferred;
            for (int i = 8; index < 0 && i < layers.arraySize; i++)
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) index = i;

            if (index < 0)
            {
                Debug.LogError("[CircuitLab] Bo'sh layer topilmadi.");
                return preferred;
            }

            layers.GetArrayElementAtIndex(index).stringValue = layerName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            return index;
        }

        private static ComponentDataSO EnsureData(string file, string display, ComponentKind kind, float voltage,
            float resistance, float maxCurrent, float ratedPower, bool breaks)
        {
            string path = $"{SoDir}/{file}.asset";
            var data = AssetDatabase.LoadAssetAtPath<ComponentDataSO>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<ComponentDataSO>();
                AssetDatabase.CreateAsset(data, path);
            }

            var so = new SerializedObject(data);
            so.FindProperty("displayName").stringValue = display;
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("voltage").floatValue = voltage;
            so.FindProperty("resistance").floatValue = resistance;
            so.FindProperty("maxCurrent").floatValue = maxCurrent;
            so.FindProperty("ratedPower").floatValue = ratedPower;
            so.FindProperty("breaksOnOverload").boolValue = breaks;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static Shader FindShader(string urpName, string fallback)
        {
            Shader s = Shader.Find(urpName);
            return s != null ? s : Shader.Find(fallback);
        }

        private static Material EnsureLit(string name, Color color, bool emissive)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(FindShader("Universal Render Pipeline/Lit", "Standard"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                mat.SetColor("_EmissionColor", Color.black);
            }
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material EnsureWireMaterial()
        {
            string texPath = $"{MaterialDir}/T_WireFlow.png";
            if (!File.Exists(texPath))
            {
                const int w = 32, h = 4;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                for (int x = 0; x < w; x++)
                {
                    // Arra tishi (sawtooth) — oqim yo'nalishi aniq ko'rinadi.
                    float v = 0.55f + 0.45f * (x / (float)(w - 1));
                    for (int y = 0; y < h; y++) tex.SetPixel(x, y, new Color(v, v, v, 1f));
                }
                File.WriteAllBytes(texPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(texPath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            string path = $"{MaterialDir}/M_Wire.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(FindShader("Universal Render Pipeline/Unlit", "Unlit/Texture"));
                AssetDatabase.CreateAsset(mat, path);
            }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            mat.SetTexture("_BaseMap", texture);
            mat.mainTexture = texture;
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        #endregion

        #region Prefabs

        private static GameObject BuildComponentPrefab(string name, ComponentDataSO data, Material body, Material metal,
            Vector3 size, System.Action<GameObject> extra)
        {
            var root = new GameObject(name);
            var component = root.AddComponent<CircuitComponent>();

            GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = "Body";
            mesh.transform.SetParent(root.transform, false);
            mesh.transform.localScale = size;
            mesh.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
            mesh.GetComponent<Renderer>().sharedMaterial = body;

            bool bodyInteractable = data.Kind == ComponentKind.Switch || data.Kind == ComponentKind.Bulb;
            if (bodyInteractable)
            {
                // Body collider'ini ildizga ko'chiramiz — ICircuitInteractable (CircuitComponent) shu yerda.
                Object.DestroyImmediate(mesh.GetComponent<Collider>());
                var box = root.AddComponent<BoxCollider>();
                box.size = size;
                box.center = mesh.transform.localPosition;
                root.layer = s_layer;
            }
            else
            {
                Object.DestroyImmediate(mesh.GetComponent<Collider>());
            }

            CircuitTerminal a = BuildTerminal(root.transform, "Terminal_A (+)", new Vector3(-size.x * 0.5f - 0.01f, size.y, 0f), metal, CircuitTerminal.Polarity.Positive);
            CircuitTerminal b = BuildTerminal(root.transform, "Terminal_B (-)", new Vector3(size.x * 0.5f + 0.01f, size.y, 0f), metal, CircuitTerminal.Polarity.Negative);

            var so = new SerializedObject(component);
            so.FindProperty("data").objectReferenceValue = data;
            so.FindProperty("terminalA").objectReferenceValue = a;
            so.FindProperty("terminalB").objectReferenceValue = b;
            so.ApplyModifiedPropertiesWithoutUndo();

            extra?.Invoke(root);

            string path = $"{PrefabDir}/{name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static CircuitTerminal BuildTerminal(Transform parent, string name, Vector3 localPos, Material metal, CircuitTerminal.Polarity polarity)
        {
            GameObject t = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            t.name = name;
            t.layer = s_layer;
            t.transform.SetParent(parent, false);
            t.transform.localPosition = localPos;
            t.transform.localScale = Vector3.one * 0.018f;
            t.GetComponent<Renderer>().sharedMaterial = metal;

            // Mobil ekranda barmoq bilan tegish oson bo'lishi uchun collider vizual sferadan kattaroq.
            t.GetComponent<SphereCollider>().radius = 1.0f;

            var terminal = t.AddComponent<CircuitTerminal>();
            var so = new SerializedObject(terminal);
            so.FindProperty("polarity").enumValueIndex = (int)polarity;
            so.ApplyModifiedPropertiesWithoutUndo();

            var highlighter = t.AddComponent<TerminalHighlighter>();
            var hso = new SerializedObject(highlighter);
            hso.FindProperty("targetRenderer").objectReferenceValue = t.GetComponent<Renderer>();
            hso.ApplyModifiedPropertiesWithoutUndo();
            return terminal;
        }

        private static void AddBulb(GameObject root, Material glass)
        {
            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "Glass_Filament";
            Object.DestroyImmediate(bulb.GetComponent<Collider>());
            bulb.transform.SetParent(root.transform, false);
            bulb.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            bulb.transform.localScale = Vector3.one * 0.045f;
            bulb.GetComponent<Renderer>().sharedMaterial = glass;

            var lightGo = new GameObject("PointLight");
            lightGo.transform.SetParent(bulb.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 0.5f;
            light.color = new Color(1f, 0.8f, 0.5f);
            light.shadows = LightShadows.None; // mobil: soyasiz
            light.intensity = 0f;
            light.enabled = false;

            var vis = root.AddComponent<BulbVisualizer>();
            var so = new SerializedObject(vis);
            so.FindProperty("filamentRenderer").objectReferenceValue = bulb.GetComponent<Renderer>();
            so.FindProperty("pointLight").objectReferenceValue = light;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddSwitch(GameObject root, Material metal)
        {
            var pivot = new GameObject("LeverPivot");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector3(-0.03f, 0.025f, 0f);

            GameObject lever = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lever.name = "Lever";
            Object.DestroyImmediate(lever.GetComponent<Collider>());
            lever.transform.SetParent(pivot.transform, false);
            lever.transform.localPosition = new Vector3(0.03f, 0f, 0f);
            lever.transform.localScale = new Vector3(0.06f, 0.006f, 0.012f);
            lever.GetComponent<Renderer>().sharedMaterial = metal;

            var vis = root.AddComponent<SwitchVisualizer>();
            var so = new SerializedObject(vis);
            so.FindProperty("lever").objectReferenceValue = pivot.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddMeter(GameObject root)
        {
            var textGo = new GameObject("Display");
            textGo.transform.SetParent(root.transform, false);
            textGo.transform.localPosition = new Vector3(0f, 0.041f, -0.031f);
            textGo.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);

            var tmp = textGo.AddComponent<TextMeshPro>();
            tmp.fontSize = 0.25f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(0.075f, 0.025f);
            tmp.text = "0.00";
            tmp.color = new Color(0.3f, 1f, 0.45f);

            var display = root.AddComponent<MeterDisplay>();
            var so = new SerializedObject(display);
            so.FindProperty("label").objectReferenceValue = tmp;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static LineRenderer BuildWirePrefab(Material wire)
        {
            var go = new GameObject("P_Wire");
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = wire;
            line.textureMode = LineTextureMode.Tile;
            line.alignment = LineAlignment.View;
            line.numCapVertices = 2;
            line.numCornerVertices = 0;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.widthMultiplier = 0.006f;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{WirePrefabDir}/P_Wire.prefab");
            Object.DestroyImmediate(go);
            return prefab.GetComponent<LineRenderer>();
        }

        private static void BuildBoardPrefab(Material boardMat, LineRenderer wirePrefab, (GameObject prefab, Vector3 pos)[] layout)
        {
            var root = new GameObject("CircuitLabBoard");
            var board = root.AddComponent<CircuitBoard>();
            root.AddComponent<CircuitWiringController>();

            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "BoardSurface";
            Object.DestroyImmediate(table.GetComponent<Collider>());
            table.transform.SetParent(root.transform, false);
            table.transform.localScale = new Vector3(0.6f, 0.01f, 0.36f);
            table.transform.localPosition = new Vector3(0f, -0.005f, 0f);
            table.GetComponent<Renderer>().sharedMaterial = boardMat;

            var components = new GameObject("Components");
            components.transform.SetParent(root.transform, false);
            foreach (var (prefab, pos) in layout)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, components.transform);
                instance.transform.localPosition = pos;
            }

            var wires = new GameObject("Wires");
            wires.transform.SetParent(root.transform, false);
            var visualizer = wires.AddComponent<WireVisualizer>();
            var vso = new SerializedObject(visualizer);
            vso.FindProperty("board").objectReferenceValue = board;
            vso.FindProperty("wirePrefab").objectReferenceValue = wirePrefab;
            vso.ApplyModifiedPropertiesWithoutUndo();

            var feedback = new GameObject("ShortCircuitFeedback");
            feedback.transform.SetParent(root.transform, false);
            var sc = feedback.AddComponent<ShortCircuitFeedback>();
            var sso = new SerializedObject(sc);
            sso.FindProperty("board").objectReferenceValue = board;
            sso.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, BoardPrefabPath);
            Object.DestroyImmediate(root);
        }

        #endregion
    }
}
