#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Siberian.EditorTools
{
    /// <summary>
    /// Общие примитивы для процедурных блокаутов (тот же подход, что SiberianBlockoutBuilder.cs).
    /// Цветовое кодирование — по LDD §5: серый ландшафт, тёмно-серый скалы, белый/голубой лёд,
    /// коричневый постройки/деревья, оранжевый интерактив, жёлтый цели-предметы,
    /// оранжевый угроза-волк (красный освобождён под маяк — см. HudPalette).
    /// </summary>
    public static class BlockoutKit
    {
        // Палитра LDD §5 + правка red overload (волк теперь оранжевый).
        public static readonly Color Ground = new Color(0.55f, 0.55f, 0.58f);
        public static readonly Color Rock = new Color(0.30f, 0.30f, 0.33f);
        public static readonly Color IceSafe = new Color(0.85f, 0.92f, 1.00f, 0.85f);
        public static readonly Color IceThin = new Color(0.35f, 0.50f, 0.70f, 0.9f);
        public static readonly Color Water = new Color(0.10f, 0.25f, 0.45f, 0.8f);
        public static readonly Color Wood = new Color(0.45f, 0.30f, 0.18f);
        public static readonly Color Interact = new Color(1.00f, 0.60f, 0.10f);
        public static readonly Color Objective = new Color(1.00f, 0.85f, 0.10f);
        public static readonly Color Threat = new Color(0.85f, 0.45f, 0.15f);
        public static readonly Color Beacon = new Color(1.00f, 0.15f, 0.10f);
        public static readonly Color TorchLight = new Color(1.00f, 0.75f, 0.35f);

        public static GameObject NewRoot(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog("Siberian Blockout",
                        $"Объект '{name}' уже есть в сцене. Удалить и построить заново?", "Пересобрать", "Отмена"))
                    return null;
                Undo.DestroyObjectImmediate(existing);
            }
            var root = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(root, "Build " + name);
            return root;
        }

        public static GameObject Group(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        public static GameObject Box(GameObject parent, string name, Vector3 center, Vector3 size, Color color)
            => Primitive(parent, PrimitiveType.Cube, name, center, size, color);

        public static GameObject Cylinder(GameObject parent, string name, Vector3 center, float radius, float height, Color color)
            => Primitive(parent, PrimitiveType.Cylinder, name, center, new Vector3(radius * 2f, height / 2f, radius * 2f), color);

        public static GameObject Sphere(GameObject parent, string name, Vector3 center, float diameter, Color color)
            => Primitive(parent, PrimitiveType.Sphere, name, center, Vector3.one * diameter, color);

        public static GameObject Capsule(GameObject parent, string name, Vector3 center, Color color)
            => Primitive(parent, PrimitiveType.Capsule, name, center, Vector3.one, color);

        public static GameObject Primitive(GameObject parent, PrimitiveType type, string name,
            Vector3 center, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = center;
            go.transform.localScale = scale;
            Paint(go, color);
            return go;
        }

        /// <summary>Коридор: пол + 2 стены + потолок. axisZ=true — вдоль Z, иначе вдоль X.</summary>
        public static void Corridor(GameObject parent, string name, Vector3 center,
            float length, float width, float height, bool axisZ, Color color, bool ceiling = true)
        {
            var g = Group(parent, name);
            g.transform.localPosition = center;
            float t = 0.3f; // толщина плит

            Vector3 floorSize = axisZ ? new Vector3(width, t, length) : new Vector3(length, t, width);
            Box(g, "Floor", new Vector3(0, -t / 2f, 0), floorSize, color);
            if (ceiling)
                Box(g, "Ceiling", new Vector3(0, height + t / 2f, 0), floorSize, Rock);

            Vector3 wallSize = axisZ ? new Vector3(t, height, length) : new Vector3(length, height, t);
            Vector3 offset = axisZ ? new Vector3(width / 2f + t / 2f, height / 2f, 0) : new Vector3(0, height / 2f, width / 2f + t / 2f);
            Box(g, "WallA", offset, wallSize, Rock);
            Box(g, "WallB", -Vector3.Scale(offset, new Vector3(1, -1, 1)), wallSize, Rock);
        }

        public static void Paint(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            // URP/Lit использует _BaseColor, Standard — _Color; ставим оба.
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.color = color;
            renderer.sharedMaterial = mat;
        }

        public static void PointLight(GameObject parent, string name, Vector3 pos, Color color, float range, float intensity)
        {
            var go = Group(parent, name);
            go.transform.localPosition = pos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
        }
    }
}
#endif
