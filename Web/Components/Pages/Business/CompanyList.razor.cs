using Core.Entities.General;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Web.Extensions;

namespace Web.Components.Pages.Business
{
    public partial class CompanyList
    {
        
        public CompanyDto _model = new();
        private IEnumerable<CompanyDto> companies = new List<CompanyDto>();

        private bool isLoading;
        private bool isProcessing;

        private readonly string UploadPath = "uploads/companies";

        protected override async Task OnInitializedAsync()
        {
            await GetListAsync();
            EnsureUploadDirectory();
        }

        private void EnsureUploadDirectory()
        {
            var uploadDir = Path.Combine("wwwroot", UploadPath);
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }
        }

        private async Task GetListAsync()
        {
            isLoading = true;
            companies = _mapper.Map<IEnumerable<CompanyDto>>(await _uow.Companies.GetAllAsync());
            isLoading = false;
        }

        private void New()
        {
            _model = new CompanyDto();
        }

        private void Edit(CompanyDto item)
        {
            _model = new CompanyDto()
            {
                Id = item.Id,
                Name = item.Name,
                NameAr = item.NameAr,
                Logo = item.Logo,
                CR = item.CR,
                Address = item.Address,
                PO = item.PO,
                PoCode = item.PoCode,
                PhoneNumber = item.PhoneNumber,
                FaxNumber = item.FaxNumber,
                Email = item.Email,
                ReceiptFooter = item.ReceiptFooter
            };
        }

        private async Task OnLogoSelected(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;
                if (file == null)
                    return;

                if (!FileValidator.ValidateImage(file.Name))
                {
                    _toastService.Notify(NotificationSeverity.Warning, _localizer["Error"], _localizer["InvalidFileType"]);
                    return;
                }

                if (!FileValidator.ValidateSize(file.Size, 4))
                {
                    _toastService.Notify(NotificationSeverity.Warning, _localizer["Error"], $"{_localizer["FileSizeTooLarge"]} (Max: 4MB)");
                    return;
                }

                _model.Logo = $"logo_{_model.Id}_{DateTime.UtcNow.Ticks}{Path.GetExtension(file.Name).ToLower()}";
                var fullPath = Path.Combine("wwwroot", Path.Combine(UploadPath, _model.Logo));

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

        private async Task OnReceiptFooterSelected(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;
                _toastService.Notify(NotificationSeverity.Warning, _localizer["Error"], $"{_localizer["ErrorUploadingFile"]}: {file.Name}");
                if (file == null)
                    return;

                if (!FileValidator.ValidateImage(file.Name))
                {
                    _toastService.Notify(NotificationSeverity.Warning, _localizer["Error"], _localizer["InvalidFileType"]);
                    return;
                }

                if (!FileValidator.ValidateSize(file.Size, 4))
                {
                    _toastService.Notify(NotificationSeverity.Warning, _localizer["Error"], $"{_localizer["FileSizeTooLarge"]} (Max: 4MB)");
                    return;
                }

                _model.ReceiptFooter = $"receipt_footer_{_model.Id}_{DateTime.UtcNow.Ticks}{Path.GetExtension(file.Name).ToLower()}";
                var fullPath = Path.Combine("wwwroot", Path.Combine(UploadPath, _model.ReceiptFooter));

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

                if (_model.Id == 0)
                {
                    if (await _uow.Companies.Existing(x => x.Name.ToLower() == _model.Name.Trim().ToLower()))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }


                    await _uow.Companies.AddAsync(new Company()
                    {
                        Name = _model.Name,
                        NameAr = _model.NameAr,
                        Logo = _model.Logo,
                        CR = _model.CR,
                        Address = _model.Address,
                        PO = _model.PO,
                        PoCode = _model.PoCode,
                        PhoneNumber = _model.PhoneNumber,
                        FaxNumber = _model.FaxNumber,
                        Email = _model.Email,
                        ReceiptFooter = _model.ReceiptFooter,
                        CreatedBy = await _userService.GetUserIdAsync(),
                        CreatedOn = DateTime.UtcNow.GetKsaDateTime()
                    });
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
                    if (await _uow.Companies.Existing(x => x.Name.ToLower() == _model.Name.Trim().ToLower() && x.Id != _model.Id))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }

                    var company = await _uow.Companies.FindAsync(x => x.Id == _model.Id);
                    if (company is null)
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["NotFound"], _localizer["NotFound"]);
                        return;
                    }

                    _mapper.Map(_model, company);
                    company.UpdatedBy = await _userService.GetUserIdAsync();
                    company.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();

                    await _uow.Companies.Update(company);
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