using Web.Extensions;

namespace Web.Components.Pages.General
{
    public partial class ManageTax
    {

        public ServiceTaxDto _model = new();
        private IEnumerable<ServiceTaxDto> vats = new List<ServiceTaxDto>();
        private IEnumerable<BasicDto> services = new List<BasicDto>();
        private IEnumerable<BasicDto> nationalities = new List<BasicDto>();

        private bool isLoading;


        protected override async Task OnInitializedAsync()
        {
            await GetLookupData();
        }

        private async Task GetLookupData()
        {
            services = _mapper.Map<IEnumerable<BasicDto>>(await _uow.Services.FindAllAsync(x=> x.Id > 19));
            nationalities = _mapper.Map<IEnumerable<BasicDto>>(await _uow.Nationalities.GetAllAsync());
        }

        private async Task GetListAsync()
        {
            isLoading = true;
            var list = await _uow.ServiceVATs.FindAllAsync(x=> x.ServiceId == _model.ServiceId && x.SchoolId == _appStateService.SchoolId
            , new[] { "Service", "Nationality" });
            vats = _mapper.Map<IEnumerable<ServiceTaxDto>>(list);
            isLoading = false;
        }

        private void Edit(ServiceTaxDto item)
        {
            _model = new ServiceTaxDto()
            {
                SchoolId = item.SchoolId,
                ServiceId = item.ServiceId,
                NationalityId = item.NationalityId,
                VAT = item.VAT,
                ServiceName = item.ServiceName,
                ServiceNameAr = item.ServiceNameAr,
                NationalityName = item.NationalityName,
                NationalityNameAr = item.NationalityNameAr
            };
        }

        private async Task SaveDataAsync()
        {
            try
            {
                if (_model.ServiceId == 0 || !_model.Nationalities.Any() || _model.VAT == 0)
                {
                    _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], _localizer["SelectServiceAndNationaltiesAndVAT"]);
                }
                
                isLoading = true;
                var existingList = await _uow.ServiceVATs.FindAllAsync(x => x.ServiceId == _model.ServiceId && _model.Nationalities.Contains(x.NationalityId) && x.SchoolId == _appStateService.SchoolId);

                List<ServiceVAT> newList = new();
                List<ServiceVAT> updatedList = new();

                string userId = await _userService.GetUserIdAsync();
                DateTime creationDate = DateTime.UtcNow.GetKsaDateTime();

                foreach (var nationalityId in _model.Nationalities)
                {
                    var item = existingList.FirstOrDefault(x => x.NationalityId == nationalityId);
                    if (item != null)
                    {
                        item.VAT = _model.VAT;
                        item.UpdatedBy = userId;
                        item.UpdatedOn = creationDate;
                        updatedList.Add(item);
                    }
                    else
                    {
                        newList.Add(new ServiceVAT
                        {
                            SchoolId = _appStateService.SchoolId,
                            ServiceId = _model.ServiceId,
                            NationalityId = nationalityId,
                            VAT = _model.VAT,
                            CreatedBy = userId,
                            CreatedOn = creationDate
                        });
                    }
                }

                if (newList.Any())
                    await _uow.ServiceVATs.AddRangeAsync(newList);

                if (updatedList.Any())
                    await _uow.ServiceVATs.UpdateRange(updatedList);

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
                isLoading = false;
            }
        }

    }
}