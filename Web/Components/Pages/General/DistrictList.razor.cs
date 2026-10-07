using Core.Entities.General;
using Microsoft.JSInterop;
using Web.Extensions;

namespace Web.Components.Pages.General
{
    public partial class DistrictList
    {
       
        public DistrictDto _model = new();
        private IEnumerable<DistrictDto> districts = new List<DistrictDto>();
        private IEnumerable<BasicDto> cities = new List<BasicDto>();

        IList<int> selectedCities = new List<int>();
        private bool isLoading;
        private bool isProcessing;

        protected override async Task OnInitializedAsync()
        {
            await GetCitiesAsync();
            await GetListAsync();
        }

        private async Task GetCitiesAsync()
        {
            cities = _mapper.Map<IEnumerable<BasicDto>>(await _uow.Cities.GetAllAsync());
        }

        private async Task GetListAsync()
        {
            isLoading = true;
            var list = await _uow.Districts.FindAllAsync(x => !selectedCities.Any() || selectedCities.Contains(x.CityId), new[] { "City"});
            districts = _mapper.Map<IEnumerable<DistrictDto>>(list);
            isLoading = false;
        }

        private void New()
        {
            _model = new DistrictDto();
        }

        private void Edit(DistrictDto item)
        {
            _model = new DistrictDto()
            {
                Id = item.Id,
                Name = item.Name,
                NameAr = item.NameAr,
                CityId = item.CityId
            };
        }

        private async Task SaveDataAsync()
        {
            try
            {
                isProcessing = true;

                if (_model.CityId == 0)
                {
                    _toastService.Notify(NotificationSeverity.Warning, _localizer["Error"], _localizer["PleaseSelectCity"]);
                    return;
                }

                if (_model.Id == 0)
                {
                    if (await _uow.Districts.Existing(x => x.Name.ToLower() == _model.Name.Trim().ToLower() && x.CityId == _model.CityId))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }

                    var district = new District()
                    {
                        Name = _model.Name,
                        NameAr = _model.NameAr,
                        CityId = _model.CityId,
                        CreatedBy = await _userService.GetUserIdAsync(),
                        CreatedOn = DateTime.UtcNow.GetKsaDateTime()
                    };

                    await _uow.Districts.AddAsync(district);
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
                    if (await _uow.Districts.Existing(x => x.Name.ToLower() == _model.Name.Trim().ToLower() && x.CityId == _model.CityId && x.Id != _model.Id))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }

                    var district = await _uow.Districts.FindAsync(x => x.Id == _model.Id);
                    if (district is null)
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["NotFound"], _localizer["NotFound"]);
                        return;
                    }

                    district.Name = _model.Name;
                    district.NameAr = _model.NameAr;
                    district.CityId = _model.CityId;
                    district.UpdatedBy = await _userService.GetUserIdAsync();
                    district.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();

                    await _uow.Districts.Update(district);
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
