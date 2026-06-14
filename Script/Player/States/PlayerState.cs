using UnityEngine;

namespace DreamKnight.Player.States
{
    /// <summary>
    /// Base class cho tất cả Player States
    /// </summary>
    public abstract class PlayerState
    {
        protected PlayerController controller;
        protected PlayerInput input;
        protected PlayerMovement movement;
        protected PlayerStats stats;
        protected Animator animator;

        public PlayerState(PlayerController controller)
        {
            this.controller = controller;
            this.input = controller.Input;
            this.movement = controller.Movement;
            this.stats = controller.Stats;
            this.animator = controller.Animator;
        }

        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void CheckTransitions() { }
    }
}
