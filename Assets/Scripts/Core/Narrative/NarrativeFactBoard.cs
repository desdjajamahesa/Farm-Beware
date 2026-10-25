using System;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// High-performance, Zero-GC implementation of the INarrativeFactBoard contract.
    /// Stores world facts in a fixed int[128] array. All queries and condition evaluations
    /// run in stack memory without heap allocation or boxing overhead.
    /// </summary>
    public class NarrativeFactBoard : INarrativeFactBoard
    {
        private readonly int[] _facts = new int[(int)NarrativeFactKey.MaxCount];

        public event Action<NarrativeFactKey, int> OnFactChanged;

        public int GetFact(NarrativeFactKey key)
        {
            int idx = (int)key;
            if (idx < 0 || idx >= _facts.Length) return 0;
            return _facts[idx];
        }

        public void SetFact(NarrativeFactKey key, int value)
        {
            int idx = (int)key;
            if (idx < 0 || idx >= _facts.Length) return;
            if (_facts[idx] == value) return;

            _facts[idx] = value;
            OnFactChanged?.Invoke(key, value);
        }

        public void AddFact(NarrativeFactKey key, int delta)
        {
            if (delta == 0) return;
            int idx = (int)key;
            if (idx < 0 || idx >= _facts.Length) return;

            _facts[idx] += delta;
            OnFactChanged?.Invoke(key, _facts[idx]);
        }

        public void MaxFact(NarrativeFactKey key, int candidate)
        {
            int idx = (int)key;
            if (idx < 0 || idx >= _facts.Length) return;
            if (candidate <= _facts[idx]) return;

            _facts[idx] = candidate;
            OnFactChanged?.Invoke(key, _facts[idx]);
        }

        public bool EvaluateConditions(ReadOnlySpan<NarrativeFactCondition> conditions, out int specificityScore)
        {
            specificityScore = 0;
            if (conditions.IsEmpty)
            {
                return true;
            }

            for (int i = 0; i < conditions.Length; i++)
            {
                ref readonly var cond = ref conditions[i];
                int idx = (int)cond.key;
                int actualVal = (idx >= 0 && idx < _facts.Length) ? _facts[idx] : 0;

                bool match = cond.op switch
                {
                    FactOperator.Equal => actualVal == cond.targetValue,
                    FactOperator.GreaterOrEqual => actualVal >= cond.targetValue,
                    FactOperator.LessOrEqual => actualVal <= cond.targetValue,
                    FactOperator.NotEqual => actualVal != cond.targetValue,
                    _ => false
                };

                if (!match)
                {
                    if (cond.isRequired)
                    {
                        specificityScore = 0;
                        return false;
                    }
                }
                else
                {
                    specificityScore += cond.scoreWeight > 0 ? cond.scoreWeight : 10;
                }
            }

            return true;
        }

        public void ApplyMutation(NarrativeFactMutation mutation)
        {
            int idx = (int)mutation.key;
            if (idx < 0 || idx >= _facts.Length) return;

            int prev = _facts[idx];
            switch (mutation.op)
            {
                case FactMutationOp.Set:
                    _facts[idx] = mutation.operandValue;
                    break;
                case FactMutationOp.Add:
                    _facts[idx] += mutation.operandValue;
                    break;
                case FactMutationOp.Max:
                    if (mutation.operandValue > _facts[idx])
                        _facts[idx] = mutation.operandValue;
                    break;
            }

            if (_facts[idx] != prev)
            {
                OnFactChanged?.Invoke(mutation.key, _facts[idx]);
            }
        }

        public void CopyRawFacts(int[] destination)
        {
            if (destination == null) return;
            int length = Math.Min(_facts.Length, destination.Length);
            Array.Copy(_facts, destination, length);
        }

        public void LoadRawFacts(int[] source)
        {
            if (source == null) return;
            int length = Math.Min(_facts.Length, source.Length);
            Array.Copy(source, _facts, length);
        }

        public void ResetFacts()
        {
            Array.Clear(_facts, 0, _facts.Length);
        }
    }
}
