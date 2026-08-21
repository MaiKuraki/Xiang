namespace Xiang.EventBus.Core
{
    /// <summary>
    /// Which command backend the composition root selects. The enum itself is Core-owned; only the
    /// VitalRouter integration assembly knows how to build the VitalRouter backend.
    /// </summary>
    public enum CommandBackend
    {
        InProcess = 0,
        VitalRouter = 1,
    }

    /// <summary>
    /// Overflow behavior for a bounded command queue.
    /// </summary>
    public enum CommandOverflowPolicy
    {
        Drop = 0,
        FailFast = 1,
    }
}
