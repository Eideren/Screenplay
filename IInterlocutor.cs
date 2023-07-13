namespace Screenplay
{
    /// <summary>
    /// An entity that can perform lines of a <see cref="Scenario"/>, this entity will receive a
    /// <see cref="IInterlocutor.HasFocusTick"/> every frame while that line is played through the <see cref="Stage"/>
    /// </summary>
    public interface IInterlocutor
    {
        /// <summary>
        /// Called every frame while lines he is bound to are shown on screen
        /// </summary>
        void HasFocusTick(Stage stage);
    }
}