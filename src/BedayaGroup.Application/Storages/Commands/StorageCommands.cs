using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Application.Common.Models;
using BedayaGroup.Application.Storages.DTOs;
using BedayaGroup.Domain.Entities;
using BedayaGroup.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Storages.Commands;

public record CreateStorageCommand(CreateStorageRequest Request) : IRequest<ApiResponse<StorageDto>>;

public class CreateStorageCommandValidator : AbstractValidator<CreateStorageCommand>
{
    public CreateStorageCommandValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().WithMessage("اسم المخزن مطلوب");
    }
}

public class CreateStorageCommandHandler : IRequestHandler<CreateStorageCommand, ApiResponse<StorageDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CreateStorageCommandHandler(IApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<ApiResponse<StorageDto>> Handle(CreateStorageCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        if (req.Type == StorageType.Project && !req.ProjectId.HasValue)
        {
            return ApiResponse<StorageDto>.FailureResult("مخزن المشروع يجب أن يتبع مشروعاً محداً");
        }

        string? projectName = null;
        if (req.ProjectId.HasValue)
        {
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId.Value, cancellationToken);
            if (project == null) return ApiResponse<StorageDto>.FailureResult("المشروع المحدد غير موجود");
            projectName = project.Name;
        }

        var storage = new Storage
        {
            Name = req.Name,
            Type = req.Type,
            ProjectId = req.ProjectId,
            Location = req.Location,
            Description = req.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Storages.Add(storage);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("Create", "Storage", storage.Id.ToString(), null, new { storage.Name, storage.Type }, cancellationToken);

        var dto = new StorageDto(storage.Id, storage.Name, storage.Type, storage.ProjectId, projectName, storage.Location, storage.Description, storage.IsActive, storage.CreatedAt);
        return ApiResponse<StorageDto>.SuccessResult(dto, "تم إنشاء المخزن بنجاح");
    }
}

public record RecordStorageTransactionCommand(RecordStorageTransactionRequest Request) : IRequest<ApiResponse<StorageTransactionDto>>;

public class RecordStorageTransactionCommandValidator : AbstractValidator<RecordStorageTransactionCommand>
{
    public RecordStorageTransactionCommandValidator()
    {
        RuleFor(x => x.Request.StorageId).GreaterThan(0).WithMessage("معرف المخزن غير صحيح");
        RuleFor(x => x.Request.MaterialName).NotEmpty().WithMessage("اسم الصنف / المادة مطلوب");
        RuleFor(x => x.Request.Unit).NotEmpty().WithMessage("وحدة القياس مطلوبة");
        RuleFor(x => x.Request.Quantity).GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");
    }
}

public class RecordStorageTransactionCommandHandler : IRequestHandler<RecordStorageTransactionCommand, ApiResponse<StorageTransactionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public RecordStorageTransactionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IAuditService auditService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<ApiResponse<StorageTransactionDto>> Handle(RecordStorageTransactionCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        var storage = await _context.Storages.FirstOrDefaultAsync(s => s.Id == req.StorageId, cancellationToken);
        if (storage == null)
        {
            return ApiResponse<StorageTransactionDto>.FailureResult("المخزن المحدد غير موجود");
        }

        var currentUserId = _currentUserService.UserId ?? 1;

        var st = new StorageTransaction
        {
            StorageId = req.StorageId,
            ProjectId = req.ProjectId,
            TransactionDate = req.TransactionDate,
            MaterialName = req.MaterialName,
            Unit = req.Unit,
            Quantity = req.Quantity,
            Type = req.Type,
            ReferenceNumber = req.ReferenceNumber,
            Description = req.Description,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.StorageTransactions.Add(st);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("RecordMaterialMovement", "StorageTransaction", st.Id.ToString(), null, new { st.StorageId, st.MaterialName, st.Quantity, st.Type }, cancellationToken);

        var user = await _context.Users.FindAsync(new object[] { currentUserId }, cancellationToken);
        var project = req.ProjectId.HasValue ? await _context.Projects.FindAsync(new object[] { req.ProjectId.Value }, cancellationToken) : null;

        var dto = new StorageTransactionDto(
            st.Id,
            st.StorageId,
            storage.Name,
            st.ProjectId,
            project?.Name,
            st.TransactionDate,
            st.MaterialName,
            st.Unit,
            st.Quantity,
            st.Type,
            GetArabicStorageTransactionTypeName(st.Type),
            st.ReferenceNumber,
            st.Description,
            st.CreatedByUserId,
            user?.FullName ?? "",
            st.CreatedAt
        );

        return ApiResponse<StorageTransactionDto>.SuccessResult(dto, "تم تسجيل حركة المخزن بنجاح");
    }

    private static string GetArabicStorageTransactionTypeName(StorageTransactionType type) => type switch
    {
        StorageTransactionType.Purchase => "شراء",
        StorageTransactionType.TransferIn => "تحويل وارد",
        StorageTransactionType.TransferOut => "تحويل صادر",
        StorageTransactionType.IssueToProject => "صرف للمشروع",
        StorageTransactionType.Return => "مرتجع",
        StorageTransactionType.Adjustment => "تسوية",
        _ => type.ToString()
    };
}
