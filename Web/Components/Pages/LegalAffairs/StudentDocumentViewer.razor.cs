namespace Web.Components.Pages.LegalAffairs
{
    public partial class StudentDocumentViewer
    {
        [Parameter]
        public Guid Id { get; set; }


        public StudentDocumentDto _model = new();
        private IEnumerable<StudentDocumentDto> studentDocuments = new List<StudentDocumentDto>();
        private IEnumerable<YearDto> years = new List<YearDto>();
        private StudentDocumentDto? previewDocument;
        private StudentDto student = new();

        private int selectedYearId;
        private bool isLoading;
        private string StudentCode => student.Code ?? "-";
        private string StudentFullName => student is null ? string.Empty : $"{student.Name} {student.FatherName}".Trim();
        private readonly string UploadPath = "uploads/documents";

        protected override async Task OnInitializedAsync()
        {
            await GetListAsync();
            await GetStudentData();
        }

        private async Task GetStudentData()
        {
            student = _mapper.Map<StudentDto>(await _uow.Students.FindAsync(x => x.Id == Id, ["School"]));
        }
       
        private async Task GetListAsync()
        {
            isLoading = true;
            _model = new StudentDocumentDto
            {
                StudentId = Id,
                YearId = selectedYearId
            };
            studentDocuments = _mapper.Map<IEnumerable<StudentDocumentDto>>(await _uow.StudentDocuments.FindAllAsync(x => x.StudentId == Id && x.YearId == _model.YearId,
                    new[] { "Type", "Student", "Year", "Status" }));
            isLoading = false;
        }

        private void View(StudentDocumentDto item) => previewDocument = item;

        private string GetDocumentUrl(StudentDocumentDto item)
        {
            var fileName = Uri.EscapeDataString(item.FilePath ?? string.Empty);
            return $"{_navigator.BaseUri}{UploadPath}/{fileName}";
        }

        private string GetDocumentTitle(StudentDocumentDto? item)
        {
            if (item is null)
                return _localizer["File"];

            var title = _localizer["SelectedLanguage"] == "ar" ? item.TypeNameAr : item.TypeName;
            return string.IsNullOrWhiteSpace(title) ? item.FileName : title;
        }

        private static bool IsPdf(StudentDocumentDto item) =>
            string.Equals(Path.GetExtension(item.FilePath ?? item.FileName), ".pdf", StringComparison.OrdinalIgnoreCase);

        private static bool IsImage(StudentDocumentDto item) =>
            FileValidator.ValidateImage(item.FilePath ?? item.FileName);

    }
}
