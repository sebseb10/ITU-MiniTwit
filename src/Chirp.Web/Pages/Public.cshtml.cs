using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Chirp.Core;
using Chirp.Infrastructure;
using Chirp.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chirp.Web.Pages;

public class PublicModel : PageModel
{
    private readonly ICheepService _service;
    private readonly IAuthorService _authorService;

    public List<CheepDTO> Cheeps { get; set; } = new();
    public HashSet<string> FollowedUserIds { get; private set; } = new();

    [BindProperty]
    [StringLength(160, ErrorMessage = "The {0} must be at max {1} characters long.")]
    public string Text { get; set; } = string.Empty;

    public PublicModel(ICheepService service, IAuthorService authorService)
    {
        _service = service;
        _authorService = authorService;
    }

    // User ID is stored in Claims — no DB call needed
    private string? GetCurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier);

    // O(1) in-memory check against the set loaded once per request
    public bool IsFollowing(string authorId) => FollowedUserIds.Contains(authorId);

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId != null)
            FollowedUserIds = await _authorService.GetFollowedUserIds(currentUserId);

        Cheeps = await _service.GetCheeps(1, currentUserId);
        return Page();
    }

    public async Task<IActionResult> OnPostRecheepAsync(int cheepId)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Redirect("/");

        var currentUserId = GetCurrentUserId();
        if (currentUserId == null)
            return Redirect("/");

        await _service.CreateRecheep(new AuthorDTO { Id = currentUserId }, cheepId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUserId = GetCurrentUserId();

        if (!ModelState.IsValid)
        {
            if (currentUserId != null)
                FollowedUserIds = await _authorService.GetFollowedUserIds(currentUserId);
            Cheeps = await _service.GetCheeps(1, currentUserId);
            return Page();
        }

        if (User.Identity?.IsAuthenticated != true)
            return Redirect("/");

        var authorName = User.Identity?.Name;
        if (string.IsNullOrEmpty(authorName))
            return Redirect("/");

        var newCheep = new CheepDTO
        {
            AuthorName = authorName,
            Text = Text,
            Timestamp = DateTime.UtcNow
        };

        await _service.CreateCheep(newCheep);
        return Redirect("/");
    }

    public async Task<IActionResult> OnPostDeleteAsync(int cheepId)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Redirect("/");

        var authorName = User.Identity?.Name;
        if (string.IsNullOrEmpty(authorName))
            return Redirect("/");

        await _service.DeleteCheep(cheepId, authorName);
        return Redirect("/");
    }

    public async Task<IActionResult> OnGetFollowBtnAsync(string authorName)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Redirect("/");

        var currentUser = await _authorService.GetAuthorEntityByName(username);
        var followTarget = await _authorService.GetAuthorEntityByName(authorName);

        if (currentUser == null || followTarget == null || currentUser.Id == followTarget.Id)
            return Redirect("/");

        bool alreadyFollow = currentUser.Following.Any(f => f.FollowedById == followTarget.Id);

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
            currentUser.Following.RemoveAll(f => f.FollowedById == followTarget.Id);
        }

        await _authorService.SaveChangesAsync();
        return RedirectToPage();
    }

    public bool LoginStatus()
    {
        return HttpContext.Session.GetString("user_id") != null;
    }
}
