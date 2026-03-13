using ERP.Application.Common.Models;
using ERP.Application.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    public ProductsController(IProductService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductListItemDto>>> Get([FromQuery] ListQuery request, CancellationToken cancellationToken)
        => Ok(await _service.GetPagedAsync(request, cancellationToken));

    [HttpGet("lookup")]
    public async Task<ActionResult<IReadOnlyCollection<LookupDto>>> Lookup(CancellationToken cancellationToken)
        => Ok(await _service.GetLookupAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Get(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] SaveProductRequest request, CancellationToken cancellationToken)
    {
        var id = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { version = "1.0", id }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveProductRequest request, CancellationToken cancellationToken)
    {
        await _service.UpdateAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
