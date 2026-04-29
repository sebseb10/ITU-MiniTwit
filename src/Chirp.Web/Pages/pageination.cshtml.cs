using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Chirp.Core;
using Chirp.Infrastructure;
using Chirp.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chirp.Web.Pages;

public class PaginationModel : PageModel
{
    private readonly ICheepService _service;
    private readonly IAuthorService _authorService;

    public List<CheepDTO> Cheeps { get; set; } = new();
    public HashSet<string> FollowedUserIds { get; private set; } = new();
    public bool hasNextPage { get; set; }
    public int currentPage { get; set; }

    public PaginationModel(ICheepService service, IAuthorService authorService)
    {
        _service = service;
        _authorService = authorService;
    }

    private string? GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    public bool IsFollowing(string authorId) => FollowedUserIds.Contains(authorId);

    public async Task<IActionResult> OnGetAsync(int index = 1)
    {
        currentPage = index < 1 ? 1 : index;

        var currentUserId = GetCurrentUserId();

        if (currentUserId != null)
            FollowedUserIds = await _authorService.GetFollowedUserIds(currentUserId);

        Cheeps = await _service.GetCheeps(currentPage, currentUserId) ?? new List<CheepDTO>();
        hasNextPage = await _service.HasNextPageCheeps(currentPage, currentUserId);

        return Page();
    }

    public async Task<IActionResult> OnPostRecheepAsync(int cheepId)
    {
        if (User.Identity?.IsAuthenticated != true)
            return RedirectToPage();

        var currentUserId = GetCurrentUserId();
        if (string.IsNullOrEmpty(currentUserId))
            return RedirectToPage();

        await _service.CreateRecheep(new AuthorDTO { Id = currentUserId }, cheepId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int cheepId)
    {
        if (User.Identity?.IsAuthenticated != true)
            return RedirectToPage();

        var authorName = User.Identity?.Name;
        if (string.IsNullOrEmpty(authorName))
            return RedirectToPage();

        await _service.DeleteCheep(cheepId, authorName);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetFollowBtnAsync(string authorName)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return RedirectToPage();

        var currentUser = await _authorService.GetAuthorEntityByName(username);
        var followTarget = await _authorService.GetAuthorEntityByName(authorName);

        if (currentUser == null || followTarget == null ||
            currentUser.Id == followTarget.Id)
            return RedirectToPage();

        bool alreadyFollow =
            currentUser.Following.Any(f => f.FollowedById == followTarget.Id);

        if (!alreadyFollow)
        {
            currentUser.Following.Add(new Follows
            {
                FollowsId = currentUser.Id,
                FollowedById = followTarget.Id
            });
        }
        else
        {
            currentUser.Following.RemoveAll(
                f => f.FollowedById == followTarget.Id);
        }

        await _authorService.SaveChangesAsync();
        return RedirectToPage();
    }
}
