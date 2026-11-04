using UnityEngine;

namespace EscapeFire.Hazards
{
    /// <summary>
    /// Hard VFX caps for Quest 2 (XR2) to keep >= 72 FPS: max 4 active fire emitters, max 5 smoke emitters.
    /// Every fire/smoke source must ask before emitting. Missing governor = no cap (editor convenience).
    /// </summary>
    public class PerformanceBudgetGovernor : MonoBehaviour
    {
        public const int MAX_FIRE_EMITTERS = 4;
        public const int MAX_SMOKE_EMITTERS = 5;

        public static PerformanceBudgetGovernor Instance { get; private set; }

        private readonly EmitterBudget _fires = new EmitterBudget(MAX_FIRE_EMITTERS);
        private readonly EmitterBudget _smokes = new EmitterBudget(MAX_SMOKE_EMITTERS);

        public int ActiveFires => _fires.Count;
        public int ActiveSmokes => _smokes.Count;
        public bool CanSpawnFire => _fires.HasRoom;
        public bool CanSpawnSmoke => _smokes.HasRoom;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool TryRegisterFire(Object source)
        {
            bool ok = _fires.TryRegister(source.GetInstanceID());
            if (!ok) Debug.LogWarning($"[PerformanceGovernor] Fire suppressed ({_fires.Count}/{MAX_FIRE_EMITTERS}) to protect 72 FPS: {source.name}");
            return ok;
        }

        public void UnregisterFire(Object source) => _fires.Unregister(source.GetInstanceID());

        public bool TryRegisterSmoke(Object source)
        {
            bool ok = _smokes.TryRegister(source.GetInstanceID());
            if (!ok) Debug.LogWarning($"[PerformanceGovernor] Smoke suppressed ({_smokes.Count}/{MAX_SMOKE_EMITTERS}): {source.name}");
            return ok;
        }

        public void UnregisterSmoke(Object source) => _smokes.Unregister(source.GetInstanceID());
    }
}
