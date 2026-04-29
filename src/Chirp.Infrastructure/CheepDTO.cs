namespace Chirp.Infrastructure;

// The data transfer object allows us to, via our LINQ Queries,
// grab only the needed amount of data, for example, only "Text".
// This is done via not having "Required" fields
public class CheepDTO
{
    public int CheepID { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public string AuthorName { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string? ProfilePicturePath { get; set; }

    public bool IsRecheepedByCurrentUser { get; set; }


}

