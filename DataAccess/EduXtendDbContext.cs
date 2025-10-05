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

    public DbSet<AcademicYear> AcademicYears { get; set; }
    public DbSet<Semester> Semesters { get; set; }
    public DbSet<Faculty> Faculties { get; set; }
    public DbSet<Class> Classes { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Staff> Staffs { get; set; }

    public DbSet<Club> Clubs { get; set; }
    public DbSet<ClubMembership> ClubMemberships { get; set; }

    public DbSet<Activity> Activities { get; set; }
    public DbSet<ActivityApproval> ActivityApprovals { get; set; }
    public DbSet<ActivityRegistration> ActivityRegistrations { get; set; }
    public DbSet<ActivityAttendance> ActivityAttendances { get; set; }
    public DbSet<ActivityPointRule> ActivityPointRules { get; set; }
    public DbSet<ActivityPointTransaction> ActivityPointTransactions { get; set; }

    public DbSet<TrainingCriterion> TrainingCriteria { get; set; }
    public DbSet<TrainingEvaluationPeriod> TrainingEvaluationPeriods { get; set; }
    public DbSet<TrainingEvaluation> TrainingEvaluations { get; set; }
    public DbSet<TrainingEvaluationLine> TrainingEvaluationLines { get; set; }
    public DbSet<EvidenceDocument> EvidenceDocuments { get; set; }
    public DbSet<Appeal> Appeals { get; set; }
    public DbSet<EvaluationAuditLog> EvaluationAuditLogs { get; set; }

    public DbSet<News> News { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ==== USER & AUTH ====
        modelBuilder.Entity<User>()
    .HasOne(u => u.Student)
    .WithOne(s => s.User)
    .HasForeignKey<Student>(s => s.UserId)
    .OnDelete(DeleteBehavior.Cascade);

        // USER ↔ STAFF 1-1
        modelBuilder.Entity<User>()
            .HasOne(u => u.Staff)
            .WithOne(st => st.User)
            .HasForeignKey<Staff>(st => st.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<UserToken>()
            .HasOne(ut => ut.User)
            .WithMany(u => u.Tokens)
            .HasForeignKey(ut => ut.UserId);

        // ==== ACADEMIC ====
        modelBuilder.Entity<Class>()
            .HasOne(c => c.MonitorStudent)
            .WithMany()
            .HasForeignKey(c => c.MonitorStudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==== CLUB ====
        modelBuilder.Entity<ClubMembership>()
            .HasIndex(cm => new { cm.ClubId, cm.StudentId })
            .IsUnique();

        // ==== ACTIVITY ====
        modelBuilder.Entity<ActivityApproval>()
            .HasOne(a => a.Activity)
            .WithMany(ac => ac.Approvals)
            .HasForeignKey(a => a.ActivityId);

        modelBuilder.Entity<ActivityRegistration>()
            .HasIndex(ar => new { ar.ActivityId, ar.StudentId })
            .IsUnique();

        modelBuilder.Entity<ActivityAttendance>()
            .HasIndex(aa => new { aa.ActivityId, aa.StudentId })
            .IsUnique();

        // ==== TRAINING EVALUATION ====
        modelBuilder.Entity<TrainingCriterion>()
            .HasOne(tc => tc.Parent)
            .WithMany(p => p.Children)
            .HasForeignKey(tc => tc.ParentId);

        modelBuilder.Entity<TrainingEvaluationLine>()
            .HasIndex(tl => new { tl.EvaluationId, tl.CriterionId })
            .IsUnique();

        modelBuilder.Entity<EvidenceDocument>()
            .HasOne(ed => ed.Student)
            .WithMany()
            .HasForeignKey(ed => ed.StudentId);

        modelBuilder.Entity<ActivityPointTransaction>()
            .HasIndex(pt => new { pt.StudentId, pt.ActivityId, pt.CriterionId })
            .IsUnique(false); // cho phép nhiều transaction nếu cần

        // ==== NOTIFICATION ====
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId);


        // ==== NEWS ====
        modelBuilder.Entity<News>()
            .HasOne(n => n.AuthorUser)
            .WithMany()
            .HasForeignKey(n => n.AuthorUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Faculty → Class (cascade)
        modelBuilder.Entity<Class>()
            .HasOne(c => c.Faculty)
            .WithMany(f => f.Classes)
            .HasForeignKey(c => c.FacultyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Class → Student (cascade)
        modelBuilder.Entity<Student>()
            .HasOne(s => s.Class)
            .WithMany(c => c.Students)
            .HasForeignKey(s => s.ClassId)
            .OnDelete(DeleteBehavior.Cascade);

        // Student → Faculty (restrict để tránh multiple cascade path)
        modelBuilder.Entity<Student>()
            .HasOne(s => s.Faculty)
            .WithMany()
            .HasForeignKey(s => s.FacultyId)
            .OnDelete(DeleteBehavior.Restrict);
        // ==== TRAINING EVALUATION ====

        // TrainingEvaluation → Student
        modelBuilder.Entity<TrainingEvaluation>()
            .HasOne(te => te.Student)
            .WithMany(s => s.TrainingEvaluations)
            .HasForeignKey(te => te.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        // TrainingEvaluation → Period
        modelBuilder.Entity<TrainingEvaluation>()
            .HasOne(te => te.Period)
            .WithMany(p => p.Evaluations)
            .HasForeignKey(te => te.PeriodId)
            .OnDelete(DeleteBehavior.Cascade);

        // TrainingEvaluationLine → TrainingEvaluation
        modelBuilder.Entity<TrainingEvaluationLine>()
            .HasOne(tl => tl.Evaluation)
            .WithMany(te => te.Lines)
            .HasForeignKey(tl => tl.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);

        // TrainingEvaluationLine → TrainingCriterion
        modelBuilder.Entity<TrainingEvaluationLine>()
            .HasOne(tl => tl.Criterion)
            .WithMany()
            .HasForeignKey(tl => tl.CriterionId)
            .OnDelete(DeleteBehavior.Restrict); // không xoá criterion khi có line

        // Unique EvaluationId + CriterionId
        modelBuilder.Entity<TrainingEvaluationLine>()
            .HasIndex(tl => new { tl.EvaluationId, tl.CriterionId })
            .IsUnique();

        // EvidenceDocument → Student
        modelBuilder.Entity<EvidenceDocument>()
            .HasOne(ed => ed.Student)
            .WithMany()
            .HasForeignKey(ed => ed.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // EvidenceDocument → TrainingEvaluationLine
        modelBuilder.Entity<EvidenceDocument>()
            .HasOne(ed => ed.EvaluationLine)
            .WithMany(el => el.EvidenceDocuments)
            .HasForeignKey(ed => ed.EvaluationLineId)
            .OnDelete(DeleteBehavior.Restrict);



        modelBuilder.Entity<EvaluationAuditLog>()
            .HasOne(ea => ea.Evaluation)
            .WithMany(te => te.AuditLogs)
            .HasForeignKey(ea => ea.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade); // xóa Evaluation thì log đi theo

        modelBuilder.Entity<EvaluationAuditLog>()
            .HasOne(ea => ea.ChangedBy)
            .WithMany()
            .HasForeignKey(ea => ea.ChangedById)
            .OnDelete(DeleteBehavior.Restrict); // hoặc .SetNull nếu muốn cho phép null

        modelBuilder.Entity<ActivityPointTransaction>()
            .HasOne(pt => pt.Evaluation)
            .WithMany(te => te.ActivityPointTransactions)
            .HasForeignKey(pt => pt.EvaluationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appeal>()
            .HasOne(a => a.Evaluation)
            .WithMany(te => te.Appeals)
            .HasForeignKey(a => a.EvaluationId)
            .OnDelete(DeleteBehavior.Restrict); // Không cascade để tránh multiple paths


    }
}