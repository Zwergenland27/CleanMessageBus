namespace CleanMessageBus.HandlerAttributes;

/// <summary>
/// Defines that the handler should be throttled
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ThrottledAttribute : Attribute
{
    /// <summary>
    /// Defines the interval in ms at which the events will be processed by the application
    /// </summary>
    public required int RequestInterval { get; init; }
}