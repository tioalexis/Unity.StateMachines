using System.Collections.Generic;
using UnityEngine;

namespace Cyl.StateMachines
{
    public abstract class State
    {
        public abstract string Name { get; }
        
        public StateMachine StateMachine { get; private set; }

        public virtual void Initialize(StateMachine stateMachine)
        {
            StateMachine = stateMachine;
        }
        
        public abstract void OnEnter();
        
        public abstract void OnUpdate(float deltaTime);
        
        public abstract void OnFixedUpdate(float fixedDeltaTime);
        
        public abstract void OnExit();
        
        public abstract void OnDestroy();
    }
    
    public struct TransitionEvent
    {
        public const string Finished = "Finished";
        
        public string EventName;
        public string ToState;
        
        public TransitionEvent(string eventName, string toState)
        {
            EventName = eventName;
            ToState = toState;
        }
    }
    
    public class StateMachine : MonoBehaviour
    {
        private readonly Dictionary<string, State> _states = new();
        private readonly Dictionary<string, List<TransitionEvent>> _transitions = new();
        private string _initialStateName;
        
        public string InitialStateName { get; set; }
        
        public State CurrentState { get; private set; }
        
        public T GetState<T>(string stateName) where T : State
        {
            if (_states.TryGetValue(stateName, out var state))
                return (T)state;

            Debug.LogError($"State '{stateName}' not found in the state machine.");
            return null;
        }
        
        public State GetState(string stateName)
        {
            if (_states.TryGetValue(stateName, out var state))
                return state;

            Debug.LogError($"State '{stateName}' not found in the state machine.");
            return null;
        }

        public void ChangeState(string toState)
        {
            var nextState = GetState(toState);
            if (nextState == null)
            {
                Debug.LogError($"Cannot change to state '{toState}' because it does not exist.");
                return;
            }

            CurrentState?.OnExit();
            CurrentState = nextState;
            CurrentState.OnEnter();
        }
        
        public bool RegisterState(State state)
        {
            var stateName = state.Name;
            if (_states.ContainsKey(stateName))
                return false;
            
            state.Initialize(this);
            _states[stateName] = state;
            
            return true;
        }
        
        public bool RegisterTransition(State fromState, State toState, string eventName = TransitionEvent.Finished)
        {
            return RegisterTransition(fromState.Name, toState.Name, eventName);
        }
        
        public bool RegisterTransition(string fromState, string toState, string eventName = TransitionEvent.Finished)
        {
            if (!_states.ContainsKey(fromState) || !_states.ContainsKey(toState))
                return false;

            var transitionEvent = new TransitionEvent(eventName, toState);
            if (!_transitions.TryGetValue(fromState, out var transitionList))
            {
                transitionList = new List<TransitionEvent>();
                _transitions[fromState] = transitionList;
            }
            
            transitionList.Add(transitionEvent);
            return true;
        }
        
        public void Begin()
        {
            if (CurrentState == null)
                ChangeState(InitialStateName);
        }

        public void FireTransitionEvent(string eventName)
        {
            if (CurrentState == null)
            {
                Debug.LogError("Cannot fire transition event because there is no current state.");
                return;
            }
            
            var currentStateName = CurrentState.Name;
            if (!_transitions.TryGetValue(currentStateName, out var transitionList))
            {
                Debug.LogError($"Cannot fire transition event because the state {currentStateName} does not have any transitions.");
                return;
            }
            
            foreach (var transition in transitionList)
            {
                if (transition.EventName != eventName) 
                    continue;
                
                ChangeState(transition.ToState);
                return;
            }
            
            Debug.LogError($"No transition found for event '{eventName}' in state '{currentStateName}'.");
        }
        
        private void Update()
        {
            if (CurrentState != null)
            {
                CurrentState.OnUpdate(Time.deltaTime);
            }
        }
        
        private void FixedUpdate()
        {
            if (CurrentState != null)
            {
                CurrentState.OnFixedUpdate(Time.fixedDeltaTime);
            }
        }
        
        private void OnDestroy()
        {
            foreach (var state in _states.Values)
                state.OnDestroy();
            
            _states.Clear();
            _transitions.Clear();
            CurrentState = null;
        }
    }

}
