using Core.Entities.Discounts;
using Shared.Dtos.Discounts;
using Shared.Enums;

namespace Web.Components.Pages.Discounts
{
    public partial class DiscountManagement
    {
        private DiscountDto _model = new();
        private IEnumerable<DiscountDto> discounts = new List<DiscountDto>();
        private IEnumerable<BasicDto> serviceCategories = new List<BasicDto>();
        private IEnumerable<BasicDto> transactionTypes = new List<BasicDto>();
        private IEnumerable<BasicByteDto> types = new List<BasicByteDto>();
        private IEnumerable<BasicByteDto> dependsOnTypes = new List<BasicByteDto>();
        private static readonly DiscountApplicationTiming[] applicationTimings = Enum.GetValues<DiscountApplicationTiming>();


        private bool isLoading;
        private bool isProcessing;
       


        protected override async Task OnInitializedAsync()
        {
            transactionTypes = _mapper.Map<IEnumerable<BasicDto>>(await _uow.TransactionTypes.FindAllAsync(x => x.Id > TransactionTypeIds.AddDues));
            types = _mapper.Map<IEnumerable<BasicByteDto>>(await _uow.DiscountTypes.GetAllAsync());
            serviceCategories = _mapper.Map<IEnumerable<BasicDto>>(await _uow.ServiceCategories.GetAllAsync());
            dependsOnTypes = _mapper.Map<IEnumerable<BasicByteDto>>(await _uow.DiscountDependencies.GetAllAsync());
            await GetListAsync();
        }

        private async Task GetListAsync()
        {
            isLoading = true;
            discounts = _mapper.Map<IEnumerable<DiscountDto>>(await _uow.Discounts.FindAllAsync(x=> true, new[] { "ServiceCategory", "TransactionType", "Type", "Dependency" }));
            isLoading = false;
        }

        private void New()
        {
            _model = new DiscountDto();
        }

        private void Edit(DiscountDto item)
        {
            _model = new DiscountDto()
            {
                Id = item.Id,
                Name = item.Name,
                NameAr = item.NameAr,
                TransactionTypeId = item.TransactionTypeId,
                TypeId = item.TypeId,
                ServiceCategoryId = item.ServiceCategoryId,
                DependencyId = item.DependencyId,
                ApplicationTiming = item.ApplicationTiming,
                IsShownInContract = item.IsShownInContract,
                SN = item.SN,
                IsCancelledOnWithdrawal = item.IsCancelledOnWithdrawal
            };
                
        }

        private async Task SaveAsync()
        {
            isProcessing = true;
            try
            {
                if (_model.Id == 0)
                {
                    var discount = _mapper.Map<Discount>(_model);
                    discount.CreatedBy = await _userService.GetUserIdAsync();
                    discount.CreatedOn = DateTime.UtcNow.GetKsaDateTime();

                    await _uow.Discounts.AddAsync(discount);
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
                    // Update existing discount
                    var discount = await _uow.Discounts.FindAsync(x => x.Id == _model.Id);
                    if (discount != null)
                    {
                        _mapper.Map(_model, discount);
                        
                        await _uow.Discounts.Update(discount);
                        if (await _uow.SaveAsync())
                            _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = _localizer["Save"], Detail = _localizer["SavedSuccessfully"] });
                        else
                            _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = _localizer["Save"], Detail = _localizer["Faild_to_save"] });
                    }
                }

                _model = new DiscountDto();
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
