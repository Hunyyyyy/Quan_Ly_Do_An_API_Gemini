using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Quan_Ly_Do_An.Data;

public partial class DatabaseDoAnContext : DbContext
{
    public DatabaseDoAnContext()
    {
    }

    public DatabaseDoAnContext(DbContextOptions<DatabaseDoAnContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Aidatum> Aidata { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<ClassMember> ClassMembers { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<ProgressReport> ProgressReports { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<ProjectMilestone> ProjectMilestones { get; set; }

    public virtual DbSet<ProjectTeam> ProjectTeams { get; set; }

    public virtual DbSet<SubTask> SubTasks { get; set; }

    public virtual DbSet<Task> Tasks { get; set; }

    public virtual DbSet<User> Users { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Aidatum>(entity =>
        {
            entity.HasKey(e => e.AidataId).HasName("PK__AIData__05F8F4B0556E8791");

            entity.ToTable("AIData");

            entity.Property(e => e.AidataId).HasColumnName("AIDataId");
            entity.Property(e => e.Airesponse).HasColumnName("AIResponse");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ProcessStage).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Project).WithMany(p => p.Aidata)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AIData__ProjectI__656C112C");

            entity.HasOne(d => d.User).WithMany(p => p.Aidata)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AIData__UserId__66603565");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("PK__Classes__CB1927C0CC1D9F76");

            entity.Property(e => e.ClassName).HasMaxLength(50);
            entity.Property(e => e.SubjectCode).HasMaxLength(50);

            entity.HasOne(d => d.Instructor).WithMany(p => p.Classes)
                .HasForeignKey(d => d.InstructorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Classes__Instruc__6754599E");
        });

        modelBuilder.Entity<ClassMember>(entity =>
        {
            entity.HasKey(e => e.ClassMemberId).HasName("PK__ClassMem__4205F71892604813");

            entity.Property(e => e.StudentCode)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Class).WithMany(p => p.ClassMembers)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ClassMemb__Class__68487DD7");

            entity.HasOne(d => d.User).WithMany(p => p.ClassMembers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ClassMemb__UserI__693CA210");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E12C8A98B15");

            entity.Property(e => e.DateSent)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsRead).HasDefaultValue(false);
            entity.Property(e => e.Message).HasMaxLength(255);

            entity.HasOne(d => d.Receiver).WithMany(p => p.NotificationReceivers)
                .HasForeignKey(d => d.ReceiverId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notifications_ReceiverId");

            entity.HasOne(d => d.User).WithMany(p => p.NotificationUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Notificat__UserI__6A30C649");
        });

        modelBuilder.Entity<ProgressReport>(entity =>
        {
            entity.HasKey(e => e.ProgressReportId).HasName("PK__Progress__E1E701E0A42B5360");

            entity.Property(e => e.AttachedFile).HasMaxLength(255);
            entity.Property(e => e.ReminderStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Chua nh?c nh?");
            entity.Property(e => e.ReportDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Chua n?p");

            entity.HasOne(d => d.Task).WithMany(p => p.ProgressReports)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProgressR__TaskI__6C190EBB");

            entity.HasOne(d => d.User).WithMany(p => p.ProgressReports)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProgressR__UserI__6D0D32F4");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.ProjectId).HasName("PK__Projects__761ABEF0CE5A4FC7");

            entity.Property(e => e.DevelopmentProcess).HasMaxLength(50);
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.ProjectName).HasMaxLength(100);
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.Class).WithMany(p => p.Projects)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Projects__ClassI__6EF57B66");

            entity.HasOne(d => d.ProjectLeader).WithMany(p => p.Projects)
                .HasForeignKey(d => d.ProjectLeaderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Projects__Projec__6FE99F9F");
        });

        modelBuilder.Entity<ProjectMilestone>(entity =>
        {
            entity.HasKey(e => e.MilestoneId).HasName("PK__ProjectM__09C480780FC528C8");

            entity.Property(e => e.StageName).HasMaxLength(255);

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectMilestones)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProjectMi__Proje__6E01572D");
        });

        modelBuilder.Entity<ProjectTeam>(entity =>
        {
            entity.HasKey(e => e.TeamId).HasName("PK__ProjectT__123AE7995569B922");

            entity.Property(e => e.GroupNumber)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Class).WithMany(p => p.ProjectTeams)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectTeams)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProjectTe__Proje__70DDC3D8");

            entity.HasOne(d => d.User).WithMany(p => p.ProjectTeams)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProjectTe__UserI__71D1E811");
        });

        modelBuilder.Entity<SubTask>(entity =>
        {
            entity.HasKey(e => e.SubTaskId).HasName("PK__SubTasks__869FF1821F5DA142");

            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.SubTaskName).HasMaxLength(100);

            entity.HasOne(d => d.AssignedToUser).WithMany(p => p.SubTasks)
                .HasForeignKey(d => d.AssignedToUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SubTasks__Assign__73BA3083");

            entity.HasOne(d => d.Task).WithMany(p => p.SubTasks)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SubTasks__TaskId__74AE54BC");
        });

        modelBuilder.Entity<Task>(entity =>
        {
            entity.HasKey(e => e.TaskId).HasName("PK__Tasks__7C6949B1A8EC6F3C");

            entity.Property(e => e.Attachment).HasMaxLength(255);
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.PriorityLevel).HasMaxLength(50);
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.SubmittedFileTypes).HasMaxLength(100);
            entity.Property(e => e.TaskName).HasMaxLength(100);

            entity.HasOne(d => d.AssignedToUser).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.AssignedToUserId)
                .HasConstraintName("FK__Tasks__AssignedT__75A278F5");

            entity.HasOne(d => d.Project).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tasks__ProjectId__76969D2E");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C32A50631");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105349AAC5321").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Role).HasMaxLength(50);
            entity.Property(e => e.StudentCode).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
