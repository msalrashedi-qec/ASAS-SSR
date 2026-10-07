namespace Web.Components.Pages.Business
{
    public partial class SchoolList
    {
        
        public SchoolDto _model = new();
        public SchoolSmsDto _smsModel = new();
        private IEnumerable<SchoolDto> schools = new List<SchoolDto>();
        private IEnumerable<BasicDto> companies = new List<BasicDto>();
        private IEnumerable<BasicDto> cities = new List<BasicDto>();
        private IEnumerable<BasicByteDto> types = new List<BasicByteDto>();

        private bool isLoading;
        private bool isProcessing;

        private readonly string UploadPath = "uploads/schools";

        protected override async Task OnInitializedAsync()
        {
            await GetListAsync();
            EnsureUploadDirectory();
            await GetLookupData();
        }

        private async Task GetLookupData()
        {
            companies = _mapper.Map<IEnumerable<BasicDto>>(await _uow.Companies.GetAllAsync());
            cities = _mapper.Map<IEnumerable<BasicDto>>(await _uow.Cities.GetAllAsync());
            types = _mapper.Map<IEnumerable<BasicByteDto>>(await _uow.SchoolTypes.GetAllAsync());
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
            schools = _mapper.Map<IEnumerable<SchoolDto>>(await _uow.Schools.FindAllAsync(x=> x.Id != Guid.Empty , new[] { "Type", "Company","City"}));
            isLoading = false;
        }

        private void New()
        {
            _model = new SchoolDto();
        }

        private void Edit(SchoolDto item)
        {
            _model = new SchoolDto()
            {
                Id = item.Id,
                CompanyId = item.CompanyId,
                Code = item.Code,
                Name = item.Name,
                NameAr = item.NameAr,
                TypeId = item.TypeId,
                CityId = item.CityId,
                Logo = item.Logo,
                Address = item.Address,
                PO = item.PO,
                PoCode = item.PoCode,
                Email = item.Email,
                SmsSender = item.SmsSender,
                SmsUserName = item.SmsUserName,
                SmsPassword = item.SmsPassword,
                ChequePayeeName = item.ChequePayeeName,
                BankName = item.BankName,
                IBAN = item.IBAN
            };
        }

        private void EditSMS(SchoolDto item)
        {
            _smsModel = new SchoolSmsDto()
            {
                SchoolId = item.Id,
                Name = item.Name,
                NameAr = item.NameAr,
                SmsSender = item.SmsSender,
                SmsUserName = item.SmsUserName,
                SmsPassword = item.SmsPassword
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

        private async Task SaveDataAsync()
        {
            try
            {
                isProcessing = true;

                if (_model.Id == Guid.Empty)
                {
                    if (await _uow.Schools.Existing(x => x.Code.ToLower() == _model.Code.Trim().ToLower()))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Error"], _localizer["ThisCodeExisting"]);
                        return;
                    }

                    if (await _uow.Schools.Existing(x => x.Name.ToLower() == _model.Name.Trim().ToLower()))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }
                    var school = _mapper.Map<School>(_model);
                    school.CreatedBy = await _userService.GetUserIdAsync();
                    school.CreatedOn = DateTime.UtcNow.GetKsaDateTime();

                    await _uow.Schools.AddAsync(school);
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
                    if (await _uow.Schools.Existing(x => x.Code.ToLower() == _model.Code.Trim().ToLower() && x.Id != _model.Id))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Error"], _localizer["ThisCodeExisting"]);
                        return;
                    }

                    if (await _uow.Schools.Existing(x => x.Name.ToLower() == _model.Name.Trim().ToLower() && x.Id != _model.Id))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }

                    var school = await _uow.Schools.FindAsync(x => x.Id == _model.Id);
                    if (school is null)
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["NotFound"], _localizer["NotFound"]);
                        return;
                    }

                    _mapper.Map(_model, school);
                    school.UpdatedBy = await _userService.GetUserIdAsync();
                    school.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();

                    await _uow.Schools.Update(school);
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

        private async Task SaveSmsAsync()
        {
            try
            {
                isProcessing = true;
                
                var school = await _uow.Schools.FindAsync(x => x.Id == _smsModel.SchoolId);
                if (school is null)
                {
                    _toastService.Notify(NotificationSeverity.Warning, _localizer["NotFound"], _localizer["NotFound"]);
                    return;
                }

                school.SmsSender = _smsModel.SmsSender;
                school.SmsUserName = _smsModel.SmsUserName;
                school.SmsPassword = _smsModel.SmsPassword;
                school.UpdatedBy = await _userService.GetUserIdAsync();
                school.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();

                await _uow.Schools.Update(school);
                if (await _uow.SaveAsync())
                {
                    _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                    await GetListAsync();
                }
                else
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
            }
            catch (Exception ex)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
            }
            finally
            {
                await _js.InvokeVoidAsync("APP.hideModal", "SmsModal");
                isProcessing = false;
            }
        }

    }
}
