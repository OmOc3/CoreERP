using ERP.Application.Common.Models;
using ERP.Application.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _service;

    public PaymentsController(IPaymentService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PaymentDto>>> Get([FromQuery] PaymentQuery request, CancellationToken cancellationToken)
        => Ok(await _service.GetPagedAsync(request, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
    {
        var id = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { version = "1.0" }, id);
    }
}
