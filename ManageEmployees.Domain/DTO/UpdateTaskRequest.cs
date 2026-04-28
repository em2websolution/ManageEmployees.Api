using System.ComponentModel.DataAnnotations;

namespace ManageEmployees.Domain.DTO
{
    public class UpdateTaskRequest
    {
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; } = null!;

        [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = null!;

        [Required(ErrorMessage = "Due date is required.")]
        public DateTime DueDate { get; set; }
    }
}
