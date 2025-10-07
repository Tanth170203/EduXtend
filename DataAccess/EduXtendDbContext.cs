using BusinessObject.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class EduXtendContext : DbContext
{
    public EduXtendContext(DbContextOptions<EduXtendContext> options) : base(options) { }

    // ==== DbSet khai báo bảng ====
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<UserToken> UserTokens { get; set; }

    public DbSet<Semester> Semesters { get; set; }
    public DbSet<Major> Majors { get; set; }
    public DbSet<Student> Students { get; set; }

    // Club related
    public DbSet<ClubCategory> ClubCategories { get; set; }
    public DbSet<Club> Clubs { get; set; }
    public DbSet<ClubDepartment> ClubDepartments { get; set; }
    public DbSet<ClubMember> ClubMembers { get; set; }
    public DbSet<JoinRequest> JoinRequests { get; set; }

    // Activity related
    public DbSet<Activity> Activities { get; set; }
    public DbSet<ActivityRegistration> ActivityRegistrations { get; set; }
    public DbSet<ActivityAttendance> ActivityAttendances { get; set; }
    public DbSet<ActivityFeedback> ActivityFeedbacks { get; set; }

    // Plan and Proposal
    public DbSet<Plan> Plans { get; set; }
    public DbSet<Proposal> Proposals { get; set; }
    public DbSet<ProposalVote> ProposalVotes { get; set; }

    // Payment
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    // Movement Criteria
    public DbSet<MovementCriterionGroup> MovementCriterionGroups { get; set; }
    public DbSet<MovementCriterion> MovementCriteria { get; set; }
    public DbSet<MovementRecord> MovementRecords { get; set; }
    public DbSet<MovementRecordDetail> MovementRecordDetails { get; set; }

    // Evidence and Awards
    public DbSet<Evidence> Evidences { get; set; }
    public DbSet<ClubAward> ClubAwards { get; set; }

    // News and Notifications
    public DbSet<ClubNews> ClubNews { get; set; }
    public DbSet<SystemNews> SystemNews { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ==== USER & AUTH ====
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<UserToken>()
            .HasOne(ut => ut.User)
            .WithMany(u => u.Tokens)
            .HasForeignKey(ut => ut.UserId);

        // ==== CLUB ====
        // ClubMember
        modelBuilder.Entity<ClubMember>()
            .HasIndex(cm => new { cm.ClubId, cm.UserId })
            .IsUnique();

        modelBuilder.Entity<ClubMember>()
            .HasOne(cm => cm.User)
            .WithMany(u => u.ClubMemberships)
            .HasForeignKey(cm => cm.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // JoinRequest
        modelBuilder.Entity<JoinRequest>()
            .HasIndex(jr => new { jr.ClubId, jr.UserId })
            .IsUnique();

        modelBuilder.Entity<JoinRequest>()
            .HasOne(jr => jr.User)
            .WithMany()
            .HasForeignKey(jr => jr.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==== ACTIVITY ====
        // Activity CreatedBy and ApprovedBy
        modelBuilder.Entity<Activity>()
            .HasOne(a => a.CreatedBy)
            .WithMany()
            .HasForeignKey(a => a.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Activity>()
            .HasOne(a => a.ApprovedBy)
            .WithMany()
            .HasForeignKey(a => a.ApprovedById)
            .OnDelete(DeleteBehavior.Restrict);

        // ActivityRegistration
        modelBuilder.Entity<ActivityRegistration>()
            .HasIndex(ar => new { ar.ActivityId, ar.UserId })
            .IsUnique();

        modelBuilder.Entity<ActivityRegistration>()
            .HasOne(ar => ar.User)
            .WithMany(u => u.ActivityRegistrations)
            .HasForeignKey(ar => ar.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ActivityAttendance
        modelBuilder.Entity<ActivityAttendance>()
            .HasIndex(aa => new { aa.ActivityId, aa.UserId })
            .IsUnique();

        modelBuilder.Entity<ActivityAttendance>()
            .HasOne(aa => aa.User)
            .WithMany(u => u.Attendances)
            .HasForeignKey(aa => aa.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ActivityFeedback
        modelBuilder.Entity<ActivityFeedback>()
            .HasOne(af => af.User)
            .WithMany()
            .HasForeignKey(af => af.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==== STUDENT ====
        // User ↔ Student 1-1
        modelBuilder.Entity<User>()
            .HasOne<Student>()
            .WithOne(s => s.User)
            .HasForeignKey<Student>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Student → Major
        modelBuilder.Entity<Student>()
            .HasOne(s => s.Major)
            .WithMany(m => m.Students)
            .HasForeignKey(s => s.MajorId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==== PLAN ====
        modelBuilder.Entity<Plan>()
            .HasOne(p => p.ApprovedBy)
            .WithMany()
            .HasForeignKey(p => p.ApprovedById)
            .OnDelete(DeleteBehavior.Restrict);

        // ==== PROPOSAL ====
        modelBuilder.Entity<Proposal>()
            .HasOne(p => p.CreatedBy)
            .WithMany()
            .HasForeignKey(p => p.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProposalVote>()
            .HasIndex(pv => new { pv.ProposalId, pv.UserId })
            .IsUnique();

        modelBuilder.Entity<ProposalVote>()
            .HasOne(pv => pv.User)
            .WithMany()
            .HasForeignKey(pv => pv.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==== MOVEMENT RECORD ====
        modelBuilder.Entity<MovementRecord>()
            .HasIndex(mr => new { mr.StudentId, mr.SemesterId })
            .IsUnique();

        modelBuilder.Entity<MovementRecordDetail>()
            .HasIndex(mrd => new { mrd.MovementRecordId, mrd.CriterionId })
            .IsUnique();

        // ==== EVIDENCE ====
        modelBuilder.Entity<Evidence>()
            .HasOne(e => e.ReviewedBy)
            .WithMany()
            .HasForeignKey(e => e.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);

        // ==== NOTIFICATION ====
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.CreatedBy)
            .WithMany()
            .HasForeignKey(n => n.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // ==== SYSTEM NEWS ====
        modelBuilder.Entity<SystemNews>()
            .HasOne(sn => sn.CreatedBy)
            .WithMany()
            .HasForeignKey(sn => sn.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // ==== PAYMENT TRANSACTION ====
        modelBuilder.Entity<PaymentTransaction>()
            .HasOne(pt => pt.CreatedBy)
            .WithMany()
            .HasForeignKey(pt => pt.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}