using System;
using System.Collections.Generic;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Contract defining an isolated individual state within a state machine.
    /// </summary>
    /// <typeparam name="TState">Enum or identifier type representing state.</typeparam>
    public interface IState<TState>
    {
        TState StateKey { get; }
        void Enter();
        void Update(float deltaTime);
        void Exit();
    }

    /// <summary>
    /// Universal state machine contract enforcing deterministic state transitions.
    /// </summary>
    /// <typeparam name="TState">Enum or identifier type representing state.</typeparam>
    public interface IStateMachine<TState>
    {
        TState CurrentStateKey { get; }
        IState<TState> CurrentState { get; }
        void AddState(IState<TState> state);
        void ChangeState(TState newStateKey);
        void Update(float deltaTime);
    }

    /// <summary>
    /// High-performance, zero-allocation generic state machine implementation.
    /// </summary>
    /// <typeparam name="TState">Enum or identifier type representing state.</typeparam>
    public class StateMachine<TState> : IStateMachine<TState>
    {
        private readonly Dictionary<TState, IState<TState>> _states = new Dictionary<TState, IState<TState>>();
        public TState CurrentStateKey { get; private set; }
        public IState<TState> CurrentState { get; private set; }

        public event Action<TState, TState> OnStateChanged;

        public void AddState(IState<TState> state)
        {
            if (state != null)
            {
                _states[state.StateKey] = state;
            }
        }

        public void ChangeState(TState newStateKey)
        {
            if (!_states.TryGetValue(newStateKey, out var nextState))
            {
                throw new InvalidOperationException($"State '{newStateKey}' is not registered in this state machine.");
            }

            if (CurrentState != null && EqualityComparer<TState>.Default.Equals(CurrentStateKey, newStateKey))
            {
                return;
            }

            var previousKey = CurrentStateKey;
            CurrentState?.Exit();
            CurrentState = nextState;
            CurrentStateKey = newStateKey;
            CurrentState.Enter();
            OnStateChanged?.Invoke(previousKey, newStateKey);
        }

        public void Update(float deltaTime)
        {
            CurrentState?.Update(deltaTime);
        }
    }
}
