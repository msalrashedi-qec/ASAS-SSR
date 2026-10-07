using Core.Entities.General;
using Microsoft.JSInterop;
using Web.Extensions;

namespace Web.Components.Pages.General
{
    public partial class BankList
    {
        
        public BasicDto _model = new();

        private IEnumerable<BasicDto> banks = new List<BasicDto>();

        private bool isLoading;
        private bool isProcessing;
        

        protected override async Task OnInitializedAsync()
        {
            await GetListAsync();
        }
       
        private async Task GetListAsync()
        {
            isLoading = true;
            var list = await _uow.Banks.GetAllAsync();
            banks = _mapper.Map<IEnumerable<BasicDto>>(list);
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
                    if (await _uow.Banks.Existing(x => x.Name.ToLower() == _model.Name.Trim().ToLower() || x.NameAr.Trim().ToLower() == _model.NameAr.Trim().ToLower()))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }

                    var bank = _mapper.Map<Bank>(_model);
                    bank.Logo = "";
                    bank.CreatedBy = await _userService.GetUserIdAsync();
                    bank.CreatedOn = DateTime.UtcNow.GetKsaDateTime();

                    await _uow.Banks.AddAsync(bank);
                    if (await _uow.SaveAsync())
                        _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                    else
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
                }
                else
                {
                    if (await _uow.Banks.Existing(x => (x.Name.ToLower() == _model.Name.Trim().ToLower() || x.NameAr.Trim().ToLower() == _model.NameAr.Trim().ToLower()) && x.Id != _model.Id))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }
                    var bank = await _uow.Banks.FindAsync(x=> x.Id == _model.Id);
                    if(bank is null)
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["NotFound"], _localizer["NotFound"]);
                        return;
                    }
                    _mapper.Map(_model, bank);
                    bank.UpdatedBy = await _userService.GetUserIdAsync();
                    bank.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();

                    await _uow.Banks.Update(bank);
                    if (await _uow.SaveAsync())
                        _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
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
                await GetListAsync();
                isProcessing = false;
            }
        }
    }
}