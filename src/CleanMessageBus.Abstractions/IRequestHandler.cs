using CleanDomainValidation.Application;
using CleanDomainValidation.Domain;

namespace CleanMessageBus.Abstractions;

/// <summary>
/// Defines handler for request of type <typeparamref name="TRequest"/>
/// </summary>
internal interface IRequestHandler<in TRequest>
    where TRequest : IRequest
{
    /// <summary>
    /// Actual request logic
    /// </summary>
    /// <param name="query">Request object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<CanFail> Handle(TRequest query, CancellationToken cancellationToken);
}