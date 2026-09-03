using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Shareholders.DTOs;
using BedayaGroup.Domain.Entities;
using BedayaGroup.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Shareholders.Commands;

public record CreateShareholderCommand(CreateShareholderRequest Request) : IRequest<ApiResponse<ShareholderDto>>;

public class CreateShareholderCommandValidator : AbstractValidator<CreateShareholderCommand>
{
    public CreateShareholderCommandValidator()
    {
        RuleFor(x => x.Request.Code).NotEmpty().WithMessage("كود المساهم مطلوب");
        RuleFor(x => x.Request.Name).NotEmpty().WithMessage("اسم المساهم مطلوب");
        RuleFor(x => x.Request.RequiredContribution).GreaterThanOrEqualTo(0).WithMessage("المبلغ المطلوب لا يمكن أن يكون بالسالب");
    }
}

public class CreateShareholderCommandHandler : IRequestHandler<CreateShareholderCommand, ApiResponse<ShareholderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CreateShareholderCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<ShareholderDto>> Handle(CreateShareholderCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (await _context.Shareholders.AnyAsync(s => s.Code == req.Code, cancellationToken))
        {
            return ApiResponse<ShareholderDto>.FailureResult("كود المساهم مستخدم بالفعل");
        }

        var shareholder = new Shareholder
        {
            Code = req.Code,
            Name = req.Name,
            Phone = req.Phone,
            OwnershipPercentage = req.OwnershipPercentage,
            RequiredContribution = req.RequiredContribution,
            ProjectId = req.ProjectId,
            Notes = req.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Shareholders.Add(shareholder);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Create", "Shareholder", shareholder.Id.ToString(), null, new { shareholder.Code, shareholder.Name, shareholder.RequiredContribution, shareholder.ProjectId }, cancellationToken);

        string? projectName = null;
        if (shareholder.ProjectId.HasValue)
        {
            projectName = await _context.Projects.Where(p => p.Id == shareholder.ProjectId.Value).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken);
        }

        var dto = new ShareholderDto(shareholder.Id, shareholder.Code, shareholder.Name, shareholder.Phone, shareholder.OwnershipPercentage, shareholder.RequiredContribution, 0m, shareholder.RequiredContribution, shareholder.ProjectId, projectName, shareholder.Notes, shareholder.IsActive, shareholder.CreatedAt);
        return ApiResponse<ShareholderDto>.SuccessResult(dto, "تم إضافة المساهم بنجاح");
    }
}

public record UpdateShareholderCommand(int Id, UpdateShareholderRequest Request) : IRequest<ApiResponse<ShareholderDto>>;

public class UpdateShareholderCommandHandler : IRequestHandler<UpdateShareholderCommand, ApiResponse<ShareholderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public UpdateShareholderCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<ShareholderDto>> Handle(UpdateShareholderCommand request, CancellationToken cancellationToken)
    {
        var shareholder = await _context.Shareholders
            .Include(s => s.ShareholderContributions)
            .Include(s => s.Project)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (shareholder == null)
        {
            throw new NotFoundException("المساهم غير موجود");
        }

        var req = request.Request;
        var oldValues = new { shareholder.Name, shareholder.OwnershipPercentage, shareholder.RequiredContribution, shareholder.ProjectId };

        shareholder.Name = req.Name;
        shareholder.Phone = req.Phone;
        shareholder.OwnershipPercentage = req.OwnershipPercentage;
        shareholder.RequiredContribution = req.RequiredContribution;
        shareholder.ProjectId = req.ProjectId;
        shareholder.Notes = req.Notes;
        shareholder.IsActive = req.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Update", "Shareholder", shareholder.Id.ToString(), oldValues, new { shareholder.Name, shareholder.RequiredContribution, shareholder.ProjectId }, cancellationToken);

        var contributed = shareholder.ShareholderContributions.Sum(c => c.Amount);
        var remaining = shareholder.RequiredContribution - contributed;

        string? projectName = null;
        if (shareholder.ProjectId.HasValue)
        {
            projectName = await _context.Projects.Where(p => p.Id == shareholder.ProjectId.Value).Select(p => p.Name).FirstOrDefaultAsync(cancellationToken);
        }

        var dto = new ShareholderDto(shareholder.Id, shareholder.Code, shareholder.Name, shareholder.Phone, shareholder.OwnershipPercentage, shareholder.RequiredContribution, contributed, remaining, shareholder.ProjectId, projectName, shareholder.Notes, shareholder.IsActive, shareholder.CreatedAt);
        return ApiResponse<ShareholderDto>.SuccessResult(dto, "تم تحديث بيانات المساهم بنجاح");
    }
}

public record RecordShareholderContributionCommand(RecordShareholderContributionRequest Request) : IRequest<ApiResponse<ShareholderContributionDto>>;

public class RecordShareholderContributionCommandHandler : IRequestHandler<RecordShareholderContributionCommand, ApiResponse<ShareholderContributionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public RecordShareholderContributionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IAuditService auditService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<ApiResponse<ShareholderContributionDto>> Handle(RecordShareholderContributionCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (req.Amount <= 0)
        {
            return ApiResponse<ShareholderContributionDto>.FailureResult("مبلغ المساهمة يجب أن يكون أكبر من صفر");
        }

        var shareholder = await _context.Shareholders.FirstOrDefaultAsync(s => s.Id == req.ShareholderId, cancellationToken);
        if (shareholder == null)
        {
            throw new NotFoundException("المساهم غير موجود");
        }

        var storage = await _context.CashStorages.FirstOrDefaultAsync(cs => cs.Id == req.CashStorageId, cancellationToken);
        if (storage == null)
        {
            return ApiResponse<ShareholderContributionDto>.FailureResult("الخزينة المحددة غير موجودة");
        }

        var currentUserId = _currentUserService.UserId ?? 1;

        using var dbTransaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            var txNumber = $"SHR-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

            var cashTx = new CashTransaction
            {
                TransactionNumber = txNumber,
                TransactionDate = req.ContributionDate,
                Type = CashTransactionType.ShareholderContribution,
                Amount = req.Amount,
                CashStorageId = req.CashStorageId,
                ProjectId = req.ProjectId,
                Description = $"مساهمة من المساهم: {shareholder.Name} - {req.Description}",
                ReferenceNumber = req.ReferenceNumber,
                CreatedByUserId = currentUserId,
                Notes = req.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.CashTransactions.Add(cashTx);
            await _context.SaveChangesAsync(cancellationToken);

            var contribution = new ShareholderContribution
            {
                ShareholderId = req.ShareholderId,
                ProjectId = req.ProjectId,
                TransactionId = cashTx.Id,
                Amount = req.Amount,
                ContributionDate = req.ContributionDate,
                Description = req.Description,
                CreatedByUserId = currentUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ShareholderContributions.Add(contribution);
            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync("RecordContribution", "Shareholder", shareholder.Id.ToString(), null, new { contribution.Amount, contribution.ProjectId, cashTx.TransactionNumber }, cancellationToken);

            await dbTransaction.CommitAsync(cancellationToken);

            var user = await _context.Users.FindAsync(new object[] { currentUserId }, cancellationToken);
            var project = req.ProjectId.HasValue ? await _context.Projects.FindAsync(new object[] { req.ProjectId.Value }, cancellationToken) : null;

            var dto = new ShareholderContributionDto(
                contribution.Id,
                shareholder.Id,
                shareholder.Name,
                contribution.ProjectId,
                project?.Name,
                cashTx.Id,
                cashTx.TransactionNumber,
                contribution.Amount,
                contribution.ContributionDate,
                contribution.Description,
                contribution.CreatedByUserId,
                user?.FullName ?? "",
                contribution.CreatedAt
            );

            return ApiResponse<ShareholderContributionDto>.SuccessResult(dto, "تم تسجيل مساهمة المساهم بنجاح");
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
