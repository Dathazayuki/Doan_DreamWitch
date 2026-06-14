namespace DreamKnight.Player.States
{
    public interface IAttackTriggerHandler
    {
        /// <summary>
        /// Handles the attack hit animation event.
        /// </summary>
        /// <returns>True to override and skip default melee hit detection; false to allow standard melee logic.</returns>
        bool OnAttackHitTriggered();
    }
}
