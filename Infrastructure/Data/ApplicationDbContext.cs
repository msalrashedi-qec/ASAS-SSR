using Core.Entities.Account;
using Core.Entities.Business;
using Core.Entities.Discounts;
using Core.Entities.General;
using Core.Entities.Workflow;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;



namespace Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<
        ApplicationUser,
        ApplicationRole,
        string,
        IdentityUserClaim<string>,
        ApplicationUserRole,
        ApplicationUserLogin,
        IdentityRoleClaim<string>,
        IdentityUserToken<string>,
        IdentityUserPasskey<string>>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }


        #region Account
        public virtual DbSet<RoleReport> RoleReports { get; set; } = null!;
        public virtual DbSet<UserSchool> UserSchools { get; set; } = null!;
        public virtual DbSet<RoleWorkflowType> RoleWorkflowTypes { get; set; } = null!;
        public virtual DbSet<UserAudit> UserAudits { get; set; } = null!;
        public virtual DbSet<ApplicationUserLogin> UserLogins { get; set; } = null!;
        public virtual DbSet<ApplicationUser> Users { get; set; } = null!;
        public virtual DbSet<ApplicationRole> Roles { get; set; } = null!;
        public virtual DbSet<ApplicationUserRole> UserRoles { get; set; } = null!;
        #endregion Account

        #region Business
        public virtual DbSet<Bus> Buses { get; set; } = null!;
        public virtual DbSet<BusAssignment> BusAssignments { get; set; } = null!;
        public virtual DbSet<BusDriver> BusDrivers { get; set; } = null!;
        public virtual DbSet<Parent> Parents { get; set; } = null!;
        public virtual DbSet<Receipt> Receipts { get; set; } = null!;
        public virtual DbSet<ReceiptDetail> ReceiptDetails { get; set; } = null!;
        public virtual DbSet<RefundReceipt> RefundReceipts { get; set; } = null!;
        public virtual DbSet<RefundReceiptDetail> RefundReceiptDetails { get; set; } = null!;
        public virtual DbSet<School> Schools { get; set; } = null!;
        public virtual DbSet<SchoolClass> SchoolClasses { get; set; } = null!;
        public virtual DbSet<SchoolClassRoom> SchoolClassRooms { get; set; } = null!;
        public virtual DbSet<SchoolLevel> SchoolLevels { get; set; } = null!;
        public virtual DbSet<SchoolSemester> SchoolSemesters { get; set; } = null!;
        public virtual DbSet<Service> Services { get; set; } = null!;
        public virtual DbSet<ServicePrice> ServicePrices { get; set; } = null!;
        public virtual DbSet<ServiceVAT> ServiceVATs { get; set; } = null!;
        public virtual DbSet<Student> Students { get; set; } = null!;
        public virtual DbSet<StudentAccount> StudentAccounts { get; set; } = null!;
        public virtual DbSet<StudentContract> StudentContracts { get; set; } = null!;
        public virtual DbSet<StudentContractDetail> StudentContractDetails { get; set; } = null!;
        public virtual DbSet<StudentDocument> StudentDocuments { get; set; } = null!;
        public virtual DbSet<StudentSubscription> StudentSubscriptions { get; set; } = null!;
        public virtual DbSet<StudentClassRoomAssignment> StudentClassRoomAssignments { get; set; } = null!;
        public virtual DbSet<StudentBusAssignment> StudentBusAssignments { get; set; } = null!;
        #endregion Business

        #region Discounts
        public virtual DbSet<Discount> Discounts { get; set; } = null!;
        public virtual DbSet<DiscountDependency> DiscountDependencies { get; set; } = null!;
        public virtual DbSet<DiscountDetail> DiscountDetails { get; set; } = null!;
        public virtual DbSet<DiscountType> DiscountTypes { get; set; } = null!;

        public virtual DbSet<DiscountRequest> DiscountRequests { get; set; } = null!;
        public virtual DbSet<DiscountRequestDetail> DiscountRequestDetails { get; set; } = null!;
        #endregion Discounts

        #region General
        public virtual DbSet<Bank> Banks { get; set; } = null!;
        public virtual DbSet<BusType> BusTypes { get; set; } = null!;
        public virtual DbSet<City> Cities { get; set; } = null!;
        public virtual DbSet<Company> Companies { get; set; } = null!;
        public virtual DbSet<District> Districts { get; set; } = null!;
        public virtual DbSet<DocumentStatus> DocumentStatuses { get; set; } = null!;
        public virtual DbSet<DocumentType> DocumentTypes { get; set; } = null!;
        public virtual DbSet<ErrorLog> ErrorLogs { get; set; } = null!;
        public virtual DbSet<Gender> Genders { get; set; } = null!;
        public virtual DbSet<Nationality> Nationalities { get; set; } = null!;
        public virtual DbSet<PaymentMethod> PaymentMethods { get; set; } = null!;
        public virtual DbSet<Report> Reports { get; set; } = null!;
        public virtual DbSet<SchoolType> SchoolTypes { get; set; } = null!;
        public virtual DbSet<Semester> Semesters { get; set; } = null!;
        public virtual DbSet<ServiceCategory> ServiceCategories { get; set; } = null!;
        public virtual DbSet<StudentStatus> StudentStatuses { get; set; } = null!;
        public virtual DbSet<SystemAction> SystemActions { get; set; } = null!;
        public virtual DbSet<SystemPage> SystemPages { get; set; } = null!;
        public virtual DbSet<TransactionType> TransactionTypes { get; set; } = null!;
        public virtual DbSet<Year> Years { get; set; } = null!;

        #endregion General

        #region Workflow
        public virtual DbSet<Workflow> Workflows { get; set; } = null!;
        public virtual DbSet<WorkflowAction> WorkflowActions { get; set; }
        public virtual DbSet<WorkflowAttachment> WorkflowAttachments { get; set; } = null!;
        public virtual DbSet<WorkflowFutureSharing> WorkflowFutureSharings { get; set; } = null!;
        public virtual DbSet<WorkflowHistory> WorkflowHistories { get; set; } = null!;
        public virtual DbSet<Permission> Permissions { get; set; } = null!;
        public virtual DbSet<WorkflowPermission> WorkflowPermissions { get; set; } = null!;
        public virtual DbSet<WorkflowProcess> WorkflowProcesses { get; set; } = null!;
        public virtual DbSet<WorkflowProcessTask> WorkflowProcessTasks { get; set; } = null!;
        public virtual DbSet<WorkflowStatus> WorkflowStatuses { get; set; } = null!;
        public virtual DbSet<WorkflowTask> WorkflowTasks { get; set; } = null!;
        public virtual DbSet<WorkflowType> WorkflowTypes { get; set; } = null!;
        #endregion Workflow


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.ConfigureWarnings(w =>
                w.Ignore(RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Remove the AspNet prefix
            builder.Entity<ApplicationUser>().ToTable("Users");
            builder.Entity<ApplicationRole>().ToTable("Roles");
            builder.Entity<ApplicationUserRole>().ToTable("UserRoles");
            builder.Entity<ApplicationUserLogin>().ToTable("UserLogins");
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
            builder.Entity<IdentityUserPasskey<string>>().ToTable("UserPasskeys");

            #region Global Query Filters

            builder.Entity<Bank>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<Bus>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<BusAssignment>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<BusDriver>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<Parent>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<Receipt>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<ReceiptDetail>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<RefundReceipt>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<RefundReceiptDetail>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<School>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<SchoolClass>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<SchoolClassRoom>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<SchoolLevel>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<SchoolSemester>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<Service>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<ServicePrice>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<ServiceVAT>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<Student>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<StudentAccount>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<StudentContract>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<StudentDocument>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<StudentSubscription>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<DiscountRequest>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<StudentClassRoomAssignment>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<StudentBusAssignment>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<Discount>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<DiscountDetail>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<City>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<Company>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<District>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<DocumentType>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<Nationality>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<ServiceCategory>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<Year>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<Workflow>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<WorkflowFutureSharing>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<WorkflowProcess>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<WorkflowTask>().HasQueryFilter(x => !x.IsDeleted);

            #endregion Global Query Filters

            #region Account
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.AuthFactorStartDate).HasColumnType("datetime");
                entity.Property(e => e.SuspenseDate).HasColumnType("datetime");            
                entity.Property(e => e.OTPSentAt).HasColumnType("datetime");            
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            builder.Entity<RoleReport>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");                
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
            });

            builder.Entity<RoleWorkflowType>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
            });
            builder.Entity<UserSchool>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.HasOne(d => d.User)
                 .WithMany(p => p.UserSchools)
                 .HasForeignKey(d => d.UserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<UserAudit>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
            });

            #endregion Account

            #region Business
           
            builder.Entity<Bus>(entity =>
            {
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });

            builder.Entity<BusAssignment>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.FromDate).HasColumnType("datetime");
                entity.Property(e => e.ToDate).HasColumnType("datetime");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

                entity.HasOne(d => d.Bus)
                   .WithMany(p => p.BusAssignments)
                   .HasForeignKey(d => d.BusId)
                   .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<BusDriver>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });

            builder.Entity<Parent>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.NationalIDExpiryDate).HasColumnType("datetime");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");

                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });

            builder.Entity<Receipt>(entity =>
            {
                entity.HasIndex(e => e.SN);
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.Date).HasColumnType("datetime");
                entity.Property(e => e.ChequeDate).HasColumnType("datetime");

                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");

                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });

            builder.Entity<ReceiptDetail>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

                entity.HasOne(d => d.Service)
                 .WithMany(p => p.ReceiptDetails)
                 .HasForeignKey(d => d.ServiceId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<RefundReceipt>(entity =>
            {
                entity.HasIndex(e => e.SN);
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.Date).HasColumnType("datetime");
                entity.Property(e => e.ChequeDate).HasColumnType("datetime");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });

            builder.Entity<RefundReceiptDetail>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

                entity.HasOne(d => d.Service)
                 .WithMany(p => p.RefundReceiptDetails)
                 .HasForeignKey(d => d.ServiceId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<School>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });

            builder.Entity<SchoolClass>(entity =>
            {
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });

            builder.Entity<SchoolClassRoom>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
                entity.HasOne(e => e.ResponsibleTeacher)
                    .WithMany()
                    .HasForeignKey(e => e.ResponsibleTeacherId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<SchoolLevel>(entity =>
            {
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });

            builder.Entity<SchoolSemester>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.StartDate).HasColumnType("datetime");
                entity.Property(e => e.EndDate).HasColumnType("datetime");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });

            builder.Entity<Service>(entity =>
            {
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });

            builder.Entity<ServicePrice>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

                entity.HasOne(d => d.Year)
                   .WithMany(p => p.ServicePrices)
                   .HasForeignKey(d => d.YearId)
                   .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Service)
                   .WithMany(p => p.ServicePrices)
                   .HasForeignKey(d => d.ServiceId)
                   .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ServiceVAT>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

                entity.HasOne(d => d.Service)
                   .WithMany(p => p.ServiceVATs)
                   .HasForeignKey(d => d.ServiceId)
                   .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Student>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.BirthDate).HasColumnType("datetime");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

                entity.HasOne(d => d.CurrentContract)
                   .WithMany(p => p.Students)
                   .HasForeignKey(d => d.CurrentContractId)
                   .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Parent)
                 .WithMany(p => p.Students)
                 .HasForeignKey(d => d.ParentId)
                 .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.School)
                 .WithMany(p => p.Students)
                 .HasForeignKey(d => d.SchoolId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<StudentAccount>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.Date).HasColumnType("datetime");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

                entity.HasOne(d => d.Service)
                 .WithMany(p => p.StudentAccounts)
                 .HasForeignKey(d => d.ServiceId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<StudentContract>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.StartDate).HasColumnType("datetime");
                entity.Property(e => e.EndDate).HasColumnType("datetime");
                entity.Property(e => e.ActualEndDate).HasColumnType("datetime");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

                entity.HasOne(d => d.Student)
                   .WithMany(p => p.Contracts)
                   .HasForeignKey(d => d.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

            });

            builder.Entity<StudentContractDetail>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");

                entity.HasOne(d => d.Contract)
                 .WithMany(p => p.Details)
                 .HasForeignKey(d => d.ContractId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<StudentDocument>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });


            builder.Entity<StudentSubscription>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.SubscriptionDate).HasColumnType("datetime");
                entity.Property(e => e.EndDate).HasColumnType("datetime");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

                entity.HasOne(d => d.Contract)
                 .WithMany(p => p.Subscriptions)
                 .HasForeignKey(d => d.ContractId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<StudentClassRoomAssignment>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
                entity.HasIndex(e => e.ContractId).IsUnique();

                entity.HasOne(e => e.Contract)
                    .WithMany(e => e.ClassRoomAssignments)
                    .HasForeignKey(e => e.ContractId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.ClassRoom)
                    .WithMany(e => e.StudentAssignments)
                    .HasForeignKey(e => e.ClassRoomId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<StudentBusAssignment>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
                entity.HasIndex(e => e.SubscriptionId).IsUnique();

                entity.HasOne(e => e.Subscription)
                    .WithMany(e => e.BusAssignments)
                    .HasForeignKey(e => e.SubscriptionId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Bus)
                    .WithMany(e => e.StudentAssignments)
                    .HasForeignKey(e => e.BusId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            #endregion Business

            #region Discounts

            builder.Entity<Discount>(entity =>
            {
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });

            builder.Entity<DiscountDetail>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });

            builder.Entity<DiscountRequest>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                
                entity.Property(e => e.Comments).HasMaxLength(1000);
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
                
                entity.HasIndex(e => e.WfProcessId).IsUnique();

                entity.HasOne(e => e.Student)
                    .WithMany(e => e.Requests)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Discount)
                    .WithMany()
                    .HasForeignKey(e => e.DiscountId)
                    .OnDelete(DeleteBehavior.Restrict);

            });

            builder.Entity<DiscountRequestDetail>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                
                entity.HasOne(e => e.Semester)
                    .WithMany(e => e.StudentRequestSemesters)
                    .HasForeignKey(e => e.SemesterId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion Discounts

            #region General

            builder.Entity<Bank>(entity =>
            {
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });

            builder.Entity<City>(entity =>
            {
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });

            builder.Entity<Company>(entity =>
            {
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });

            builder.Entity<PaymentMethod>(entity =>
            {
                entity.HasData(
                    new PaymentMethod { Id = 1, Name = "Cash", NameAr = "نقدي" },
                    new PaymentMethod { Id = 2, Name = "Cheque", NameAr = "شيك" },
                    new PaymentMethod { Id = 3, Name = "Bank transfer", NameAr = "تحويل بنكي" },
                    new PaymentMethod { Id = 4, Name = "POS", NameAr = "شبكة" });
            });

            builder.Entity<TransactionType>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.HasData(
                    new TransactionType {Id = 100, Name = "اضافة مستحقات", NameAr = "اضافة مستحقات" },
                    new TransactionType {Id = 200, Name = "خصم ترويجي", NameAr = "خصم ترويجي" },
                    new TransactionType {Id = 300, Name = "خصم تسجيل متأخر", NameAr = "خصم تسجيل متأخر" },
                    new TransactionType {Id = 400, Name = "خصم اخوة", NameAr = "خصم اخوة" },
                    new TransactionType {Id = 500, Name = "خصم أبناء العاملين", NameAr = "خصم أبناء العاملين" },
                    new TransactionType {Id = 600, Name = "خصم سداد كامل الرسوم", NameAr = "خصم سداد كامل الرسوم" },
                    new TransactionType {Id = 700, Name = "خصم اداري", NameAr = "خصم اداري" },
                    new TransactionType {Id = 800, Name = "تسوية مالية", NameAr = "تسوية مالية" },
                    new TransactionType {Id = 810, Name = "تسوية سداد مستحقات", NameAr = "تسوية سداد مستحقات" },
                    new TransactionType {Id = 900, Name = "خصم على خدمة النقل", NameAr = "خصم على خدمة النقل" },
                    new TransactionType {Id = 1000, Name = "سحب خصم", NameAr = "سحب خصم" },
                    new TransactionType {Id = 1100, Name = "سحب مستحقات", NameAr = "سحب مستحقات" },
                    new TransactionType {Id = 1200, Name = "غرامة انسحاب", NameAr = "غرامة انسحاب" },
                    new TransactionType {Id = 1300, Name = "سداد مستحقات", NameAr = "سداد مستحقات" },
                    new TransactionType {Id = 1400, Name = "Refund payment", NameAr = "رد مبلغ"}
                );
            });

            builder.Entity<District>(entity =>
            {
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });

            builder.Entity<DocumentType>(entity =>
            {
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });

            builder.Entity<ErrorLog>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("newid()");
                entity.Property(e => e.LoggedOn).HasColumnType("datetime");
            });

            builder.Entity<Nationality>(entity =>
            {
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });

            builder.Entity<ServiceCategory>(entity =>
            {
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });

            builder.Entity<Year>(entity =>
            {
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });

            builder.Entity<Report>(entity =>
            {
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.DescriptionAr).HasMaxLength(1000);
                entity.Property(e => e.Url).HasMaxLength(100);
                entity.Property(e => e.Icon).HasMaxLength(100);
            });

            #endregion General

            #region Workflow
            builder.Entity<Workflow>(entity =>
            {
                entity.Property(e => e.TypeId).HasDefaultValue(1);
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");
            });

            builder.Entity<WorkflowAttachment>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("(NEWID())");
            });

            builder.Entity<WorkflowFutureSharing>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("(NEWID())");
                entity.Property(e => e.ShareStartDate).HasColumnType("datetime");
                entity.Property(e => e.ShareEndDate).HasColumnType("datetime");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

                entity.HasOne(d => d.SharedFromUser)
                  .WithMany(p => p.WfFutureSharingsFromUsers)
                  .HasForeignKey(d => d.SharedFromUserId)
                  .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.SharedWithUser)
                  .WithMany(p => p.WfFutureSharingsWithUsers)
                  .HasForeignKey(d => d.SharedWithUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<WorkflowHistory>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("(NEWID())");
                entity.Property(e => e.CreatorId).HasMaxLength(450);
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");

                entity.HasOne(d => d.WfProcess)
                  .WithMany(p => p.WorkflowHistories)
                  .HasForeignKey(d => d.WfProcessId)
                  .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<WorkflowPermission>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                .HasDefaultValueSql("getdate()");
            });

            builder.Entity<WorkflowProcess>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("(NEWID())");
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

                entity.HasOne(d => d.Workflow)
                 .WithMany(p => p.WorkflowProcesses)
                 .HasForeignKey(d => d.WorkflowId)
                 .OnDelete(DeleteBehavior.Restrict);

            });

            builder.Entity<WorkflowProcessTask>(entity =>
            {
                entity.Property(e => e.Id).HasDefaultValueSql("(NEWID())");
                entity.Property(e => e.StartDate).HasColumnType("datetime");
                entity.Property(e => e.EndDate).HasColumnType("datetime");
                entity.Property(e => e.ShareStartDate).HasColumnType("datetime");
                entity.Property(e => e.ShareEndDate).HasColumnType("datetime");

                entity.HasOne(d => d.Status)
                  .WithMany(p => p.WorkflowProcessTasks)
                  .HasForeignKey(d => d.StatusId)
                  .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.WfTask)
                  .WithMany(p => p.WorkflowProcessTasks)
                  .HasForeignKey(d => d.WfTaskId)
                  .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<WorkflowTask>(entity =>
            {
                entity.Property(e => e.CreatedOn).HasColumnType("datetime")
                                                 .HasDefaultValueSql("getdate()");
                entity.Property(e => e.UpdatedOn).HasColumnType("datetime");

                entity.HasOne(d => d.Workflow)
                   .WithMany(p => p.WorkflowTasks)
                   .HasForeignKey(d => d.WorkflowId)
                   .OnDelete(DeleteBehavior.Restrict);
            });

            #endregion Workflow
        }
    }
}
