using System.ComponentModel.DataAnnotations;

namespace DDACAssignment.Dtos.Tournaments
{
    public class CreateTournamentDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = "single_elim";

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
