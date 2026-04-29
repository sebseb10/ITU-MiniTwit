using Chirp.Infrastructure.Interfaces;
namespace Chirp.Infrastructure;

public class CheepService : ICheepService
{
    private readonly ICheepRepository _repository;

    public CheepService(ICheepRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<CheepDTO>> GetCheeps(int page)
    {
        return await _repository.GetCheeps(page);
    }

    public Task<int> CreateCheep(CheepDTO newMessage)
    {
        return _repository.CreateCheep(newMessage);
    }

    /* public Task<int> CreateAuthor(AuthorDTO newAuthor)
    {
        return _repository.CreateAuthor(newAuthor);
    }*/

    public Task<List<CheepDTO>> GetCheepsFromAuthor(string authorName, int page)
    {
        return _repository.GetCheepsFromAuthor(authorName, page);
    }

    public Task<int> UpdateCheep(CheepDTO alteredMessage)
    {
        return _repository.UpdateCheep(alteredMessage);
    }



    public Task<bool> DeleteCheep(int cheepId, string authorName)
    {
        return _repository.DeleteCheep(cheepId, authorName);
    }

    public Task<List<CheepDTO>> GetCheepsFromFollowedAuthor(string userId, int page)
    {
        return _repository.GetCheepsFromFollowedAuthor(userId, page);
    }
    public Task<List<CheepDTO>> GetCheeps(int page, string? currentUserId)
    {
        return _repository.GetCheeps(page, currentUserId);
    }

    public async Task<int> CreateRecheep(AuthorDTO Author, int cheepID)
    {
        return await _repository.CreateRecheep(Author, cheepID);
    }

    public Task<bool> HasNextPageCheeps(int currentPage, string? currentUserId = null)
        => _repository.HasNextPageCheeps(currentPage, currentUserId);

    public Task<bool> HasNextPageFromAuthor(string authorName, int currentPage)
        => _repository.HasNextPageFromAuthor(authorName, currentPage);

    public Task<bool> HasNextPageFromFollowedAuthor(string userId, int currentPage)
        => _repository.HasNextPageFromFollowedAuthor(userId, currentPage);
}