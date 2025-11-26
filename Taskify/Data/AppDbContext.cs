using Microsoft.EntityFrameworkCore;
using Taskify.Models;
namespace Taskify.Data
{
    public class AppDbContext:DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // 1. Khai báo các bảng
        public DbSet<User> Users { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamMember> TeamMembers { get; set; }
        public DbSet<Board> Boards { get; set; }
        public DbSet<TaskList> TaskLists { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<TaskAssignment> TaskAssignments { get; set; }
        public DbSet<TaskHistory> TaskHistories { get; set; }
        public DbSet<TaskComment> TaskComments { get; set; } 
        public DbSet<Category> Categories { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // 2. Cấu hình quan hệ (Fluent API)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- USER & TEAM ---
            modelBuilder.Entity<TeamMember>()
                .HasOne(tm => tm.Team)
                .WithMany(t => t.Members)
                .HasForeignKey(tm => tm.TeamId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa Team -> Xóa Member

            modelBuilder.Entity<TeamMember>()
                .HasOne(tm => tm.User)
                .WithMany(u => u.TeamMembers) 
                .HasForeignKey(tm => tm.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Xóa User -> Không xóa Team

            // --- BOARD (Logic Cá nhân/Team) ---
            modelBuilder.Entity<Board>()
                .HasOne(b => b.Team)
                .WithMany(t => t.Boards)
                .HasForeignKey(b => b.TeamId)
                .IsRequired(false) // [QUAN TRỌNG] Cho phép Null
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Board>()
                .HasOne(b => b.Owner)
                .WithMany() // User tạo nhiều bảng
                .HasForeignKey(b => b.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- TASK & ASSIGNMENT ---
            modelBuilder.Entity<TaskAssignment>()
                .HasOne(ta => ta.Task)
                .WithMany(t => t.Assignments)
                .HasForeignKey(ta => ta.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaskAssignment>()
                .HasOne(ta => ta.User)
                .WithMany(u => u.TaskAssignments)
                .HasForeignKey(ta => ta.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- TASK & LIST ---
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.List)
                .WithMany(l => l.Tasks)
                .HasForeignKey(t => t.ListId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

}

