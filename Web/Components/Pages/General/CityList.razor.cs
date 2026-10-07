using Core.Entities.General;
using Microsoft.JSInterop;
using Web.Extensions;

namespace Web.Components.Pages.General
{
    public partial class CityList
    {
       
        public BasicDto _model = new();

        private IEnumerable<BasicDto> cities = new List<BasicDto>();

        private bool isLoading;
        private bool isProcessing;
        

        protected override async Task OnInitializedAsync()
        {
            await GetListAsync();
        }
       
        private async Task GetListAsync()
        {
            isLoading = true;
            var list = await _uow.Cities.GetAllAsync();
            cities = _mapper.Map<IEnumerable<BasicDto>>(list);
            isLoading = false;
        }
        private void New()
        {
            _model = new BasicDto();
        }
        private void Edit(BasicDto item)
        {
            _model = new BasicDto()
            {
                Id = item.Id,
                Name = item.Name,
                NameAr = item.NameAr
            };
        }

        private async Task SaveDataAsync()
        {
            try
            {
                isProcessing = true;

                if (_model.Id == 0)
                {
                    if (await _uow.Cities.Existing(x => x.Name.ToLower() == _model.Name.Trim().ToLower() || x.NameAr.Trim().ToLower() == _model.NameAr.Trim().ToLower()))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, "Existing", _localizer["ItemExisting"]);
                        return;
                    }

                    var city = _mapper.Map<City>(_model);
                    city.CreatedBy = await _userService.GetUserIdAsync();
                    city.CreatedOn = DateTime.UtcNow.GetKsaDateTime();

                    await _uow.Cities.AddAsync(city);
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
                    if (await _uow.Cities.Existing(x => (x.Name.ToLower() == _model.Name.Trim().ToLower() || x.NameAr.Trim().ToLower() == _model.NameAr.Trim().ToLower()) && x.Id != _model.Id))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }
                    var city = await _uow.Cities.FindAsync(x=> x.Id == _model.Id);
                    if(city is null)
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["NotFound"], _localizer["NotFound"]);
                        return;
                    }
                    _mapper.Map(_model, city);
                    city.UpdatedBy = await _userService.GetUserIdAsync();
                    city.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();

                    await _uow.Cities.Update(city);
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