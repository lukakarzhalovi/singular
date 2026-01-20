using Microsoft.EntityFrameworkCore.Storage;
using VirtualRoulette.Common;
using VirtualRoulette.Common.Errors;

namespace VirtualRoulette.Persistence.Repositories;

public interface IUnitOfWork
{
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    void Dispose();
}

public sealed class UnitOfWork(AppDbContext context) : BaseRepository(context), IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception e)
        {
            return Result.Failure(DomainError.DbError.Error(nameof(SaveChangesAsync), e.Message));
        }
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await Context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
    }
}
