using DDACAssignment.Data;
using DDACAssignment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DDACAssignment.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TournamentsController : ControllerBase
    {
        private readonly DDACDbContext _context;
        public TournamentsController(DDACDbContext context) => _context = context;

        [HttpPost]
        [Authorize(Roles = "Organizer")]
        public async Task<ActionResult<Tournament>> CreateTournament([FromBody] CreateTournamentDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized("Invalid user");

            // Check if organizer profile exists
            var organizer = await _context.Organizers.FirstOrDefaultAsync(o => o.Id == userGuid);
            if (organizer is null)
                return BadRequest("Organizer profile not found. Please create an organizer profile first.");

            var tournament = new Tournament
            {
                Name = dto.Name,
                StartDate = dto.StartDate ?? DateTime.UtcNow,
                EndDate = dto.EndDate ?? DateTime.UtcNow.AddDays(7),
                Type = dto.Type ?? "Single Elimination",
                Status = "Draft",
                OrganizerId = userGuid
            };

            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTournament), new { id = tournament.Id }, tournament);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Tournament>> GetTournament(Guid id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Organizer)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament is null)
                return NotFound();

            return Ok(tournament);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetTournaments([FromQuery] Guid? organizerId)
        {
            var query = _context.Tournaments.Include(t => t.Organizer).AsQueryable();

            if (organizerId.HasValue)
            {
                query = query.Where(t => t.OrganizerId == organizerId.Value);
            }

            var tournaments = await query
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.StartDate,
                    t.EndDate,
                    t.Type,
                    t.Status,
                    t.OrganizerId,
                    OrganizerName = t.Organizer.OrganizationName
                })
                .ToListAsync();

            return Ok(tournaments);
        }
    }

    public class CreateTournamentDto
    {
        public string Name { get; set; } = null!;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Type { get; set; }
    }
}
