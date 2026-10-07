using Core.Entities.Account;
using Core.Entities.Business;
using Core.Entities.Discounts;
using Core.Entities.General;
using Core.Entities.Workflow;
using Core.Interfaces;
using Core.Services;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;



namespace Infrastructure.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDbContextFactory<ApplicationDbContext> _factory;
        private readonly WorkflowUpdateNotifier _workflowUpdateNotifier;
        private readonly SemaphoreSlim _writeGate = new(1, 1);
        private ApplicationDbContext? _writeContext;
        private bool _disposed;


        #region Account
        public IBaseRepository<RoleWorkflowType> RoleWorkflowTypes { get; private set; }
        public IBaseRepository<RoleReport> RoleReports { get; private set; }
        public IBaseRepository<UserAudit> UserAudits { get; private set; }
        public IBaseRepository<ApplicationUserLogin> UserLogins { get; private set; }
        public IBaseRepository<ApplicationUser> Users { get; private set; }
        public IBaseRepository<ApplicationRole> Roles { get; private set; }
        public IBaseRepository<ApplicationUserRole> UserRoles { get; private set; }
        public IBaseRepository<UserSchool> UserSchools { get; private set; }
        #endregion Account

        #region Business
        public IBaseRepository<Bus> Buses { get; private set; }
        public IBaseRepository<BusAssignment> BusAssignments { get; private set; }
        public IBaseRepository<BusDriver> BusDrivers { get; private set; }
        public IBaseRepository<Parent> Parents { get; private set; }
        public IBaseRepository<Receipt> Receipts { get; private set; }
        public IBaseRepository<ReceiptDetail> ReceiptDetails { get; private set; }
        public IBaseRepository<RefundReceipt> RefundReceipts { get; private set; }
        public IBaseRepository<RefundReceiptDetail> RefundReceiptDetails { get; private set; }
        public IBaseRepository<School> Schools { get; private set; }
        public IBaseRepository<SchoolClass> SchoolClasses { get; private set; }
        public IBaseRepository<SchoolClassRoom> SchoolClassRooms { get; private set; }
        public IBaseRepository<SchoolLevel> SchoolLevels { get; private set; }
        public IBaseRepository<SchoolSemester> SchoolSemesters { get; private set; }
        public IBaseRepository<Service> Services { get; private set; }
        public IBaseRepository<ServicePrice> ServicePrices { get; private set; }
        public IBaseRepository<ServiceVAT> ServiceVATs { get; private set; }
        public IBaseRepository<Student> Students { get; private set; }
        public IBaseRepository<StudentAccount> StudentAccounts { get; private set; }
        public IBaseRepository<StudentContract> StudentContracts { get; private set; }
        public IBaseRepository<StudentContractDetail> StudentContractDetails { get; private set; }
        public IBaseRepository<StudentDocument> StudentDocuments { get; private set; }
        public IBaseRepository<StudentSubscription> StudentSubscriptions { get; private set; }
        public IBaseRepository<StudentClassRoomAssignment> StudentClassRoomAssignments { get; private set; }
        public IBaseRepository<StudentBusAssignment> StudentBusAssignments { get; private set; }
        #endregion Business

        #region Discounts
        public IBaseRepository<Discount> Discounts { get; private set; }
        public IBaseRepository<DiscountDependency> DiscountDependencies { get; private set; }
        public IBaseRepository<DiscountDetail> DiscountDetails { get; private set; }
        public IBaseRepository<DiscountType> DiscountTypes { get; private set; }

        public IBaseRepository<DiscountRequest> DiscountRequests { get; private set; }
        public IBaseRepository<DiscountRequestDetail> DiscountRequestDetails { get; private set; }

        #endregion Discounts

        #region General
        public IBaseRepository<BusType> BusTypes { get; private set; }
        public IBaseRepository<Bank> Banks { get; private set; }
        public IBaseRepository<City> Cities { get; private set; }
        public IBaseRepository<Company> Companies { get; private set; }
        public IBaseRepository<District> Districts { get; private set; }
        public IBaseRepository<DocumentStatus> DocumentStatuses { get; private set; }
        public IBaseRepository<DocumentType> DocumentTypes { get; private set; }
        public IBaseRepository<ErrorLog> ErrorLogs { get; private set; }
        public IBaseRepository<Gender> Genders { get; private set; }
        public IBaseRepository<Nationality> Nationalities { get; private set; }
        public IBaseRepository<PaymentMethod> PaymentMethods { get; private set; }
        public IBaseRepository<Report> Reports { get; private set; }
        public IBaseRepository<SchoolType> SchoolTypes { get; private set; }
        public IBaseRepository<Semester> Semesters { get; private set; }
        public IBaseRepository<ServiceCategory> ServiceCategories { get; private set; }
        public IBaseRepository<StudentStatus> StudentStatuses { get; private set; }
        public IBaseRepository<SystemAction> SystemActions { get; private set; }
        public IBaseRepository<SystemPage> SystemPages { get; private set; }
        public IBaseRepository<TransactionType> TransactionTypes { get; private set; }
        public IBaseRepository<Year> Years { get; private set; }
        #endregion General

        #region Workflow
        public IBaseRepository<Workflow> Workflows { get; private set; }
        public IBaseRepository<WorkflowAction> WorkflowActions { get; private set; }
        public IBaseRepository<WorkflowAttachment> WorkflowAttachments { get; private set; }
        public IBaseRepository<WorkflowFutureSharing> WorkflowFutureSharings { get; private set; }
        public IBaseRepository<WorkflowHistory> WorkflowHistories { get; private set; }
        public IBaseRepository<Permission> Permissions { get; private set; }
        public IBaseRepository<WorkflowPermission> WorkflowPermissions { get; private set; }
        public IBaseRepository<WorkflowProcess> WorkflowProcesses { get; private set; }
        public IBaseRepository<WorkflowProcessTask> WorkflowProcessTasks { get; private set; }
        public IBaseRepository<WorkflowStatus> WorkflowStatuses { get; private set; }
        public IBaseRepository<WorkflowTask> WorkflowTasks { get; private set; }
        public IBaseRepository<WorkflowType> WorkflowTypes { get; private set; }

        #endregion Workflow

        public UnitOfWork(
            IDbContextFactory<ApplicationDbContext> factory,
            WorkflowUpdateNotifier workflowUpdateNotifier)
        {
            _factory = factory;
            _workflowUpdateNotifier = workflowUpdateNotifier;

            #region Account
            RoleWorkflowTypes = CreateRepository<RoleWorkflowType>();
            RoleReports = CreateRepository<RoleReport>();
            UserAudits = CreateRepository<UserAudit>();
            UserLogins = CreateRepository<ApplicationUserLogin>();
            Users = CreateRepository<ApplicationUser>();
            Roles = CreateRepository<ApplicationRole>();
            UserRoles = CreateRepository<ApplicationUserRole>();
            UserSchools = CreateRepository<UserSchool>();
            #endregion Account

            #region Business
            Buses = CreateRepository<Bus>();
            BusAssignments = CreateRepository<BusAssignment>();
            BusDrivers = CreateRepository<BusDriver>();
            Parents = CreateRepository<Parent>();
            Receipts = CreateRepository<Receipt>();
            ReceiptDetails = CreateRepository<ReceiptDetail>();
            RefundReceipts = CreateRepository<RefundReceipt>();
            RefundReceiptDetails = CreateRepository<RefundReceiptDetail>();
            Schools = CreateRepository<School>();
            SchoolClasses = CreateRepository<SchoolClass>();
            SchoolClassRooms = CreateRepository<SchoolClassRoom>();
            SchoolLevels = CreateRepository<SchoolLevel>();
            SchoolSemesters = CreateRepository<SchoolSemester>();
            Services = CreateRepository<Service>();
            ServicePrices = CreateRepository<ServicePrice>();
            ServiceVATs = CreateRepository<ServiceVAT>();
            Students = CreateRepository<Student>();
            StudentAccounts = CreateRepository<StudentAccount>();
            StudentContracts = CreateRepository<StudentContract>();
            StudentContractDetails = CreateRepository<StudentContractDetail>();
            StudentDocuments = CreateRepository<StudentDocument>();
            StudentSubscriptions = CreateRepository<StudentSubscription>();
            StudentClassRoomAssignments = CreateRepository<StudentClassRoomAssignment>();
            StudentBusAssignments = CreateRepository<StudentBusAssignment>();
            #endregion Business

            #region Discounts
            Discounts = CreateRepository<Discount>();
            DiscountDependencies = CreateRepository<DiscountDependency>();
            DiscountDetails = CreateRepository<DiscountDetail>();
            DiscountTypes = CreateRepository<DiscountType>();

            DiscountRequests = CreateRepository<DiscountRequest>();
            DiscountRequestDetails = CreateRepository<DiscountRequestDetail>();
            #endregion Discounts

            #region General
            BusTypes = CreateRepository<BusType>();
            Banks = CreateRepository<Bank>();
            Cities = CreateRepository<City>();
            Companies = CreateRepository<Company>();
            Districts = CreateRepository<District>();
            DocumentStatuses = CreateRepository<DocumentStatus>();
            DocumentTypes = CreateRepository<DocumentType>();
            ErrorLogs = CreateRepository<ErrorLog>();
            Genders = CreateRepository<Gender>();
            Nationalities = CreateRepository<Nationality>();
            PaymentMethods = CreateRepository<PaymentMethod>();
            Reports = CreateRepository<Report>();
            SchoolTypes = CreateRepository<SchoolType>();
            Semesters = CreateRepository<Semester>();
            ServiceCategories = CreateRepository<ServiceCategory>();
            StudentStatuses = CreateRepository<StudentStatus>();
            SystemActions = CreateRepository<SystemAction>();
            SystemPages = CreateRepository<SystemPage>();
            TransactionTypes = CreateRepository<TransactionType>();
            Years = CreateRepository<Year>();
            #endregion General

            #region Workflow
            Workflows = CreateRepository<Workflow>();
            WorkflowActions = CreateRepository<WorkflowAction>();
            WorkflowAttachments = CreateRepository<WorkflowAttachment>();
            WorkflowFutureSharings = CreateRepository<WorkflowFutureSharing>();
            WorkflowHistories = CreateRepository<WorkflowHistory>();
            Permissions = CreateRepository<Permission>();
            WorkflowPermissions = CreateRepository<WorkflowPermission>();
            WorkflowProcesses = CreateRepository<WorkflowProcess>();
            WorkflowProcessTasks = CreateRepository<WorkflowProcessTask>();
            WorkflowTasks = CreateRepository<WorkflowTask>();
            WorkflowStatuses = CreateRepository<WorkflowStatus>();
            WorkflowTypes = CreateRepository<WorkflowType>();
            #endregion Workflow
        }

        private BaseRepository<T> CreateRepository<T>() where T : class =>
            new(_factory, GetWriteContext, _writeGate);

        private ApplicationDbContext GetWriteContext()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _writeContext ??= _factory.CreateDbContext();
        }

        public Task<int> GetNextReceiptNumberAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
            GetNextReceiptNumberAsync(schoolId, false, cancellationToken);

        public Task<int> GetNextRefundReceiptNumberAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
            GetNextReceiptNumberAsync(schoolId, true, cancellationToken);

        private async Task<int> GetNextReceiptNumberAsync(Guid schoolId, bool refund, CancellationToken cancellationToken)
        {
            if (schoolId == Guid.Empty)
                throw new ArgumentException("A school is required for receipt numbering.", nameof(schoolId));

            await _writeGate.WaitAsync(cancellationToken);
            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (_writeContext == null)
                {
                    await using var template = await _factory.CreateDbContextAsync(cancellationToken);
                    var options = new DbContextOptionsBuilder<ApplicationDbContext>(
                        (DbContextOptions<ApplicationDbContext>)template.GetService<IDbContextOptions>());
                    // A partial retry cannot safely replay number allocation plus the later save.
                    options.UseSqlServer(sql => sql.ExecutionStrategy(dependencies =>
                        new NonRetryingExecutionStrategy(dependencies)));
                    _writeContext = new ApplicationDbContext(options.Options);
                }
                var context = GetWriteContext();
                if (context.Database.CurrentTransaction == null)
                    await context.Database.BeginTransactionAsync(cancellationToken);

                // Hold the school/type lock until the receipt and its accounts are saved.
                var resource = $"ReceiptNumber:{schoolId:D}:{(refund ? "Refund" : "Receipt")}";
                await context.Database.ExecuteSqlInterpolatedAsync($"""
                    DECLARE @result int;
                    EXEC @result = sys.sp_getapplock @Resource = {resource},
                        @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000;
                    IF @result < 0
                        THROW 50001, 'Unable to lock receipt numbering. Please retry.', 1;
                    """, cancellationToken);

                // Use insertion time, independently of the document date; retain deleted numbers.
                var lastNumber = refund
                    ? await context.RefundReceipts.IgnoreQueryFilters().AsNoTracking()
                        .Where(x => x.Contract.Student.SchoolId == schoolId)
                        .OrderByDescending(x => x.CreatedOn).ThenByDescending(x => x.SN)
                        .Select(x => (int?)x.SN).FirstOrDefaultAsync(cancellationToken)
                    : await context.Receipts.IgnoreQueryFilters().AsNoTracking()
                        .Where(x => x.Contract.Student.SchoolId == schoolId)
                        .OrderByDescending(x => x.CreatedOn).ThenByDescending(x => x.SN)
                        .Select(x => (int?)x.SN).FirstOrDefaultAsync(cancellationToken);
                return checked((lastNumber ?? 0) + 1);
            }
            catch
            {
                if (_writeContext != null)
                {
                    await _writeContext.DisposeAsync();
                    _writeContext = null;
                }
                throw;
            }
            finally
            {
                _writeGate.Release();
            }
        }

        public async Task DiscardChangesAsync()
        {
            await _writeGate.WaitAsync();
            try
            {
                if (_writeContext != null)
                {
                    await _writeContext.DisposeAsync();
                    _writeContext = null;
                }
            }
            finally
            {
                _writeGate.Release();
            }
        }

        public async Task<long> GetNextStudentRequestNumberAsync(DateTime requestDate, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            await using var context = await _factory.CreateDbContextAsync(cancellationToken);
            await context.Database.OpenConnectionAsync(cancellationToken);

            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText =
                """
                SET NOCOUNT ON;
                SET XACT_ABORT ON;
                SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
                BEGIN TRANSACTION;

                DECLARE @NextNumber bigint;

                SELECT @NextNumber = [LastNumber] + 1
                FROM [dbo].[StudentRequestMonthlyCounters] WITH (UPDLOCK, HOLDLOCK)
                WHERE [MonthKey] = @MonthKey;

                IF @NextNumber IS NULL
                BEGIN
                    SET @NextNumber = 1;
                    INSERT INTO [dbo].[StudentRequestMonthlyCounters] ([MonthKey], [LastNumber])
                    VALUES (@MonthKey, @NextNumber);
                END
                ELSE
                BEGIN
                    UPDATE [dbo].[StudentRequestMonthlyCounters]
                    SET [LastNumber] = @NextNumber
                    WHERE [MonthKey] = @MonthKey;
                END

                COMMIT TRANSACTION;
                SELECT @NextNumber;
                """;
            var monthKey = requestDate.ToString("yyMM", System.Globalization.CultureInfo.InvariantCulture);
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@MonthKey";
            parameter.Value = monthKey;
            command.Parameters.Add(parameter);
            var value = await command.ExecuteScalarAsync(cancellationToken);

            return value is long number
                ? number
                : throw new InvalidOperationException("The database did not return a valid student request number.");
        }

        public async Task<bool> SaveAsync()
        {
            await _writeGate.WaitAsync();
            try
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                if (_writeContext == null)
                    return false;

                var context = _writeContext;
                var processCreated = context.ChangeTracker
                    .Entries<WorkflowProcess>()
                    .Any(entry => entry.State == EntityState.Added);
                var workflowChanged = processCreated
                    || context.ChangeTracker.Entries<WorkflowProcess>()
                        .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted)
                    || context.ChangeTracker.Entries<WorkflowProcessTask>()
                        .Any(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                    || context.ChangeTracker.Entries<WorkflowHistory>()
                        .Any(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

                var saved = await context.SaveChangesAsync() > 0;
                if (context.Database.CurrentTransaction != null)
                    await context.Database.CurrentTransaction.CommitAsync();

                if (saved && workflowChanged)
                    _workflowUpdateNotifier.Publish(processCreated
                        ? WorkflowUpdateKind.ProcessCreated
                        : WorkflowUpdateKind.Changed);

                return saved;
            }
            finally
            {
                if (_writeContext != null)
                {
                    await _writeContext.DisposeAsync();
                    _writeContext = null;
                }

                _writeGate.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _writeGate.Wait();
            try
            {
                if (_disposed)
                    return;

                _writeContext?.Dispose();
                _writeContext = null;
                _disposed = true;
            }
            finally
            {
                _writeGate.Release();
                _writeGate.Dispose();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            await _writeGate.WaitAsync();
            try
            {
                if (_disposed)
                    return;

                if (_writeContext != null)
                    await _writeContext.DisposeAsync();

                _writeContext = null;
                _disposed = true;
            }
            finally
            {
                _writeGate.Release();
                _writeGate.Dispose();
            }
        }
    }
}
