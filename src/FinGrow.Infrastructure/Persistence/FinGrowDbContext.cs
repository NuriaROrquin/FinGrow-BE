namespace FinGrow.Infrastructure.Persistence;

using System.Reflection;
using FinGrow.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

public sealed class FinGrowDbContext : DbContext, IUnitOfWork
{
    public FinGrowDbContext(DbContextOptions<FinGrowDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }
}
