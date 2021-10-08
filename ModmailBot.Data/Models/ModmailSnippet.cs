using System.ComponentModel.DataAnnotations.Schema;

namespace ModmailBot.Data.Models
{
    public class ModmailSnippet
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Column(TypeName = "citext")]
        public string Name { get; set; }
        
        public string Content { get; set; }
    }
}