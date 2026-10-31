using Microsoft.EntityFrameworkCore;

namespace Company.Api.Data;

public class CompanyDbContext : DbContext
{
    public CompanyDbContext(DbContextOptions<CompanyDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<EmployeeAccount> Accounts => Set<EmployeeAccount>();

    public DbSet<StateHoliday> StateHolidays => Set<StateHoliday>();

    public DbSet<ReligiousHoliday> ReligiousHolidays => Set<ReligiousHoliday>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<MonthSheet> Sheets => Set<MonthSheet>();

    public DbSet<SheetEntry> SheetEntries => Set<SheetEntry>();

    public DbSet<SheetOvertime> SheetOvertime => Set<SheetOvertime>();

    public DbSet<SheetMessage> SheetMessages => Set<SheetMessage>();

    public DbSet<SheetEvent> SheetEvents => Set<SheetEvent>();

    public DbSet<SheetRevision> SheetRevisions => Set<SheetRevision>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(e =>
        {
            e.ToTable("employees");
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(10).IsRequired();
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.SupervisorCode).HasMaxLength(10);
            e.Property(x => x.DesignatedApproverCode).HasMaxLength(10);
            e.HasMany(x => x.Accounts).WithOne().HasForeignKey(a => a.EmployeeCode).HasPrincipalKey(x => x.Code).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmployeeAccount>(e =>
        {
            e.ToTable("employee_accounts");
            e.HasIndex(x => x.Account).IsUnique();
            e.Property(x => x.Account).HasMaxLength(256).IsRequired();
            e.Property(x => x.EmployeeCode).HasMaxLength(10).IsRequired();
        });

        modelBuilder.Entity<StateHoliday>(e =>
        {
            e.ToTable("state_holidays");
            e.HasIndex(x => x.Date).IsUnique();
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
        });

        modelBuilder.Entity<ReligiousHoliday>(e =>
        {
            e.ToTable("religious_holidays");
            e.HasIndex(x => new { x.EmployeeCode, x.Date }).IsUnique();
            e.Property(x => x.EmployeeCode).HasMaxLength(10).IsRequired();
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
        });

        modelBuilder.Entity<MonthSheet>(e =>
        {
            e.ToTable("month_sheets");
            e.HasIndex(x => new { x.EmployeeCode, x.Year, x.Month }).IsUnique();
            e.Property(x => x.EmployeeCode).HasMaxLength(10).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasMany(x => x.Entries).WithOne().HasForeignKey(x => x.SheetId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Overtime).WithOne().HasForeignKey(x => x.SheetId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Messages).WithOne().HasForeignKey(x => x.SheetId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.SheetId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.Property(x => x.Chain).HasMaxLength(400).IsRequired();
            e.Ignore(x => x.ChainCodes);
        });

        modelBuilder.Entity<SheetMessage>(e =>
        {
            e.ToTable("sheet_messages");
            e.HasIndex(x => new { x.SheetId, x.Position }).IsUnique();
            e.Property(x => x.FromCode).HasMaxLength(10).IsRequired();
            e.Property(x => x.Text).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Answer).HasMaxLength(2000);
        });

        modelBuilder.Entity<SheetEvent>(e =>
        {
            e.ToTable("sheet_events");
            e.HasIndex(x => x.SheetId);
            e.Property(x => x.Kind).HasMaxLength(30).IsRequired();
            e.Property(x => x.ActorCode).HasMaxLength(10).IsRequired();
            e.Property(x => x.OnBehalfOf).HasMaxLength(10);
            e.Property(x => x.Text).HasMaxLength(2000);
        });

        modelBuilder.Entity<SheetRevision>(e =>
        {
            e.ToTable("sheet_revisions");
            e.HasIndex(x => new { x.SheetId, x.Revision }).IsUnique();
            e.HasOne<MonthSheet>().WithMany().HasForeignKey(x => x.SheetId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SheetEntry>(e =>
        {
            e.ToTable("sheet_entries");
            e.HasIndex(x => new { x.SheetId, x.Date, x.Code }).IsUnique();
        });

        modelBuilder.Entity<SheetOvertime>(e =>
        {
            e.ToTable("sheet_overtime");
            e.HasIndex(x => new { x.SheetId, x.Date });
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<AuditEvent>(e =>
        {
            e.ToTable("audit_events");
            e.HasIndex(x => x.AtUtc);
            e.Property(x => x.Actor).HasMaxLength(256).IsRequired();
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
            e.Property(x => x.Details).HasMaxLength(2000);
        });
    }

    /// <summary>The audit log is append-only: changing or deleting an event is refused in the application itself</summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        GuardAudit();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        GuardAudit();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void GuardAudit()
    {
        if (ChangeTracker.Entries<AuditEvent>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("The audit log is append-only.");
    }
}
