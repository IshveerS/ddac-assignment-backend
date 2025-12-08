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
    public class MatchesController : ControllerBase
    {
        private readonly DDACDbContext _context;
        public MatchesController(DDACDbContext context) => _context = context;

        [HttpPost]
        [Authorize(Roles = "Organizer")]
        public async Task<ActionResult<Match>> CreateMatch([FromBody] CreateMatchDto dto)
        {
            var tournament = await _context.Tournaments.FindAsync(dto.TournamentId);
            if (tournament is null)
                return NotFound("Tournament not found");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized("Invalid user");

            if (tournament.OrganizerId != userGuid)
                return Forbid("You don't have permission to create matches for this tournament");

            var match = new Match
            {
                TournamentId = dto.TournamentId,
                Status = "Scheduled",
                CreatedAt = DateTime.UtcNow
            };

            if (dto.ScheduledTime.HasValue)
            {
                match.Schedule = new Schedule
                {
                    StartTime = dto.ScheduledTime.Value,
                    Venue = dto.Location ?? "TBD"
                };
            }

            _context.Matches.Add(match);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMatch), new { id = match.Id }, match);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Match>> GetMatch(Guid id)
        {
            var match = await _context.Matches
                .Include(m => m.Schedule)
                .Include(m => m.Tournament)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match is null)
                return NotFound();

            return Ok(match);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetMatches([FromQuery] Guid? tournamentId)
        {
            var query = _context.Matches
                .Include(m => m.Schedule)
                .Include(m => m.Tournament)
                .AsQueryable();

            if (tournamentId.HasValue)
            {
                query = query.Where(m => m.TournamentId == tournamentId.Value);
            }

            var matches = await query
                .Select(m => new
                {
                    m.Id,
                    m.Status,
                    m.CreatedAt,
                    m.TournamentId,
                    TournamentName = m.Tournament.Name,
                    Schedule = m.Schedule != null ? new
                    {
                        m.Schedule.StartTime,
                        m.Schedule.Venue
                    } : null
                })
                .ToListAsync();

            return Ok(matches);
        }

        [HttpPatch("{id}")]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> UpdateMatchStatus(Guid id, [FromBody] UpdateMatchDto dto)
        {
            var match = await _context.Matches
                .Include(m => m.Tournament)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match is null)
                return NotFound("Match not found");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized("Invalid user");

            if (match.Tournament.OrganizerId != userGuid)
                return Forbid("You don't have permission to update this match");

            if (!string.IsNullOrEmpty(dto.Status))
            {
                match.Status = dto.Status;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Match updated", matchId = id, status = match.Status });
        }
    }

    public class CreateMatchDto
    {
        public Guid TournamentId { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public string? Location { get; set; }
    }

    public class UpdateMatchDto
    {
        public string? Status { get; set; }
    }
}
