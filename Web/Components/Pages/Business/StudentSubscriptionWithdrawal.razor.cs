using System.Globalization;
using Core.Entities.Discounts;
using Shared.Enums;

namespace Web.Components.Pages.Business;

public partial class StudentSubscriptionWithdrawal
{
    private const int FeeTransactionTypeId = TransactionTypeIds.AddDues;
    private const int WithdrawalCreditTransactionTypeId = TransactionTypeIds.DuesWithdrawal;
    private const byte SubscriptionElapsedDaysDependencyId = 10;
    private const byte WithdrawalDateDependencyId = 20;

    [Parameter]
    public Guid StudentId { get; set; }

    private Student? student;
    private StudentContract? contract;
    private List<WithdrawalRow> subscriptions = [];
    private bool isLoading;
    private bool isSaving;

    private bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
    private string StudentCode => student is null ? string.Empty : $"{student.School.Code}-{student.SN:D5}";
    private string StudentFullName => student is null ? string.Empty : $"{student.Name} {student.FatherName}".Trim();

    protected override async Task OnParametersSetAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        isLoading = true;
        try
        {
            await _appStateService.InitializeAsync();
            student = await _uow.Students.FindAsync(
                x => x.Id == StudentId && x.SchoolId == _appStateService.SchoolId,
                ["School"]);

            if (student is null)
            {
                contract = null;
                subscriptions = [];
                return;
            }

            contract = await _uow.StudentContracts.FindAsync(
                x => x.Id == student.CurrentContractId && x.StudentId == student.Id,
                ["Class", "Year"]);

            if (contract is null)
            {
                subscriptions = [];
                return;
            }

            var activeSubscriptions = (await _uow.StudentSubscriptions.FindAllAsync(
                    x => x.ContractId == contract.Id
                         && x.ServiceId != StudentService.StudyServiceId
                         && x.EndDate == null,
                    ["Service", "Service.Category", "Semester"]))
                .OrderBy(x => x.Service.CategoryId)
                .ThenBy(x => x.Service.Name)
                .ThenBy(x => x.SemesterId)
                .ToList();

            var accountEntries = (await _uow.StudentAccounts.FindAllAsync(x => x.ContractId == contract.Id)).ToList();
            var fees = accountEntries
                .Where(x => x.TransactionTypeId == FeeTransactionTypeId)
                .GroupBy(x => (x.ServiceId, x.SemesterId))
                .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.Date).ToList());

            subscriptions = activeSubscriptions.Select(x => new WithdrawalRow
            {
                Id = x.Id,
                ServiceId = x.ServiceId,
                SemesterId = x.SemesterId,
                CategoryId = x.Service.CategoryId,
                ServiceName = x.Service.Name,
                ServiceNameAr = x.Service.NameAr,
                SemesterName = x.Semester.Name,
                SemesterNameAr = x.Semester.NameAr,
                SubscriptionDate = x.SubscriptionDate,
                FeeAmount = fees.GetValueOrDefault((x.ServiceId, x.SemesterId))?
                    .FirstOrDefault(y => y.Date.Date >= x.SubscriptionDate.Date)?.Debit ?? 0,
                NetFeeAmount = (double)StudentAccountDiscountCalculator.CalculateNetServiceFee(
                    accountEntries, contract.Id, x.ServiceId, x.SemesterId)
            }).ToList();
        }
        catch (Exception ex)
        {
            student = null;
            contract = null;
            subscriptions = [];
            _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task PreviewPenaltyAsync(WithdrawalRow row)
    {
        if (!row.WithdrawalDate.HasValue || contract is null || student is null)
        {
            row.PenaltyAmount = 0;
            row.PenaltyName = null;
            return;
        }

        var penalties = await FindPenaltiesAsync(row, row.WithdrawalDate.Value.Date);
        row.PenaltyAmount = penalties.Sum(x => CalculatePenalty(row.NetFeeAmount, x));
        row.PenaltyName = penalties.Count == 0
            ? null
            : string.Join("، ", penalties.Select(x => Localized(x.Discount.Name, x.Discount.NameAr)));
    }

    private async Task WithdrawAsync(WithdrawalRow row)
    {
        if (contract is null || student is null || !row.WithdrawalDate.HasValue || isSaving)
            return;

        var withdrawalDate = row.WithdrawalDate.Value.Date;
        if (withdrawalDate < row.SubscriptionDate.Date || withdrawalDate > contract.EndDate.Date)
        {
            _toastService.Notify(NotificationSeverity.Warning, _localizer["Withdrawal"], _localizer["InvalidWithdrawalDate"]);
            return;
        }

        var confirmed = await _dialogService.Confirm(
            _localizer["ConfirmSubscriptionWithdrawal"],
            _localizer["Confirm"],
            new ConfirmOptions
            {
                OkButtonText = _localizer["Withdraw"],
                CancelButtonText = _localizer["Cancel"]
            });
        if (confirmed != true)
            return;

        isSaving = true;
        try
        {
            var subscription = await _uow.StudentSubscriptions.FindAsync(x => x.Id == row.Id && x.EndDate == null);
            if (subscription is null)
            {
                _toastService.Notify(NotificationSeverity.Warning, _localizer["Withdrawal"], _localizer["SubscriptionAlreadyWithdrawn"]);
                await LoadAsync();
                return;
            }

            var userId = await _userService.GetUserIdAsync();
            var now = DateTime.UtcNow.GetKsaDateTime();
            var penalties = await FindPenaltiesAsync(row, withdrawalDate);
            var discountWithdrawals = StudentDiscountWithdrawal.CreateEntries(
                await _uow.StudentAccounts.FindAllAsync(x => x.ContractId == contract.Id
                    && x.ServiceId == row.ServiceId && x.SemesterId == row.SemesterId),
                await _uow.Discounts.FindAllAsync(x => true),
                contract.Id, withdrawalDate, userId, now);

            subscription.EndDate = withdrawalDate;
            subscription.UpdatedBy = userId;
            subscription.UpdatedOn = now;
            await _uow.StudentSubscriptions.Update(subscription);

            if (row.FeeAmount > 0)
            {
                await _uow.StudentAccounts.AddAsync(new StudentAccount
                {
                    ContractId = contract.Id,
                    ServiceId = row.ServiceId,
                    SemesterId = row.SemesterId,
                    Categoryid = row.CategoryId,
                    TransactionTypeId = WithdrawalCreditTransactionTypeId,
                    Debit = 0,
                    Credit = row.FeeAmount,
                    Percentage = 0,
                    Date = withdrawalDate,
                    Comments = _localizer["SubscriptionWithdrawalCredit"],
                    CreatedBy = userId,
                    CreatedOn = now
                });
            }

            foreach (var penalty in penalties)
            {
                var penaltyAmount = CalculatePenalty(row.NetFeeAmount, penalty);
                if (penaltyAmount <= 0)
                    continue;

                await _uow.StudentAccounts.AddAsync(new StudentAccount
                {
                    ContractId = contract.Id,
                    ServiceId = row.ServiceId,
                    SemesterId = row.SemesterId,
                    Categoryid = row.CategoryId,
                    TransactionTypeId = penalty.Discount.TransactionTypeId,
                    Debit = penaltyAmount,
                    Credit = 0,
                    Percentage = (decimal)(penalty.Amount / 100d),
                    Date = withdrawalDate,
                    Comments = penalty.Comments ?? penalty.Discount.Name,
                    CreatedBy = userId,
                    CreatedOn = now
                });
            }

            await _uow.StudentAccounts.AddRangeAsync(discountWithdrawals);
            if (await _uow.SaveAsync())
            {
                _toastService.Notify(NotificationSeverity.Success, _localizer["Withdrawal"], _localizer["SubscriptionWithdrawnSuccessfully"]);
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

    private async Task<IReadOnlyList<DiscountDetail>> FindPenaltiesAsync(WithdrawalRow row, DateTime withdrawalDate)
    {
        if (contract is null || student is null)
            return Array.Empty<DiscountDetail>();

        var semester = await _uow.SchoolSemesters.FindAsync(x =>
            x.SchoolId == student.SchoolId
            && x.YearId == contract.RegisteredForYearId
            && x.SemesterId == row.SemesterId);
        var calculationStartDate = semester is null || row.SubscriptionDate.Date >= semester.StartDate.Date
            ? row.SubscriptionDate.Date
            : semester.StartDate.Date;

        var candidates = (await _uow.DiscountDetails.FindAllAsync(
                x => x.SchoolId == student.SchoolId
                     && x.YearId == contract.RegisteredForYearId
                     && (x.SemesterId == row.SemesterId || x.SemesterId == 0)
                     && x.Discount.ServiceCategoryId == row.CategoryId
                     && x.Discount.ApplicationTiming == DiscountApplicationTiming.OnWithdrawal,
                ["Discount"]))
            .OrderByDescending(x => x.SemesterId == row.SemesterId)
            .ThenBy(x => x.Discount.SN)
            .ToList();

        if (candidates.Count == 0)
            return Array.Empty<DiscountDetail>();

        return candidates
            .GroupBy(x => x.DiscountId)
            .Select(group => group
                .OrderByDescending(x => x.SemesterId == row.SemesterId)
                .FirstOrDefault(x => MatchesWithdrawalRange(
                    x,
                    calculationStartDate,
                    withdrawalDate)))
            .Where(x => x is not null)
            .Select(x => x!)
            .OrderBy(x => x.Discount.SN)
            .ThenBy(x => x.DiscountId)
            .ToArray();
    }

    private static bool MatchesWithdrawalRange(
        DiscountDetail detail,
        DateTime calculationStartDate,
        DateTime withdrawalDate)
    {
        return detail.Discount.DependencyId switch
        {
            // Numeric ranges are measured from the later of the service
            // subscription date and the beginning of its school semester.
            SubscriptionElapsedDaysDependencyId => IsNumberInRange(
                Math.Max(0, (withdrawalDate.Date - calculationStartDate.Date).Days),
                detail.FromRange,
                detail.ToRange),
            WithdrawalDateDependencyId => IsDateInRange(withdrawalDate, detail.FromRange, detail.ToRange),
            _ => false
        };
    }

    private static double CalculatePenalty(double feeAmount, DiscountDetail detail) => Math.Round(
        feeAmount * detail.Amount / 100d + detail.AdditionalFixedAmount,
        2,
        MidpointRounding.AwayFromZero);

    private static bool IsNumberInRange(double value, string? fromText, string? toText)
    {
        var hasFrom = TryParseNumber(fromText, out var from);
        var hasTo = TryParseNumber(toText, out var to);
        return (hasFrom || hasTo)
               && (!hasFrom || value >= from)
               && (!hasTo || value <= to);
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
        return (hasFrom || hasTo)
               && (!hasFrom || value.Date >= from.Date)
               && (!hasTo || value.Date <= to.Date);
    }

    private string Localized(string? english, string? arabic) =>
        IsArabic ? arabic ?? english ?? string.Empty : english ?? arabic ?? string.Empty;

    private sealed class WithdrawalRow
    {
        public Guid Id { get; init; }
        public int ServiceId { get; init; }
        public byte SemesterId { get; init; }
        public int CategoryId { get; init; }
        public string ServiceName { get; init; } = string.Empty;
        public string ServiceNameAr { get; init; } = string.Empty;
        public string SemesterName { get; init; } = string.Empty;
        public string SemesterNameAr { get; init; } = string.Empty;
        public DateTime SubscriptionDate { get; init; }
        public double FeeAmount { get; init; }
        public double NetFeeAmount { get; init; }
        public DateTime? WithdrawalDate { get; set; }
        public double PenaltyAmount { get; set; }
        public string? PenaltyName { get; set; }
    }
}
