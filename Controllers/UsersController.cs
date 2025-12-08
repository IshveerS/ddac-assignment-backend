using DDACAssignment.Data;
using DDACAssignment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDACAssignment.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {

        private readonly DDACDbContext _context;
        public UsersController(DDACDbContext context) => _context = context;

        [HttpGet("me")]
        public string Me()
        {
            return "me";
        }


        [HttpPost("apply/organizer")]
        public string ApplyOrganizer()
        {
            return "apply organizer";
        }

        [HttpPost("apply/personnel")]
        public string ApplyPersonnel()
        {
            return "apply personnel";
        }

        [HttpPost("apply/player")]
        public string ApplyPlayer()
        {
            return "apply player";
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<object>>> GetPendingUsers()
        {
            // Return all users with Role = "User" (base role, awaiting approval or role assignment)
            var users = await _context.Users
                .Where(u => u.Role == "User")
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user is null)
                return NotFound("User not found");

            // Mark as approved (for now just return success; you can add IsApproved field later)
            return Ok(new { message = "User approved", userId = id });
        }

        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user is null)
                return NotFound("User not found");

            // Remove the user
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User rejected and removed", userId = id });
        }
    }
}
