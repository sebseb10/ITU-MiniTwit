using Chirp.Core;
using Chirp.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Chirp.Infrastructure;

public class AuthorRepository : IAuthorRepository
{
    private readonly CheepDbContext _dbContext;

    public AuthorRepository(CheepDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuthorDTO> GetAuthorByName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Name cannot be null or empty");

        var author = await _dbContext.Authors
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserName == name)
            ?? throw new InvalidOperationException("No such author with name: " + name);

        return new AuthorDTO { Id = author.Id, Name = author.UserName ?? string.Empty, Email = author.Email ?? string.Empty };
    }

    public async Task<AuthorDTO> GetAuthorByEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Email cannot be null or empty");

        var author = await _dbContext.Authors
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Email == email)
            ?? throw new InvalidOperationException("No such author with email: " + email);

        return new AuthorDTO { Id = author.Id, Name = author.UserName ?? string.Empty, Email = author.Email ?? string.Empty };
    }
}