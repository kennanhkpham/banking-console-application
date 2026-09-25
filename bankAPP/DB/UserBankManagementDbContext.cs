using System;
using System.Collections.Generic;
using bankLIB;
using Microsoft.EntityFrameworkCore;

namespace bankAPP.DB;

public partial class UserBankManagementDbContext : DbContext
{
    
    public UserBankManagementDbContext()
    {
    }

    public UserBankManagementDbContext(DbContextOptions<UserBankManagementDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // #warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
            // => optionsBuilder.UseSqlServer("server=DESKTOP-675F6T8;database=userBankManagementDB;Trusted_Connection=true;trustservercertificate=true;");

        }
    }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Account>(entity =>
    {
        entity.HasKey(e => e.AccNo)
            .HasName("PK__accounts__A471970548783782");

        entity.ToTable("UserAccounts", table =>
        {
            table.HasCheckConstraint(
                "CK_UserAccounts_ChequeBookStatus",
                "[chequebookstatus] IN (0, 1, 2)"
            );
        });

        entity.Ignore(e => e.Transactions);

        entity.Property(e => e.AccNo)
            .ValueGeneratedNever()
            .HasColumnName("accNo");

        entity.Property(e => e.AccBalance)
            .HasColumnName("accBalance");

        entity.Property(e => e.Name)
            .HasMaxLength(30)
            .IsUnicode(false)
            .HasColumnName("name");

        entity.Property(e => e.Username)
            .HasMaxLength(50)
            .IsUnicode(false)
            .HasColumnName("Username");

        entity.Property(e => e.Password)
            .HasMaxLength(50)
            .IsUnicode(false)
            .HasColumnName("Password");

        // Cheque book status
        entity.Property(e => e.ChequeBookStatus)
            .HasColumnName("chequebookstatus")
            .HasDefaultValue(ChequeBookStatus.None);
        });
}

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
