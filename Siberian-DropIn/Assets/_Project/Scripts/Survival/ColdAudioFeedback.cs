using UnityEngine;

namespace Siberian.Survival
{
    /// <summary>
    /// Roadmap v0.2 №3 — многоканальный сигнал холода (Hodent, The Gamer's Brain:
    /// критичный сигнал не должен быть только визуальным). Три аудио-слоя по мере замерзания:
    ///   дыхание   — Warmth &lt; breathingThreshold (60): игрок слышит, что мёрзнет, ДО опасности;
    ///   дрожь     — Warmth &lt; tremorThreshold (25, порог тремора из SurvivalStats);
    ///   сердце    — Warmth &lt; heartbeatThreshold (15): предсмертное состояние.
    ///
    /// Громкость растёт НЕлинейно к нулю тепла (Weber–Fechner, GDD v0.2 §6) — закрывает
    /// заодно и Roadmap №5 для аудио-канала.
    ///
    /// ИНТЕГРАЦИЯ:
    /// 1. Повесить на объект игрока (или камеры), назначить клипы-лупы в инспекторе.
    /// 2. Подписка на существующее событие (одна строка в SurvivalStats или в этом же объекте):
    ///      stats.OnStatsChanged += () => coldAudio.SetWarmth(stats.Warmth, stats.MaxWarmth);
    ///    (Имена свойств подставить по фактическому API SurvivalStats — событие уже есть, GDD v0.2 §13 п.3.)
    /// Пока значение не приходит, компонент молчит — безопасен до интеграции.
    /// </summary>
    public class ColdAudioFeedback : MonoBehaviour
    {
        [Header("Клипы (лупы)")]
        [SerializeField] private AudioClip breathingLoop;
        [SerializeField] private AudioClip shiverLoop;      // дрожь/стук зубов
        [SerializeField] private AudioClip heartbeatLoop;

        [Header("Пороги (в единицах Warmth, максимум 100)")]
        [SerializeField] private float breathingThreshold = 60f;
        [SerializeField] private float tremorThreshold = 25f;   // = tremorThreshold из SurvivalStats
        [SerializeField] private float heartbeatThreshold = 15f;

        [Header("Громкость")]
        [Range(0f, 1f)] [SerializeField] private float breathingMaxVolume = 0.7f;
        [Range(0f, 1f)] [SerializeField] private float shiverMaxVolume = 0.8f;
        [Range(0f, 1f)] [SerializeField] private float heartbeatMaxVolume = 1f;

        [Tooltip("Степень нелинейности Weber–Fechner: 1 = линейно, 2-3 = тихо вначале, резкий рост к нулю тепла.")]
        [Range(1f, 4f)] [SerializeField] private float perceptionExponent = 2.2f;

        [Tooltip("Скорость подстройки громкости, чтобы слои не щёлкали при скачках тепла (провал, укус).")]
        [SerializeField] private float volumeLerpSpeed = 3f;

        private AudioSource _breathing;
        private AudioSource _shiver;
        private AudioSource _heartbeat;
        private float _warmth01 = 1f; // 1 = полное тепло, звук выключен

        private void Awake()
        {
            _breathing = CreateLayer(breathingLoop, "ColdAudio_Breathing");
            _shiver = CreateLayer(shiverLoop, "ColdAudio_Shiver");
            _heartbeat = CreateLayer(heartbeatLoop, "ColdAudio_Heartbeat");
        }

        private AudioSource CreateLayer(AudioClip clip, string childName)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, worldPositionStays: false);
            var src = child.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f; // сигнал состояния тела — всегда «в голове», не в мире
            src.volume = 0f;
            if (clip != null) src.Play();
            return src;
        }

        /// <summary>Основная точка интеграции: передавать текущее и максимальное тепло.</summary>
        public void SetWarmth(float current, float max)
        {
            _warmth01 = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        }

        /// <summary>Альтернатива, если удобнее нормализованное значение 0..1.</summary>
        public void SetWarmth01(float warmth01) => _warmth01 = Mathf.Clamp01(warmth01);

        private void Update()
        {
            float warmth = _warmth01 * 100f;
            ApplyLayer(_breathing, warmth, breathingThreshold, breathingMaxVolume);
            ApplyLayer(_shiver, warmth, tremorThreshold, shiverMaxVolume);
            ApplyLayer(_heartbeat, warmth, heartbeatThreshold, heartbeatMaxVolume);
        }

        private void ApplyLayer(AudioSource src, float warmth, float threshold, float maxVolume)
        {
            if (src == null || src.clip == null) return;

            // Насколько глубоко мы «под порогом»: 0 у порога → 1 при нуле тепла.
            float depth = threshold > 0f ? Mathf.Clamp01((threshold - warmth) / threshold) : 0f;
            // Weber–Fechner: восприятие нелинейно — тихий вход, резкое нарастание у нуля.
            float targetVolume = Mathf.Pow(depth, perceptionExponent) * maxVolume;

            src.volume = Mathf.MoveTowards(src.volume, targetVolume, volumeLerpSpeed * Time.deltaTime);

            if (targetVolume > 0f && !src.isPlaying) src.Play();
            else if (Mathf.Approximately(src.volume, 0f) && targetVolume <= 0f && src.isPlaying) src.Stop();
        }
    }
}
