using UnityEngine;
using UnityEngine.Events;

namespace Siberian.Survival
{
    /// <summary>
    /// Roadmap v0.2 №2 — «Честный первый провал» (Hodent, The Gamer's Brain: fair first failure).
    /// Первая ошибка на льду за забег учит, а не убивает: штраф первого провала снижен,
    /// и тепло гарантированно не падает ниже "пола выживания". Со второго провала — полный
    /// штраф 25 (текущий баланс из кода/метрик v0.2, снижен с 35 по плейтесту).
    ///
    /// ИНТЕГРАЦИЯ:
    /// 1. Повесить компонент на любой объект сцены (например, корень систем выживания).
    /// 2. В ThinIce.cs заменить фиксированный штраф:
    ///      было:   ApplyWarmthPenalty(fallPenalty);                       // 25
    ///      стало:  ApplyWarmthPenalty(IceFallGrace.NextPenalty(currentWarmth, fallPenalty));
    /// 3. В респавне (SurvivalStats) вызвать IceFallGrace.ResetRun() — новый забег, новый «урок».
    /// 4. (Опционально) на OnFirstFall повесить усиленную обратную связь: громкий треск,
    ///    вскрик, виньетка — игрок должен ЗАПОМНИТЬ урок, даже если выжил.
    /// </summary>
    public class IceFallGrace : MonoBehaviour
    {
        [Header("Баланс")]
        [Tooltip("Штраф тепла за первый провал в забеге (полный штраф 25 включается со второго).")]
        [SerializeField] private float firstFallPenalty = 10f;

        [Tooltip("Ниже этого значения Warmth первый провал не опускает никогда — первая ошибка не убивает.")]
        [SerializeField] private float firstFallWarmthFloor = 20f;

        [Header("События")]
        [Tooltip("Срабатывает на первом провале забега — сюда вешать усиленный аудио/визуальный «урок».")]
        public UnityEvent OnFirstFall;

        [Tooltip("Срабатывает на повторных провалах (полный штраф).")]
        public UnityEvent OnRepeatFall;

        private static IceFallGrace _instance;
        private int _fallsThisRun;

        public static int FallsThisRun => _instance != null ? _instance._fallsThisRun : 0;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>
        /// Возвращает фактический штраф тепла для очередного провала и регистрирует провал.
        /// Если компонента нет в сцене — возвращает полный штраф (поведение без фичи).
        /// </summary>
        public static float NextPenalty(float currentWarmth, float fullPenalty)
        {
            if (_instance == null) return fullPenalty;
            return _instance.NextPenaltyInternal(currentWarmth, fullPenalty);
        }

        /// <summary>Сбросить счётчик провалов — вызывать при респавне/старте нового забега.</summary>
        public static void ResetRun()
        {
            if (_instance != null) _instance._fallsThisRun = 0;
        }

        private float NextPenaltyInternal(float currentWarmth, float fullPenalty)
        {
            _fallsThisRun++;

            if (_fallsThisRun > 1)
            {
                OnRepeatFall?.Invoke();
                return fullPenalty;
            }

            // Первый провал: щадящий штраф + гарантия, что тепло не уйдёт ниже пола выживания.
            float penalty = Mathf.Min(firstFallPenalty, Mathf.Max(0f, currentWarmth - firstFallWarmthFloor));
            OnFirstFall?.Invoke();
            return penalty;
        }
    }
}
