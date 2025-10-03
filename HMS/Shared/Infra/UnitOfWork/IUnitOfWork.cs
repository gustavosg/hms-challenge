using Microsoft.EntityFrameworkCore;

namespace Shared.Infra.UnitOfWork;

public interface IUnitOfWork<TContext> : IDisposable where TContext : DbContext
{
    TContext Context { get; }

    Task<int> CommitAsync();
}