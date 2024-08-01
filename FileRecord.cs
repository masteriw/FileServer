using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("filerecords", Schema = "public")]
public class FileRecord
{
    [Key]
    public int gid { get; set; }
    public string filename { get; set; }
    public string agency { get; set; } 
}
