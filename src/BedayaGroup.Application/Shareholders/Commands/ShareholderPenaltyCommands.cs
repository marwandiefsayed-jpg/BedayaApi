using BedayaGroup.Application.Common.Exceptions;
using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Shareholders.DTOs;
using BedayaGroup.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Shareholders.Commands;

// =============================================
// Add Penalty Command (إضافة غرامة تأخر)
// =============================================

public record AddShareholderPenaltyCommand(AddShareholderPenaltyRequest Request)
    : IRequest<ApiResponse<ShareholderInstallmentPenaltyDto>>;

public class AddShareholderPenaltyCommandHandler
    : IRequestHandler<AddShareholderPenaltyCommand, ApiResponse<ShareholderInstallmentPenaltyDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddShareholderPenaltyCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<ShareholderInstallmentPenaltyDto>> Handle(
        AddShareholderPenaltyCommand request,
        CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (req.PenaltyAmount <= 0)
            return ApiResponse<ShareholderInstallmentPenaltyDto>.FailureResult("مبلغ الغرامة يجب أن يكون أكبر من صفر");

        var shareholder = await _context.Shareholders
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.ShareholderId, cancellationToken);

        if (shareholder == null)
            throw new NotFoundException("المساهم غير موجود");

        var installment = await _context.ProjectInstallments
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == req.ProjectInstallmentId, cancellationToken);

        if (installment == null)
            throw new NotFoundException("الدفعة غير موجودة");

        var currentUserId = _currentUserService.UserId ?? 1;

        var penalty = new ShareholderInstallmentPenalty
        {
            ShareholderId = req.ShareholderId,
            ProjectInstallmentId = req.ProjectInstallmentId,
            PenaltyAmount = req.PenaltyAmount,
            Notes = req.Notes,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ShareholderInstallmentPenalties.Add(penalty);
        await _context.SaveChangesAsync(cancellationToken);

        var user = await _context.Users.FindAsync(new object[] { currentUserId }, cancellationToken);

        var dto = new ShareholderInstallmentPenaltyDto(
            penalty.Id,
            shareholder.Id,
            shareholder.Name,
            installment.Id,
            installment.Name,
            penalty.PenaltyAmount,
            penalty.Notes,
            penalty.CreatedByUserId,
            user?.FullName ?? "",
            penalty.CreatedAt
        );

        return ApiResponse<ShareholderInstallmentPenaltyDto>.SuccessResult(dto, "تم إضافة الغرامة بنجاح");
    }
}

// =============================================
// Delete Penalty Command
// =============================================

public record DeleteShareholderPenaltyCommand(int PenaltyId)
    : IRequest<ApiResponse<bool>>;

public class DeleteShareholderPenaltyCommandHandler
    : IRequestHandler<DeleteShareholderPenaltyCommand, ApiResponse<bool>>
{
    private readonly IApplicationDbContext _context;

    public DeleteShareholderPenaltyCommandHandler(IApplicationDbContext context)
        => _context = context;

    public async Task<ApiResponse<bool>> Handle(
        DeleteShareholderPenaltyCommand request,
        CancellationToken cancellationToken)
    {
        var penalty = await _context.ShareholderInstallmentPenalties
            .FirstOrDefaultAsync(p => p.Id == request.PenaltyId, cancellationToken);

        if (penalty == null)
            throw new NotFoundException("الغرامة غير موجودة");

        _context.ShareholderInstallmentPenalties.Remove(penalty);
        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<bool>.SuccessResult(true, "تم حذف الغرامة بنجاح");
    }
}
