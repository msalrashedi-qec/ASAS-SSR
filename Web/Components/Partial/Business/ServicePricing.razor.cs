using Web.Extensions;

namespace Web.Components.Partial.Business
{
    public partial class ServicePricing
    {
        
        [Parameter]
        public int ServiceId { get; set; }

        private ServiceDto service = new();
        private ServicePriceDto _model = new();
        private IEnumerable<ServicePriceDto> servicePrices = new List<ServicePriceDto>();
        private IEnumerable<BasicByteDto> semesters = new List<BasicByteDto>();
        private IEnumerable<BasicDto> classes = new List<BasicDto>();

        private bool isProcessing;

        protected override async Task OnParametersSetAsync()
        {
            await GetService();
            await GetListAsync();
        }
        private async Task GetService()
        {
            service = _mapper.Map<ServiceDto>(await _uow.Services.FindAsync(x => x.Id == ServiceId));
            if(service == null)
            {
                _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = _localizer["Error"], Detail = _localizer["DotFound"] });
                return;
            }
            var schoolClasses = await _uow.SchoolClasses.FindAllAsync(x =>
            (x.Level.SchoolId == _appStateService.SchoolId)
            , new[] { "Level" });
            classes = _mapper.Map<IEnumerable<BasicDto>>(schoolClasses);

            semesters = (_mapper.Map<IEnumerable<BasicByteDto>>(await _uow.SchoolSemesters.FindAllAsync(x => x.SchoolId == _appStateService.SchoolId && x.Year.IsCurrent && x.SemesterId > 0, new[] { "Semester" }))).OrderBy(x => x.Id);
        }

        private async Task GetListAsync()
        {
            isProcessing = true;
            var list = await _uow.ServicePrices.FindAllAsync(x => x.ServiceId == ServiceId && x.Year.IsCurrent, new[] { "Class", "Class.Level", "Semester" });
            servicePrices = _mapper.Map<IEnumerable<ServicePriceDto>>(list.OrderBy(x=> x.SemesterId).ThenBy(x=> x.Class.SN));
            isProcessing = false;
        }
        private async Task SaveAsync()
        {
            isProcessing = true;
            try
            {
                var currentYear = await _uow.Years.FindAsync(x => x.IsCurrent);

                List<ServicePrice> newItems = new();
                string userId = await _userService.GetUserIdAsync();
                DateTime creationDate = DateTime.UtcNow.GetKsaDateTime();
                foreach (var item in _model.Classes)
                {
                    foreach (var semester in _model.Semesters)
                    {
                        newItems.Add(new ServicePrice
                        {
                            ServiceId = service.Id,
                            YearId = currentYear.Id,
                            Price = _model.Price,
                            ClassId = item,
                            SemesterId = semester,
                            CreatedBy = userId,
                            CreatedOn = creationDate
                        });
                    }
                }

                await _uow.ServicePrices.AddRangeAsync(newItems);
                if (await _uow.SaveAsync())
                {
                    _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                    _model = new();
                }
                else
                    _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Error", Detail = "Failed to save service price" });
            }
            catch (Exception ex)
            {
                _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = _localizer["Error"], Detail = ex.Message });
            }
            finally
            {
                isProcessing = false;
                await GetListAsync();
            }
        }

        private async Task EditAsync(ServicePriceDto item)
        {
            isProcessing = true;
            try
            {
                var price = await _uow.ServicePrices.FindAsync(x => x.Id == item.Id);
                if (price != null)
                {
                    price.Price = item.Price;
                    price.ClassId = item.ClassId;
                    price.SemesterId = item.SemesterId;
                    price.UpdatedBy = await _userService.GetUserIdAsync();
                    price.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();

                    await _uow.ServicePrices.Update(price);
                    if (await _uow.SaveAsync())
                        _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                    else
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
                }
            }
            catch (Exception ex)
            {
                _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = _localizer["Error"], Detail = ex.Message });
            }
            finally
            {
                isProcessing = false;
                await GetListAsync();
            }
        }
        private async Task DeletePriceAsync(ServicePriceDto item)
        {
            isProcessing = true;
            try
            {
                var price = await _uow.ServicePrices.FindAsync(x => x.Id == item.Id);
                if (price != null)
                {
                    price.IsDeleted = true;
                    price.UpdatedBy = await _userService.GetUserIdAsync();
                    price.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();

                    await _uow.ServicePrices.Update(price);
                    if (await _uow.SaveAsync())
                        _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                    else
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
                }
            }
            catch (Exception ex)
            {
                _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Error", Detail = ex.Message });
            }
            finally
            {
                isProcessing = false;
                await GetListAsync();
            }
        }
    }
}