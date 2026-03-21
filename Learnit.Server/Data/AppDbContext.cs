
using Microsoft.EntityFrameworkCore;
using Learnit.Server.Models;
using System.Collections.Generic;



namespace Learnit.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseModule> CourseModules { get; set; }
        public DbSet<CourseSubModule> CourseSubModules { get; set; }
        public DbSet<ScheduleEvent> ScheduleEvents { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<ExternalLink> ExternalLinks { get; set; }
        public DbSet<StudySession> StudySessions { get; set; }
        public DbSet<PlaybackPosition> PlaybackPositions { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<QuizOption> QuizOptions { get; set; }
        public DbSet<QuizAttempt> QuizAttempts { get; set; }
        public DbSet<QuizAnswer> QuizAnswers { get; set; }
        public DbSet<Classroom> Classrooms { get; set; }
        public DbSet<ClassroomMember> ClassroomMembers { get; set; }
        public DbSet<ClassroomCourse> ClassroomCourses { get; set; }
        public DbSet<CourseCopy> CourseCopies { get; set; }
        public DbSet<Award> Awards { get; set; }
        public DbSet<UserAward> UserAwards { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and indexes for production scalability

            // User - Course relationship (One-to-Many) - CASCADE DELETE ensures data consistency
            modelBuilder.Entity<Course>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Course - StudySession relationship - CASCADE ensures sessions deleted with course
            modelBuilder.Entity<StudySession>()
                .HasOne(s => s.Course)
                .WithMany(c => c.StudySessions)
                .HasForeignKey(s => s.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // CourseModule - StudySession relationship - SET NULL preserves study session history
            modelBuilder.Entity<StudySession>()
                .HasOne(s => s.CourseModule)
                .WithMany()
                .HasForeignKey(s => s.CourseModuleId)
                .OnDelete(DeleteBehavior.SetNull);

            // User - ActivityLog relationship - CASCADE ensures logs deleted with user
            modelBuilder.Entity<ActivityLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User - ScheduleEvent relationship (no navigation property, just foreign key)
            modelBuilder.Entity<ScheduleEvent>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User - PlaybackPosition relationship
            modelBuilder.Entity<PlaybackPosition>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Course - PlaybackPosition relationship
            modelBuilder.Entity<PlaybackPosition>()
                .HasOne(p => p.Course)
                .WithMany()
                .HasForeignKey(p => p.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // CourseModule - PlaybackPosition relationship (optional)
            modelBuilder.Entity<PlaybackPosition>()
                .HasOne(p => p.Module)
                .WithMany()
                .HasForeignKey(p => p.ModuleId)
                .OnDelete(DeleteBehavior.SetNull);

            // CRITICAL INDEXES FOR PRODUCTION PERFORMANCE (with many users)
            
            // Course indexes - filter by UserId is most common query
            modelBuilder.Entity<Course>()
                .HasIndex(c => c.UserId)
                .HasDatabaseName("IX_Courses_UserId");

            // StudySession indexes - CRITICAL for streaks/heatmap queries (most frequent)
            modelBuilder.Entity<StudySession>()
                .HasIndex(s => s.CourseId)
                .HasDatabaseName("IX_StudySessions_CourseId");
            
            // Composite index for streak calculations (filters by date and completion status)
            modelBuilder.Entity<StudySession>()
                .HasIndex(s => new { s.StartTime, s.IsCompleted })
                .HasDatabaseName("IX_StudySessions_StartTime_IsCompleted");

            // ActivityLog indexes - CRITICAL for heatmap (very frequent queries)
            modelBuilder.Entity<ActivityLog>()
                .HasIndex(a => a.UserId)
                .HasDatabaseName("IX_ActivityLogs_UserId");
            
            // Unique constraint: one log per user per day (prevents duplicates, improves query performance)
            modelBuilder.Entity<ActivityLog>()
                .HasIndex(a => new { a.UserId, a.Date })
                .HasDatabaseName("IX_ActivityLogs_UserId_Date")
                .IsUnique();

            // ScheduleEvent indexes
            modelBuilder.Entity<ScheduleEvent>()
                .HasIndex(e => e.UserId)
                .HasDatabaseName("IX_ScheduleEvents_UserId");
            
            modelBuilder.Entity<ScheduleEvent>()
                .HasIndex(e => new { e.UserId, e.StartUtc })
                .HasDatabaseName("IX_ScheduleEvents_UserId_StartUtc");

            // PlaybackPosition indexes
            modelBuilder.Entity<PlaybackPosition>()
                .HasIndex(p => new { p.UserId, p.CourseId })
                .HasDatabaseName("IX_PlaybackPositions_UserId_CourseId");

            // Quiz relationships
            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.CourseModule)
                .WithMany()
                .HasForeignKey(q => q.CourseModuleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizQuestion>()
                .HasOne(qq => qq.Quiz)
                .WithMany(q => q.Questions)
                .HasForeignKey(qq => qq.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizOption>()
                .HasOne(qo => qo.QuizQuestion)
                .WithMany(qq => qq.Options)
                .HasForeignKey(qo => qo.QuizQuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizAttempt>()
                .HasOne(qa => qa.Quiz)
                .WithMany(q => q.Attempts)
                .HasForeignKey(qa => qa.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizAttempt>()
                .HasOne(qa => qa.User)
                .WithMany()
                .HasForeignKey(qa => qa.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizAnswer>()
                .HasOne(qa => qa.QuizAttempt)
                .WithMany(qat => qat.Answers)
                .HasForeignKey(qa => qa.QuizAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizAnswer>()
                .HasOne(qa => qa.QuizQuestion)
                .WithMany()
                .HasForeignKey(qa => qa.QuizQuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Quiz indexes
            modelBuilder.Entity<Quiz>()
                .HasIndex(q => q.CourseModuleId)
                .HasDatabaseName("IX_Quizzes_CourseModuleId");

            modelBuilder.Entity<QuizAttempt>()
                .HasIndex(qa => new { qa.QuizId, qa.UserId })
                .HasDatabaseName("IX_QuizAttempts_QuizId_UserId");

            modelBuilder.Entity<QuizAttempt>()
                .HasIndex(qa => qa.UserId)
                .HasDatabaseName("IX_QuizAttempts_UserId");

            // Classroom relationships
            // User - Classroom relationship (Creator)
            modelBuilder.Entity<Classroom>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.CreatorId)
                .OnDelete(DeleteBehavior.Restrict); // Don't delete classroom if creator is deleted (or change to SetNull)

            // Classroom - ClassroomMember relationship
            modelBuilder.Entity<ClassroomMember>()
                .HasOne(cm => cm.Classroom)
                .WithMany(c => c.Members)
                .HasForeignKey(cm => cm.ClassroomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClassroomMember>()
                .HasOne(cm => cm.User)
                .WithMany()
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Classroom - ClassroomCourse relationship
            modelBuilder.Entity<ClassroomCourse>()
                .HasOne(cc => cc.Classroom)
                .WithMany(c => c.SharedCourses)
                .HasForeignKey(cc => cc.ClassroomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClassroomCourse>()
                .HasOne(cc => cc.Course)
                .WithMany()
                .HasForeignKey(cc => cc.CourseId)
                .OnDelete(DeleteBehavior.Restrict); // Don't delete if course is deleted (or change to SetNull)

            // CourseCopy relationships
            modelBuilder.Entity<CourseCopy>()
                .HasOne(cc => cc.OriginalCourse)
                .WithMany()
                .HasForeignKey(cc => cc.OriginalCourseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourseCopy>()
                .HasOne(cc => cc.CopiedCourse)
                .WithMany()
                .HasForeignKey(cc => cc.CopiedCourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CourseCopy>()
                .HasOne(cc => cc.User)
                .WithMany()
                .HasForeignKey(cc => cc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CourseCopy>()
                .HasOne(cc => cc.Classroom)
                .WithMany()
                .HasForeignKey(cc => cc.ClassroomId)
                .OnDelete(DeleteBehavior.SetNull);

            // Classroom indexes
            modelBuilder.Entity<Classroom>()
                .HasIndex(c => c.CreatorId)
                .HasDatabaseName("IX_Classrooms_CreatorId");

            modelBuilder.Entity<Classroom>()
                .HasIndex(c => c.InviteCode)
                .IsUnique()
                .HasDatabaseName("IX_Classrooms_InviteCode");

            modelBuilder.Entity<ClassroomMember>()
                .HasIndex(cm => new { cm.ClassroomId, cm.UserId })
                .IsUnique()
                .HasDatabaseName("IX_ClassroomMembers_ClassroomId_UserId");

            modelBuilder.Entity<ClassroomMember>()
                .HasIndex(cm => cm.UserId)
                .HasDatabaseName("IX_ClassroomMembers_UserId");

            modelBuilder.Entity<ClassroomCourse>()
                .HasIndex(cc => cc.ClassroomId)
                .HasDatabaseName("IX_ClassroomCourses_ClassroomId");

            modelBuilder.Entity<ClassroomCourse>()
                .HasIndex(cc => cc.CourseId)
                .HasDatabaseName("IX_ClassroomCourses_CourseId");

            modelBuilder.Entity<ClassroomCourse>()
                .HasIndex(cc => new { cc.ClassroomId, cc.CourseId })
                .IsUnique()
                .HasDatabaseName("IX_ClassroomCourses_ClassroomId_CourseId");

            modelBuilder.Entity<CourseCopy>()
                .HasIndex(cc => cc.UserId)
                .HasDatabaseName("IX_CourseCopies_UserId");

            modelBuilder.Entity<CourseCopy>()
                .HasIndex(cc => cc.OriginalCourseId)
                .HasDatabaseName("IX_CourseCopies_OriginalCourseId");

            // Award relationships
            modelBuilder.Entity<UserAward>()
                .HasOne(ua => ua.User)
                .WithMany()
                .HasForeignKey(ua => ua.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserAward>()
                .HasOne(ua => ua.Award)
                .WithMany(a => a.UserAwards)
                .HasForeignKey(ua => ua.AwardId)
                .OnDelete(DeleteBehavior.Restrict);

            // Award indexes
            modelBuilder.Entity<Award>()
                .HasIndex(a => a.Code)
                .IsUnique()
                .HasDatabaseName("IX_Awards_Code");

            modelBuilder.Entity<UserAward>()
                .HasIndex(ua => new { ua.UserId, ua.AwardId })
                .IsUnique()
                .HasDatabaseName("IX_UserAwards_UserId_AwardId");

            modelBuilder.Entity<UserAward>()
                .HasIndex(ua => ua.UserId)
                .HasDatabaseName("IX_UserAwards_UserId");
        }
    }
}
