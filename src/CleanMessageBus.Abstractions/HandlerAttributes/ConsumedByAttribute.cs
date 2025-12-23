namespace CleanMessageBus.Abstractions.HandlerAttributes;

/// <summary>
/// Defines custom consumer name
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ConsumedByAttribute : Attribute
{
    /// <summary>
    /// Name of the consumer
    /// </summary>
    public required string Name { get; init; }
}