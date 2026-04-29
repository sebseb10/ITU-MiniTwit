using System.ComponentModel.DataAnnotations;
using Chirp.Core;
using Chirp.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Chirp.Infrastructure;


public class CheepRepository : ICheepRepository
{
    private readonly CheepDbContext _dbContext;
    private const int PageSize = 32;

    public CheepRepository(CheepDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> CreateCheep(CheepDTO cheep)
    {
        var author = await _dbContext.Authors
            .FirstOrDefaultAsync(a => a.UserName == cheep.AuthorName);

        if (author == null)
            throw new InvalidOperationException("No such author: " + cheep.AuthorName);

        if (cheep.Text.Length > 160)
            throw new ValidationException("Cheep text too long: " + cheep.Text);

        Cheep newCheep = new Cheep
        {
            CheepID = cheep.CheepID,
            Text = cheep.Text,
            Author = author,
            AuthorID = author.Id,
            Timestamp = cheep.Timestamp == default ? DateTime.UtcNow : cheep.Timestamp
        };

        var queryResult = await _dbContext.Cheeps.AddAsync(newCheep);
        await _dbContext.SaveChangesAsync();
        return queryResult.Entity.CheepID;
    }

    public async Task<int> UpdateCheep(CheepDTO alteredMessage)
    {
        var existingCheep = await _dbContext.Cheeps.FindAsync(alteredMessage.CheepID);
        if (existingCheep != null)
        {
            existingCheep.Text = alteredMessage.Text;
            existingCheep.Timestamp = alteredMessage.Timestamp;
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            throw new Exception($"Message with ID {alteredMessage.CheepID} not found.");
        }
        return alteredMessage.CheepID;
    }

    public async Task<bool> DeleteCheep(int cheepId, string authorName)
    {
        var cheep = await _dbContext.Cheeps
            .Include(a => a.Author)
            .FirstOrDefaultAsync(a => a.CheepID == cheepId && a.Author!.UserName == authorName);
        if (cheep == null)
            return false;

        _dbContext.Cheeps.Remove(cheep);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<List<CheepDTO>> GetCheeps(int page)
    {
        return await _dbContext.Cheeps
            .AsNoTracking()
            .OrderByDescending(c => c.Timestamp)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(c => new CheepDTO
            {
                CheepID = c.CheepID,
                Text = c.Text,
                AuthorName = c.Author!.UserName ?? string.Empty,
                AuthorId = c.AuthorID ?? string.Empty,
                ProfilePicturePath = c.Author.ProfilePicturePath,
                Timestamp = c.Timestamp
            })
            .ToListAsync();
    }

    public async Task<List<CheepDTO>> GetCheeps(int page, string? currentUserId)
    {
        if (string.IsNullOrEmpty(currentUserId))
        {
            return await _dbContext.Cheeps
                .AsNoTracking()
                .OrderByDescending(c => c.Timestamp)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(c => new CheepDTO
                {
                    CheepID = c.CheepID,
                    Text = c.Text,
                    AuthorName = c.Author!.UserName ?? string.Empty,
                    AuthorId = c.AuthorID ?? string.Empty,
                    ProfilePicturePath = c.Author.ProfilePicturePath,
                    Timestamp = c.Timestamp,
                    IsRecheepedByCurrentUser = false
                })
                .ToListAsync();
        }

        var recheepedIdsQuery = _dbContext.Recheeps
            .Where(r => r.AuthorID == currentUserId)
            .Select(r => r.CheepID);

        return await _dbContext.Cheeps
            .AsNoTracking()
            .OrderByDescending(c => c.Timestamp)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(c => new CheepDTO
            {
                CheepID = c.CheepID,
                Text = c.Text,
                AuthorName = c.Author!.UserName ?? string.Empty,
                AuthorId = c.AuthorID ?? string.Empty,
                ProfilePicturePath = c.Author.ProfilePicturePath,
                Timestamp = c.Timestamp,
                IsRecheepedByCurrentUser = recheepedIdsQuery.Contains(c.CheepID)
            })
            .ToListAsync();
    }

    public Task<bool> HasNextPageCheeps(int currentPage, string? currentUserId = null)
    {
        return _dbContext.Cheeps
            .OrderByDescending(c => c.Timestamp)
            .Skip(currentPage * PageSize)
            .AnyAsync();
    }

    public async Task<List<CheepDTO>> GetCheepsFromAuthor(string author, int page)
    {
        var authorId = await _dbContext.Authors
            .Where(a => a.UserName == author)
            .Select(a => a.Id)
            .FirstOrDefaultAsync();

        if (authorId == null)
            return new List<CheepDTO>();

        var authored = _dbContext.Cheeps
            .Where(c => c.AuthorID == authorId)
            .Select(c => new CheepDTO
            {
                Text = c.Text,
                AuthorName = c.Author!.UserName ?? string.Empty,
                AuthorId = c.AuthorID ?? string.Empty,
                Timestamp = c.Timestamp,
                ProfilePicturePath = c.Author.ProfilePicturePath ?? "/images/default.png"
            });

        var recheeped = _dbContext.Recheeps
            .Where(r => r.AuthorID == authorId)
            .Join(
                _dbContext.Cheeps,
                r => r.CheepID,
                c => c.CheepID,
                (r, c) => new CheepDTO
                {
                    Text = c.Text,
                    AuthorName = c.Author!.UserName ?? string.Empty,
                    AuthorId = c.AuthorID ?? string.Empty,
                    Timestamp = c.Timestamp,
                    ProfilePicturePath = c.Author.ProfilePicturePath ?? "/images/default.png"
                });

        return await authored
            .Concat(recheeped)
            .OrderByDescending(c => c.Timestamp)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    public async Task<bool> HasNextPageFromAuthor(string author, int currentPage)
    {
        var authorId = await _dbContext.Authors
            .Where(a => a.UserName == author)
            .Select(a => a.Id)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (authorId == null)
            return false;

        var authored = _dbContext.Cheeps
            .Where(c => c.AuthorID == authorId)
            .Select(c => c.Timestamp);

        var recheeped = _dbContext.Recheeps
            .Where(r => r.AuthorID == authorId)
            .Join(_dbContext.Cheeps, r => r.CheepID, c => c.CheepID, (r, c) => c.Timestamp);

        return await authored
            .Concat(recheeped)
            .OrderByDescending(t => t)
            .Skip(currentPage * PageSize)
            .AnyAsync();
    }

    // Keep followedIds as a subquery (IQueryable) — stays in SQL, never materialised to memory
    private IQueryable<CheepDTO> FollowedAuthorCheepsQuery(string userId)
    {
        var followedIdsQuery = _dbContext.Follows
            .Where(f => f.FollowsId == userId)
            .Select(f => f.FollowedById);

        var authoredCheeps = _dbContext.Cheeps
            .Where(c => c.AuthorID == userId || followedIdsQuery.Contains(c.AuthorID))
            .Select(c => new CheepDTO
            {
                CheepID = c.CheepID,
                Text = c.Text,
                AuthorName = c.Author!.UserName ?? string.Empty,
                AuthorId = c.AuthorID ?? string.Empty,
                ProfilePicturePath = c.Author.ProfilePicturePath,
                Timestamp = c.Timestamp
            });

        var recheepedCheeps = _dbContext.Recheeps
            .Where(r => r.AuthorID == userId || followedIdsQuery.Contains(r.AuthorID))
            .Join(
                _dbContext.Cheeps,
                r => r.CheepID,
                c => c.CheepID,
                (r, c) => new CheepDTO
                {
                    CheepID = c.CheepID,
                    Text = c.Text,
                    AuthorName = c.Author!.UserName ?? string.Empty,
                    AuthorId = c.AuthorID ?? string.Empty,
                    ProfilePicturePath = c.Author.ProfilePicturePath,
                    Timestamp = c.Timestamp
                });

        return authoredCheeps
            .Concat(recheepedCheeps)
            .OrderByDescending(c => c.Timestamp);
    }

    public Task<List<CheepDTO>> GetCheepsFromFollowedAuthor(string userId, int page)
    {
        return FollowedAuthorCheepsQuery(userId)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    public Task<bool> HasNextPageFromFollowedAuthor(string userId, int currentPage)
    {
        return FollowedAuthorCheepsQuery(userId)
            .Skip(currentPage * PageSize)
            .AnyAsync();
    }

    public async Task<int> CreateRecheep(AuthorDTO? author, int cheepId)
    {
        if (author == null)
            throw new InvalidOperationException("No such author");

        var existing = await _dbContext.Recheeps
            .FirstOrDefaultAsync(r => r.AuthorID == author.Id && r.CheepID == cheepId);

        if (existing != null)
        {
            _dbContext.Recheeps.Remove(existing);
            await _dbContext.SaveChangesAsync();
            return cheepId;
        }

        var newRecheep = new Recheep
        {
            AuthorID = author.Id,
            CheepID = cheepId
        };

        await _dbContext.Recheeps.AddAsync(newRecheep);
        await _dbContext.SaveChangesAsync();
        return cheepId;
    }
}
