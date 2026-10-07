using Core.Entities.General;
using Microsoft.JSInterop;
using Web.Extensions;

namespace Web.Components.Pages.General
{
    public partial class DocumentTypeList
    {
       
        public BasicDto _model = new();
        private IEnumerable<BasicDto> documentTypes = new List<BasicDto>();

        private bool isLoading;
        private bool isProcessing;

        protected override async Task OnInitializedAsync()
        {
            await GetListAsync();
        }

        private async Task GetListAsync()
        {
            isLoading = true;
            documentTypes = _mapper.Map<IEnumerable<BasicDto>>(await _uow.DocumentTypes.GetAllAsync());
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
                    if (await _uow.DocumentTypes.Existing(x => x.Name.ToLower() == _model.Name.Trim().ToLower() || x.NameAr.Trim().ToLower() == _model.NameAr.Trim().ToLower()))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }

                    var documentType = new DocumentType()
                    {
                        Name = _model.Name,
                        NameAr = _model.NameAr,
                        CreatedBy = await _userService.GetUserIdAsync(),
                        CreatedOn = DateTime.UtcNow.GetKsaDateTime()
                    };

                    await _uow.DocumentTypes.AddAsync(documentType);
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
                    if (await _uow.DocumentTypes.Existing(x => (x.Name.ToLower() == _model.Name.Trim().ToLower() || x.NameAr.Trim().ToLower() == _model.NameAr.Trim().ToLower()) && x.Id != _model.Id))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }

                    var documentType = await _uow.DocumentTypes.FindAsync(x => x.Id == _model.Id);
                    if (documentType is null)
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["NotFound"], _localizer["NotFound"]);
                        return;
                    }

                    documentType.Name = _model.Name;
                    documentType.NameAr = _model.NameAr;
                    documentType.UpdatedBy = await _userService.GetUserIdAsync();
                    documentType.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();

                    await _uow.DocumentTypes.Update(documentType);
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
