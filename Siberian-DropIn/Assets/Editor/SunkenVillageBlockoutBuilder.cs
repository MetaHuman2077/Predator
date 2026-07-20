#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Siberian.EditorTools
{
    /// <summary>
    /// Блокаут уровня 3 «Затонувший посёлок» по концепт-доку «Новые уровни v0.1».
    /// Столпы: лёд — основное пространство (не полоса, а половина карты), плотная застройка
    /// для обыска (POI ~1 на 25-30м против 40-80м в Siberian), стая волков как давление.
    /// Прогрессия риска: доля тонкого льда 30% у берега → 60% к центру озера;
    /// риск растёт К финалу (обратная декомпрессия — долгий переход по льду к маяку).
    ///
    /// Структура: Причал (старт) → Улица домов (сжатие/обыск) → Церковь-склад (хаб)
    /// → Открытый лёд озера (риск-пространство) → Ледовая стоянка (стая) → Дальний берег + маяк (финал).
    ///
    /// Запуск: меню Siberian/Blockout/Build Sunken Village (Level 3). Детеминированно, seed 2077.
    /// </summary>
    public static class SunkenVillageBlockoutBuilder
    {
        const float PlateSize = 6f; // шаг сетки ледяных плит

        [MenuItem("Siberian/Blockout/Build Sunken Village (Level 3)")]
        public static void Build()
        {
            var root = BlockoutKit.NewRoot("SunkenVillage_Blockout");
            if (root == null) return;

            BuildShores(root);
            BuildPier(root);
            BuildVillageStreet(root);
            BuildChurchHub(root);
            BuildLakeIce(root);
            BuildWolfPackCamp(root);
            BuildLighthouseFinale(root);

            Debug.Log("[Siberian] Блокаут «Затонувший посёлок» построен (seed 2077). " +
                      "Тонкие плиты — тёмно-синие; доля растёт 30%→60% к центру озера. " +
                      "Стая: 3 капсулы-волка на ледовой стоянке (требует group behaviors — Roadmap, ранее отложено).");
        }

        // Южный берег (посёлок) и северный берег (маяк); между ними — озеро.
        static void BuildShores(GameObject root)
        {
            var g = BlockoutKit.Group(root, "Shores");
            BlockoutKit.Box(g, "ShoreSouth", new Vector3(0, -0.3f, -160), new Vector3(400, 0.6f, 80), BlockoutKit.Ground);
            BlockoutKit.Box(g, "ShoreNorth", new Vector3(0, -0.3f, 180), new Vector3(400, 0.6f, 40), BlockoutKit.Ground);
            // Скальные стены-границы по периметру (как в Siberian, ±202м).
            BlockoutKit.Box(g, "WallW", new Vector3(-202, 6, 0), new Vector3(4, 12, 404), BlockoutKit.Rock);
            BlockoutKit.Box(g, "WallE", new Vector3(202, 6, 0), new Vector3(4, 12, 404), BlockoutKit.Rock);
            BlockoutKit.Box(g, "WallS", new Vector3(0, 6, -202), new Vector3(404, 12, 4), BlockoutKit.Rock);
            BlockoutKit.Box(g, "WallN", new Vector3(0, 6, 202), new Vector3(404, 12, 4), BlockoutKit.Rock);
        }

        // Старт: причал, разбитая лодка, первый костёр; вид на посёлок и маяк (лендмарк).
        static void BuildPier(GameObject root)
        {
            var g = BlockoutKit.Group(root, "S-A_Pier");
            g.transform.localPosition = new Vector3(0, 0, -128);
            BlockoutKit.Box(g, "PierDeck", new Vector3(0, 0.4f, 4), new Vector3(4, 0.3f, 16), BlockoutKit.Wood);
            for (int i = 0; i < 4; i++)
                BlockoutKit.Cylinder(g, $"Pile_{i}", new Vector3(i % 2 == 0 ? -1.6f : 1.6f, 0f, i * 4f - 2f), 0.25f, 1.6f, BlockoutKit.Wood);
            var boat = BlockoutKit.Box(g, "BrokenBoat", new Vector3(6, 0.3f, 8), new Vector3(2.2f, 1.0f, 5.5f), BlockoutKit.Wood);
            boat.transform.localRotation = Quaternion.Euler(0, 25f, 12f);
            BlockoutKit.Cylinder(g, "StartCampfire", new Vector3(-4, 0.25f, -2), 0.8f, 0.5f, BlockoutKit.Interact);
            BlockoutKit.PointLight(g, "CampfireLight", new Vector3(-4, 1f, -2), BlockoutKit.TorchLight, 8f, 1.6f);
        }

        // Улица: 8 домов, шаг ~27м (плотность по Кадикову 25-30м); у двух — просевшие крыши (риск обыска).
        static void BuildVillageStreet(GameObject root)
        {
            var g = BlockoutKit.Group(root, "S-B_VillageStreet");
            var rng = new System.Random(2077);
            for (int i = 0; i < 8; i++)
            {
                float z = -150f + i * 27f + rng.Next(-3, 4);
                float x = (i % 2 == 0 ? -18f : 18f) + rng.Next(-4, 5);
                var house = BlockoutKit.Group(g, $"House_{i + 1}");
                house.transform.localPosition = new Vector3(x, 0, z);
                BlockoutKit.Box(house, "Walls", new Vector3(0, 2.0f, 0), new Vector3(8, 4f, 6), BlockoutKit.Wood);
                // Дверной проём 1×2.1 (метрики LDD §1) — вырез обозначаем контрастной плитой.
                BlockoutKit.Box(house, "Doorway", new Vector3(0, 1.05f, 3.05f), new Vector3(1.0f, 2.1f, 0.2f), BlockoutKit.Ground);

                bool collapsed = i == 2 || i == 5; // просевшие крыши: телеграфированный риск обыска
                var roof = BlockoutKit.Box(house, collapsed ? "Roof_COLLAPSED" : "Roof",
                    new Vector3(0, collapsed ? 3.6f : 4.6f, 0), new Vector3(9, 1.2f, 7), collapsed ? BlockoutKit.Rock : BlockoutKit.Wood);
                if (collapsed) roof.transform.localRotation = Quaternion.Euler(0, 0, 10f);

                // Добыча в каждом доме (оранжевый интерактив) — обыск вместо пустого леса.
                BlockoutKit.Box(house, "Loot", new Vector3(rng.Next(-2, 3), 0.3f, rng.Next(-2, 2)), new Vector3(0.6f, 0.6f, 0.6f), BlockoutKit.Interact);
            }
        }

        // Хаб: церковь/склад — центральное укрытие, самое тёплое место уровня.
        static void BuildChurchHub(GameObject root)
        {
            var g = BlockoutKit.Group(root, "S-C_ChurchHub");
            g.transform.localPosition = new Vector3(0, 0, -50);
            BlockoutKit.Box(g, "Hall", new Vector3(0, 3.5f, 0), new Vector3(12, 7f, 10), BlockoutKit.Wood);
            BlockoutKit.Box(g, "Doorway", new Vector3(0, 1.05f, 5.05f), new Vector3(1.2f, 2.1f, 0.2f), BlockoutKit.Ground);
            BlockoutKit.Cylinder(g, "BellTower", new Vector3(0, 9f, -3), 1.5f, 6f, BlockoutKit.Wood);
            BlockoutKit.Cylinder(g, "Stove_WarmZone", new Vector3(-4, 0.8f, -3), 0.6f, 1.6f, BlockoutKit.Interact);
            BlockoutKit.PointLight(g, "StoveLight", new Vector3(-4, 1.8f, -3), BlockoutKit.TorchLight, 10f, 1.8f);
            BlockoutKit.Box(g, "SupplyCrates", new Vector3(4, 0.6f, -3.5f), new Vector3(2.4f, 1.2f, 1.6f), BlockoutKit.Interact);
        }

        // Озеро: сетка плит 6×6м; тонкие (тёмно-синие) — 30% у берега → 60% в центре. Полыньи и острова-укрытия.
        static void BuildLakeIce(GameObject root)
        {
            var g = BlockoutKit.Group(root, "S-D_LakeIce");
            const float zMin = -20f, zMax = 152f, xMin = -100f, xMax = 100f;
            float zCenter = (zMin + zMax) / 2f;

            for (float x = xMin; x < xMax; x += PlateSize)
            {
                for (float z = zMin; z < zMax; z += PlateSize)
                {
                    // Детерминированный хэш плиты (тот же приём, что ~30% тонких плит в SiberianBlockoutBuilder).
                    float h = Mathf.Abs(Mathf.Sin(x * 127.1f + z * 311.7f) * 43758.5453f);
                    h -= Mathf.Floor(h);

                    // Прогрессия риска: 30% тонких у берегов → 60% в центре озера.
                    float centerness = 1f - Mathf.Clamp01(Mathf.Abs(z - zCenter) / ((zMax - zMin) / 2f));
                    float thinChance = Mathf.Lerp(0.30f, 0.60f, centerness);
                    bool thin = h < thinChance;

                    BlockoutKit.Box(g, thin ? "IcePlate_THIN" : "IcePlate",
                        new Vector3(x + PlateSize / 2f, -0.1f, z + PlateSize / 2f),
                        new Vector3(PlateSize - 0.1f, 0.2f, PlateSize - 0.1f),
                        thin ? BlockoutKit.IceThin : BlockoutKit.IceSafe);
                }
            }

            // Полыньи — открытая вода, мгновенная угроза (читаются издалека тёмным пятном).
            var rng = new System.Random(2077);
            for (int i = 0; i < 5; i++)
                BlockoutKit.Box(g, $"Polynya_{i}", new Vector3(rng.Next(-80, 81), -0.15f, rng.Next(10, 140)),
                    new Vector3(7, 0.1f, 7), BlockoutKit.Water);

            // Острова-укрытия с валежником: точки передышки на большом льду (prospect-refuge, Totten).
            int[] ix = { -60, 10, 70 };
            int[] iz = { 40, 90, 55 };
            for (int i = 0; i < 3; i++)
            {
                var island = BlockoutKit.Group(g, $"RefugeIsland_{i + 1}");
                island.transform.localPosition = new Vector3(ix[i], 0, iz[i]);
                BlockoutKit.Box(island, "Ground", new Vector3(0, 0.2f, 0), new Vector3(10, 0.6f, 8), BlockoutKit.Ground);
                BlockoutKit.Box(island, "Deadwood", new Vector3(2, 0.7f, 1), new Vector3(1.8f, 0.5f, 0.6f), BlockoutKit.Wood);
                BlockoutKit.Cylinder(island, "FirRing", new Vector3(-2, 0.4f, -1), 0.7f, 0.3f, BlockoutKit.Interact);
            }
        }

        // Угроза: ледовая стоянка — стая (3 волка), открытое пространство без укрытий.
        static void BuildWolfPackCamp(GameObject root)
        {
            var g = BlockoutKit.Group(root, "S-E_WolfPack");
            g.transform.localPosition = new Vector3(55, 0, 75);
            Vector3[] wolves = { new Vector3(0, 1f, 0), new Vector3(6, 1f, 4), new Vector3(-4, 1f, 6) };
            for (int i = 0; i < wolves.Length; i++)
                BlockoutKit.Capsule(g, $"Wolf_{i + 1}", wolves[i], BlockoutKit.Threat); // оранжевый — красный освобождён (red overload)
            BlockoutKit.Box(g, "Carcass_Lore", new Vector3(2, 0.2f, 2), new Vector3(2.4f, 0.4f, 1.0f), BlockoutKit.Wood);
        }

        // Финал: дальний берег + маяк. Риск растёт к финалу — последний отрезок через центр озера (60% тонкого льда).
        static void BuildLighthouseFinale(GameObject root)
        {
            var g = BlockoutKit.Group(root, "S-F_Lighthouse");
            g.transform.localPosition = new Vector3(0, 0, 180);
            BlockoutKit.Cylinder(g, "Tower", new Vector3(0, 15f, 0), 3f, 30f, BlockoutKit.Ground);
            BlockoutKit.Sphere(g, "Beacon", new Vector3(0, 31.5f, 0), 3f, BlockoutKit.Beacon); // красный = только цель (HudPalette)
            BlockoutKit.PointLight(g, "BeaconLight", new Vector3(0, 31.5f, 0), BlockoutKit.Beacon, 60f, 3f);
            BlockoutKit.Box(g, "KeeperShed", new Vector3(8, 1.5f, -2), new Vector3(5, 3f, 4), BlockoutKit.Wood);
            BlockoutKit.Cylinder(g, "LastCampfire", new Vector3(-6, 0.25f, -3), 0.8f, 0.5f, BlockoutKit.Interact);
        }
    }
}
#endif
