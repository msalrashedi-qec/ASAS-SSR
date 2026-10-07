using Core.Entities.Discounts;
using Shared.Dtos.Discounts;


namespace Web.Components.Pages.Discounts
{
    public partial class DiscountConfiguration
    {

        [Parameter]
        public int Id { get; set; }



        private DiscountDto discount = new();
        private DiscountDetailDto _model = new();
        private IEnumerable<BasicByteDto> semesters = new List<BasicByteDto>();
       
        private bool isLoading;
        private bool isProcessing;
        private int currentYearId;


        protected override async Task OnInitializedAsync()
        {
            try
            {
                var currentYear = await _uow.Years.FindAsync(x => x.IsCurrent);
                if (currentYear is null)
                {
                    _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = _localizer["Error"], Detail = _localizer["CurrentYearNotFound"] });
                    return;
                }
                currentYearId = currentYear.Id;

                semesters = (_mapper.Map<IEnumerable<BasicByteDto>>(await _uow.SchoolSemesters.FindAllAsync(x => x.SchoolId == _appStateService.SchoolId && x.Year.IsCurrent, new[] { "Semester" }))).OrderBy(x => x.Id);
                discount = _mapper.Map<DiscountDto>(await _uow.Discounts.FindAsync(x => x.Id == Id));
                await GetListAsync();
            }
            catch (Exception ex)
            {
                _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Error", Detail = $"Failed to load discount: {ex.Message}" });
            }
        }

        private async Task GetListAsync()
        {
            try
            {
                isLoading = true;

                discount.Details = _mapper.Map<List<DiscountDetailDto>>(await _uow.DiscountDetails.FindAllAsync(x => x.DiscountId == discount.Id && x.YearId == currentYearId && x.SchoolId == _appStateService.SchoolId, new[] { "Semester" }));
            }
            catch (Exception ex)
            {
                _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Error", Detail = $"Failed to load discount: {ex.Message}" });
            }
            finally
            {
                isLoading = false;
            }
        }

        private void New()
        {
            _model = new DiscountDetailDto();
        }

        private void Edit(DiscountDetailDto item)
        {
            _model = new DiscountDetailDto()
            {
                DiscountId = item.DiscountId,
                SchoolId = item.SchoolId,
                Id = item.Id,
                SemesterId = item.SemesterId,
                FromRange = item.FromRange,
                ToRange = item.ToRange,
                Amount = item.Amount,
                AdditionalFixedAmount = item.AdditionalFixedAmount,
                Comments = item.Comments,
            };
        }

        private async Task SaveAsync()
        {
            isProcessing = true;
            try
            {
                if (_model.Id == Guid.Empty)
                {
                    var detail = _mapper.Map<DiscountDetail>(_model);
                    detail.DiscountId = Id;
                    detail.SchoolId = _appStateService.SchoolId;
                    detail.YearId = currentYearId;
                    detail.CreatedBy = await _userService.GetUserIdAsync();
                    detail.CreatedOn = DateTime.UtcNow.GetKsaDateTime();

                    await _uow.DiscountDetails.AddAsync(detail);
                    if (await _uow.SaveAsync())
                    {
                        _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = _localizer["Save"], Detail = _localizer["SavedSuccessfully"] });
                        _model = new DiscountDetailDto();
                    }
                    else
                        _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = _localizer["Save"], Detail = _localizer["Faild_to_save"] });
                }
                else
                {
                    // Update existing discount
                    var detail = await _uow.DiscountDetails.FindAsync(x => x.Id == _model.Id);
                    if (detail != null)
                    {
                        _mapper.Map(_model, detail);
                        detail.UpdatedBy = await _userService.GetUserIdAsync();
                        detail.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();
                        await _uow.DiscountDetails.Update(detail);
                        if (await _uow.SaveAsync())
                        {
                            _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = _localizer["Save"], Detail = _localizer["SavedSuccessfully"] });
                            _model = new DiscountDetailDto();
                        }
                        else
                            _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = _localizer["Save"], Detail = _localizer["Faild_to_save"] });
                    }
                }
            }
            catch (Exception ex)
            {
                _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = _localizer["Error"], Detail = ex.Message });
            }
            finally
            {
                isProcessing = false;
                await _js.InvokeVoidAsync("APP.hideModal", "DetailsModal");
                await GetListAsync();
            }
        }
        private async Task DeleteAsync()
        {
            try
            {
                isProcessing = true;
                var detail = await _uow.DiscountDetails.FindAsync(x => x.Id == _model.Id);
                if(detail == null)
                {
                    _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = _localizer["Save"], Detail = _localizer["NotFound"] });
                    return;
                }
                detail.IsDeleted = true;
                await _uow.DiscountDetails.Update(detail);
                if (await _uow.SaveAsync())
                    _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = _localizer["Save"], Detail = _localizer["SavedSuccessfully"] });
                else
                    _toastService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = _localizer["Save"], Detail = _localizer["Faild_to_save"] });
            }
            catch (Exception ex)
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
            }
            finally
            {
                isProcessing = false;
                await _js.InvokeVoidAsync("APP.hideModal", "DeleteModal");
                await GetListAsync();
            }
        }
    }
}
