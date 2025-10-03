using Microsoft.EntityFrameworkCore;

namespace Shared.Infra.UnitOfWork;

public class UnitOfWork<TContext>(TContext context) 
    : IUnitOfWork<TContext> where TContext : DbContext
{
    public TContext Context => context;

    public async Task<int> CommitAsync()
        => await context.SaveChangesAsync();

    public void Dispose()
        => context.Dispose();
}
