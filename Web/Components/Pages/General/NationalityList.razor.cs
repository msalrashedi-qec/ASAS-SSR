using Core.Entities.General;
using Microsoft.JSInterop;
using Web.Extensions;

namespace Web.Components.Pages.General
{
    public partial class NationalityList
    {
       
        public BasicDto _model = new();
        private IEnumerable<BasicDto> nationalities = new List<BasicDto>();

        private bool isLoading;
        private bool isProcessing;

        protected override async Task OnInitializedAsync()
        {
            await GetListAsync();
        }

        private async Task GetListAsync()
        {
            isLoading = true;
            var list = await _uow.Nationalities.GetAllAsync();
            nationalities = _mapper.Map<IEnumerable<BasicDto>>(list);
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
                    if (await _uow.Nationalities.Existing(x => x.Name.ToLower() == _model.Name.Trim().ToLower() || x.NameAr.Trim().ToLower() == _model.NameAr.Trim().ToLower()))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }

                    var nationality = new Nationality()
                    {
                        Name = _model.Name,
                        NameAr = _model.NameAr,
                        CreatedBy = await _userService.GetUserIdAsync(),
                        CreatedOn = DateTime.UtcNow.GetKsaDateTime()
                    };

                    await _uow.Nationalities.AddAsync(nationality);
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
                    if (await _uow.Nationalities.Existing(x => (x.Name.ToLower() == _model.Name.Trim().ToLower() || x.NameAr.Trim().ToLower() == _model.NameAr.Trim().ToLower()) && x.Id != _model.Id))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }

                    var nationality = await _uow.Nationalities.FindAsync(x => x.Id == _model.Id);
                    if (nationality is null)
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["NotFound"], _localizer["NotFound"]);
                        return;
                    }

                    nationality.Name = _model.Name;
                    nationality.NameAr = _model.NameAr;
                    nationality.UpdatedBy = await _userService.GetUserIdAsync();
                    nationality.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();

                    await _uow.Nationalities.Update(nationality);
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
