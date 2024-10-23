using Comments_app.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Comments_app.Common.Data
{
    public class CommentsAppContext(DbContextOptions<CommentsAppContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Comment> Comments { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.UserName)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(u => u.Email)
                      .IsRequired()
                      .HasMaxLength(100);
            });

            modelBuilder.Entity<Comment>(entity =>
            {
                entity.Property(c => c.Text)
                      .IsRequired()
                      .HasMaxLength(500);

                entity.Property(c => c.Captcha)
                      .IsRequired()
                      .HasMaxLength(10);
            });

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(c => c.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<User>()
               .HasIndex(u => u.Email)
               .IsUnique();
            modelBuilder.Entity<Comment>()
                .HasIndex(c => c.CreatedAt);
            base.OnModelCreating(modelBuilder);
        }
    }
}
