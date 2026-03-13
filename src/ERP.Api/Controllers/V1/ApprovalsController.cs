using ERP.Application.Approvals;
using ERP.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Api.Controllers.V1;

public sealed class ReviewApprovalRequest
{
    public string? Comments { get; init; }
}

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/approvals")]
public sealed class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _service;

    public ApprovalsController(IApprovalService service)
    {
        _service = service;
    }

    [HttpGet("requests")]
    public async Task<ActionResult<PagedResult<ApprovalRequestDto>>> GetRequests([FromQuery] ApprovalRequestQuery request, CancellationToken cancellationToken)
        => Ok(await _service.GetRequestsAsync(request, cancellationToken));

    [HttpPost("requests/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ReviewApprovalRequest request, CancellationToken cancellationToken)
    {
        await _service.ApproveAsync(id, request.Comments, cancellationToken);
        return NoContent();
    }

    [HttpPost("requests/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ReviewApprovalRequest request, CancellationToken cancellationToken)
    {
        await _service.RejectAsync(id, request.Comments, cancellationToken);
        return NoContent();
    }

    [HttpGet("rules")]
    public async Task<ActionResult<PagedResult<ApprovalRuleDto>>> GetRules([FromQuery] ListQuery request, CancellationToken cancellationToken)
        => Ok(await _service.GetRulesAsync(request, cancellationToken));

    [HttpGet("rules/{id:guid}")]
    public async Task<ActionResult<ApprovalRuleDto>> GetRule(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetRuleAsync(id, cancellationToken));

    [HttpPost("rules")]
    public async Task<ActionResult<Guid>> CreateRule([FromBody] SaveApprovalRuleRequest request, CancellationToken cancellationToken)
    {
        var id = await _service.CreateRuleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetRule), new { version = "1.0", id }, id);
    }

    [HttpPut("rules/{id:guid}")]
    public async Task<IActionResult> UpdateRule(Guid id, [FromBody] SaveApprovalRuleRequest request, CancellationToken cancellationToken)
    {
        await _service.UpdateRuleAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("rules/{id:guid}")]
    public async Task<IActionResult> DeleteRule(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteRuleAsync(id, cancellationToken);
        return NoContent();
    }
}
