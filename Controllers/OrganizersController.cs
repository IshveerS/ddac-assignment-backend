using System.Security.Claims;
using DDACAssignment.Data;
using DDACAssignment.Dtos.Tournaments;
using DDACAssignment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDACAssignment.Controllers
{
    [Route("api/organizers")]
    [ApiController]
    [Authorize(Roles = "Organizer")]
    public class OrganizersController : ControllerBase
    {
        private readonly DDACDbContext _context;
        public OrganizersController(DDACDbContext context) => _context = context;

        private Guid GetUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return id is null ? Guid.Empty : Guid.Parse(id);
        }

        [HttpPost("tournaments")]
        public async Task<ActionResult<Tournament>> CreateTournament([FromBody] CreateTournamentDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            var organizer = await _context.Organizers.FindAsync(userId);
            if (organizer is null)
            {
                organizer = new Organizer { Id = userId, OrganizationName = "Organizer" };
                _context.Organizers.Add(organizer);
            }

            var tournament = new Tournament
            {
                Name = dto.Name,
                Type = dto.Type,
                Status = "Draft",
                StartDate = dto.StartDate ?? DateTime.UtcNow,
                EndDate = dto.EndDate ?? DateTime.UtcNow.AddDays(7),
                OrganizerId = organizer.Id
            };

            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync();
            return Ok(tournament);
        }

        [HttpGet("tournaments")]
        public async Task<ActionResult<IEnumerable<Tournament>>> GetMyTournaments()
        {
            var userId = GetUserId();
            var list = await _context.Tournaments
                .Where(t => t.OrganizerId == userId)
                .ToListAsync();
            return Ok(list);
        }

        [HttpPost("tournaments/{tournamentId}/registrations")]
        public async Task<ActionResult<Registration>> RegisterTeam(Guid tournamentId, [FromBody] RegistrationRequest request)
        {
            var tournament = await _context.Tournaments.FindAsync(tournamentId);
            if (tournament is null) return NotFound("Tournament not found");

            var team = await _context.Teams.FindAsync(request.TeamId);
            if (team is null) return NotFound("Team not found");

            var reg = new Registration
            {
                TournamentId = tournamentId,
                TeamId = request.TeamId,
                Status = "Pending"
            };
            _context.Registrations.Add(reg);
            await _context.SaveChangesAsync();
            return Ok(reg);
        }

        [HttpGet("tournaments/{tournamentId}/registrations")]
        public async Task<ActionResult<IEnumerable<Registration>>> GetRegistrations(Guid tournamentId, [FromQuery] string? status)
        {
            var query = _context.Registrations.Where(r => r.TournamentId == tournamentId);
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }
            var list = await query.ToListAsync();
            return Ok(list);
        }

        [HttpPost("registrations/{registrationId}/approve")]
        public async Task<IActionResult> ApproveRegistration(Guid registrationId)
        {
            var reg = await _context.Registrations.FindAsync(registrationId);
            if (reg is null) return NotFound();
            reg.Status = "Approved";
            await _context.SaveChangesAsync();
            return Ok(new { message = "approved", reg.Id });
        }

        [HttpPost("registrations/{registrationId}/reject")]
        public async Task<IActionResult> RejectRegistration(Guid registrationId)
        {
            var reg = await _context.Registrations.FindAsync(registrationId);
            if (reg is null) return NotFound();
            reg.Status = "Rejected";
            await _context.SaveChangesAsync();
            return Ok(new { message = "rejected", reg.Id });
        }

        [HttpPost("matches/{matchId}/results")]
        public async Task<IActionResult> UpdateMatchResults(Guid matchId, [FromBody] MatchResultRequest request)
        {
            var match = await _context.Matches.FindAsync(matchId);
            if (match is null) return NotFound("Match not found");

            var result = new MatchResult
            {
                MatchId = matchId,
                TeamId = request.TeamId,
                Result = request.Result,
                Score = request.Score,
                Kills = request.Kills,
                Deaths = request.Deaths,
                Assists = request.Assists
            };
            _context.MatchResults.Add(result);
            await _context.SaveChangesAsync();
            return Ok(result);
        }

        public class RegistrationRequest
        {
            public Guid TeamId { get; set; }
        }

        public class MatchResultRequest
        {
            public Guid TeamId { get; set; }
            public string Result { get; set; } = string.Empty;
            public int Score { get; set; }
            public int Kills { get; set; }
            public int Deaths { get; set; }
            public int Assists { get; set; }
        }
    }
}
