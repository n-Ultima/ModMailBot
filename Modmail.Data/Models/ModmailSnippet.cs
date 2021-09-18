using System.ComponentModel.DataAnnotations.Schema;

namespace Modmail.Data.Models
{
    public class ModmailSnippet
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string Name { get; set; }
        
        public string Content { get; set; }
    }
}