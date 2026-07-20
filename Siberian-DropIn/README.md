# Siberian — Drop-In пакет улучшений прототипа

**Это НЕ часть Unreal-проекта Predator.** Пакет подготовлен в облачной сессии, где доступен
только этот репозиторий; Unity-проект Siberian (`D:\Unity\Unity Projects\Siberian`) существует
только локально, поэтому код доставлен через эту ветку. Папка `Assets/` зеркалит структуру
Unity-проекта — содержимое копируется поверх него.

## Установка (на машине с Unity-проектом)

```powershell
cd "D:\Unity\Unity Projects\Siberian"
git clone -b claude/siberian-prototype-improvements-2sn9ab https://github.com/MetaHuman2077/Predator.git %TEMP%\siberian-dropin
xcopy /E /I /Y "%TEMP%\siberian-dropin\Siberian-DropIn\Assets" "Assets"
powershell -ExecutionPolicy Bypass -File "%TEMP%\siberian-dropin\Siberian-DropIn\Tools\fix_autoplaytester.ps1"
```

Затем добавить пакет MCP в `Packages/manifest.json` — см. `Tools/manifest-snippet.jsonc`.

## Состав пакета — по задачам

| Задача | Файл | Статус |
| --- | --- | --- |
| 1. Unity MCP-мост | `Assets/Editor/McpAutoStartBootstrap.cs` + `Tools/manifest-snippet.jsonc` | Код готов; проверка — только в редакторе |
| 2. Фикс компиляции | `Tools/fix_autoplaytester.ps1` (атрибут `[DefaultExecutionTag]`, `realtimeSinceSeconds`→`realtimeSinceStartup`) | Скрипт готов; прогнать локально |
| 3–4. Плейтесты 3+5 | `PLAYTEST_PLAN.md` — протокол и телеметрия | **Не выполнено: требует Editor** |
| 6.1 Pursuit-AI волка | `Assets/_Project/Scripts/AI/WolfPursuitTargeting.cs` | Компилируется автономно; интеграция — 2 строки в WolfAI.ChaseTick |
| 6.2 Честный первый провал | `Assets/_Project/Scripts/Survival/IceFallGrace.cs` | Интеграция — 1 строка в ThinIce + 1 в респавн |
| 6.3 Звук холода | `Assets/_Project/Scripts/Survival/ColdAudioFeedback.cs` | Подписка на OnStatsChanged — 1 строка; клипы назначить в инспекторе |
| 6.4 Развод цветов волк/тепло | `Assets/_Project/Scripts/UI/HudPalette.cs` | Точечные замены в GameHUD.DrawBar + перекраска волка |
| 7. Blockout «Штольня» | `Assets/Editor/ShtolnyaBlockoutBuilder.cs` (меню Siberian/Blockout) | Генерация — запуск в редакторе |
| 7. Blockout «Затонувший посёлок» | `Assets/Editor/SunkenVillageBlockoutBuilder.cs` | Генерация — запуск в редакторе |
| — общий хелпер | `Assets/Editor/BlockoutKit.cs` (палитра LDD §5, оранжевый волк) | — |

Задача 5 (правки LDD/GDD) выполнена отдельными Google-документами (см. Drive),
задача 8 — хронология на Trello-доске «Unity 6».

## Точки интеграции (свод)

Файлы написаны самодостаточными: они компилируются без ссылок на существующий код проекта,
чтобы вслепую не угадывать сигнатуры. Каждая фича включается 1–2 строками:

1. **WolfAI.cs** — поле `WolfPursuitTargeting pursuit;` на волке; в ChaseTick цель
   `pursuit.GetChaseDestination(chaseSpeed)` вместо `player.position`.
2. **ThinIce.cs** — `ApplyWarmthPenalty(IceFallGrace.NextPenalty(currentWarmth, 25f))`;
   в респавне `IceFallGrace.ResetRun()`.
3. **SurvivalStats.cs** — `OnStatsChanged += () => coldAudio.SetWarmth(warmth, maxWarmth);`.
4. **GameHUD.cs** — `HudPalette.WarmthColor(...)` / `HudPalette.WarningBlink(...)` /
   `HudPalette.WolfThreat`; материал капсулы волка → `HudPalette.WolfBody`.

Точные имена полей/методов проекта в облаке не видны — если сигнатуры отличаются,
правки сводятся к переименованию в этих 4 строках.
