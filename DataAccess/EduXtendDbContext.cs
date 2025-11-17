using BusinessObject.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class EduXtendContext : DbContext
{
    public EduXtendContext(DbContextOptions<EduXtendContext> options) : base(options) { }

    // ==== DbSet khai báo bảng ====
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserToken> UserTokens { get; set; }
    public DbSet<LoggedOutToken> LoggedOutTokens { get; set; }

    public DbSet<Semester> Semesters { get; set; }
    public DbSet<Major> Majors { get; set; }
    public DbSet<Student> Students { get; set; }

    // Club related
    public DbSet<ClubCategory> ClubCategories { get; set; }
    public DbSet<Club> Clubs { get; set; }
    public DbSet<ClubDepartment> ClubDepartments { get; set; }
    public DbSet<ClubMember> ClubMembers { get; set; }
    public DbSet<JoinRequest> JoinRequests { get; set; }
    public DbSet<Interview> Interviews { get; set; }

    // Activity related
    public DbSet<Activity> Activities { get; set; }
    public DbSet<ActivityRegistration> ActivityRegistrations { get; set; }
    public DbSet<ActivityAttendance> ActivityAttendances { get; set; }
    public DbSet<ActivityFeedback> ActivityFeedbacks { get; set; }
    public DbSet<ActivitySchedule> ActivitySchedules { get; set; }
    public DbSet<ActivityScheduleAssignment> ActivityScheduleAssignments { get; set; }

    // Plan and Proposal
    public DbSet<Plan> Plans { get; set; }
    public DbSet<Proposal> Proposals { get; set; }
    public DbSet<ProposalVote> ProposalVotes { get; set; }

    // Payment and Fund Collection
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public DbSet<FundCollectionRequest> FundCollectionRequests { get; set; }
    public DbSet<FundCollectionPayment> FundCollectionPayments { get; set; }
    public DbSet<VnpayTransactionDetail> VnpayTransactionDetails { get; set; }

    // Movement Criteria
    public DbSet<MovementCriterionGroup> MovementCriterionGroups { get; set; }
    public DbSet<MovementCriterion> MovementCriteria { get; set; }
    public DbSet<MovementRecord> MovementRecords { get; set; }
    public DbSet<MovementRecordDetail> MovementRecordDetails { get; set; }
    
    // Club Movement Records
    public DbSet<ClubMovementRecord> ClubMovementRecords { get; set; }
    public DbSet<ClubMovementRecordDetail> ClubMovementRecordDetails { get; set; }

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

        // ==== CONFIGURE AUTO-INCREMENT IDs ====
        ConfigureIdentityColumns(modelBuilder);

        // ==== USER & AUTH ====
        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserToken>()
            .HasOne(ut => ut.User)
            .WithMany(u => u.UserTokens)
            .HasForeignKey(ut => ut.UserId);

        // LoggedOutToken
        modelBuilder.Entity<LoggedOutToken>()
            .HasIndex(lot => lot.TokenHash)
            .IsUnique();

        modelBuilder.Entity<LoggedOutToken>()
            .HasOne(lot => lot.User)
            .WithMany()
            .HasForeignKey(lot => lot.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==== CLUB ====
        // ClubMember
        modelBuilder.Entity<ClubMember>()
            .HasIndex(cm => new { cm.ClubId, cm.StudentId })
            .IsUnique();

        modelBuilder.Entity<ClubMember>()
            .HasOne(cm => cm.Student)
            .WithMany(u => u.ClubMembers)
            .HasForeignKey(cm => cm.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // JoinRequest
        modelBuilder.Entity<JoinRequest>()
            .HasOne(jr => jr.User)
            .WithMany()
            .HasForeignKey(jr => jr.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<JoinRequest>()
            .HasOne(jr => jr.ProcessedBy)
            .WithMany()
            .HasForeignKey(jr => jr.ProcessedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<JoinRequest>()
            .HasOne(jr => jr.Department)
            .WithMany()
            .HasForeignKey(jr => jr.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Interview
        modelBuilder.Entity<Interview>()
            .HasOne(i => i.JoinRequest)
            .WithMany()
            .HasForeignKey(i => i.JoinRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Interview>()
            .HasOne(i => i.CreatedBy)
            .WithMany()
            .HasForeignKey(i => i.CreatedById)
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

        modelBuilder.Entity<ActivityAttendance>()
            .HasOne(aa => aa.CheckedBy)
            .WithMany()
            .HasForeignKey(aa => aa.CheckedById)
            .OnDelete(DeleteBehavior.Restrict);

        // ActivityFeedback
        modelBuilder.Entity<ActivityFeedback>()
            .HasOne(af => af.User)
            .WithMany()
            .HasForeignKey(af => af.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==== ACTIVITY SCHEDULE ====
        // ActivitySchedule
        modelBuilder.Entity<ActivitySchedule>()
            .HasOne(s => s.Activity)
            .WithMany(a => a.Schedules)
            .HasForeignKey(s => s.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActivitySchedule>()
            .HasIndex(s => new { s.ActivityId, s.Order });

        // ActivityScheduleAssignment
        modelBuilder.Entity<ActivityScheduleAssignment>()
            .HasOne(a => a.ActivitySchedule)
            .WithMany(s => s.Assignments)
            .HasForeignKey(a => a.ActivityScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActivityScheduleAssignment>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

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

        modelBuilder.Entity<MovementRecord>()
            .HasOne(mr => mr.Student)
            .WithMany(s => s.MovementRecords)
            .HasForeignKey(mr => mr.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MovementRecord>()
            .HasOne(mr => mr.Semester)
            .WithMany(s => s.MovementRecords)
            .HasForeignKey(mr => mr.SemesterId)
            .OnDelete(DeleteBehavior.Restrict);

        // Allow multiple scores for same criterion (e.g., multiple competitions, multiple volunteer activities)
        modelBuilder.Entity<MovementRecordDetail>()
            .HasIndex(mrd => new { mrd.MovementRecordId, mrd.CriterionId });
        
        // Chống trùng tuyệt đối theo Activity (nếu có ActivityId)
        modelBuilder.Entity<MovementRecordDetail>()
            .HasIndex(mrd => new { mrd.MovementRecordId, mrd.CriterionId, mrd.ActivityId });

        modelBuilder.Entity<MovementRecordDetail>()
            .HasOne(mrd => mrd.CreatedByUser)
            .WithMany()
            .HasForeignKey(mrd => mrd.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);
            // Note: Removed .IsUnique() to allow students to receive multiple scores for same criterion

        // ==== CLUB MOVEMENT RECORD ====
        modelBuilder.Entity<ClubMovementRecord>()
            .HasIndex(cmr => new { cmr.ClubId, cmr.SemesterId, cmr.Month })
            .IsUnique(); // Mỗi CLB chỉ có 1 record cho 1 tháng trong 1 kỳ

        modelBuilder.Entity<ClubMovementRecord>()
            .HasOne(cmr => cmr.Club)
            .WithMany()
            .HasForeignKey(cmr => cmr.ClubId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClubMovementRecord>()
            .HasOne(cmr => cmr.Semester)
            .WithMany()
            .HasForeignKey(cmr => cmr.SemesterId)
            .OnDelete(DeleteBehavior.Restrict);

        // Allow multiple scores for same criterion (CLB can have multiple collaborations, competitions)
        modelBuilder.Entity<ClubMovementRecordDetail>()
            .HasIndex(cmrd => new { cmrd.ClubMovementRecordId, cmrd.CriterionId });

        modelBuilder.Entity<ClubMovementRecordDetail>()
            .HasOne(cmrd => cmrd.ClubMovementRecord)
            .WithMany(cmr => cmr.Details)
            .HasForeignKey(cmrd => cmrd.ClubMovementRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClubMovementRecordDetail>()
            .HasOne(cmrd => cmrd.Criterion)
            .WithMany()
            .HasForeignKey(cmrd => cmrd.CriterionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClubMovementRecordDetail>()
            .HasOne(cmrd => cmrd.Activity)
            .WithMany()
            .HasForeignKey(cmrd => cmrd.ActivityId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ClubMovementRecordDetail>()
            .HasOne(cmrd => cmrd.CreatedByUser)
            .WithMany()
            .HasForeignKey(cmrd => cmrd.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);

        // ==== EVIDENCE ====
        modelBuilder.Entity<Evidence>()
            .HasOne(e => e.Student)
            .WithMany(s => s.Evidences)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

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

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.TargetUser)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==== SYSTEM NEWS ====
        modelBuilder.Entity<SystemNews>()
            .HasOne(sn => sn.CreatedBy)
            .WithMany()
            .HasForeignKey(sn => sn.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // ==== CLUB NEWS ====
        modelBuilder.Entity<ClubNews>()
            .HasOne(cn => cn.CreatedBy)
            .WithMany()
            .HasForeignKey(cn => cn.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // ==== PAYMENT TRANSACTION ====
        modelBuilder.Entity<PaymentTransaction>()
            .HasOne(pt => pt.Club)
            .WithMany(c => c.Transactions)
            .HasForeignKey(pt => pt.ClubId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaymentTransaction>()
            .HasOne(pt => pt.CreatedBy)
            .WithMany()
            .HasForeignKey(pt => pt.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaymentTransaction>()
            .HasOne(pt => pt.Student)
            .WithMany()
            .HasForeignKey(pt => pt.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaymentTransaction>()
            .HasOne(pt => pt.Activity)
            .WithMany()
            .HasForeignKey(pt => pt.ActivityId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PaymentTransaction>()
            .HasOne(pt => pt.Semester)
            .WithMany(s => s.PaymentTransactions)
            .HasForeignKey(pt => pt.SemesterId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PaymentTransaction>()
            .HasIndex(pt => new { pt.ClubId, pt.TransactionDate });

        modelBuilder.Entity<PaymentTransaction>()
            .HasIndex(pt => new { pt.SemesterId, pt.Type, pt.Status });

        modelBuilder.Entity<PaymentTransaction>()
            .HasIndex(pt => pt.Status);

        modelBuilder.Entity<PaymentTransaction>()
            .HasIndex(pt => pt.Type);

        // ==== FUND COLLECTION ====
        // FundCollectionRequest
        modelBuilder.Entity<FundCollectionRequest>()
            .HasOne(r => r.Club)
            .WithMany()
            .HasForeignKey(r => r.ClubId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FundCollectionRequest>()
            .HasOne(r => r.CreatedBy)
            .WithMany()
            .HasForeignKey(r => r.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FundCollectionRequest>()
            .HasOne(r => r.Semester)
            .WithMany(s => s.FundCollectionRequests)
            .HasForeignKey(r => r.SemesterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FundCollectionRequest>()
            .HasIndex(r => new { r.ClubId, r.Status });

        modelBuilder.Entity<FundCollectionRequest>()
            .HasIndex(r => new { r.SemesterId, r.Status });

        modelBuilder.Entity<FundCollectionRequest>()
            .HasIndex(r => r.DueDate);

        // FundCollectionPayment
        modelBuilder.Entity<FundCollectionPayment>()
            .HasOne(p => p.FundCollectionRequest)
            .WithMany(r => r.Payments)
            .HasForeignKey(p => p.FundCollectionRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FundCollectionPayment>()
            .HasOne(p => p.ClubMember)
            .WithMany()
            .HasForeignKey(p => p.ClubMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FundCollectionPayment>()
            .HasOne(p => p.PaymentTransaction)
            .WithMany()
            .HasForeignKey(p => p.PaymentTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FundCollectionPayment>()
            .HasOne(p => p.ConfirmedBy)
            .WithMany()
            .HasForeignKey(p => p.ConfirmedById)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: 1 member chỉ có 1 payment record cho 1 request
        modelBuilder.Entity<FundCollectionPayment>()
            .HasIndex(p => new { p.FundCollectionRequestId, p.ClubMemberId })
            .IsUnique();

        modelBuilder.Entity<FundCollectionPayment>()
            .HasIndex(p => p.Status);

        // VnpayTransactionDetail
        modelBuilder.Entity<VnpayTransactionDetail>()
            .HasOne(v => v.FundCollectionPayment)
            .WithOne(p => p.VnpayTransactionDetail)
            .HasForeignKey<VnpayTransactionDetail>(v => v.FundCollectionPaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VnpayTransactionDetail>()
            .HasIndex(v => v.VnpayTransactionId)
            .IsUnique();

        modelBuilder.Entity<VnpayTransactionDetail>()
            .HasIndex(v => v.TransactionStatus);
    }

    /// <summary>
    /// Cấu hình tất cả các cột Id tự động tăng (IDENTITY)
    /// </summary>
    private void ConfigureIdentityColumns(ModelBuilder modelBuilder)
    {
        // Các entity với Id tự tăng
        modelBuilder.Entity<User>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<Role>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<UserToken>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<LoggedOutToken>().Property(e => e.Id).UseIdentityColumn();
        
        modelBuilder.Entity<Semester>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<Major>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<Student>().Property(e => e.Id).UseIdentityColumn();
        
        modelBuilder.Entity<ClubCategory>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<Club>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<ClubDepartment>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<ClubMember>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<JoinRequest>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<Interview>().Property(e => e.Id).UseIdentityColumn();
        
        modelBuilder.Entity<Activity>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<ActivityRegistration>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<ActivityAttendance>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<ActivityFeedback>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<ActivitySchedule>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<ActivityScheduleAssignment>().Property(e => e.Id).UseIdentityColumn();
        
        modelBuilder.Entity<Plan>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<Proposal>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<ProposalVote>().Property(e => e.Id).UseIdentityColumn();
        
        modelBuilder.Entity<PaymentTransaction>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<FundCollectionRequest>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<FundCollectionPayment>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<VnpayTransactionDetail>().Property(e => e.Id).UseIdentityColumn();
        
        modelBuilder.Entity<MovementCriterionGroup>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<MovementCriterion>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<MovementRecord>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<MovementRecordDetail>().Property(e => e.Id).UseIdentityColumn();
        
        modelBuilder.Entity<ClubMovementRecord>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<ClubMovementRecordDetail>().Property(e => e.Id).UseIdentityColumn();
        
        modelBuilder.Entity<Evidence>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<ClubAward>().Property(e => e.Id).UseIdentityColumn();
        
        modelBuilder.Entity<ClubNews>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<SystemNews>().Property(e => e.Id).UseIdentityColumn();
        modelBuilder.Entity<Notification>().Property(e => e.Id).UseIdentityColumn();
    }
}