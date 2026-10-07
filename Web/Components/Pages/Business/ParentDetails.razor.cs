namespace Web.Components.Pages.Business
{
    public partial class ParentDetails
    {
        
        [Parameter]
        public string? Id { get; set; }

        [Inject]
        private StudentParentChangeNotificationService StudentParentChanges { get; set; } = null!;


        private bool isLoading;


        private ParentDto _model = new();
        private IEnumerable<BasicDto> nationalities = new List<BasicDto>();
        private IEnumerable<TitleDto> genders = new List<TitleDto>();


        protected override async Task OnInitializedAsync()
        {
            await GetDataAsync();
            nationalities = _mapper.Map<IEnumerable<BasicDto>>(await _uow.Nationalities.GetAllAsync());
            genders = _mapper.Map<IEnumerable<TitleDto>>(await _uow.Genders.FindAllAsync(x=> x.Id < 3));
        }

        private async Task GetDataAsync()
        {
            if(Id != null)
                _model = _mapper.Map<ParentDto>(await _uow.Parents.FindAsync(x => x.Id == Guid.Parse(Id)));
        }

        private async Task SaveDataAsync()
        {
            try
            {
                isLoading = true;

                if (_model.Id == Guid.Empty)
                {
                    var changeType = StudentParentChangeType.ParentCreated;

                    if (await _uow.Parents.Existing(x => x.NationalID == _model.NationalID.Trim()))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }

                    var parent = _mapper.Map<Parent>(_model);
                    parent.CreatedBy = await _userService.GetUserIdAsync();
                    parent.CreatedOn = DateTime.UtcNow.GetKsaDateTime();

                    await _uow.Parents.AddAsync(parent);
                    
                    if (await _uow.SaveAsync())
                    {
                        _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                        await StudentParentChanges.NotifyAsync(new StudentParentChangedEvent(changeType, ParentId: parent.Id));
                        _appStateService.ParentId = parent.Id;
                        _navigator.NavigateTo("index");
                    }
                    else
                        _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
                }
                else
                {
                    var changeType = StudentParentChangeType.ParentUpdated;

                    if (await _uow.Parents.Existing(x => x.NationalID == _model.NationalID.Trim() && x.Id != _model.Id))
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["Existing"], _localizer["ItemExisting"]);
                        return;
                    }

                    var Parent = await _uow.Parents.FindAsync(x => x.Id == _model.Id);
                    if (Parent is null)
                    {
                        _toastService.Notify(NotificationSeverity.Warning, _localizer["NotFound"], _localizer["NotFound"]);
                        return;
                    }

                    _mapper.Map(_model, Parent);
                    Parent.UpdatedBy = await _userService.GetUserIdAsync();
                    Parent.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();

                    await _uow.Parents.Update(Parent);
                    if (await _uow.SaveAsync())
                    {
                        _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SavedSuccessfully"]);
                        await StudentParentChanges.NotifyAsync(new StudentParentChangedEvent(changeType, ParentId: Parent.Id));
                        _navigator.NavigateTo("index");
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
                isLoading = false;
            }
        }
    }
}
