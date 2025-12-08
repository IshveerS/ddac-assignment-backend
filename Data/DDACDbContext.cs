using DDACAssignment.Models;
using DDACAssignment.Models.Request;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace DDACAssignment.Data
{
    public class DDACDbContext : DbContext
    {
        public DDACDbContext(DbContextOptions<DDACDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();

        public DbSet<Player> Players => Set<Player>();
        public DbSet<Personnel> Personnels => Set<Personnel>();
        public DbSet<Organizer> Organizers => Set<Organizer>();

        public DbSet<Team> Teams => Set<Team>();
        public DbSet<Tournament> Tournaments => Set<Tournament>();
        public DbSet<Match> Matches => Set<Match>();
        public DbSet<Round> Rounds => Set<Round>();
        public DbSet<Schedule> Schedules => Set<Schedule>();

        public DbSet<TournamentResult> TournamentResults => Set<TournamentResult>();
        public DbSet<MatchResult> MatchResults => Set<MatchResult>();
        public DbSet<PlayerMatchStat> PlayerMatchStats => Set<PlayerMatchStat>();

        public DbSet<TeamStatistic> TeamStatistics => Set<TeamStatistic>();
        public DbSet<PlayerPerformance> PlayerPerformances => Set<PlayerPerformance>();

        public DbSet<Request> Requests => Set<Request>();
        public DbSet<PromoteRequest> PRequests => Set<PromoteRequest>();
        public DbSet<ApplyRequest> ARequests => Set<ApplyRequest>();

        public DbSet<Admin> Admins => Set<Admin>();
        public DbSet<Registration> Registrations => Set<Registration>();

        // Seed default admin and organizer accounts if they do not exist.
        public async Task SeedDefaultUsersAsync()
        {
            await UpsertUserAsync("admin", "admin@gmail.com", "Admin", "admin123");
            await UpsertUserAsync("organiser", "organiser@gmail.com", "Organizer", "organiser123");
        }

        private async Task UpsertUserAsync(string username, string email, string role, string password)
        {
            var user = await Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user is null)
            {
                user = new User { Username = username };
                Users.Add(user);
            }

            user.Email = email;
            user.Role = role;
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);

            await SaveChangesAsync();
        }
    }

}
