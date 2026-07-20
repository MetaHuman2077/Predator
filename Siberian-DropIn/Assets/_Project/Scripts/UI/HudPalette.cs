using UnityEngine;

namespace Siberian.UI
{
    /// <summary>
    /// Roadmap v0.2 №4 — развести цветовые каналы «холод» и «волк» (Hodent: red overload —
    /// сейчас красный занят и баром тепла, и волком; при одновременной угрозе сигналы гасят
    /// друг друга). Новая схема:
    ///   ХОЛОД  — сине-ледяная шкала: белый (тепло) → голубой → глубокий синий (замерзание).
    ///            Холод «выглядит холодным» — считывается периферией без чтения цифр.
    ///   ВОЛК   — оранжевый (модель, индикатор угрозы) + рычание как аудио-канал.
    ///   КРАСНЫЙ — освобождён, остаётся только за маяком вышки (лендмарк-цель, LDD §5).
    ///
    /// ИНТЕГРАЦИЯ (GameHUD.cs, IMGUI):
    ///   бар тепла:   GUI.color = HudPalette.WarmthColor(warmth01);
    ///   мигание:     if (HudPalette.WarningBlink(warmth01)) { ...рамка... }  // нелинейно, Roadmap №5
    ///   волк:        GUI.color = HudPalette.WolfThreat;  // вместо Color.red
    ///   модель волка: перекрасить материал капсулы в HudPalette.WolfBody (вместо красного, LDD §5).
    /// </summary>
    public static class HudPalette
    {
        // --- Канал холода (сине-ледяной) ---
        public static readonly Color WarmthFull = new Color(0.95f, 0.97f, 1.00f); // почти белый
        public static readonly Color WarmthChilly = new Color(0.45f, 0.75f, 1.00f); // голубой
        public static readonly Color WarmthFreezing = new Color(0.10f, 0.25f, 0.85f); // глубокий синий

        // --- Канал волка (оранжевый) ---
        public static readonly Color WolfThreat = new Color(1.00f, 0.55f, 0.10f); // индикатор/HUD
        public static readonly Color WolfBody = new Color(0.85f, 0.45f, 0.15f); // капсула-модель

        // --- Красный остаётся только целям ---
        public static readonly Color BeaconGoal = new Color(1.00f, 0.15f, 0.10f); // маяк вышки

        /// <summary>Цвет бара тепла: 1 → белый, 0.5 → голубой, 0 → глубокий синий.</summary>
        public static Color WarmthColor(float warmth01)
        {
            warmth01 = Mathf.Clamp01(warmth01);
            return warmth01 > 0.5f
                ? Color.Lerp(WarmthChilly, WarmthFull, (warmth01 - 0.5f) * 2f)
                : Color.Lerp(WarmthFreezing, WarmthChilly, warmth01 * 2f);
        }

        /// <summary>
        /// Нелинейное мигание предупреждения (Weber–Fechner, Roadmap №5): частота растёт
        /// квадратично к нулю тепла. Выше 40% тепла не мигает вовсе.
        /// </summary>
        public static bool WarningBlink(float warmth01)
        {
            warmth01 = Mathf.Clamp01(warmth01);
            if (warmth01 >= 0.4f) return false;

            float danger = 1f - warmth01 / 0.4f;            // 0 у порога → 1 при нуле
            float frequency = Mathf.Lerp(1f, 7f, danger * danger); // Гц, квадратичный рост
            return Mathf.PingPong(Time.unscaledTime * frequency, 1f) > 0.5f;
        }
    }
}
