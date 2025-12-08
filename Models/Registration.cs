using System.ComponentModel.DataAnnotations;

namespace DDACAssignment.Models
{
    public class Registration
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TournamentId { get; set; }
        public Guid? TeamId { get; set; }
        public Guid UserId { get; set; }
        public string TeamName { get; set; } = null!;
        public string Status { get; set; } = "Pending"; // Pending | Approved | Rejected
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Tournament Tournament { get; set; } = null!;
        public Team? Team { get; set; }
        public User User { get; set; } = null!;
    }
}
