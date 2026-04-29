using Chirp.Core;
using Chirp.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Chirp.Infrastructure;

public class AuthorService : IAuthorService
{
    private readonly IAuthorRepository _repository;
    private readonly CheepDbContext _context;

    public AuthorService(IAuthorRepository repository, CheepDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public Task<AuthorDTO> GetAuthorByName(string name)
    {
        return _repository.GetAuthorByName(name);
    }
    public Task<AuthorDTO> GetAuthorByEmail(string email)
    {
        return _repository.GetAuthorByEmail(email);
    }


    public Task<Author?> GetAuthorEntityByName(string name)
    {
        // Only load the Following join table rows (IDs), not the full FollowedByAuthor entities
        return _context.Authors
            .Include(a => a.Following)
            .FirstOrDefaultAsync(a => a.UserName == name);
    }

    public async Task<HashSet<string>> GetFollowedUserIds(string userId)
    {
        var ids = await _context.Follows
            .Where(f => f.FollowsId == userId)
            .Select(f => f.FollowedById)
            .AsNoTracking()
            .ToListAsync();
        return ids.ToHashSet();
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }

}