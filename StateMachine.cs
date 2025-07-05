using System.Collections.Generic;
using UnityEngine;

namespace Cyl.StateMachines
{
    /// <summary>
    /// Marker interface for state change parameters.
    /// This allows passing additional data when changing states.
    /// </summary>
    public interface IStateChangeParams { }
        
    /// <summary>
    /// The base class for all states in the state machine.
    /// Each state should inherit from this class and implement the required methods.
    /// Only one state of each name can exist in the state machine.
    /// </summary>
    public abstract class State
    {
        /// <summary>
        /// The unique name of the state.
        /// </summary>
        public abstract string Name { get; }
        
        /// <summary>
        /// The state machine that this state belongs to.
        /// </summary>
        protected StateMachine StateMachine { get; private set; }

        /// <summary>
        /// Initializes the state with the given state machine.
        /// </summary>
        /// <param name="stateMachine"></param>
        public virtual void Initialize(StateMachine stateMachine)
        {
            StateMachine = stateMachine;
        }
        
        /// <summary>
        /// Invoked when the state is entered.
        /// </summary>
        public abstract void OnEnter(IStateChangeParams stateChangeParams);
        
        /// <summary>
        /// Invoked every frame while the state is active.
        /// </summary>
        /// <param name="deltaTime">The time since the last frame.</param>
        public abstract void OnUpdate(float deltaTime);
        
        /// <summary>
        /// Invoked every fixed frame rate frame while the state is active.
        /// </summary>
        /// <param name="fixedDeltaTime">The fixed time since the last frame.</param>
        public abstract void OnFixedUpdate(float fixedDeltaTime);
        
        /// <summary>
        /// Invoked when the state is exited.
        /// </summary>
        public abstract void OnExit();
        
        /// <summary>
        /// Invoked when the state machine is destroyed.
        /// </summary>
        public abstract void OnDestroy();
    }
    
    /// <summary>
    /// Represents a transition event in the state machine.
    /// Each transition event has a name and a target state to transition to.
    /// A state can have multiple transitions associated with it, each with a different event name.
    /// </summary>
    public struct TransitionEvent
    {
        /// <summary>
        /// Default event name for a transition that signifies the state has finished its work.
        /// </summary>
        public const string Finished = "Finished";
        
        /// <summary>
        /// The name of the event that triggers the transition.
        /// </summary>
        public readonly string EventName;
        
        /// <summary>
        /// The name of the state to transition to when this event is fired.
        /// </summary>
        public readonly string ToState;
        
        /// <summary>
        /// Constructs a new transition event with the specified event name and target state.
        /// </summary>
        /// <param name="eventName">The name of the event that triggers the transition.</param>
        /// <param name="toState">The name of the state to transition to.</param>
        public TransitionEvent(string eventName, string toState)
        {
            EventName = eventName;
            ToState = toState;
        }
    }
    
    /// <summary>
    /// A state machine that manages states and transitions between them.
    /// The state machine can only have one active state at a time, which is then
    /// updated every frame.
    /// </summary>
    public class StateMachine : MonoBehaviour
    {
        private readonly Dictionary<string, State> _states = new();
        private readonly Dictionary<string, List<TransitionEvent>> _transitions = new();
        
        /// <summary>
        /// The name of the initial state to start the state machine with. <seealso cref="Begin"/>
        /// </summary>
        public string InitialStateName { get; set; }
        
        /// <summary>
        /// The current active state of the state machine.
        /// </summary>
        public State CurrentState { get; private set; }
        
        /// <summary>
        /// Retrieves a state of the specified type by its name.
        /// </summary>
        /// <param name="stateName">The name of the state to retrieve.</param>
        /// <typeparam name="T">The type of the state to retrieve. Must inherit from <see cref="State"/>.</typeparam>
        /// <returns>The state of the specified type if found, otherwise null.</returns>
        public T GetState<T>(string stateName) where T : State
        {
            if (_states.TryGetValue(stateName, out var state))
                return (T)state;

            Debug.LogError($"State '{stateName}' not found in the state machine.");
            return null;
        }
        
        /// <summary>
        /// Retrieves a state by its name.
        /// </summary>
        /// <param name="stateName">The name of the state to retrieve.</param>
        /// <returns>The state if found, otherwise null.</returns>
        public State GetState(string stateName)
        {
            if (_states.TryGetValue(stateName, out var state))
                return state;

            Debug.LogError($"State '{stateName}' not found in the state machine.");
            return null;
        }
        
        /// <summary>
        /// Registers a new state in the state machine.
        /// </summary>
        /// <param name="state">The state to register.</param>
        /// <returns>True if the state was successfully registered, false if a state with the same name already exists.</returns>
        public bool RegisterState(State state)
        {
            var stateName = state.Name;
            if (_states.ContainsKey(stateName))
                return false;
            
            state.Initialize(this);
            _states[stateName] = state;
            
            return true;
        }
        
        /// <summary>
        /// Registers a transition from one state to another.
        /// </summary>
        /// <param name="fromState">The state to transition from.</param>
        /// <param name="toState">The state to transition to.</param>
        /// <param name="eventName">The name of the event that triggers the transition. Defaults to <see cref="TransitionEvent.Finished"/>.</param>
        /// <returns>True if the transition was successfully registered, false if either state does not exist.</returns>
        public bool RegisterTransition(State fromState, State toState, string eventName = TransitionEvent.Finished)
        {
            return RegisterTransition(fromState.Name, toState.Name, eventName);
        }
        
        /// <summary>
        /// Registers a transition from one state to another by their names.
        /// </summary>
        /// <param name="fromState">The name of the state to transition from.</param>
        /// <param name="toState">The name of the state to transition to.</param>
        /// <param name="eventName">The name of the event that triggers the transition. Defaults to <see cref="TransitionEvent.Finished"/>.</param>
        /// <returns>True if the transition was successfully registered, false if either state does not exist.</returns>
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
        
        /// <summary>
        /// Begins the state machine by transitioning to the initial state.
        /// Only call this method once, after all states and transitions have been registered.
        /// Will not do anything if there is already a state active.
        /// </summary>
        public void Begin()
        {
            if (CurrentState == null)
                ChangeState(InitialStateName);
        }

        /// <summary>
        /// Fires a transition event to change the current state.
        /// If no valid transition exists for the current state and the specified event name,
        /// does nothing and logs an error.
        /// </summary>
        /// <param name="eventName">The name of the event that triggers the transition.</param>
        /// <param name="stateChangeParams">Additional parameters to pass to the state when transitioning.</param>
        public void FireTransitionEvent(string eventName, IStateChangeParams stateChangeParams = null)
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
                
                ChangeState(transition.ToState, stateChangeParams);
                return;
            }
            
            Debug.LogError($"No transition found for event '{eventName}' in state '{currentStateName}'.");
        }
        
        private void ChangeState(string toState, IStateChangeParams stateChangeParams = null)
        {
            var nextState = GetState(toState);
            if (nextState == null)
            {
                Debug.LogError($"Cannot change to state '{toState}' because it does not exist.");
                return;
            }

            CurrentState?.OnExit();
            CurrentState = nextState;
            CurrentState.OnEnter(stateChangeParams);
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
