using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ReMindHealth.Models;

namespace ReMindHealth.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ExtractedAppointment> ExtractedAppointments { get; set; }
    public DbSet<ExtractedTask> ExtractedTasks { get; set; }
    public DbSet<ExtractedNote> ExtractedNotes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Removing role tables
        modelBuilder.Ignore<Microsoft.AspNetCore.Identity.IdentityRole>();
        modelBuilder.Ignore<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>();
        modelBuilder.Ignore<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>();

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.ConversationId);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Conversations)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.RecordedAt);
            entity.HasIndex(e => e.ProcessingStatus);
            entity.HasIndex(e => e.UserId);

            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.AudioFormat).HasMaxLength(10);
            entity.Property(e => e.TranscriptionLanguage).HasMaxLength(10);
            entity.Property(e => e.ProcessingStatus).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<ExtractedAppointment>(entity =>
        {
            entity.HasKey(e => e.AppointmentId);
            entity.HasOne(e => e.Conversation)
                  .WithMany(c => c.ExtractedAppointments)
                  .HasForeignKey(e => e.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.AppointmentDateTime);
            entity.HasIndex(e => e.ConversationId);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Location).HasMaxLength(300);
            entity.Property(e => e.AttendeeNames).HasMaxLength(500);
            entity.Property(e => e.ConfidenceScore).HasPrecision(5, 4);
        });

        modelBuilder.Entity<ExtractedTask>(entity =>
        {
            entity.HasKey(e => e.TaskId);
            entity.HasOne(e => e.Conversation)
                  .WithMany(c => c.ExtractedTasks)
                  .HasForeignKey(e => e.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.DueDate);
            entity.HasIndex(e => e.IsCompleted);
            entity.HasIndex(e => e.ConversationId);
            entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Priority).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ConfidenceScore).HasPrecision(5, 4);
        });

        modelBuilder.Entity<ExtractedNote>(entity =>
        {
            entity.HasKey(e => e.NoteId);
            entity.HasOne(e => e.Conversation)
                  .WithMany(c => c.ExtractedNotes)
                  .HasForeignKey(e => e.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.NoteType);
            entity.HasIndex(e => e.IsPinned);
            entity.HasIndex(e => e.ConversationId);
            entity.Property(e => e.NoteType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.ConfidenceScore).HasPrecision(5, 4);
        });
    }
}