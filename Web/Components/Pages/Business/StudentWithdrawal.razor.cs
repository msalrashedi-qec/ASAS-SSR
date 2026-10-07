using Core.Entities.Discounts;
using Core.Entities.General;
using Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace Web.Components.Pages.Business;

public partial class StudentWithdrawal
{
    [Parameter]
    public Guid StudentId { get; set; }

    [Inject]
    private StudentParentChangeNotificationService StudentParentChanges { get; set; } = null!;

    [Inject]
    private StudentService StudentService { get; set; } = null!;


    private const int StudyServiceCategoryId = 20;
    private const int TransportServiceCategoryId = 30;
    private const int FeeTransactionTypeId = TransactionTypeIds.AddDues;
    private const int WithdrawalCreditTransactionTypeId = TransactionTypeIds.DuesWithdrawal;
    private const byte WithdrawnContractStatusId = 30;
    private const byte ElapsedDaysDependencyId = 10;
    private const byte WithdrawalDateDependencyId = 20;

    private Student? student;
    private StudentContract? contract;
    private WithdrawalModel model = new();
    private List<SubscriptionInfo> transportSubscriptions = [];
    private double schoolPenalty;
    private double transportPenalty;
    private double[] schoolPenaltyPercentages = [];
    private double[] transportPenaltyPercentages = [];
    private bool isLoading;
    private bool isSaving;

    private bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
    private bool IsActiveContract => contract is not null && contract.StatusId >= 10 && contract.StatusId < 30;
    private string StudentCode => student is null ? string.Empty : $"{student.School.Code}-{student.SN:D5}";
    private string StudentFullName => student is null ? string.Empty : $"{student.Name} {student.FatherName}".Trim();
   
    protected override async Task OnParametersSetAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        isLoading = true;
        try
        {
            await _appStateService.InitializeAsync();
            student = await _uow.Students.FindAsync(x => x.Id == StudentId && x.SchoolId == _appStateService.SchoolId, ["School"]);

            if (student is null)
            {
                contract = null;
                transportSubscriptions = [];
                return;
            }

            contract = await _uow.StudentContracts.FindAsync(x => x.Id == student.CurrentContractId && x.StudentId == student.Id, ["Class", "Class.Level", "Year"]);

            if (contract is null)
            {
                transportSubscriptions = [];
                return;
            }

            model = new WithdrawalModel
            {
                WithdrawalDate = DateTime.Today < contract.StartDate.Date ? contract.StartDate.Date : DateTime.Today > contract.EndDate.Date ? contract.EndDate.Date : DateTime.Today
            };

            var activeSubscriptions = await _uow.StudentSubscriptions.FindAllAsync( x => x.ContractId == contract.Id && x.EndDate == null, ["Service", "Service.Category"]);

            var transport = activeSubscriptions.Where(x => IsTransportCategory(x.Service.CategoryId, x.Service.Category.Name, x.Service.Category.NameAr)).ToList();
            var accountEntries = (await _uow.StudentAccounts.FindAllAsync(x => x.ContractId == contract.Id)).ToList();
            var feeAccounts = accountEntries.Where(x => x.TransactionTypeId == FeeTransactionTypeId)
                .GroupBy(x => (x.ServiceId, x.SemesterId)).ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.Date).ToList());

            transportSubscriptions = transport.Select(x => new SubscriptionInfo(
                x.Id,
                x.ServiceId,
                x.SemesterId,
                x.Service.CategoryId,
                x.SubscriptionDate,
                feeAccounts.GetValueOrDefault((x.ServiceId, x.SemesterId))?.FirstOrDefault(y => y.Date.Date >= x.SubscriptionDate.Date)?.Debit ?? 0,
                (double)StudentAccountDiscountCalculator.CalculateNetServiceFee(accountEntries, contract.Id, x.ServiceId, x.SemesterId)
                )).ToList();

            await PreviewAsync();
        }
        catch (Exception ex)
        {
            student = null;
            contract = null;
            transportSubscriptions = [];
            _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task PreviewAsync()
    {
        if (student is null || contract is null || !model.WithdrawalDate.HasValue)
        {
            schoolPenalty = 0;
            transportPenalty = 0;
            schoolPenaltyPercentages = [];
            transportPenaltyPercentages = [];
            return;
        }

        var withdrawalDate = model.WithdrawalDate.Value.Date;
        var schoolPenaltyPreview = await CalculateStudyPenaltyAsync(withdrawalDate, model.IncludeAdditionalPenalty);
        schoolPenalty = schoolPenaltyPreview.Amount;
        schoolPenaltyPercentages = schoolPenaltyPreview.Percentages;
        transportPenalty = 0;
        var transportPercentages = new HashSet<double>();

        foreach (var subscription in transportSubscriptions)
        {
            var penalties = await FindPenaltiesAsync(subscription.CategoryId, subscription.SemesterId, subscription.SubscriptionDate, withdrawalDate);
            transportPenalty += penalties.Sum(x => CalculatePenalty(subscription.NetFeeAmount, x, includeAdditionalAmount: true));
            foreach (var penalty in penalties)
                transportPercentages.Add(penalty.Amount);
        }

        transportPenaltyPercentages = transportPercentages.OrderBy(x => x).ToArray();
    }

    private async Task WithdrawAsync()
    {
        if (student is null || contract is null || isSaving || !model.WithdrawalDate.HasValue)
            return;

        var withdrawalDate = model.WithdrawalDate.Value.Date;
        if (withdrawalDate < contract.StartDate.Date || withdrawalDate > contract.EndDate.Date)
        {
            _toastService.Notify(NotificationSeverity.Warning, _localizer["Withdrawal"], _localizer["InvalidStudentWithdrawalDate"]);
            return;
        }

        var confirmed = await _dialogService.Confirm(_localizer["ConfirmStudentWithdrawalMessage"], _localizer["Confirm"]
            , new ConfirmOptions { OkButtonText = _localizer["Withdraw"], CancelButtonText = _localizer["Cancel"] });
        if (confirmed != true)
            return;

        isSaving = true;
        try
        {
            var currentContract = await _uow.StudentContracts.FindAsync(x => x.Id == contract.Id && x.StudentId == student.Id && x.StatusId >= 10 && x.StatusId < 30);
            if (currentContract is null)
            {
                _toastService.Notify(NotificationSeverity.Warning, _localizer["Withdrawal"], _localizer["StudentAlreadyWithdrawn"]);
                await LoadAsync();
                return;
            }

            var userId = await _userService.GetUserIdAsync();
            var now = DateTime.UtcNow.GetKsaDateTime();
            var discounts = (await _uow.Discounts.FindAllAsync(x => true)).ToList();
            var accountEntries = (await _uow.StudentAccounts.FindAllAsync(x => x.ContractId == contract.Id)).ToList();
            var discountWithdrawals = StudentDiscountWithdrawal.CreateEntries(accountEntries, discounts, contract.Id, withdrawalDate, userId, now).ToList();

            var siblingChanges = await StudentService.BuildSiblingDiscountChangesAsync(_uow, student, currentContract, withdrawalDate, userId, withdrawing: true);

            // Transportation is closed first and uses the same configured withdrawal
            // policies as the standalone subscription-withdrawal screen.
            foreach (var transport in transportSubscriptions)
                await WithdrawTransportSubscriptionAsync(transport, withdrawalDate, userId, now);

            await AddStudyWithdrawalTransactionsAsync(withdrawalDate, userId, now);
            await _uow.StudentAccounts.AddRangeAsync(discountWithdrawals);
            await StudentService.StageSiblingDiscountChangesAsync(_uow, siblingChanges);

            currentContract.ActualEndDate = withdrawalDate;
            currentContract.StatusId = WithdrawnContractStatusId;
            currentContract.Comments = model.Reason.Trim();
            currentContract.UpdatedBy = userId;
            currentContract.UpdatedOn = now;
            await _uow.StudentContracts.Update(currentContract);

            //var student = await _uow.Students.FindAsync(x=> x.Id == currentContract.StudentId);
            student.CurrentStatusId = currentContract.StatusId;
            await _uow.Students.Update(student);

            if (await _uow.SaveAsync())
            {
                _toastService.Notify(NotificationSeverity.Success, _localizer["Withdrawal"], _localizer["StudentWithdrawnSuccessfully"]);
                await StudentParentChanges.NotifyAsync(new StudentParentChangedEvent( StudentParentChangeType.StudentUpdated, student.SchoolId, student.ParentId, student.Id));
                await LoadAsync();
            }
            else
                _toastService.Notify(NotificationSeverity.Error, _localizer["Save"], _localizer["Faild_to_save"]);
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

    private async Task WithdrawTransportSubscriptionAsync( SubscriptionInfo info, DateTime withdrawalDate, string userId, DateTime now)
    {
        var subscription = await _uow.StudentSubscriptions.FindAsync(x => x.Id == info.Id && x.EndDate == null);
        if (subscription is null)
            return;

        subscription.EndDate = withdrawalDate;
        subscription.UpdatedBy = userId;
        subscription.UpdatedOn = now;
        await _uow.StudentSubscriptions.Update(subscription);

        await AddWithdrawalCreditAsync(info.ServiceId, info.SemesterId, info.CategoryId, info.FeeAmount, withdrawalDate, userId, now);

        var penalties = await FindPenaltiesAsync(info.CategoryId, info.SemesterId, info.SubscriptionDate, withdrawalDate);
        foreach (var detail in penalties)
            await AddPenaltyAsync(info.ServiceId, info.SemesterId, info.CategoryId, info.NetFeeAmount, detail, withdrawalDate, userId, now, true);
    }

    private async Task AddStudyWithdrawalTransactionsAsync(DateTime withdrawalDate, string userId, DateTime now)
    {
        if (contract is null)
            return;

        var yearSemesters = await _uow.SchoolSemesters.FindAllAsync(x => x.SchoolId == student.SchoolId && x.YearId == contract.RegisteredForYearId);
        var accountEntries = (await _uow.StudentAccounts.FindAllAsync(x => x.ContractId == contract.Id && x.ServiceId == StudentService.StudyServiceId)).ToList();

        var fees = accountEntries.Where(x => x.TransactionTypeId == FeeTransactionTypeId)
            .GroupBy(x => (x.ServiceId, x.SemesterId, x.Categoryid))
            .Select(x => new
            {
                x.Key.ServiceId,
                x.Key.SemesterId,
                CategoryId = x.Key.Categoryid,
                Amount = x.Sum(y => y.Debit),
                NetAmount = accountEntries.Where(a => a.Categoryid == x.Key.Categoryid && a.SemesterId == x.Key.SemesterId && a.TransactionTypeId == FeeTransactionTypeId).Sum(a => a.Debit)
                //NetAmount = (double)StudentAccountDiscountCalculator.CalculateNetServiceFee(accountEntries, contract.Id, x.Key.ServiceId, x.Key.SemesterId)
            }).ToList();

        foreach (var fee in fees)
        {
            await AddWithdrawalCreditAsync(fee.ServiceId, fee.SemesterId, fee.CategoryId, fee.Amount, withdrawalDate, userId, now);

            var feeSemester = yearSemesters.First(x => x.SemesterId == fee.SemesterId);
            bool canGetPenalty = (feeSemester.StartDate.Date <= withdrawalDate.Date && feeSemester.EndDate.Date >= withdrawalDate.Date)
                || (withdrawalDate.Date < feeSemester.StartDate.Date && fee.SemesterId == 1);
            if (canGetPenalty)
            {
                var penalties = await FindPenaltiesAsync(fee.CategoryId == 0 ? StudyServiceCategoryId : fee.CategoryId, fee.SemesterId, contract.StartDate, withdrawalDate);
                foreach (var detail in penalties)
                    await AddPenaltyAsync(fee.ServiceId, fee.SemesterId, fee.CategoryId, fee.NetAmount, detail, withdrawalDate, userId, now, model.IncludeAdditionalPenalty);
            }  
        }
    }

    private async Task<PenaltyPreview> CalculateStudyPenaltyAsync(DateTime withdrawalDate, bool includeAdditionalAmount)
    {
        if (contract is null)
            return new PenaltyPreview(0, []);

        var yearSemesters = await _uow.SchoolSemesters.FindAllAsync(x => x.SchoolId == student.SchoolId && x.YearId == contract.RegisteredForYearId);
        var accountEntries = (await _uow.StudentAccounts.FindAllAsync(x => x.ContractId == contract.Id && x.ServiceId == StudentService.StudyServiceId)).ToList();
        var fees = accountEntries.Where(x => x.TransactionTypeId == FeeTransactionTypeId)
            .GroupBy(x => (x.SemesterId, x.Categoryid))
            .Select(x => new
            {
                x.Key.SemesterId,
                CategoryId = x.Key.Categoryid,
                NetAmount = accountEntries.Where(a=> a.Categoryid == x.Key.Categoryid && a.SemesterId == x.Key.SemesterId && a.TransactionTypeId == FeeTransactionTypeId).Sum(a => a.Debit) //(double)StudentAccountDiscountCalculator.CalculateNetServiceFee(accountEntries, contract.Id, StudentService.StudyServiceId, x.Key.SemesterId)
            }).ToList();

        var total = 0d;
        var percentages = new HashSet<double>();
        foreach (var fee in fees)
        {
            var feeSemester = yearSemesters.First(x=> x.SemesterId == fee.SemesterId);
            bool canGetPenalty = (feeSemester.StartDate.Date <= withdrawalDate.Date && feeSemester.EndDate.Date >= withdrawalDate.Date)
                || (withdrawalDate.Date < feeSemester.StartDate.Date && fee.SemesterId == 1);
            if (canGetPenalty)
            {
                var penalties = await FindPenaltiesAsync(fee.CategoryId == 0 ? StudyServiceCategoryId : fee.CategoryId, fee.SemesterId, contract.StartDate, withdrawalDate);
                total += penalties.Sum(x => CalculatePenalty(fee.NetAmount, x, includeAdditionalAmount));
                foreach (var penalty in penalties)
                    percentages.Add(penalty.Amount);
            }
        }

        return new PenaltyPreview(total, percentages.OrderBy(x => x).ToArray());
    }

    private async Task AddWithdrawalCreditAsync(int serviceId, byte semesterId, int categoryId, double amount, DateTime withdrawalDate, string userId, DateTime now)
    {
        if (contract is null || amount <= 0)
            return;

        await _uow.StudentAccounts.AddAsync(new StudentAccount
        {
            ContractId = contract.Id,
            ServiceId = serviceId,
            SemesterId = semesterId,
            Categoryid = categoryId,
            TransactionTypeId = WithdrawalCreditTransactionTypeId,
            Debit = 0,
            Credit = amount,
            Percentage = 0,
            Date = withdrawalDate,
            Comments = _localizer["StudentWithdrawalCredit"],
            CreatedBy = userId,
            CreatedOn = now
        });
    }

    private async Task AddPenaltyAsync(int serviceId, byte semesterId, int categoryId, double baseAmount, DiscountDetail detail, DateTime withdrawalDate, string userId, DateTime now, bool includeAdditionalAmount)
    {
        if (contract is null)
            return;

        var amount = CalculatePenalty(baseAmount, detail, includeAdditionalAmount);
        if (amount <= 0)
            return;

        await _uow.StudentAccounts.AddAsync(new StudentAccount
        {
            ContractId = contract.Id,
            ServiceId = serviceId,
            SemesterId = semesterId,
            Categoryid = categoryId,
            TransactionTypeId = detail.Discount.TransactionTypeId,
            Debit = amount,
            Credit = 0,
            Percentage = (decimal)(detail.Amount / 100d),
            Date = withdrawalDate,
            Comments = detail.Comments ?? detail.Discount.Name,
            CreatedBy = userId,
            CreatedOn = now
        });
    }

    private async Task<IReadOnlyList<DiscountDetail>> FindPenaltiesAsync(int categoryId, byte semesterId, DateTime calculationStartDate, DateTime withdrawalDate)
    {
        if (student is null || contract is null)
            return [];

        var semester = await _uow.SchoolSemesters.FindAsync(x => x.SchoolId == student.SchoolId && x.YearId == contract.RegisteredForYearId && x.SemesterId == semesterId);
        var rangeStart = semester is null || calculationStartDate.Date >= semester.StartDate.Date ? calculationStartDate.Date : semester.StartDate.Date;

        var candidates = (await _uow.DiscountDetails.FindAllAsync(x => x.SchoolId == student.SchoolId && x.YearId == contract.RegisteredForYearId 
        && (x.SemesterId == semesterId || x.SemesterId == 0) && x.Discount.ServiceCategoryId == categoryId && x.Discount.ApplicationTiming == DiscountApplicationTiming.OnWithdrawal,
                ["Discount"]))
            .OrderByDescending(x => x.SemesterId == semesterId)
            .ThenBy(x => x.Discount.SN)
            .ToList();

        return candidates.GroupBy(x => x.DiscountId).Select(group => group.OrderByDescending(x => x.SemesterId == semesterId)
            .FirstOrDefault(x => MatchesWithdrawalRange(x, rangeStart, withdrawalDate)))
            .Where(x => x is not null)
            .Select(x => x!)
            .OrderBy(x => x.Discount.SN)
            .ThenBy(x => x.DiscountId)
            .ToArray();
    }

    private static bool MatchesWithdrawalRange(DiscountDetail detail, DateTime startDate, DateTime withdrawalDate) =>
        detail.Discount.DependencyId switch
        {
            ElapsedDaysDependencyId => IsNumberInRange(Math.Max(0, (withdrawalDate.Date - startDate.Date).Days), detail.FromRange, detail.ToRange),
            WithdrawalDateDependencyId => IsDateInRange(withdrawalDate, detail.FromRange, detail.ToRange),
            0 => true,
            _ => false
        };

    private static double CalculatePenalty(double baseAmount, DiscountDetail detail, bool includeAdditionalAmount) => 
        Math.Round( baseAmount * detail.Amount / 100d + (includeAdditionalAmount && detail.AdditionalFixedAmount > 0 ? detail.AdditionalFixedAmount : 0), 2, MidpointRounding.AwayFromZero);

    private static bool IsTransportCategory(int categoryId, string? name, string? nameAr) =>
        categoryId == TransportServiceCategoryId
        || name?.Contains("transport", StringComparison.OrdinalIgnoreCase) == true
        || nameAr?.Contains("نقل", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsNumberInRange(double value, string? fromText, string? toText)
    {
        var hasFrom = TryParseNumber(fromText, out var from);
        var hasTo = TryParseNumber(toText, out var to);
        return (hasFrom || hasTo) && (!hasFrom || value >= from) && (!hasTo || value <= to);
    }

    private static bool TryParseNumber(string? value, out double result) =>
        double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result)
        || double.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result);

    private static bool IsDateInRange(DateTime value, string? fromText, string? toText)
    {
        var hasFrom = DateTime.TryParse(fromText, CultureInfo.CurrentCulture, DateTimeStyles.None, out var from)
                      || DateTime.TryParse(fromText, CultureInfo.InvariantCulture, DateTimeStyles.None, out from);
        var hasTo = DateTime.TryParse(toText, CultureInfo.CurrentCulture, DateTimeStyles.None, out var to)
                    || DateTime.TryParse(toText, CultureInfo.InvariantCulture, DateTimeStyles.None, out to);
        return (hasFrom || hasTo) && (!hasFrom || value.Date >= from.Date) && (!hasTo || value.Date <= to.Date);
    }

    private string Localized(string? english, string? arabic) => IsArabic ? arabic ?? english ?? string.Empty : english ?? arabic ?? string.Empty;

    private static string FormatPercentages(IEnumerable<double> percentages)
    {
        var values = percentages.Where(x => x > 0).Select(x => $"{x:0.##}%").ToArray();
        return values.Length == 0 ? "0%" : string.Join(" + ", values);
    }

    private sealed class WithdrawalModel
    {
        [Required]
        public DateTime? WithdrawalDate { get; set; }

        [Required(AllowEmptyStrings = false)]
        [StringLength(1000, MinimumLength = 3)]
        public string Reason { get; set; } = string.Empty;

        public bool IncludeAdditionalPenalty { get; set; }
    }

    private sealed record SubscriptionInfo(
        Guid Id,
        int ServiceId,
        byte SemesterId,
        int CategoryId,
        DateTime SubscriptionDate,
        double FeeAmount,
        double NetFeeAmount);

    private sealed record PenaltyPreview(double Amount, double[] Percentages);
}
