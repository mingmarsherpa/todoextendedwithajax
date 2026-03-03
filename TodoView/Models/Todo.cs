using System.ComponentModel.DataAnnotations;

namespace TodoView.Models;

public class Todo
{
    [Key]
    public int Id { get; set; }
    [Required]
    [StringLength(100, ErrorMessage = "the length must be less than 100 characters")]
    public string Title { get; set; }=string.Empty;
    [Required]
    [StringLength(100, ErrorMessage = "the length must be less than 100 characters")]
    public string Description { get; set; }=string.Empty;
    public bool IsDone { get; set; }=false;
    [Required]
    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; } = default!;
}
