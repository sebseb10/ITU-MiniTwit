namespace Chirp.Infrastructure.Interfaces;

public interface ICheepRepository
{
    Task<int> CreateCheep(CheepDTO newMessage);
    Task<List<CheepDTO>> GetCheeps(int page);
    Task<List<CheepDTO>> GetCheeps(int page, string? currentUserId);
    Task<List<CheepDTO>> GetCheepsFromAuthor(string authorName, int page);
    Task<int> UpdateCheep(CheepDTO alteredMessage);
    Task<bool> DeleteCheep(int cheepId, string authorName);
    Task<List<CheepDTO>> GetCheepsFromFollowedAuthor(string userId, int page);
    Task<int> CreateRecheep(AuthorDTO Author, int cheepID);
    Task<bool> HasNextPageCheeps(int currentPage, string? currentUserId = null);
    Task<bool> HasNextPageFromAuthor(string authorName, int currentPage);
    Task<bool> HasNextPageFromFollowedAuthor(string userId, int currentPage);
}