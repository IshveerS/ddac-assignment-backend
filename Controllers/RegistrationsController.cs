using DDACAssignment.Data;
using DDACAssignment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DDACAssignment.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationsController : ControllerBase
    {
        private readonly DDACDbContext _context;
        public RegistrationsController(DDACDbContext context) => _context = context;

        // POST: api/tournaments/{tournamentId}/register
        [HttpPost("/api/tournaments/{tournamentId}/register")]
        [Authorize]
        public async Task<ActionResult<Registration>> RegisterForTournament(Guid tournamentId, [FromBody] RegisterDto dto)
        {
            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament is null)
                return NotFound("Tournament not found");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                return Unauthorized("Invalid user");

            // Check if user already registered
            var existing = await _context.Registrations
                .FirstOrDefaultAsync(r => r.TournamentId == tournamentId && r.UserId == userGuid);
            
            if (existing != null)
                return BadRequest("You have already registered for this tournament");

            var registration = new Registration
            {
                TournamentId = tournamentId,
                UserId = userGuid,
                TeamName = dto.TeamName,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRegistration), new { id = registration.Id }, registration);
        }

        // GET: api/registrations/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Registration>> GetRegistration(Guid id)
        {
            var registration = await _context.Registrations
                .Include(r => r.Tournament)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (registration is null)
                return NotFound();

            return Ok(registration);
        }

        // GET: api/tournaments/{tournamentId}/registrations
        [HttpGet("/api/tournaments/{tournamentId}/registrations")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<ActionResult<IEnumerable<object>>> GetTournamentRegistrations(Guid tournamentId, [FromQuery] string? status = null)
        {
            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament is null)
                return NotFound("Tournament not found");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Organizers can only see their own tournaments
            if (userRole == "Organizer" && tournament.OrganizerId.ToString() != userId)
                return Forbid();

            var query = _context.Registrations
                .Include(r => r.User)
                .Include(r => r.Tournament)
                .Where(r => r.TournamentId == tournamentId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var registrations = await query
                .Select(r => new
                {
                    r.Id,
                    r.TeamName,
                    r.Status,
                    r.CreatedAt,
                    r.TournamentId,
                    TournamentName = r.Tournament.Name,
                    User = new
                    {
                        r.User.Id,
                        r.User.Username,
                        r.User.Email
                    }
                })
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            return Ok(registrations);
        }

        // POST: api/registrations/{id}/approve
        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<ActionResult> ApproveRegistration(Guid id)
        {
            var registration = await _context.Registrations
                .Include(r => r.Tournament)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (registration is null)
                return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Organizers can only approve for their own tournaments
            if (userRole == "Organizer" && registration.Tournament.OrganizerId.ToString() != userId)
                return Forbid();

            registration.Status = "Approved";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registration approved" });
        }

        // POST: api/registrations/{id}/reject
        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<ActionResult> RejectRegistration(Guid id)
        {
            var registration = await _context.Registrations
                .Include(r => r.Tournament)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (registration is null)
                return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Organizers can only reject for their own tournaments
            if (userRole == "Organizer" && registration.Tournament.OrganizerId.ToString() != userId)
                return Forbid();

            registration.Status = "Rejected";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Registration rejected" });
        }
    }

    public class RegisterDto
    {
        public string TeamName { get; set; } = null!;
    }
}
