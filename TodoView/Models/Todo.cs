using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace TodoView.Models;

public class Todo
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "the length must be less than 100 characters")]
    public string Title { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "the length must be less than 100 characters")]
    public string Description { get; set; } = string.Empty;

    public bool IsDone { get; set; } = false;

    [Display(Name = "Reminder")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? ReminderAt { get; set; }

    public DateTime? ReminderTriggeredAt { get; set; }
    
    public string? HangfireJobId { get; set; }

    [Required]
    [ValidateNever]
    public string UserId { get; set; } = string.Empty;

    [ValidateNever]
    public User? User { get; set; } = default!;
}
