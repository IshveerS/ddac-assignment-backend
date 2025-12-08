using DDACAssignment.Data;
using DDACAssignment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDACAssignment.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly DDACDbContext _context;
        public AdminController(DDACDbContext context) => _context = context;

        // Pending means default role "User" (not yet promoted)
        [HttpGet("accounts/pending")]
        public async Task<ActionResult<IEnumerable<object>>> GetPendingAccounts()
        {
            var users = await _context.Users
                .Where(u => u.Role == "User")
                .Select(u => new { u.Id, u.Username, u.Email, u.Role })
                .ToListAsync();
            return Ok(users);
        }

        [HttpPost("accounts/{id}/approve")]
        public async Task<IActionResult> ApproveAccount(Guid id, [FromBody] RoleRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user is null) return NotFound();

            user.Role = NormalizeRole(request.Role ?? "Player");
            await _context.SaveChangesAsync();
            return Ok(new { message = "approved", user.Id, user.Role });
        }

        [HttpPost("accounts/{id}/reject")]
        public async Task<IActionResult> RejectAccount(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user is null) return NotFound();

            user.Role = "Rejected";
            await _context.SaveChangesAsync();
            return Ok(new { message = "rejected", user.Id, user.Role });
        }

        [HttpPost("accounts/{id}/role")]
        public async Task<IActionResult> ChangeRole(Guid id, [FromBody] RoleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Role)) return BadRequest("Role is required");

            var user = await _context.Users.FindAsync(id);
            if (user is null) return NotFound();

            user.Role = NormalizeRole(request.Role);
            await _context.SaveChangesAsync();
            return Ok(new { message = "role updated", user.Id, user.Role });
        }

        private static string NormalizeRole(string role)
        {
            return role.Equals("organizer", StringComparison.OrdinalIgnoreCase)
                ? "Organizer"
                : role.Equals("admin", StringComparison.OrdinalIgnoreCase)
                    ? "Admin"
                    : role.Equals("rejected", StringComparison.OrdinalIgnoreCase)
                        ? "Rejected"
                        : "Player";
        }

        public class RoleRequest
        {
            public string? Role { get; set; }
        }
    }
}
