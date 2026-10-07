using Core.Entities.Account;
using Core.Entities.Business;
using Core.Entities.Discounts;
using Core.Entities.General;
using Core.Entities.Workflow;

namespace Core.Interfaces
{
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        #region Account
        IBaseRepository<RoleReport> RoleReports { get; }
        IBaseRepository<UserAudit> UserAudits { get; }
        IBaseRepository<RoleWorkflowType> RoleWorkflowTypes { get; }
        IBaseRepository<ApplicationUserLogin> UserLogins { get; }
        IBaseRepository<ApplicationUser> Users { get; }
        IBaseRepository<ApplicationRole> Roles { get; }
        IBaseRepository<ApplicationUserRole> UserRoles { get; }
        IBaseRepository<UserSchool> UserSchools { get; }
        #endregion Account

        #region Business
        IBaseRepository<Bus> Buses { get; }
        IBaseRepository<BusAssignment> BusAssignments { get; }
        IBaseRepository<BusDriver> BusDrivers { get; }
        IBaseRepository<Parent> Parents { get; }
        IBaseRepository<Receipt> Receipts { get; }
        IBaseRepository<ReceiptDetail> ReceiptDetails { get; }
        IBaseRepository<RefundReceipt> RefundReceipts { get; }
        IBaseRepository<RefundReceiptDetail> RefundReceiptDetails { get; }
        IBaseRepository<School> Schools { get; }
        IBaseRepository<SchoolClass> SchoolClasses { get; }
        IBaseRepository<SchoolClassRoom> SchoolClassRooms { get; }
        IBaseRepository<SchoolLevel> SchoolLevels { get; }
        IBaseRepository<SchoolSemester> SchoolSemesters { get; }
        IBaseRepository<Service> Services { get; }
        IBaseRepository<ServicePrice> ServicePrices { get; }
        IBaseRepository<ServiceVAT> ServiceVATs { get; }
        IBaseRepository<Student> Students { get; }
        IBaseRepository<StudentAccount> StudentAccounts { get; }
        IBaseRepository<StudentContract> StudentContracts { get; }
        IBaseRepository<StudentContractDetail> StudentContractDetails { get; }
        IBaseRepository<StudentDocument> StudentDocuments { get; }
        IBaseRepository<StudentSubscription> StudentSubscriptions { get; }
        IBaseRepository<StudentClassRoomAssignment> StudentClassRoomAssignments { get; }
        IBaseRepository<StudentBusAssignment> StudentBusAssignments { get; }
        #endregion Business

        #region Discounts
        IBaseRepository<Discount> Discounts { get; }
        IBaseRepository<DiscountDependency> DiscountDependencies { get; }
        IBaseRepository<DiscountDetail> DiscountDetails { get; }
        IBaseRepository<DiscountType> DiscountTypes { get; }
        IBaseRepository<DiscountRequest> DiscountRequests { get; }
        IBaseRepository<DiscountRequestDetail> DiscountRequestDetails { get; }
        #endregion Discounts

        #region General
        IBaseRepository<BusType> BusTypes { get; }
        IBaseRepository<Bank> Banks { get; }
        IBaseRepository<City> Cities { get; }
        IBaseRepository<Company> Companies { get; }
        IBaseRepository<District> Districts { get; }
        IBaseRepository<DocumentStatus> DocumentStatuses { get; }
        IBaseRepository<DocumentType> DocumentTypes { get; }
        IBaseRepository<ErrorLog> ErrorLogs { get; }
        IBaseRepository<Gender> Genders { get; }
        IBaseRepository<Nationality> Nationalities { get; }
        IBaseRepository<PaymentMethod> PaymentMethods { get; }
        IBaseRepository<Report> Reports { get; }
        IBaseRepository<SchoolType> SchoolTypes { get; }
        IBaseRepository<Semester> Semesters { get; }
        IBaseRepository<ServiceCategory> ServiceCategories { get; }
        IBaseRepository<StudentStatus> StudentStatuses { get; }
        IBaseRepository<SystemAction> SystemActions { get; }
        IBaseRepository<SystemPage> SystemPages { get; }
        IBaseRepository<TransactionType> TransactionTypes { get; }
        IBaseRepository<Year> Years { get; }
        #endregion General

        #region Workflow
        IBaseRepository<Workflow> Workflows { get; }
        IBaseRepository<WorkflowAction> WorkflowActions { get; }
        IBaseRepository<WorkflowAttachment> WorkflowAttachments { get; }
        IBaseRepository<WorkflowFutureSharing> WorkflowFutureSharings { get; }
        IBaseRepository<WorkflowHistory> WorkflowHistories { get; }
        IBaseRepository<Permission> Permissions { get; }
        IBaseRepository<WorkflowPermission> WorkflowPermissions { get; }
        IBaseRepository<WorkflowProcess> WorkflowProcesses { get; }
        IBaseRepository<WorkflowProcessTask> WorkflowProcessTasks { get; }
        IBaseRepository<WorkflowStatus> WorkflowStatuses { get; }
        IBaseRepository<WorkflowTask> WorkflowTasks { get; }
        IBaseRepository<WorkflowType> WorkflowTypes { get; }

        #endregion Workflow


        Task<int> GetNextReceiptNumberAsync(Guid schoolId, CancellationToken cancellationToken = default);
        Task<int> GetNextRefundReceiptNumberAsync(Guid schoolId, CancellationToken cancellationToken = default);
        Task DiscardChangesAsync();
        Task<long> GetNextStudentRequestNumberAsync(DateTime requestDate, CancellationToken cancellationToken = default);
        Task<bool> SaveAsync();
    }
}
