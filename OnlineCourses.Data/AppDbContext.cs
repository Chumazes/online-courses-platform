using Microsoft.EntityFrameworkCore;
using OnlineCourses.Models.Entities;

namespace OnlineCourses.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<CourseTag> CourseTags { get; set; }
    public DbSet<Section> Sections { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Уникальные индексы
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        
        modelBuilder.Entity<Enrollment>()
            .HasIndex(e => new { e.UserId, e.CourseId })
            .IsUnique();
        
        modelBuilder.Entity<Review>()
            .HasIndex(r => new { r.UserId, r.CourseId })
            .IsUnique();
        
        // Composite primary key для CourseTag
        modelBuilder.Entity<CourseTag>()
            .HasKey(ct => new { ct.CourseId, ct.TagId });
        
        // Связи
        modelBuilder.Entity<Course>()
            .HasOne(c => c.Category)
            .WithMany(cat => cat.Courses)
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
        
        modelBuilder.Entity<Course>()
            .HasOne(c => c.Author)
            .WithMany(u => u.AuthoredCourses)
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}