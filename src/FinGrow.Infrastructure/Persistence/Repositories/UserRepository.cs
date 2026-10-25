namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class UserRepository : IUserRepository
{
    private readonly FinGrowDbContext _dbContext;

    public UserRepository(FinGrowDbContext dbContext) => _dbContext = dbContext;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        _dbContext.Users.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

    public void Remove(User user) => _dbContext.Users.Remove(user);
}
