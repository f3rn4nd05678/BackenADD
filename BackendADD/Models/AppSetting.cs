using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BackendADD.Models;

[Table("app_settings")]
public class AppSetting
{
    [Key]
    [Column("k")]
    public string K { get; set; } = null!;

    [Column("v")]
    public string V { get; set; } = null!;
}