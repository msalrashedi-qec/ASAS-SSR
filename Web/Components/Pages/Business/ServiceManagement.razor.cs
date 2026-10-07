
namespace Web.Components.Pages.Business
{
    public partial class ServiceManagement
    {
        private ServiceDto _model = new();
        private IEnumerable<ServiceDto> services = new List<ServiceDto>();
        private IEnumerable<BasicDto> categories = new List<BasicDto>();


        private bool isLoading;
        private bool isProcessing;
       


        protected override async Task OnInitializedAsync()
        {
            categories = _mapper.Map<IEnumerable<BasicDto>>(await _uow.ServiceCategories.FindAllAsync(x=> x.Id > 19));
            await GetListAsync();
        }

        private async Task GetListAsync()
        {
            isLoading = true;
            services = _mapper.Map<IEnumerable<ServiceDto>>(await _uow.Services.FindAllAsync(x => x.CategoryId > 19 && (x.SchoolId == _appStateService.SchoolId || x.SchoolId == Guid.Empty) && (x.YearId == 0 || x.Year.IsCurrent), new[] { "Category", "Year" }));
            isLoading = false;
        }

        private void New()
        {
            _model = new ServiceDto()
            {
                SchoolId = _appStateService.SchoolId
            };
        }

        private void Edit(ServiceDto item)
        {
            _model = new ServiceDto()
            {
                Id = item.Id,
                Name = item.Name,
                NameAr = item.NameAr,
                SchoolId = item.SchoolId,
                CategoryId = item.CategoryId,
                YearId = item.YearId,
                ServicePrices = item.ServicePrices,
                DefaultTax = item.DefaultTax
            };
        }

        private async Task SaveAsync()
        {
            isProcessing = true;
            try
            {
                if (_model.Id == 0)
                {
                    var currentYear = await _uow.Years.FindAsync(x => x.IsCurrent);
                    var nationalities = await _uow.Nationalities.GetAllAsync();

                    var service = _mapper.Map<Service>(_model);
                    service.YearId = currentYear.Id;
                    service.CreatedBy = await _userService.GetUserIdAsync();
                    service.CreatedOn = DateTime.UtcNow.GetKsaDateTime();

                    foreach (var nationality in nationalities)
                    {
                        service.ServiceVATs.Add(new ServiceVAT
                        {
                            SchoolId = service.SchoolId,
                            NationalityId = nationality.Id,
                            VAT = _model.DefaultTax,
                            CreatedBy = service.CreatedBy,
                            CreatedOn = service.CreatedOn
                        });
                     }
                    
                    await _uow.Services.AddAsync(service);
                    if (await _uow.SaveAsync())
                    {
                        _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = _localizer["Save"], Detail = _localizer["SavedSuccessfully"] });
                        await GetListAsync();
                    }
                    else
                    {
                        _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = _localizer["Save"], Detail = _localizer["Faild_to_save"] });
                    }
                }
                else
                {
                    // Update existing service
                    var service = await _uow.Services.FindAsync(x => x.Id == _model.Id);
                    if (service != null)
                    {
                        _mapper.Map(_model, service);
                        
                        await _uow.Services.Update(service);
                        if (await _uow.SaveAsync())
                            _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = _localizer["Save"], Detail = _localizer["SavedSuccessfully"] });
                        else
                            _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = _localizer["Save"], Detail = _localizer["Faild_to_save"] });
                    }
                }

                _model = new ServiceDto();
                await GetListAsync();
            }
            catch (Exception ex)
            {
                _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = _localizer["Error"], Detail = ex.Message });
            }
            finally
            {
                isProcessing = false;
                await _js.InvokeVoidAsync("APP.hideModal", "DetailsModal");
            }
        }

    }
}
