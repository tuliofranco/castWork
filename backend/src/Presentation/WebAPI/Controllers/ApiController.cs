using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace Castwork.WebAPI.Controllers;

/// <summary>
/// Base dos controllers: fina de propósito. Cada action só monta o request e chama <see cref="Mediator"/>;
/// regra, validação e autorização ficam no pipeline do MediatR (o mesmo usado pelo GraphQL).
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiController : ControllerBase
{
    protected ISender Mediator => HttpContext.RequestServices.GetRequiredService<ISender>();
}
