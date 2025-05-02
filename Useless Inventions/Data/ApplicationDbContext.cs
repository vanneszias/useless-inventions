using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Useless_Inventions.Models;

namespace Useless_Inventions.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        /// <summary>
        /// Initializes a new instance of the ApplicationDbContext with the specified options.
        /// </summary>
        /// <param name="options">The options to be used by the database context.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Invention> Inventions { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Like> Likes { get; set; }

        /// <summary>
        /// Configures the entity relationships and constraints for the application's data model, including cascade delete behaviors and unique indexes.
        /// </summary>
        /// <param name="builder">The model builder used to configure entity mappings.</param>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure relationships and constraints
            // Ensure that the UserId in the Invention table is not nullable
            builder.Entity<Invention>()
                .HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure that the InventionId in the Comment table is not nullable
            builder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Ensure that the InventionId in the Comment table is not nullable
            builder.Entity<Comment>()
                .HasOne(c => c.Invention)
                .WithMany(i => i.Comments)
                .HasForeignKey(c => c.InventionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure that the UserId in the Comment table is not nullable
            builder.Entity<Like>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Ensure that the UserId in the Like table is not nullable
            builder.Entity<Like>()
                .HasOne(l => l.Invention)
                .WithMany(i => i.Likes)
                .HasForeignKey(l => l.InventionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Make sure that a user can only like an invention once
            builder.Entity<Like>()  
                .HasIndex(l => new { l.UserId, l.InventionId })  
                .IsUnique();  
        }
    }
}
