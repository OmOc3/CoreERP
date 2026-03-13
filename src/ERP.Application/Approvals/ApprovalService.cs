using ERP.Application.Common.Contracts;
using ERP.Application.Common.Exceptions;
using ERP.Application.Common.Mappings;
using ERP.Application.Common.Models;
using ERP.Application.Common.Security;
using ERP.Application.Sales;
using ERP.Domain.Entities;
using ERP.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Approvals;

public interface IApprovalService
{
    Task<ApprovalSubmissionResult> CreateRequestsAsync(ApprovalDocumentType documentType, Guid documentId, Guid? branchId, decimal amount, CancellationToken cancellationToken);
    Task<PagedResult<ApprovalRequestDto>> GetRequestsAsync(ApprovalRequestQuery request, CancellationToken cancellationToken);
    Task ApproveAsync(Guid requestId, string? comments, CancellationToken cancellationToken);
    Task RejectAsync(Guid requestId, string? comments, CancellationToken cancellationToken);
    Task<PagedResult<ApprovalRuleDto>> GetRulesAsync(ListQuery request, CancellationToken cancellationToken);
    Task<ApprovalRuleDto> GetRuleAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid> CreateRuleAsync(SaveApprovalRuleRequest request, CancellationToken cancellationToken);
    Task UpdateRuleAsync(Guid id, SaveApprovalRuleRequest request, CancellationToken cancellationToken);
    Task DeleteRuleAsync(Guid id, CancellationToken cancellationToken);
}

public sealed record ApprovalSubmissionResult(bool HasApprovalRequests, IReadOnlyCollection<Guid> RequestIds);

public sealed class ApprovalRequestQuery : BranchScopedQuery
{
    public ApprovalDocumentType? DocumentType { get; init; }
    public ApprovalStatus? Status { get; init; }
}

public sealed record ApprovalRequestDto(
    Guid Id,
    ApprovalDocumentType DocumentType,
    Guid DocumentId,
    Guid? BranchId,
    string? BranchName,
    string RuleName,
    ApprovalStatus Status,
    DateTime RequestedAtUtc,
    string? RequestedBy,
    string? Comments);

public sealed record ApprovalRuleDto(
    Guid Id,
    string Name,
    ApprovalDocumentType DocumentType,
    Guid? BranchId,
    string? BranchName,
    decimal MinimumAmount,
    decimal? MaximumAmount,
    string? ApproverRoleName,
    Guid? ApproverUserId,
    bool IsActive);

public sealed class SaveApprovalRuleRequest
{
    public string Name { get; init; } = string.Empty;
    public ApprovalDocumentType DocumentType { get; init; }
    public Guid? BranchId { get; init; }
    public decimal MinimumAmount { get; init; }
    public decimal? MaximumAmount { get; init; }
    public string? ApproverRoleName { get; init; }
    public Guid? ApproverUserId { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class SaveApprovalRuleRequestValidator : AbstractValidator<SaveApprovalRuleRequest>
{
    public SaveApprovalRuleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.MinimumAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaximumAmount)
            .GreaterThanOrEqualTo(x => x.MinimumAmount)
            .When(x => x.MaximumAmount.HasValue);
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.ApproverRoleName) || x.ApproverUserId.HasValue)
            .WithMessage("An approver role or approver user is required.");
    }
}

public sealed class ApprovalService : IApprovalService
{
    private readonly IErpDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IClock _clock;
    private readonly IInvoicePostingService _invoicePostingService;
    private readonly IValidator<SaveApprovalRuleRequest> _validator;

    public ApprovalService(
        IErpDbContext dbContext,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IClock clock,
        IInvoicePostingService invoicePostingService,
        IValidator<SaveApprovalRuleRequest> validator)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _clock = clock;
        _invoicePostingService = invoicePostingService;
        _validator = validator;
    }

    public async Task<ApprovalSubmissionResult> CreateRequestsAsync(
        ApprovalDocumentType documentType,
        Guid documentId,
        Guid? branchId,
        decimal amount,
        CancellationToken cancellationToken)
    {
        _currentUserService.EnsureAuthenticated();

        var rules = await _dbContext.ApprovalRules
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.MinimumAmount)
            .ToListAsync(cancellationToken);

        var matchedRules = rules
            .Where(x => x.Matches(branchId, documentType, amount))
            .ToList();

        if (matchedRules.Count == 0)
        {
            return new ApprovalSubmissionResult(false, []);
        }

        var userId = _currentUserService.GetRequiredUserId();
        var requestIds = new List<Guid>();

        foreach (var rule in matchedRules)
        {
            var request = new ApprovalRequest(rule.Id, documentType, documentId, branchId, userId);
            request.SetCreationAudit(_clock.UtcNow, _currentUserService.User.UserName);
            _dbContext.ApprovalRequests.Add(request);
            requestIds.Add(request.Id);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new ApprovalSubmissionResult(true, requestIds);
    }

    public async Task<PagedResult<ApprovalRequestDto>> GetRequestsAsync(ApprovalRequestQuery request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Approvals.View);

        var query = _dbContext.ApprovalRequests
            .AsNoTracking()
            .Include(x => x.Rule)
            .ThenInclude(x => x!.Branch)
            .Where(x => !x.IsDeleted);

        if (request.DocumentType.HasValue)
        {
            query = query.Where(x => x.DocumentType == request.DocumentType.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        if (request.BranchId.HasValue)
        {
            _currentUserService.EnsureBranchAccess(request.BranchId.Value);
            query = query.Where(x => x.BranchId == request.BranchId);
        }
        else if (!_currentUserService.User.IsAdministrator && _currentUserService.User.BranchIds.Count > 0)
        {
            var branchIds = _currentUserService.User.BranchIds;
            query = query.Where(x => x.BranchId == null || branchIds.Contains(x.BranchId.Value));
        }

        query = query.OrderByDescending(x => x.RequestedAtUtc);

        return await query
            .Select(x => new ApprovalRequestDto(
                x.Id,
                x.DocumentType,
                x.DocumentId,
                x.BranchId,
                x.Rule!.Branch != null ? x.Rule.Branch.Name : null,
                x.Rule!.Name,
                x.Status,
                x.RequestedAtUtc,
                null,
                x.Comments))
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task ApproveAsync(Guid requestId, string? comments, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Approvals.Act);

        var request = await _dbContext.ApprovalRequests
            .Include(x => x.Rule)
            .SingleOrDefaultAsync(x => x.Id == requestId && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Approval request was not found.");

        EnsureCanReview(request);

        request.Approve(_currentUserService.GetRequiredUserId(), comments);
        request.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);

        await UpdateDocumentStatusAsync(request.DocumentType, request.DocumentId, request.BranchId, true, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(ApprovalRequest), request.Id.ToString(), "Approve", null, request, request.BranchId, cancellationToken);
    }

    public async Task RejectAsync(Guid requestId, string? comments, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Approvals.Act);

        var request = await _dbContext.ApprovalRequests
            .Include(x => x.Rule)
            .SingleOrDefaultAsync(x => x.Id == requestId && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Approval request was not found.");

        EnsureCanReview(request);

        request.Reject(_currentUserService.GetRequiredUserId(), comments);
        request.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);

        await UpdateDocumentStatusAsync(request.DocumentType, request.DocumentId, request.BranchId, false, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(ApprovalRequest), request.Id.ToString(), "Reject", null, request, request.BranchId, cancellationToken);
    }

    public async Task<PagedResult<ApprovalRuleDto>> GetRulesAsync(ListQuery request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Approvals.View);

        var query = _dbContext.ApprovalRules
            .AsNoTracking()
            .Include(x => x.Branch)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x => x.Name.ToLower().Contains(search));
        }

        query = query.OrderBy(x => x.DocumentType).ThenBy(x => x.MinimumAmount);

        return await query
            .Select(x => new ApprovalRuleDto(
                x.Id,
                x.Name,
                x.DocumentType,
                x.BranchId,
                x.Branch != null ? x.Branch.Name : null,
                x.MinimumAmount,
                x.MaximumAmount,
                x.ApproverRoleName,
                x.ApproverUserId,
                x.IsActive))
            .ToPagedResultAsync(request, cancellationToken);
    }

    public async Task<ApprovalRuleDto> GetRuleAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Approvals.View);

        return await _dbContext.ApprovalRules
            .AsNoTracking()
            .Include(x => x.Branch)
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new ApprovalRuleDto(
                x.Id,
                x.Name,
                x.DocumentType,
                x.BranchId,
                x.Branch != null ? x.Branch.Name : null,
                x.MinimumAmount,
                x.MaximumAmount,
                x.ApproverRoleName,
                x.ApproverUserId,
                x.IsActive))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Approval rule was not found.");
    }

    public async Task<Guid> CreateRuleAsync(SaveApprovalRuleRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Approvals.ManageRules);
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        if (request.BranchId.HasValue)
        {
            var branchExists = await _dbContext.Branches.AnyAsync(x => x.Id == request.BranchId && !x.IsDeleted, cancellationToken);
            if (!branchExists)
            {
                throw new NotFoundException("Branch was not found.");
            }
        }

        var entity = new ApprovalRule(
            request.Name,
            request.DocumentType,
            request.BranchId,
            request.MinimumAmount,
            request.MaximumAmount,
            request.ApproverRoleName,
            request.ApproverUserId);
        entity.Update(
            request.Name,
            request.BranchId,
            request.MinimumAmount,
            request.MaximumAmount,
            request.ApproverRoleName,
            request.ApproverUserId,
            request.IsActive);
        entity.SetCreationAudit(_clock.UtcNow, _currentUserService.User.UserName);

        _dbContext.ApprovalRules.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(ApprovalRule), entity.Id.ToString(), "Create", null, entity, request.BranchId, cancellationToken);
        return entity.Id;
    }

    public async Task UpdateRuleAsync(Guid id, SaveApprovalRuleRequest request, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Approvals.ManageRules);
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var entity = await _dbContext.ApprovalRules.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Approval rule was not found.");
        var before = await GetRuleAsync(id, cancellationToken);

        entity.Update(
            request.Name,
            request.BranchId,
            request.MinimumAmount,
            request.MaximumAmount,
            request.ApproverRoleName,
            request.ApproverUserId,
            request.IsActive);
        entity.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(ApprovalRule), entity.Id.ToString(), "Update", before, entity, request.BranchId, cancellationToken);
    }

    public async Task DeleteRuleAsync(Guid id, CancellationToken cancellationToken)
    {
        _currentUserService.EnsurePermission(PermissionCatalog.Approvals.ManageRules);
        var entity = await _dbContext.ApprovalRules.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("Approval rule was not found.");
        entity.SoftDelete(_clock.UtcNow, _currentUserService.User.UserName);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(nameof(ApprovalRule), entity.Id.ToString(), "Delete", entity, null, entity.BranchId, cancellationToken);
    }

    private void EnsureCanReview(ApprovalRequest request)
    {
        var currentUser = _currentUserService.User;
        if (request.BranchId.HasValue)
        {
            _currentUserService.EnsureBranchAccess(request.BranchId.Value);
        }

        var rule = request.Rule ?? throw new NotFoundException("Approval rule was not found.");
        var roleAllowed = string.IsNullOrWhiteSpace(rule.ApproverRoleName) || currentUser.Roles.Contains(rule.ApproverRoleName);
        var userAllowed = !rule.ApproverUserId.HasValue || currentUser.UserId == rule.ApproverUserId;

        if (!roleAllowed || !userAllowed)
        {
            throw new ForbiddenException("You are not authorized to review this approval request.");
        }
    }

    private async Task UpdateDocumentStatusAsync(
        ApprovalDocumentType documentType,
        Guid documentId,
        Guid? branchId,
        bool isApproved,
        CancellationToken cancellationToken)
    {
        var allRequests = await _dbContext.ApprovalRequests
            .Where(x => !x.IsDeleted && x.DocumentType == documentType && x.DocumentId == documentId)
            .ToListAsync(cancellationToken);

        if (!isApproved)
        {
            foreach (var pending in allRequests.Where(x => x.Status == ApprovalStatus.Pending))
            {
                pending.Reject(_currentUserService.GetRequiredUserId(), "Cascade rejection after a workflow rejection.");
                pending.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
            }

            await RejectDocumentAsync(documentType, documentId, branchId, cancellationToken);
            return;
        }

        if (allRequests.Any(x => x.Status == ApprovalStatus.Pending))
        {
            return;
        }

        await ApproveDocumentAsync(documentType, documentId, branchId, cancellationToken);
    }

    private async Task ApproveDocumentAsync(ApprovalDocumentType documentType, Guid documentId, Guid? branchId, CancellationToken cancellationToken)
    {
        switch (documentType)
        {
            case ApprovalDocumentType.PurchaseOrder:
                var purchaseOrder = await _dbContext.PurchaseOrders.SingleAsync(x => x.Id == documentId, cancellationToken);
                purchaseOrder.Approve();
                purchaseOrder.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
                await _auditService.LogAsync(nameof(PurchaseOrder), purchaseOrder.Id.ToString(), "Approve", null, purchaseOrder, branchId, cancellationToken);
                break;
            case ApprovalDocumentType.SalesOrder:
                var salesOrder = await _dbContext.SalesOrders.SingleAsync(x => x.Id == documentId, cancellationToken);
                salesOrder.Approve();
                salesOrder.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
                await _auditService.LogAsync(nameof(SalesOrder), salesOrder.Id.ToString(), "Approve", null, salesOrder, branchId, cancellationToken);
                break;
            case ApprovalDocumentType.PurchaseInvoice:
                await _invoicePostingService.PostPurchaseInvoiceAsync(documentId, cancellationToken);
                break;
            case ApprovalDocumentType.SalesInvoice:
                await _invoicePostingService.PostSalesInvoiceAsync(documentId, cancellationToken);
                break;
        }
    }

    private async Task RejectDocumentAsync(ApprovalDocumentType documentType, Guid documentId, Guid? branchId, CancellationToken cancellationToken)
    {
        switch (documentType)
        {
            case ApprovalDocumentType.PurchaseOrder:
                var purchaseOrder = await _dbContext.PurchaseOrders.SingleAsync(x => x.Id == documentId, cancellationToken);
                purchaseOrder.Reject();
                purchaseOrder.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
                await _auditService.LogAsync(nameof(PurchaseOrder), purchaseOrder.Id.ToString(), "Reject", null, purchaseOrder, branchId, cancellationToken);
                break;
            case ApprovalDocumentType.SalesOrder:
                var salesOrder = await _dbContext.SalesOrders.SingleAsync(x => x.Id == documentId, cancellationToken);
                salesOrder.Reject();
                salesOrder.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
                await _auditService.LogAsync(nameof(SalesOrder), salesOrder.Id.ToString(), "Reject", null, salesOrder, branchId, cancellationToken);
                break;
            case ApprovalDocumentType.PurchaseInvoice:
                var purchaseInvoice = await _dbContext.PurchaseInvoices.SingleAsync(x => x.Id == documentId, cancellationToken);
                purchaseInvoice.Reject();
                purchaseInvoice.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
                await _auditService.LogAsync(nameof(PurchaseInvoice), purchaseInvoice.Id.ToString(), "Reject", null, purchaseInvoice, branchId, cancellationToken);
                break;
            case ApprovalDocumentType.SalesInvoice:
                var salesInvoice = await _dbContext.SalesInvoices.SingleAsync(x => x.Id == documentId, cancellationToken);
                salesInvoice.Reject();
                salesInvoice.SetUpdateAudit(_clock.UtcNow, _currentUserService.User.UserName);
                await _auditService.LogAsync(nameof(SalesInvoice), salesInvoice.Id.ToString(), "Reject", null, salesInvoice, branchId, cancellationToken);
                break;
        }
    }
}
