#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Siberian.EditorTools
{
    /// <summary>
    /// Блокаут уровня 2 «Штольня» по концепт-доку «Новые уровни v0.1».
    /// Столпы: свет — ресурс (редкие факелы, дальность фонаря 6-8м), вертикальность как риск,
    /// звук как навигация (метки-источники: капель у воды, гул у ствола).
    /// Метрики: коридоры 2-2.5м, потолки 2.2-2.8м (Osorio); уклоны пандусов &lt; slopeLimit 45°.
    ///
    /// Структура (вертикальный аналог зон A-F Siberian):
    ///   M-A Вход/обвал (y0)  → M-B Штрек-развилка (y-4) → M-C Лагерь шахтёров, хаб (y-4)
    ///   → выбор: M-D Затопленный горизонт, брод-шорткат (y-8)  ИЛИ  M-E Обвальная галерея (y-6)
    ///   → M-F Шахтный ствол + подъёмник (финал): 3 детали лебёдки (аналог канистр).
    ///
    /// Запуск: меню Siberian/Blockout/Build Shtolnya (Level 2). Повторный запуск пересобирает.
    /// </summary>
    public static class ShtolnyaBlockoutBuilder
    {
        [MenuItem("Siberian/Blockout/Build Shtolnya (Level 2)")]
        public static void Build()
        {
            var root = BlockoutKit.NewRoot("Shtolnya_Blockout");
            if (root == null) return;

            BuildEntrance(root);
            BuildFork(root);
            BuildCamp(root);
            BuildFloodedHorizon(root);
            BuildCollapseGallery(root);
            BuildShaft(root);

            Debug.Log("[Siberian] Блокаут «Штольня» построен. Детали лебёдки: лагерь / затопленный горизонт / обвальная галерея. " +
                      "Свет намеренно редкий — фонарь игрока (FlashlightResource) добавляется отдельной системой.");
        }

        // M-A: вход — единственный путь вниз, основной ход завален (обучение: назад дороги нет).
        static void BuildEntrance(GameObject root)
        {
            var zone = BlockoutKit.Group(root, "M-A_Entrance");
            BlockoutKit.Box(zone, "Floor", new Vector3(0, -0.15f, 0), new Vector3(8, 0.3f, 8), BlockoutKit.Ground);
            BlockoutKit.Box(zone, "Ceiling", new Vector3(0, 3.1f, 0), new Vector3(8, 0.3f, 8), BlockoutKit.Rock);
            BlockoutKit.Box(zone, "WallW", new Vector3(-4.15f, 1.5f, 0), new Vector3(0.3f, 3, 8), BlockoutKit.Rock);
            BlockoutKit.Box(zone, "WallE", new Vector3(4.15f, 1.5f, 0), new Vector3(0.3f, 3, 8), BlockoutKit.Rock);

            // Заваленный основной ход: груда камней в северной стене.
            BlockoutKit.Box(zone, "CollapsedGate", new Vector3(0, 1.2f, -3.9f), new Vector3(6, 2.4f, 1.2f), BlockoutKit.Rock);
            BlockoutKit.Box(zone, "Rubble1", new Vector3(-1.2f, 0.4f, -3.0f), new Vector3(1.6f, 0.8f, 1.2f), BlockoutKit.Rock);
            BlockoutKit.Box(zone, "Rubble2", new Vector3(1.0f, 0.3f, -3.2f), new Vector3(1.2f, 0.6f, 1.0f), BlockoutKit.Rock);

            // Первый факел — обучение «свет = ресурс» (единственный источник у входа).
            BlockoutKit.Sphere(zone, "Torch_Tutorial", new Vector3(3.4f, 1.8f, 2.0f), 0.3f, BlockoutKit.Interact);
            BlockoutKit.PointLight(zone, "TorchLight", new Vector3(3.4f, 1.9f, 2.0f), BlockoutKit.TorchLight, 7f, 1.4f);

            // Пандус вниз к развилке: 12м хода, 4м спуска (~18° < slopeLimit 45°).
            var ramp = BlockoutKit.Box(zone, "RampDown", new Vector3(0, -2f, 10f), new Vector3(2.5f, 0.3f, 12.8f), BlockoutKit.Ground);
            ramp.transform.localRotation = Quaternion.Euler(-18.4f, 0, 0);
        }

        // M-B: развилка — сжатие до 2м (против 3-5м на поверхности Siberian).
        static void BuildFork(GameObject root)
        {
            var zone = BlockoutKit.Group(root, "M-B_Fork");
            zone.transform.localPosition = new Vector3(0, -4f, 20f);
            BlockoutKit.Box(zone, "Floor", new Vector3(0, -0.15f, 0), new Vector3(6, 0.3f, 6), BlockoutKit.Ground);
            BlockoutKit.Box(zone, "Ceiling", new Vector3(0, 2.65f, 0), new Vector3(6, 0.3f, 6), BlockoutKit.Rock);
            BlockoutKit.Sphere(zone, "Torch_Fork", new Vector3(0, 1.9f, 2.6f), 0.3f, BlockoutKit.Interact);
            BlockoutKit.PointLight(zone, "TorchLight", new Vector3(0, 2.0f, 2.6f), BlockoutKit.TorchLight, 6f, 1.1f);

            // Западный штрек → лагерь (2.2м), северный штрек → затопленный горизонт (2.0м).
            BlockoutKit.Corridor(root, "Drift_West_ToCamp", new Vector3(-12, -4, 20), 18f, 2.2f, 2.5f, axisZ: false, BlockoutKit.Ground);
            BlockoutKit.Corridor(root, "Drift_North_ToFlood", new Vector3(0, -4, 32), 18f, 2.0f, 2.4f, axisZ: true, BlockoutKit.Ground);
        }

        // M-C: лагерь шахтёров — хаб (аналог избы): буржуйка-тепло, верстак, записки.
        static void BuildCamp(GameObject root)
        {
            var zone = BlockoutKit.Group(root, "M-C_MinersCamp");
            zone.transform.localPosition = new Vector3(-26, -4f, 20f);
            BlockoutKit.Box(zone, "Floor", new Vector3(0, -0.15f, 0), new Vector3(10, 0.3f, 8), BlockoutKit.Ground);
            BlockoutKit.Box(zone, "Ceiling", new Vector3(0, 2.95f, 0), new Vector3(10, 0.3f, 8), BlockoutKit.Rock);

            BlockoutKit.Cylinder(zone, "Stove_WarmZone", new Vector3(-3.5f, 0.8f, -2.5f), 0.5f, 1.6f, BlockoutKit.Interact);
            BlockoutKit.PointLight(zone, "StoveLight", new Vector3(-3.5f, 1.6f, -2.5f), BlockoutKit.TorchLight, 8f, 1.6f);
            BlockoutKit.Box(zone, "Workbench", new Vector3(3.0f, 0.5f, -2.8f), new Vector3(2.2f, 1.0f, 1.0f), BlockoutKit.Wood);
            BlockoutKit.Box(zone, "Bunks", new Vector3(3.2f, 0.4f, 2.6f), new Vector3(2.0f, 0.8f, 3.0f), BlockoutKit.Wood);
            BlockoutKit.Box(zone, "Note_Lore", new Vector3(3.0f, 1.15f, -2.8f), new Vector3(0.3f, 0.05f, 0.4f), BlockoutKit.Interact);

            // Деталь лебёдки №1 — в хабе, «бесплатная» (обучение цели).
            BlockoutKit.Sphere(zone, "WinchPart_1", new Vector3(-3.0f, 0.3f, 2.8f), 0.5f, BlockoutKit.Objective);

            // Штрек из лагеря на север → обвальная галерея.
            BlockoutKit.Corridor(root, "Drift_Camp_ToCollapse", new Vector3(-26, -4, 32), 16f, 2.5f, 2.6f, axisZ: true, BlockoutKit.Ground);
        }

        // M-D: затопленный горизонт — риск-шорткат (вода вместо льда): брод срезает путь к стволу.
        static void BuildFloodedHorizon(GameObject root)
        {
            var zone = BlockoutKit.Group(root, "M-D_FloodedHorizon");
            zone.transform.localPosition = new Vector3(0, -8f, 46f);

            // Пандус вниз из северного штрека (спуск 4м на 10м хода, ~22°).
            var ramp = BlockoutKit.Box(root, "Ramp_ToFlood", new Vector3(0, -6f, 42f), new Vector3(2.0f, 0.3f, 10.8f), BlockoutKit.Ground);
            ramp.transform.localRotation = Quaternion.Euler(-21.8f, 0, 0);

            BlockoutKit.Box(zone, "Floor", new Vector3(0, -0.15f, 4), new Vector3(14, 0.3f, 12), BlockoutKit.Ground);
            BlockoutKit.Box(zone, "Ceiling", new Vector3(0, 2.75f, 4), new Vector3(14, 0.3f, 12), BlockoutKit.Rock);

            // Вода: затопленная чаша; брод — притопленные плиты (медленный проход, риск).
            BlockoutKit.Box(zone, "Water", new Vector3(0, 0.25f, 4), new Vector3(11, 0.5f, 8), BlockoutKit.Water);
            for (int i = 0; i < 4; i++)
                BlockoutKit.Box(zone, $"FordStone_{i}", new Vector3(-3.6f + i * 2.4f, 0.45f, 4f + (i % 2 == 0 ? -0.6f : 0.6f)),
                    new Vector3(1.2f, 0.35f, 1.2f), BlockoutKit.Ground);

            // Звук-навигация: маркер капели (аудио-лендмарк по Кадикову).
            BlockoutKit.Sphere(zone, "AudioMarker_Dripping", new Vector3(0, 2.4f, 4), 0.25f, BlockoutKit.Water);

            // Деталь лебёдки №2 — за бродом (награда за риск).
            BlockoutKit.Sphere(zone, "WinchPart_2", new Vector3(5.4f, 0.4f, 7.5f), 0.5f, BlockoutKit.Objective);

            // Короткий выход к стволу (шорткат оправдывает риск воды).
            BlockoutKit.Corridor(root, "Drift_Flood_ToShaft", new Vector3(-6, -8, 56), 12f, 2.0f, 2.3f, axisZ: false, BlockoutKit.Ground);
        }

        // M-E: обвальная галерея — угроза вместо волка: нестабильный потолок, скрипы = предупреждение.
        static void BuildCollapseGallery(GameObject root)
        {
            var zone = BlockoutKit.Group(root, "M-E_CollapseGallery");
            zone.transform.localPosition = new Vector3(-26, -6f, 48f);

            var ramp = BlockoutKit.Box(root, "Ramp_ToCollapse", new Vector3(-26, -5f, 42f), new Vector3(2.5f, 0.3f, 5.4f), BlockoutKit.Ground);
            ramp.transform.localRotation = Quaternion.Euler(-21.8f, 0, 0);

            BlockoutKit.Box(zone, "Floor", new Vector3(0, -0.15f, 4), new Vector3(8, 0.3f, 16), BlockoutKit.Ground);

            // Нависшие нестабильные блоки: проход требует медленного маневрирования между ними.
            for (int i = 0; i < 5; i++)
            {
                float z = -2f + i * 3f;
                float x = (i % 2 == 0) ? -1.6f : 1.6f;
                var block = BlockoutKit.Box(zone, $"UnstableCeiling_{i}", new Vector3(x, 2.0f, z), new Vector3(3.2f, 0.9f, 2.0f), BlockoutKit.Rock);
                block.transform.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0) ? 6f : -6f); // видимый «крен» = читаемая угроза
            }
            // Осыпь на полу — телеграфирует опасность до входа (честная обратная связь, GDD v0.2 §6).
            BlockoutKit.Box(zone, "DebrisWarning", new Vector3(0, 0.2f, -4.5f), new Vector3(3f, 0.4f, 1.2f), BlockoutKit.Rock);

            // Деталь лебёдки №3 — в глубине галереи.
            BlockoutKit.Sphere(zone, "WinchPart_3", new Vector3(1.8f, 0.4f, 10.5f), 0.5f, BlockoutKit.Objective);

            // Выход к стволу.
            BlockoutKit.Corridor(root, "Drift_Collapse_ToShaft", new Vector3(-20, -6, 58), 10f, 2.2f, 2.4f, axisZ: false, BlockoutKit.Ground);
        }

        // M-F: шахтный ствол — вертикальный финал: лебёдка (3 детали) поднимает клеть на поверхность.
        static void BuildShaft(GameObject root)
        {
            var zone = BlockoutKit.Group(root, "M-F_Shaft");
            zone.transform.localPosition = new Vector3(-12, -8f, 60f);

            BlockoutKit.Box(zone, "Floor", new Vector3(0, -0.15f, 0), new Vector3(6, 0.3f, 6), BlockoutKit.Ground);
            // Ствол уходит вверх до поверхности (+8м у M-D-горизонта → открытое небо).
            BlockoutKit.Box(zone, "ShaftWallN", new Vector3(0, 10f, -3.15f), new Vector3(6, 20f, 0.3f), BlockoutKit.Rock);
            BlockoutKit.Box(zone, "ShaftWallS", new Vector3(0, 10f, 3.15f), new Vector3(6, 20f, 0.3f), BlockoutKit.Rock);
            BlockoutKit.Box(zone, "ShaftWallW", new Vector3(-3.15f, 10f, 0), new Vector3(0.3f, 20f, 6), BlockoutKit.Rock);
            BlockoutKit.Box(zone, "ShaftWallE", new Vector3(3.15f, 10f, 0), new Vector3(0.3f, 20f, 6), BlockoutKit.Rock);

            BlockoutKit.Box(zone, "LiftCage", new Vector3(0, 0.6f, 0), new Vector3(2.4f, 1.2f, 2.4f), BlockoutKit.Wood);
            BlockoutKit.Box(zone, "WinchMachine_Goal", new Vector3(2.2f, 0.7f, -2.2f), new Vector3(1.4f, 1.4f, 1.4f), BlockoutKit.Interact);

            // Гул подъёмника — звуковой маяк финала (слышен из обеих галерей).
            BlockoutKit.Sphere(zone, "AudioMarker_Hum", new Vector3(0, 3f, 0), 0.3f, BlockoutKit.Objective);
            // Дневной свет сверху — единственный «бесплатный» свет уровня, виден снизу как цель.
            BlockoutKit.PointLight(zone, "DaylightShaft", new Vector3(0, 19f, 0), Color.white, 14f, 2.2f);
        }
    }
}
#endif
