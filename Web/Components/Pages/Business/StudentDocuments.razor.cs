using static Web.Extensions.Policies;

namespace Web.Components.Pages.Business
{
    public partial class StudentDocuments
    {
        [Parameter]
        public Guid Id { get; set; }


        public StudentDocumentDto _model = new();
        private IEnumerable<StudentDocumentDto> studentDocuments = new List<StudentDocumentDto>();
        private IEnumerable<BasicDto> types = new List<BasicDto>();
        private IEnumerable<YearDto> years = new List<YearDto>();
        private StudentDocumentDto? previewDocument;
        private StudentDto student = new();

        private int selectedYearId;
        private bool isLoading;
        private bool isProcessing;
        private string StudentCode => student.Code ?? "-";
        private string StudentFullName => student is null ? string.Empty : $"{student.Name} {student.FatherName}".Trim();


        private readonly string UploadPath = "uploads/documents";

        protected override async Task OnInitializedAsync()
        {
            await GetLookupData();
            await GetListAsync();
            EnsureUploadDirectory();
        }

        private async Task GetLookupData()
        {
            types = _mapper.Map<IEnumerable<BasicDto>>(await _uow.DocumentTypes.GetAllAsync());
            years = _mapper.Map<IEnumerable<YearDto>>(await _uow.Years.FindAllAsync(x=> x.Id > 0));
            if(years.Any(x => x.IsCurrent))
            {
                var currentYear = years.FirstOrDefault(x => x.IsCurrent);
                if (currentYear != null)
                    selectedYearId = currentYear.Id;
            }

            student = _mapper.Map<StudentDto>(await _uow.Students.FindAsync(x => x.Id == Id,["School"]));
        }
        private void EnsureUploadDirectory()
        {
            var uploadDir = Path.Combine("wwwroot", UploadPath);
            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);
        }

        private async Task GetListAsync()
        {
            isLoading = true;
            _model = new StudentDocumentDto
            {
                StudentId = Id,
                YearId = selectedYearId
            };
            studentDocuments = _mapper.Map<IEnumerable<StudentDocumentDto>>(
                await _uow.StudentDocuments.FindAllAsync(
                    x => x.StudentId == Id && x.YearId == _model.YearId,
                    new[] { "Type", "Student", "Year", "Status" }));
            isLoading = false;
        }

        private void New()
        {
            _model = new StudentDocumentDto
            {
                StudentId = Id,
                YearId = selectedYearId
            };
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

        private void Edit(StudentDocumentDto item)
        {
            _model = new StudentDocumentDto()
            {
                Id = item.Id,
                StudentId = item.StudentId,
                FileName = item.FileName,
                FilePath = item.FilePath,
                TypeId = item.TypeId,
                YearId = item.YearId,
                StatusId = item.StatusId,
                Comments = item.Comments
            };
        }
        private async Task OnLogoSelected(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;
                if (file == null)
                    return;

                if (!FileValidator.ValidateImageOrPdf(file.Name))
                {
                    _toastService.Notify(NotificationSeverity.Warning, _localizer["Error"], _localizer["InvalidFileType"]);
                    return;
                }

                if (!FileValidator.ValidateSize(file.Size, 4))
                {
                    _toastService.Notify(NotificationSeverity.Warning, _localizer["Error"], $"{_localizer["FileSizeTooLarge"]} (Max: 4MB)");
                    return;
                }

                _model.FileName = file.Name;
                _model.FilePath = $"file_{_model.Id}_{DateTime.UtcNow.Ticks}{Path.GetExtension(file.Name).ToLower()}";
                var fullPath = Path.Combine("wwwroot", Path.Combine(UploadPath, _model.FilePath));

                using (var stream = file.OpenReadStream(file.Size))
                {
                    using (var fileStream = File.Create(fullPath))
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                }
            }
            catch (Exception ex)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
            }
        }

        private async Task SaveDataAsync()
        {
            try
            {
                isProcessing = true;

                if (_model.Id == Guid.Empty)
                {
                   
                    if (string.IsNullOrEmpty(_model.FilePath) || string.IsNullOrEmpty(_model.FileName))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Error"], _localizer["FileRequired"]);
                        return;
                    }
                    
                    var document = _mapper.Map<Core.Entities.Business.StudentDocument>(_model);
                    document.CreatedBy = await _userService.GetUserIdAsync();
                    document.CreatedOn = DateTime.UtcNow.GetKsaDateTime();
                    document.StatusId = 0;
                    await _uow.StudentDocuments.AddAsync(document);
                    if (await _uow.SaveAsync())
                    {
                        _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                        await GetListAsync();
                    }
                    else
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
                }
                else
                {
                    if (string.IsNullOrEmpty(_model.FilePath) || string.IsNullOrEmpty(_model.FileName))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Error"], _localizer["FileRequired"]);
                        return;
                    }

                    var document = await _uow.StudentDocuments.FindAsync(x => x.Id == _model.Id);
                    if (document is null)
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["NotFound"], _localizer["NotFound"]);
                        return;
                    }

                    _mapper.Map(_model, document);
                    document.UpdatedBy = await _userService.GetUserIdAsync();
                    document.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();

                    await _uow.StudentDocuments.Update(document);
                    if (await _uow.SaveAsync())
                    {
                        _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                        await GetListAsync();
                    }
                    else
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
                }
            }
            catch (Exception ex)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
            }
            finally
            {
                await _js.InvokeVoidAsync("APP.hideModal", "DetailsModal");
                isProcessing = false;
            }
        }

    }
}
