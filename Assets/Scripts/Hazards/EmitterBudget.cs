using System.Collections.Generic;

namespace EscapeFire.Hazards
{
    /// <summary>Pure capped set of active emitters (ids). Registering an existing id is idempotent.</summary>
    public sealed class EmitterBudget
    {
        private readonly HashSet<int> _active = new HashSet<int>();

        public int Capacity { get; }
        public int Count => _active.Count;
        public bool HasRoom => _active.Count < Capacity;

        public EmitterBudget(int capacity)
        {
            Capacity = capacity < 0 ? 0 : capacity;
        }

        public bool TryRegister(int id)
        {
            if (_active.Contains(id)) return true;
            if (!HasRoom) return false;
            _active.Add(id);
            return true;
        }

        public void Unregister(int id) => _active.Remove(id);

        public bool Contains(int id) => _active.Contains(id);

        public void Clear() => _active.Clear();
    }
}
