namespace CleanMessageBus.Abstractions.HandlerAttributes;

/// <summary>
/// Defines custom producer name
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ProducedByAttribute : Attribute
{
    /// <summary>
    /// Name of the producer
    /// </summary>
    public required string Name { get; init; }
}