namespace DreamKnight.Player.States
{
    public class DeathState : PlayerState
    {
        public DeathState(PlayerController controller) : base(controller) { }

        public override void Enter()
        {
            if (controller.IsDeathSequencePlaying)
                return;

            controller.DisablePlayerInputForDeath();
            controller.StopPlayerMovementForDeath();
            controller.ApplyDeadBodyLayer();
            controller.AudioEvents?.PlayDeath();
            controller.DeathSequence?.Play();
        }

        public override void CheckTransitions() { }
    }
}
