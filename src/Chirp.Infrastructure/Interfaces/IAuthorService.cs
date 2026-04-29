using Chirp.Core;

namespace Chirp.Infrastructure.Interfaces;

public interface IAuthorService
{
    Task<AuthorDTO> GetAuthorByName(string name);
    Task SaveChangesAsync();
    Task<AuthorDTO> GetAuthorByEmail(string email);
    Task<Author?> GetAuthorEntityByName(string name);
    Task<HashSet<string>> GetFollowedUserIds(string userId);
}