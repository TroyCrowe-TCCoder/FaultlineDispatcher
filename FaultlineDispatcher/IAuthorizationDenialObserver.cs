namespace FaultlineDispatcher;

using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Notified when the authorization middleware challenges (401) or forbids (403) a request, before the response
/// is written. Use it for host-specific side effects such as security auditing. Register implementations with
/// <see cref="ServiceCollectionExtensions.AddAuthorizationDenialObserver{TObserver}"/>.
/// </summary>
/// <remarks>
/// Observers are singletons. Resolve scoped services from <see cref="HttpContext.RequestServices"/>. A failing
/// observer is logged and never changes the response.
/// </remarks>
public interface IAuthorizationDenialObserver
{
    /// <summary>
    /// Called for a challenged or forbidden request. Check <see cref="PolicyAuthorizationResult.Challenged"/> and
    /// <see cref="PolicyAuthorizationResult.Forbidden"/> to tell the two apart.
    /// </summary>
    Task OnDeniedAsync(HttpContext context, PolicyAuthorizationResult result, CancellationToken cancellationToken);
}
