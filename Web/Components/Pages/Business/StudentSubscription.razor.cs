using System.Globalization;

namespace Web.Components.Pages.Business;

public partial class StudentSubscription
{
    private const int FeeTransactionTypeId = TransactionTypeIds.AddDues;

    [Parameter]
    public Guid StudentId { get; set; }

    [Inject]
    private StudentService StudentService { get; set; } = null!;

    private Student? student;
    private StudentContract? contract;
    private List<ActiveSubscriptionRow> activeSubscriptions = [];
    private List<AvailableServiceRow> availableServices = [];
    private bool isLoading;
    private bool isSaving;

    private bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
    private string StudentCode => student is null ? string.Empty : $"{student.School.Code}-{student.SN:D5}";
    private string StudentFullName => student is null ? string.Empty : $"{student.Name} {student.FatherName}".Trim();
    private int SelectedCount => availableServices.Count(x => !x.IsSubscribed && x.SubscriptionDate.HasValue);

    protected override async Task OnParametersSetAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        isLoading = true;
        try
        {
            await _appStateService.InitializeAsync();
            student = await _uow.Students.FindAsync(x => x.Id == StudentId && x.SchoolId == _appStateService.SchoolId,["School"]);

            if (student is null)
            {
                contract = null;
                activeSubscriptions = [];
                availableServices = [];
                return;
            }

            contract = await _uow.StudentContracts.FindAsync(
                x => x.Id == student.CurrentContractId && x.StudentId == student.Id,
                ["Class", "Year"]);

            if (contract is null)
            {
                activeSubscriptions = [];
                availableServices = [];
                return;
            }

            var subscriptions = (await _uow.StudentSubscriptions.FindAllAsync(
                    x => x.ContractId == contract.Id
                         && x.ServiceId != StudentService.StudyServiceId
                         && x.EndDate == null,
                    ["Service", "Semester"]))
                .OrderBy(x => x.Service.CategoryId)
                .ThenBy(x => x.Service.Name)
                .ThenBy(x => x.SemesterId)
                .ToList();

            var fees = (await _uow.StudentAccounts.FindAllAsync(
                    x => x.ContractId == contract.Id
                         && x.ServiceId != StudentService.StudyServiceId
                         && x.TransactionTypeId == FeeTransactionTypeId
                         && x.Categoryid != StudentSubscriptionTax.TaxCategoryId))
                .GroupBy(x => (x.ServiceId, x.SemesterId))
                .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.Date).First());

            activeSubscriptions = subscriptions.Select(x => new ActiveSubscriptionRow(
                x.ServiceId,
                x.SemesterId,
                x.Service.Name,
                x.Service.NameAr,
                x.Semester.Name,
                x.Semester.NameAr,
                fees.GetValueOrDefault((x.ServiceId, x.SemesterId))?.Debit ?? 0,
                x.SubscriptionDate))
                .ToList();

            var subscribedKeys = subscriptions.Select(x => (x.ServiceId, x.SemesterId)).ToHashSet();
            var prices = (await _uow.ServicePrices.FindAllAsync(
                    x => x.YearId == contract.RegisteredForYearId
                         && x.ClassId == contract.ClassId
                         && x.ServiceId != StudentService.StudyServiceId,
                    ["Service", "Service.Category", "Semester"]))
                .GroupBy(x => (x.ServiceId, x.SemesterId))
                .Select(x => x.First())
                .OrderBy(x => x.Service.Category.Name)
                .ThenBy(x => x.Service.Name)
                .ThenBy(x => x.SemesterId);

            availableServices = prices.Select(x => new AvailableServiceRow
            {
                PriceId = x.Id,
                ServiceId = x.ServiceId,
                SemesterId = x.SemesterId,
                CategoryId = x.Service.CategoryId,
                CategoryName = x.Service.Category.Name,
                CategoryNameAr = x.Service.Category.NameAr,
                ServiceName = x.Service.Name,
                ServiceNameAr = x.Service.NameAr,
                SemesterName = x.Semester.Name,
                SemesterNameAr = x.Semester.NameAr,
                Price = x.Price,
                IsSubscribed = subscribedKeys.Contains((x.ServiceId, x.SemesterId))
            }).OrderBy(x=> x.CategoryId).ToList();
        }
        catch (Exception ex)
        {
            student = null;
            contract = null;
            activeSubscriptions = [];
            availableServices = [];
            _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task SaveSubscriptionsAsync()
    {
        if (contract is null || student is null || isSaving)
            return;

        var selected = availableServices
            .Where(x => !x.IsSubscribed && x.SubscriptionDate.HasValue)
            .ToList();

        if (selected.Count == 0)
        {
            _toastService.Notify(NotificationSeverity.Warning, _localizer["Subscriptions"], _localizer["SelectAtLeastOneService"]);
            return;
        }

        isSaving = true;
        try
        {
            var userId = await _userService.GetUserIdAsync();
            var now = DateTime.UtcNow.GetKsaDateTime();
            var subscriptions = new List<Core.Entities.Business.StudentSubscription>();
            var accounts = new List<StudentAccount>();

            foreach (var item in selected)
            {
                var subscriptionDate = item.SubscriptionDate!.Value.Date;
                if (subscriptionDate < contract.StartDate.Date || subscriptionDate > contract.EndDate.Date)
                    throw new InvalidOperationException(_localizer["SubscriptionDateOutsideContract"]);

                var alreadySubscribed = await _uow.StudentSubscriptions.Existing(x =>
                    x.ContractId == contract.Id
                    && x.ServiceId == item.ServiceId
                    && x.SemesterId == item.SemesterId
                    && x.EndDate == null);
                if (alreadySubscribed)
                    continue;

                subscriptions.Add(new Core.Entities.Business.StudentSubscription
                {
                    ContractId = contract.Id,
                    ServiceId = item.ServiceId,
                    SemesterId = item.SemesterId,
                    SubscriptionDate = subscriptionDate,
                    IsPaid = false,
                    Comments = _localizer["ServiceSubscription"],
                    CreatedBy = userId,
                    CreatedOn = now
                });

                var feeAccount = new StudentAccount
                {
                    ContractId = contract.Id,
                    ServiceId = item.ServiceId,
                    SemesterId = item.SemesterId,
                    Categoryid = item.CategoryId,
                    TransactionTypeId = FeeTransactionTypeId,
                    Debit = item.Price,
                    Credit = 0,
                    Percentage = 0,
                    Date = subscriptionDate,
                    Comments = _localizer["ServiceSubscription"],
                    CreatedBy = userId,
                    CreatedOn = now
                };
                accounts.Add(feeAccount);
                var tax = await StudentSubscriptionTax.CreateEntryAsync(_uow, student.SchoolId,
                    student.NationalityId, feeAccount);
                if (tax is not null)
                {
                    accounts.Add(tax);
                }

                var applicableDiscounts = await StudentService.GetApplicableSubscriptionDiscounts(
                    _uow,
                    student,
                    contract,
                    item.CategoryId,
                    item.SemesterId,
                    subscriptionDate);
                foreach (var detail in applicableDiscounts)
                {
                    var netFee = StudentAccountDiscountCalculator.CalculateNetServiceFee(
                        accounts,
                        contract.Id,
                        item.ServiceId,
                        item.SemesterId);
                    if (netFee <= 0)
                        break;

                    var discountAmount = StudentAccountDiscountCalculator.CalculateDiscountAmount(
                        netFee,
                        (decimal)detail.Amount,
                        (decimal)detail.AdditionalFixedAmount);
                    if (discountAmount <= 0)
                        continue;

                    var discountAccount = new StudentAccount
                    {
                        ContractId = contract.Id,
                        ServiceId = item.ServiceId,
                        SemesterId = item.SemesterId,
                        Categoryid = item.CategoryId,
                        TransactionTypeId = detail.Discount.TransactionTypeId,
                        RefNo = StudentDiscountWithdrawal.DiscountReference(detail.Discount.Id),
                        Debit = 0,
                        Credit = (double)discountAmount,
                        Percentage = (decimal)(detail.Amount / 100d),
                        Date = subscriptionDate,
                        Comments = detail.Comments ?? detail.Discount.Name,
                        CreatedBy = userId,
                        CreatedOn = now
                    };
                    var taxDiscount = StudentSubscriptionTax.CreateDiscountEntry(discountAccount, accounts);
                    if (taxDiscount is not null)
                    {
                        accounts.Add(taxDiscount);
                    }
                    accounts.Add(discountAccount);
                }


            }

            if (subscriptions.Count == 0)
            {
                _toastService.Notify(NotificationSeverity.Info, _localizer["Subscriptions"], _localizer["ServicesAlreadySubscribed"]);
                await LoadAsync();
                return;
            }

            await _uow.StudentSubscriptions.AddRangeAsync(subscriptions);
            await _uow.StudentAccounts.AddRangeAsync(accounts);

            if (await _uow.SaveAsync())
            {
                _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], _localizer["SubscriptionsSavedSuccessfully"]);
                await LoadAsync();
            }
            else
            {
                _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
            }
        }
        catch (Exception ex)
        {
            _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
        }
        finally
        {
            isSaving = false;
        }
    }

    private string Localized(string? english, string? arabic) =>
        IsArabic ? arabic ?? english ?? string.Empty : english ?? arabic ?? string.Empty;

    private sealed record ActiveSubscriptionRow(
        int ServiceId,
        byte SemesterId,
        string ServiceName,
        string ServiceNameAr,
        string SemesterName,
        string SemesterNameAr,
        double Amount,
        DateTime SubscriptionDate);

    private sealed class AvailableServiceRow
    {
        public Guid PriceId { get; init; }
        public int ServiceId { get; init; }
        public byte SemesterId { get; init; }
        public int CategoryId { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public string CategoryNameAr { get; init; } = string.Empty;
        public string ServiceName { get; init; } = string.Empty;
        public string ServiceNameAr { get; init; } = string.Empty;
        public string SemesterName { get; init; } = string.Empty;
        public string SemesterNameAr { get; init; } = string.Empty;
        public double Price { get; init; }
        public bool IsSubscribed { get; init; }
        public DateTime? SubscriptionDate { get; set; }
    }
}
