using Chirp.Core;

namespace Chirp.Infrastructure;

public class AuthorDTO
{
    public string Id  { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    
    public ICollection<Cheep> Cheeps { get; set; } = new List<Cheep>();

    public List<Recheep> Recheeps { get; set; } = new();

    public List<Follows> Following { get; set; } = new();
}