using BedayaGroup.Application.Common.Interfaces;
using BedayaGroup.Domain.Entities;
using BedayaGroup.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BedayaGroup.Application.Cash;

/// <summary>
/// Resolves the dedicated financial storage ("خزنة المشروع") that belongs to a project.
/// Project storages are <see cref="CashStorageType.Project"/> records linked to a project
/// and are the automatic destination/source for project money movements. Company-level
/// storages remain reserved for manual operations only.
/// </summary>
public static class ProjectCashStorage
{
    public const string NamePrefix = "خزينة مشروع: ";

    public static string BuildName(string projectName) => $"{NamePrefix}{projectName}";

    public static Task<CashStorage?> FindAsync(
        IApplicationDbContext context,
        int projectId,
        CancellationToken cancellationToken = default)
        => context.CashStorages
            .Include(cs => cs.CashTransactions)
            .FirstOrDefaultAsync(
                cs => cs.Type == CashStorageType.Project && cs.ProjectId == projectId,
                cancellationToken);

    public static async Task<CashStorage> GetOrCreateAsync(
        IApplicationDbContext context,
        Project project,
        CancellationToken cancellationToken = default)
    {
        var storage = await context.CashStorages
            .Include(cs => cs.CashTransactions)
            .FirstOrDefaultAsync(
                cs => cs.Type == CashStorageType.Project && cs.ProjectId == project.Id,
                cancellationToken);

        if (storage != null)
        {
            return storage;
        }

        storage = new CashStorage
        {
            Name = BuildName(project.Name),
            Type = CashStorageType.Project,
            ProjectId = project.Id,
            OpeningBalance = 0m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.CashStorages.Add(storage);
        await context.SaveChangesAsync(cancellationToken);

        return storage;
    }

    public static decimal ComputeBalance(CashStorage storage)
    {
        var totalIn = storage.CashTransactions
            .Where(t => t.Type == CashTransactionType.CashIn
                     || t.Type == CashTransactionType.OwnerDeposit
                     || t.Type == CashTransactionType.OtherIncome
                     || t.Type == CashTransactionType.ShareholderContribution)
            .Sum(t => t.Amount);

        var totalOut = storage.CashTransactions
            .Where(t => t.Type == CashTransactionType.CashOut
                     || t.Type == CashTransactionType.ExpensePayment
                     || t.Type == CashTransactionType.OtherExpense)
            .Sum(t => t.Amount);

        return storage.OpeningBalance + totalIn - totalOut;
    }
}
