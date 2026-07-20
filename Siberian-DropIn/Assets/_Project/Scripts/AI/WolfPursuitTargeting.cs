using UnityEngine;

namespace Siberian.AI
{
    /// <summary>
    /// Roadmap v0.2 №1 — Pursuit вместо Seek (Buckland, Programming Game AI by Example, гл.3).
    /// Волк целится в экстраполированную позицию игрока, а не в текущую:
    /// LookAheadTime = distance / (chaseSpeed + playerSpeed).
    ///
    /// Компонент самодостаточен: скорость игрока оценивается конечной разностью позиций
    /// Transform, ссылок на код проекта нет — компилируется до интеграции.
    ///
    /// ИНТЕГРАЦИЯ (2 строки в WolfAI.cs):
    ///   поле:      [SerializeField] WolfPursuitTargeting pursuit;   // повесить на объект волка
    ///   ChaseTick: вместо  MoveTowards(player.position)
    ///              использовать  MoveTowards(pursuit.GetChaseDestination(chaseSpeed));
    /// Остальная FSM (Patrol/Chase/Attack/Retreat) не меняется — апгрейд не трогает архитектуру.
    /// </summary>
    public class WolfPursuitTargeting : MonoBehaviour
    {
        [Tooltip("Transform игрока. Если пусто — ищется по тегу Player при старте.")]
        [SerializeField] private Transform target;

        [Tooltip("Максимальное время упреждения, с. Ограничивает прицел на дальней дистанции, иначе волк убегает 'в никуда'.")]
        [SerializeField] private float maxLookAheadTime = 1.5f;

        [Tooltip("Если игрок ближе этой дистанции — упреждение не нужно, целимся напрямую (Buckland: близкая цель = Seek).")]
        [SerializeField] private float directSeekDistance = 3f;

        [Tooltip("Сглаживание оценки скорости игрока (0 — без сглаживания). Убирает дёрганье прицела от шага/прыжков.")]
        [Range(0f, 0.95f)]
        [SerializeField] private float velocitySmoothing = 0.6f;

        private Vector3 _lastTargetPos;
        private Vector3 _estimatedVelocity;
        private bool _hasSample;

        public Transform Target => target;

        private void Start()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }
        }

        private void Update()
        {
            if (target == null) return;

            if (!_hasSample)
            {
                _lastTargetPos = target.position;
                _hasSample = true;
                return;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 raw = (target.position - _lastTargetPos) / dt;
            _estimatedVelocity = Vector3.Lerp(raw, _estimatedVelocity, velocitySmoothing);
            _lastTargetPos = target.position;
        }

        /// <summary>
        /// Точка, в которую должен бежать волк. Упреждение по Buckland:
        /// t = dist / (mySpeed + targetSpeed), прицел = позиция цели + скорость цели * t.
        /// </summary>
        public Vector3 GetChaseDestination(float myChaseSpeed)
        {
            if (target == null) return transform.position;

            Vector3 toTarget = target.position - transform.position;
            float distance = toTarget.magnitude;

            // Вплотную упреждать нечего — обычный Seek.
            if (distance <= directSeekDistance)
                return target.position;

            float targetSpeed = _estimatedVelocity.magnitude;
            float closingSpeed = myChaseSpeed + targetSpeed;
            if (closingSpeed < 0.01f)
                return target.position;

            float lookAhead = Mathf.Min(distance / closingSpeed, maxLookAheadTime);

            // Упреждаем только горизонтально: вертикальная составляющая (прыжок) прицелу мешает.
            Vector3 flatVelocity = _estimatedVelocity;
            flatVelocity.y = 0f;

            return target.position + flatVelocity * lookAhead;
        }

        private void OnDrawGizmosSelected()
        {
            if (target == null || !Application.isPlaying) return;
            Vector3 aim = GetChaseDestination(4.5f); // chaseSpeed из метрик v0.2
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(aim, 0.4f);
            Gizmos.DrawLine(transform.position, aim);
        }
    }
}
