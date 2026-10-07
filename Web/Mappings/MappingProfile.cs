using Core.Entities.Discounts;
using Core.Entities.General;
using Core.Entities.Workflow;
using Shared.Dtos.Account;
using Shared.Dtos.Discounts;
using Shared.Dtos.Workflow;



namespace Web.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            #region Account
            CreateMap<ApplicationUser, ApplicationUserDto>().ReverseMap();
            CreateMap<IdentityRole, RoleDto>().ReverseMap();
            #endregion Account

            #region Business
            // Student Mappings
            CreateMap<Company, BasicDto>().ReverseMap();

            // Company Mappings
            CreateMap<Company, CompanyDto>().ReverseMap();
            CreateMap<Bus, BusDto>().ReverseMap();
            CreateMap<BusDriver, BusDriverDto>().ReverseMap();
            CreateMap<BusAssignment, BusAssignmentDto>().ReverseMap();
            CreateMap<School, SchoolDto>().ReverseMap();
            CreateMap<School, BasicGuidDto>().ReverseMap();
            CreateMap<Parent, BasicGuidDto>().ReverseMap();
            CreateMap<SchoolLevel, BasicDto>().ReverseMap();
            CreateMap<Service, BasicDto>().ReverseMap();
            CreateMap<Nationality, BasicDto>().ReverseMap();
            CreateMap<DiscountType, BasicByteDto>().ReverseMap();
            CreateMap<DiscountDependency, BasicByteDto>().ReverseMap();
            CreateMap<TransactionType, BasicDto>().ReverseMap();
            
            CreateMap<SchoolClass, BasicDto>()
                .ForMember(d => d.Name, opt => opt.MapFrom(src => src.Level.Name != null ? $"{src.Level.Name} - {src.Name}" : src.Name))
                .ForMember(d => d.NameAr, opt => opt.MapFrom(src => src.Level.NameAr != null ? $"{src.Level.NameAr} - {src.NameAr}" : src.NameAr));

            CreateMap<SchoolSemester, BasicByteDto>()
                .ForMember(d => d.Id, opt => opt.MapFrom(src => src.SemesterId))
                .ForMember(d => d.Name, opt => opt.MapFrom(src => src.Semester.Name))
                .ForMember(d => d.NameAr, opt => opt.MapFrom(src => src.Semester.NameAr));

            CreateMap<School, SchoolDto>()
                .ForMember(d => d.CompanyName, opt => opt.MapFrom(src => src.Company.Name))
                .ForMember(d => d.CompanyNameAr, opt => opt.MapFrom(src => src.Company.NameAr))
                .ForMember(d => d.TypeName, opt => opt.MapFrom(src => src.Type.Name))
                .ForMember(d => d.TypeNameAr, opt => opt.MapFrom(src => src.Type.NameAr))
                .ForMember(d => d.CityName, opt => opt.MapFrom(src => src.City.Name))
                .ForMember(d => d.CityNameAr, opt => opt.MapFrom(src => src.City.NameAr));
            CreateMap<SchoolDto, School>();

            CreateMap<Parent, ParentDto>()
               .ForMember(d => d.Title, opt => opt.MapFrom(src => src.Gender.Title))
               .ForMember(d => d.TitleAr, opt => opt.MapFrom(src => src.Gender.TitleAr))
               .ForMember(d => d.NationalityName, opt => opt.MapFrom(src => src.Nationality.Name))
               .ForMember(d => d.NationalityNameAr, opt => opt.MapFrom(src => src.Nationality.NameAr));
            CreateMap<ParentDto, Parent>();

            CreateMap<Student, BasicGuidDto>()
               .ForMember(d => d.Name, opt => opt.MapFrom(src => $"{src.School.Code}-{src.SN.ToString("D5")} : {src.Name} {src.FatherName}"))
               .ForMember(d => d.NameAr, opt => opt.MapFrom(src => $"{src.School.Code}-{src.SN.ToString("D5")} : {src.Name} {src.FatherName}"));

            CreateMap<Student, StudentListDto>()
               .ForMember(d => d.Name, opt => opt.MapFrom(src => $"{src.School.Code}-{src.SN.ToString("D5")} : {src.Name} {src.FatherName}"));

            CreateMap<StudentDocument, StudentDocumentDto>()
                .ForMember(d => d.StudentName, opt => opt.MapFrom(src => $"{src.Student.Name} {src.Student.FatherName}"))
                .ForMember(d => d.TypeName, opt => opt.MapFrom(src => src.Type.Name))
                .ForMember(d => d.TypeNameAr, opt => opt.MapFrom(src => src.Type.NameAr))
                .ForMember(d => d.YearName, opt => opt.MapFrom(src => src.Year.Name))
                .ForMember(d => d.StatusName, opt => opt.MapFrom(src => src.Status.Name))
                .ForMember(d => d.StatusNameAr, opt => opt.MapFrom(src => src.Status.NameAr))
                .ForMember(d => d.ColorCode, opt => opt.MapFrom(src => src.Status.ColorCode));
            CreateMap<StudentDocumentDto, StudentDocument>();
            
            CreateMap<Parent, ParentListDto>()
               .ForMember(d => d.NationalIDName, opt => opt.MapFrom(src => $"{src.NationalID} : {src.Name}"))
               .ForMember(d => d.MobileName, opt => opt.MapFrom(src => $"{src.Mobile} : {src.Name}"));
            
            CreateMap<Student, StudentDto>()
               .ForMember(d => d.Code, opt => opt.MapFrom(src => $"{src.School.Code}-{src.SN.ToString("D5")}"))
               .ForMember(d => d.ParentName, opt => opt.MapFrom(src => src.Parent.Name))
               .ForMember(d => d.ParentMobile, opt => opt.MapFrom(src => src.Parent.Mobile))
               .ForMember(d => d.ParentAddress, opt => opt.MapFrom(src => src.Parent.HomeAddress))
               .ForMember(d => d.SchoolName, opt => opt.MapFrom(src => src.School.Name))
               .ForMember(d => d.SchoolNameAr, opt => opt.MapFrom(src => src.School.NameAr))
               .ForMember(d => d.NationalityName, opt => opt.MapFrom(src => src.Nationality.Name))
               .ForMember(d => d.NationalityNameAr, opt => opt.MapFrom(src => src.Nationality.NameAr))
               
               .ForMember(d => d.StatusId, opt => opt.MapFrom(src => src.CurrentStatusId))
               .ForMember(d => d.StatusName, opt => opt.MapFrom(src => src.CurrentStatus.Name))
               .ForMember(d => d.StatusNameAr, opt => opt.MapFrom(src => src.CurrentStatus.NameAr))
               .ForMember(d => d.ColorCode, opt => opt.MapFrom(src => src.CurrentStatus.ColorCode))
               .ForMember(d => d.RegisteredForYearId, opt => opt.MapFrom(src => src.CurrentContract.RegisteredForYearId))
               .ForMember(d => d.RegistrationDate, opt => opt.MapFrom(src => src.CurrentContract.StartDate))
               .ForMember(d => d.ClassId, opt => opt.MapFrom(src => src.CurrentContract.ClassId))
               .ForMember(d => d.ClassSN, opt => opt.MapFrom(src => src.CurrentContract.Class.SN))
               .ForMember(d => d.LevelSN, opt => opt.MapFrom(src => src.CurrentContract.Class.Level.SN))
               .ForMember(d => d.ClassName, opt => opt.MapFrom(src => $"{src.CurrentContract.Class.Level.Name} - {src.CurrentContract.Class.Name}"))
               .ForMember(d => d.ClassNameAr, opt => opt.MapFrom(src => $"{src.CurrentContract.Class.Level.NameAr} - {src.CurrentContract.Class.NameAr}"))
               .ForMember(d => d.Semesters, opt => opt.MapFrom(src => src.CurrentContract.Details.Select(x=> x.SemesterId).Distinct().ToList()));
          CreateMap<StudentDto, Student>();

            CreateMap<SchoolLevel, SchoolLevelDto>()
                .ForMember(d => d.SchoolName, opt => opt.MapFrom(src => src.School.Name))
                .ForMember(d => d.SchoolNameAr, opt => opt.MapFrom(src => src.School.NameAr))
                .ForMember(d => d.SchoolCode, opt => opt.MapFrom(src => src.School.Code));
            CreateMap<SchoolLevelDto, SchoolLevel>();

            CreateMap<SchoolClass, SchoolClassDto>()
                .ForMember(d => d.LevelName, opt => opt.MapFrom(src => src.Level.Name))
                .ForMember(d => d.LevelNameAr, opt => opt.MapFrom(src => src.Level.NameAr))
                .ForMember(d => d.SchoolId, opt => opt.MapFrom(src => src.Level.SchoolId))
                .ForMember(d => d.SchoolName, opt => opt.MapFrom(src => src.Level.School.Name))
                .ForMember(d => d.SchoolNameAr, opt => opt.MapFrom(src => src.Level.School.NameAr))
                .ForMember(d => d.LevelSN, opt => opt.MapFrom(src => src.Level.SN));
            CreateMap<SchoolClassDto, SchoolClass>();

            CreateMap<Service, ServiceDto>()
                .ForMember(d => d.SchoolName, opt => opt.MapFrom(src => src.School.Name))
                .ForMember(d => d.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
                .ForMember(d => d.CategoryNameAr, opt => opt.MapFrom(src => src.Category.NameAr))
                .ForMember(d => d.YearName, opt => opt.MapFrom(src => src.Year.Name));
            CreateMap<ServiceDto, Service>();

            CreateMap<ServicePrice, ServicePriceDto>()
                .ForMember(d => d.ClassName, opt => opt.MapFrom(src => $"{src.Class.Level.Name} - {src.Class.Name}"))
               .ForMember(d => d.ClassNameAr, opt => opt.MapFrom(src => $"{src.Class.Level.NameAr} - {src.Class.NameAr}"))
                .ForMember(d => d.SemesterName, opt => opt.MapFrom(src => src.Semester.Name))
                .ForMember(d => d.SemesterNameAr, opt => opt.MapFrom(src => src.Semester.NameAr));
            CreateMap<ServicePriceDto, ServicePrice>();

            CreateMap<Discount, DiscountDto>()
                .ForMember(d => d.TransactionTypeName, opt => opt.MapFrom(src => src.TransactionType.Name))
                .ForMember(d => d.TransactionTypeNameAr, opt => opt.MapFrom(src => src.TransactionType.NameAr))
                .ForMember(d => d.DependencyName, opt => opt.MapFrom(src => src.Dependency.Name))
                .ForMember(d => d.DependencyNameAr, opt => opt.MapFrom(src => src.Dependency.NameAr))
                .ForMember(d => d.ServiceCategoryName, opt => opt.MapFrom(src => src.ServiceCategory.Name))
                .ForMember(d => d.ServiceCategoryNameAr, opt => opt.MapFrom(src => src.ServiceCategory.NameAr))
                .ForMember(d => d.TypeName, opt => opt.MapFrom(src => src.Type.Name))
                .ForMember(d => d.TypeNameAr, opt => opt.MapFrom(src => src.Type.NameAr));
            CreateMap<DiscountDto, Discount>();

            CreateMap<DiscountDetail, DiscountDetailDto>()
                .ForMember(d => d.SemesterName, opt => opt.MapFrom(src => src.Semester.Name))
               .ForMember(d => d.SemesterNameAr, opt => opt.MapFrom(src => src.Semester.NameAr));
            CreateMap<DiscountDetailDto, DiscountDetail>();

            CreateMap<DiscountRequest, DiscountRequestDto>()
                .ForMember(d => d.DiscountName, opt => opt.MapFrom(src => src.Discount == null ? null : src.Discount.Name))
                .ForMember(d => d.DiscountNameAr, opt => opt.MapFrom(src => src.Discount == null ? null : src.Discount.NameAr))
                .ForMember(d => d.StatusName, opt => opt.MapFrom(src => src.WfProcess.Status.Name))
                .ForMember(d => d.ColorCode, opt => opt.MapFrom(src => src.WfProcess.Status.ColorCode));
            CreateMap<DiscountRequestDto, DiscountRequest>();

            CreateMap<DiscountRequestDetail, DiscountRequestDetailDto>()
                .ForMember(d => d.IsSelected, opt => opt.MapFrom(_ => true))
                .ForMember(d => d.HasConfiguredPercentage, opt => opt.MapFrom(src => src.Percentage > 0))
                .ForMember(d => d.SemesterName, opt => opt.MapFrom(src => src.Semester.Name))
                .ForMember(d => d.SemesterNameAr, opt => opt.MapFrom(src => src.Semester.NameAr));
            CreateMap<DiscountRequestDetailDto, DiscountRequestDetail>();

            #endregion Business

            #region General
            CreateMap<DocumentType, BasicDto>().ReverseMap();
            CreateMap<Bank, BasicDto>().ReverseMap();
            CreateMap<City, BasicDto>().ReverseMap();
            CreateMap<Nationality, BasicDto>().ReverseMap();
            CreateMap<District, BasicDto>().ReverseMap();
            CreateMap<ServiceCategory, BasicDto>().ReverseMap();
            CreateMap<Gender, TitleDto>()
             .ForMember(d => d.Name, opt => opt.MapFrom(src => src.Title))
             .ForMember(d => d.NameAr, opt => opt.MapFrom(src => src.TitleAr));


            CreateMap<Semester, BasicByteDto>().ReverseMap();
            CreateMap<Gender, BasicByteDto>().ReverseMap();
            CreateMap<DocumentStatus, BasicByteDto>().ReverseMap();
            CreateMap<Semester, BasicByteDto>().ReverseMap();
            CreateMap<Year, BasicDto>().ReverseMap();

           
            CreateMap<Year, YearDto>()
              .ForMember(d => d.Semesters, opt => opt.MapFrom(src => src.Semesters));
            CreateMap<YearDto, Year>();

            CreateMap<SchoolSemester, SemesterDto>()
              .ForMember(d => d.Name, opt => opt.MapFrom(src => src.Semester.Name))
              .ForMember(d => d.NameAr, opt => opt.MapFrom(src => src.Semester.NameAr));
            CreateMap<SemesterDto, SchoolSemester>();

            CreateMap<SchoolType, BasicByteDto>().ReverseMap();
            CreateMap<BusType, BasicByteDto>().ReverseMap();
            CreateMap<District, DistrictDto>()
              .ForMember(d => d.CityName, opt => opt.MapFrom(src => src.City.Name))
              .ForMember(d => d.CityNameAr, opt => opt.MapFrom(src => src.City.NameAr));
            CreateMap<DistrictDto, District>();


            CreateMap<ServiceVAT, ServiceTaxDto>()
               .ForMember(d => d.ServiceName, opt => opt.MapFrom(src => src.Service.Name))
               .ForMember(d => d.ServiceNameAr, opt => opt.MapFrom(src => src.Service.NameAr))
               .ForMember(d => d.NationalityName, opt => opt.MapFrom(src => src.Nationality.Name))
               .ForMember(d => d.NationalityNameAr, opt => opt.MapFrom(src => src.Nationality.NameAr));
            CreateMap<ServiceTaxDto, ServiceVAT>();
            #endregion General

            #region Workflow
            CreateMap<WorkflowTask, BasicDto>().ReverseMap();
            CreateMap<WorkflowType, BasicDto>().ReverseMap();
            CreateMap<WorkflowStatus, BasicByteDto>().ReverseMap();
            CreateMap<WorkflowAction, BasicDto>().ReverseMap();

            CreateMap<RoleWorkflowType, BasicDto>()
               .ForMember(d => d.Id, opt => opt.MapFrom(src => src.WorkflowType.Id))
              .ForMember(d => d.Name, opt => opt.MapFrom(src => src.WorkflowType.Name));

            CreateMap<WorkflowPermission, BasicDto>()
                .ForMember(d => d.Id, opt => opt.MapFrom(src => src.PermissionId))
               .ForMember(d => d.Name, opt => opt.MapFrom(src => src.WorkflowTask.Name));

            CreateMap<Permission, PermissionDto>().ReverseMap();

            CreateMap<WorkflowProcessTask, BasicGuidDto>()
               .ForMember(d => d.Id, opt => opt.MapFrom(src => src.Id))
               .ForMember(d => d.Name, opt => opt.MapFrom(src => src.WfTask.Name));

            CreateMap<WorkflowProcessTask, WorkflowProcessDto>()
                .ForMember(d => d.TaskId, opt => opt.MapFrom(src => src.Id))

                .ForMember(d => d.ProcessId, opt => opt.MapFrom(src => src.WfProcessId))
                .ForMember(d => d.RefNo, opt => opt.MapFrom(src => src.WfProcess.RefNo))
                .ForMember(d => d.RequestedForId, opt => opt.MapFrom(src => src.WfProcess.RequestedForId))
                .ForMember(d => d.CurrentTaskId, opt => opt.MapFrom(src => src.WfTaskId))
                .ForMember(d => d.CurrentTaskName, opt => opt.MapFrom(src => src.WfTask.Name))
                .ForMember(d => d.CurrentTaskSN, opt => opt.MapFrom(src => src.WfTask.SN))
                .ForMember(d => d.ActionId, opt => opt.MapFrom(src => src.WfTask.ActionId))
                .ForMember(d => d.StatusName, opt => opt.MapFrom(src => src.WfProcess.Status.Name))
                .ForMember(d => d.ColorCode, opt => opt.MapFrom(src => src.WfProcess.Status.ColorCode))
                .ForMember(d => d.WorkflowTypeId, opt => opt.MapFrom(src => src.WfProcess.Workflow.TypeId));


            CreateMap<WorkflowProcess, WorkflowProcessDto>()
                .ForMember(d => d.ProcessId, opt => opt.MapFrom(src => src.Id))
                //.ForMember(d => d.TaskName, opt => opt.MapFrom(src => src.CurrentTask.Name))
                //.ForMember(d => d.TaskSN, opt => opt.MapFrom(src => src.CurrentTask.SN))
                //.ForMember(d => d.ActionId, opt => opt.MapFrom(src => src.CurrentTask.ActionId))
                .ForMember(d => d.StatusName, opt => opt.MapFrom(src => src.Status.Name))
                .ForMember(d => d.ColorCode, opt => opt.MapFrom(src => src.Status.ColorCode))
                .ForMember(d => d.WorkflowTypeId, opt => opt.MapFrom(src => src.Workflow.TypeId));
            CreateMap<WorkflowProcessDto, WorkflowProcess>();

            CreateMap<WorkflowHistory, WorkflowHistoryDto>()
                .ForMember(d => d.UserName, opt => opt.MapFrom(src => src.Creator.Name))
                .ForMember(d => d.ActionTypeName, opt => opt.MapFrom(src => src.Action.Name));
            CreateMap<WorkflowHistoryDto, WorkflowHistory>();

            CreateMap<Workflow, WorkflowDto>()
               .ForMember(d => d.TypeName, opt => opt.MapFrom(src => src.Type.Name));

            CreateMap<WorkflowDto, Workflow>();

            CreateMap<WorkflowTask, WorkflowTaskDto>()
              .ForMember(d => d.ActionName, opt => opt.MapFrom(src => src.Action.Name));
            CreateMap<WorkflowTaskDto, WorkflowTask>();



            CreateMap<WorkflowProcessTask, InboxDto>()
               .ForMember(t => t.RefNo, opt => opt.MapFrom(src => src.WfProcess.RefNo))
               .ForMember(t => t.SchoolName, opt => opt.MapFrom(src => src.WfProcess.RequestedFor.School.Name))
               .ForMember(t => t.SchoolNameAr, opt => opt.MapFrom(src => src.WfProcess.RequestedFor.School.NameAr))
               .ForMember(t => t.RequesterName, opt => opt.MapFrom(src => src.WfProcess.RequestedFor.Name))
               .ForMember(t => t.TypeName, opt => opt.MapFrom(src => src.WfProcess.Workflow.Type.Name))
               .ForMember(t => t.TypeNameAr, opt => opt.MapFrom(src => src.WfProcess.Workflow.Type.Name))
               .ForMember(t => t.TaskName, opt => opt.MapFrom(src => src.WfTask.Name))
               .ForMember(t => t.TaskNameAr, opt => opt.MapFrom(src => src.WfTask.Name))
               .ForMember(t => t.StatusName, opt => opt.MapFrom(src => src.Status.Name))
               .ForMember(t => t.StatusNameAr, opt => opt.MapFrom(src => src.Status.NameAr))
               .ForMember(t => t.ColorCode, opt => opt.MapFrom(src => src.Status.ColorCode))
               .ForMember(t => t.RequiredDays, opt => opt.MapFrom(src => src.WfTask.RequiredDays))
               .ForMember(t => t.SharedWithName, opt => opt.MapFrom(src => src.SharedWithUser.Name));
            CreateMap<BasicDto, WorkflowProcessTask>().ReverseMap();

            CreateMap<WorkflowProcess, InboxDto>()
                .ForMember(t => t.WfProcessId, opt => opt.MapFrom(src => src.Id))
              .ForMember(t => t.RefNo, opt => opt.MapFrom(src => src.RefNo))
              .ForMember(t => t.RequesterName, opt => opt.MapFrom(src => src.RequestedFor.Name))
              .ForMember(t => t.TypeName, opt => opt.MapFrom(src => src.Workflow.Type.Name))
              .ForMember(t => t.TypeNameAr, opt => opt.MapFrom(src => src.Workflow.Type.Name))
              //.ForMember(t => t.TaskName, opt => opt.MapFrom(src => src.CurrentTask.Name))
              .ForMember(t => t.StatusName, opt => opt.MapFrom(src => src.Status.Name))
              .ForMember(t => t.StatusNameAr, opt => opt.MapFrom(src => src.Status.NameAr))
              .ForMember(t => t.ColorCode, opt => opt.MapFrom(src => src.Status.ColorCode))
              .ForMember(t => t.CurrentTasks, opt => opt.MapFrom(src =>
              src.WorkflowProcessTasks.Where(x => x.StatusId == 2).Select(task => new BasicDto
              {
                  Name = task.WfTask.Name,
              }).ToList()
              ));

            CreateMap<WorkflowProcessTask, TrackProcessDto>()
                .ForMember(d => d.TaskSN, opt => opt.MapFrom(src => src.WfTask.SN))
                .ForMember(d => d.TaskName, opt => opt.MapFrom(src => src.WfTask.Name))
                .ForMember(d => d.StatusName, opt => opt.MapFrom(src => src.Status.Name))
                .ForMember(d => d.ColorCode, opt => opt.MapFrom(src => src.Status.ColorCode))
                .ForMember(d => d.CompleteDate, opt => opt.MapFrom(src => src.EndDate));

            CreateMap<WorkflowProcess, WfProcessSummaryDto>()
             .ForMember(d => d.ProcessId, opt => opt.MapFrom(src => src.Id))
             .ForMember(d => d.TypeName, opt => opt.MapFrom(src => src.Workflow.Type.Name))
             .ForMember(d => d.StatusName, opt => opt.MapFrom(src => src.Status.Name))
             .ForMember(d => d.ColorCode, opt => opt.MapFrom(src => src.Status.ColorCode));


            CreateMap<WorkflowFutureSharing, WorkflowFutureSharingDto>()
               .ForMember(t => t.TypeName, opt => opt.MapFrom(src => src.WorkflowTask.Workflow.Type.Name))
               .ForMember(t => t.TaskName, opt => opt.MapFrom(src => src.WorkflowTask.Name))
               .ForMember(t => t.WfVersion, opt => opt.MapFrom(src => src.WorkflowTask.Workflow.Version))
               .ForMember(t => t.SharedWithName, opt => opt.MapFrom(src => src.SharedWithUser.Name));

            #endregion Workflow

        }
    }
}
