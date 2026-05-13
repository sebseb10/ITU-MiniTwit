using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Chirp.Core
{
    public class Cheep
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-increment primary key
        public int CheepID { get; set; }

        [Required]
        [StringLength(160)]
        
        public required string Text { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        public Author? Author { get; set; }

        [ForeignKey(nameof(Author))]
        public string? AuthorID { get; set; }
    }
}