namespace Screenplay
{
    public interface IPreconditionCollector
    {
        /// <summary> The locals which will be sent to a running <see cref="IEventContext"/> </summary>
        Locals SharedLocals { get; }

        /// <summary> Whether the system is currently running a <see cref="IEventContext"/> and as such cannot transition into this one </summary>
        LatentVariable<bool> IsBusy { get; }

        /// <summary>
        /// Sets whether this precondition has been fulfilled (<paramref name="state"/>=true)
        /// or is still missing something (<paramref name="state"/>=false)
        /// </summary>
        /// <remarks>
        /// When inside a <see cref="IPrecondition.Setup"/>, calling this method and passing true for
        /// <paramref name="state"/> will let the event waiting on that <see cref="IPrecondition"/> run
        /// </remarks>
        void SetUnlockedState(bool state, params GlobalId[] locals);
    }
}
