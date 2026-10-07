
namespace Web.Components.Pages.Business;

public partial class Payment
{
    [Inject] private ISmsSender SmsSender { get; set; } = null!;
    private bool showFullPaymentDiscount;
    private List<StudentAccount> fullPaymentDiscounts = [];
    private bool hasFullPaymentDiscountConfiguration;
    private const int RegularPaymentTransactionTypeId = TransactionTypeIds.DuesPayment;
    private const double Epsilon = 0.005;

    [Parameter] public Guid ParentId { get; set; }
    [Parameter] public Guid? ReceiptId { get; set; }

    private ParentDto parent = new();
    private School? school;
    private List<StudentPaymentCard> students = [];
    private List<DueItem> dueItems = [];
    private List<PaymentMethodOption> paymentMethods = [];
    private List<BankOption> banks = [];
    private Guid? selectedStudentId;
    private StudentPaymentCard? selectedStudent;
    private DateTime receiptDate = DateTime.Today;
    private DateTime? chequeDate;
    private byte paymentMethodId;
    private string? bankName;
    private string? chequeNo;
    private double excessAmount;
    private bool isLoading;
    private bool isSaving;
    private string? validationMessage;
    private SavedReceipt? savedReceipt;

    private bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
    private bool IsCheque => paymentMethods.FirstOrDefault(x => x.Id == paymentMethodId)?.IsCheque == true;
    private double SelectedDueTotal => dueItems.Sum(x => Math.Max(0, x.PaymentAmount));
    private double PaymentTotal => SelectedDueTotal + Math.Max(0, excessAmount);
    private string SchoolName => Localized(school?.Name, school?.NameAr);
    private string CompanyName => Localized(school?.Company?.Name, school?.Company?.NameAr);
    private string? ReceiptFooterImage => school?.Company?.ReceiptFooter;
    private string ReceiptAmountInWords => savedReceipt is null
        ? string.Empty
        : new ToWord(savedReceipt.Total, new CurrencyInfo(CurrencyInfo.Currencies.SaudiArabia)).ConvertToArabic();

    protected override async Task OnParametersSetAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        isLoading = true;
        try
        {
            await _appStateService.InitializeAsync();
            if (_appStateService.SchoolId == Guid.Empty) return;

            parent = _mapper.Map<ParentDto>(await _uow.Parents.FindAsync(x => x.Id == ParentId , new[] { "Gender" }));
            school = await _uow.Schools.FindAsync(x => x.Id == _appStateService.SchoolId, ["Company"]);
            if (parent is null || school is null) return;

            var studentEntities = (await _uow.Students.FindAllAsync(x => x.ParentId == ParentId && x.SchoolId == _appStateService.SchoolId,
                ["CurrentContract", "CurrentContract.Class", "CurrentContract.Class.Level"]))
                .OrderBy(x => x.SN).ToList();

            var contractIds = studentEntities.Where(x => x.CurrentContractId != Guid.Empty).Select(x => x.CurrentContractId).ToArray();
            var accounts = contractIds.Length == 0
                ? []
                : (await _uow.StudentAccounts.FindAllAsync(x => contractIds.Contains(x.ContractId))).ToList();
            var balances = accounts.GroupBy(x => x.ContractId).ToDictionary(
                contract => contract.Key,
                contract => contract
                    .GroupBy(x => new { x.SemesterId, x.ServiceId, x.Categoryid })
                    .Sum(service => Math.Max(0, service.Sum(a => a.Debit - a.Credit))));

            students = studentEntities.Select(x => new StudentPaymentCard(
                x.Id,
                x.CurrentContractId,
                x.GenderId,
                $"{x.Name} {x.FatherName}".Trim(),
                $"{school.Code}-{x.SN:D5}",
                Localized(x.CurrentContract?.Class?.Level?.Name, x.CurrentContract?.Class?.Level?.NameAr),
                Localized(x.CurrentContract?.Class?.Name, x.CurrentContract?.Class?.NameAr),
                balances.GetValueOrDefault(x.CurrentContractId))).ToList();

            paymentMethods = (await _uow.PaymentMethods.GetAllAsync())
                .OrderBy(x => x.Id)
                .Select(x => new PaymentMethodOption(x.Id, Localized(x.Name, x.NameAr), IsChequeName(x.Name, x.NameAr)))
                .ToList();
            banks = (await _uow.Banks.GetAllAsync())
                .OrderBy(x => Localized(x.Name, x.NameAr))
                .Select(x => new BankOption(x.Id, Localized(x.Name, x.NameAr)))
                .ToList();

            if (ReceiptId.HasValue)
            {
                await LoadSavedReceiptAsync(ReceiptId.Value);
                return;
            }

            if (paymentMethods.Count > 0) paymentMethodId = paymentMethods[0].Id;
            if (students.Count > 0) await SelectStudentAsync(students[0].Id);
        }
        catch (Exception ex)
        {
            parent = new();
            _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task LoadSavedReceiptAsync(Guid receiptId)
    {
        var receipt = await _uow.Receipts.FindAsync(x => x.Id == receiptId,
            ["Contract.Student.Parent", "Contract.Student.School", "Contract.Class.Level", "PaymentMethod", "ReceiptDetails"]);
        if (receipt is null || receipt.Contract.Student.ParentId != ParentId || receipt.Contract.Student.SchoolId != _appStateService.SchoolId)
        {
            parent = new();
            return;
        }

        var student = receipt.Contract.Student;
        selectedStudent = students.FirstOrDefault(x => x.Id == receipt.Contract.StudentId);
        savedReceipt = new SavedReceipt(
            receipt.SN, receipt.Date, Localized(receipt.PaymentMethod.Name, receipt.PaymentMethod.NameAr),
            IsChequeName(receipt.PaymentMethod.Name, receipt.PaymentMethod.NameAr), receipt.BankName, receipt.ChequeNo, receipt.ChequeDate,
            $"{student.Name} {student.FatherName}".Trim(), $"{student.School.Code}-{student.SN:D5}",
            Localized(receipt.Contract.Class?.Level?.Name, receipt.Contract.Class?.Level?.NameAr),
            Localized(receipt.Contract.Class?.Name, receipt.Contract.Class?.NameAr),
            Math.Round(receipt.ReceiptDetails.Sum(x => x.Amount), 2));
    }

    private async Task SelectStudentAsync(Guid studentId)
    {
        selectedStudentId = studentId;
        selectedStudent = students.FirstOrDefault(x => x.Id == studentId);
        dueItems = [];
        fullPaymentDiscounts = [];
        hasFullPaymentDiscountConfiguration = false;
        excessAmount = 0;
        validationMessage = null;
        if (selectedStudent is null || selectedStudent.ContractId == Guid.Empty) return;

        var accounts = await LoadContractAccountsAsync(selectedStudent.ContractId);
        dueItems = BuildDueItems(accounts);
        fullPaymentDiscounts = await CalculateFullPaymentDiscountsAsync(accounts);
    }

    private async Task RefreshDiscountPreviewAsync()
    {
        if (selectedStudent is not null)
            fullPaymentDiscounts = await CalculateFullPaymentDiscountsAsync(await LoadContractAccountsAsync(selectedStudent.ContractId));
    }

    private double ExpectedDiscount(DueItem item) => fullPaymentDiscounts
        .Where(x => x.SemesterId == item.SemesterId && x.ServiceId == item.ServiceId && x.Categoryid == item.CategoryId)
        .Sum(x => x.Credit);

    private double ExpectedPayment(DueItem item) => Math.Max(0, Math.Round(item.Outstanding - ExpectedDiscount(item), 2));

    private async Task<List<StudentAccount>> CalculateFullPaymentDiscountsAsync(List<StudentAccount> accounts)
    {
        var result = new List<StudentAccount>();
        hasFullPaymentDiscountConfiguration = false;
        if (selectedStudent is null) return result;
        var contract = await _uow.StudentContracts.FindAsync(x => x.Id == selectedStudent.ContractId);
        if (contract is null) return result;
        var details = (await _uow.DiscountDetails.FindAllAsync(x => x.SchoolId == _appStateService.SchoolId
            && x.YearId == contract.RegisteredForYearId && x.Discount.ServiceCategoryId == 20
            && x.Discount.TransactionTypeId == TransactionTypeIds.FullPaymentDiscount
            && x.Discount.ApplicationTiming == Shared.Enums.DiscountApplicationTiming.OnPayment, ["Discount"]))
            .OrderBy(x => x.Discount.SN).ThenBy(x => x.DiscountId).ToList();
        hasFullPaymentDiscountConfiguration = details.Count > 0;
        var semesters = (await _uow.SchoolSemesters.FindAllAsync(x => x.SchoolId == _appStateService.SchoolId && x.YearId == contract.RegisteredForYearId)).ToList();
        var siblingOrder = await _uow.Students.CountAsync(x => x.Id != selectedStudent.Id && x.ParentId == ParentId
            && x.SchoolId == _appStateService.SchoolId && x.CurrentContract.RegisteredForYearId == contract.RegisteredForYearId
            && x.CurrentContract.StatusId >= 10 && x.CurrentContract.StatusId < 30) + 1;
        foreach (var service in accounts.Where(x => x.Categoryid == 20).GroupBy(x => new { x.ServiceId, x.SemesterId }))
        {
            if (service.Any(x => x.TransactionTypeId == TransactionTypeIds.FullPaymentDiscount)) continue;
            var net = StudentAccountDiscountCalculator.CalculateNetServiceFee(accounts, contract.Id, service.Key.ServiceId, service.Key.SemesterId);
            foreach (var rule in details.GroupBy(x => x.DiscountId))
            {
                var detail = rule.Where(x => x.SemesterId == service.Key.SemesterId || x.SemesterId == 0)
                    .OrderByDescending(x => x.SemesterId == service.Key.SemesterId)
                    .FirstOrDefault(x => StudentService.IsRegistrationStateInRange(x,
                        semesters.FirstOrDefault(s => s.SemesterId == service.Key.SemesterId), receiptDate, siblingOrder, contract.ClassId));
                if (detail is null) continue;
                var amount = StudentAccountDiscountCalculator.CalculateDiscountAmount(net, (decimal)detail.Amount, (decimal)detail.AdditionalFixedAmount);
                if (amount <= 0) continue;
                var discountEntry = new StudentAccount
                {
                    ContractId = contract.Id,
                    ServiceId = service.Key.ServiceId,
                    Categoryid = 20,
                    SemesterId = service.Key.SemesterId,
                    TransactionTypeId = TransactionTypeIds.FullPaymentDiscount, 
                    Credit = (double)amount, 
                    Percentage = (decimal)detail.Amount,
                    RefNo = StudentDiscountWithdrawal.DiscountReference(detail.DiscountId),
                    Comments = detail.Discount.NameAr
                };
                var taxDiscount = StudentSubscriptionTax.CreateDiscountEntry(discountEntry, accounts.Concat(result));
                result.Add(discountEntry);
                if (taxDiscount is not null) result.Add(taxDiscount);
                net -= amount;
            }
        }
        return result;
    }

    private async Task<List<StudentAccount>> LoadContractAccountsAsync(Guid contractId) =>
        (await _uow.StudentAccounts.FindAllAsync( x => x.ContractId == contractId,["Semester", "Service", "Category"]))
        .OrderBy(x => x.SemesterId).ThenBy(x => x.Categoryid).ThenBy(x => x.ServiceId).ToList();

    private List<DueItem> BuildDueItems(IEnumerable<StudentAccount> accounts) => accounts
        .GroupBy(x => new { x.SemesterId, x.ServiceId, x.Categoryid })
        .Select(group =>
        {
            var first = group.First();
            return new DueItem
            {
                SemesterId = group.Key.SemesterId,
                ServiceId = group.Key.ServiceId,
                CategoryId = group.Key.Categoryid,
                SemesterName = Localized(first.Semester.Name, first.Semester.NameAr),
                ServiceName = Localized(first.Service.Name, first.Service.NameAr),
                CategoryName = Localized(first.Category.Name, first.Category.NameAr),
                Outstanding = Math.Round(group.Sum(x => x.Debit - x.Credit), 2)
            };
        })
        .Where(x => x.Outstanding > Epsilon)
        .OrderBy(x => x.SemesterId).ThenBy(x => x.CategoryName).ThenBy(x => x.ServiceName)
        .ToList();

    private void PaymentMethodChanged(object? _)
    {
        validationMessage = null;
        if (IsCheque) return;
        bankName = null;
        chequeNo = null;
        chequeDate = null;
    }

    private void AllocationChanged(DueItem item)
    {
        item.Error = null;
        if (item.PaymentAmount < 0) item.PaymentAmount = 0;
        if (item.PaymentAmount > item.Outstanding + Epsilon)
        {
            item.PaymentAmount = item.Outstanding;
            item.Error = "لا يمكن سداد مبلغ أكبر من المستحق.";
        }
        validationMessage = null;
    }

    private async Task SaveAsync()
    {
        if (isSaving || selectedStudent is null) return;
        validationMessage = ValidateForm();
        if (validationMessage is not null) return;

        isSaving = true;
        try
        {
            var freshAccounts = await LoadContractAccountsAsync(selectedStudent.ContractId);
            fullPaymentDiscounts = await CalculateFullPaymentDiscountsAsync(freshAccounts);
            var freshDues = BuildDueItems(freshAccounts).ToDictionary(x => (x.SemesterId, x.ServiceId, x.CategoryId));
            foreach (var item in dueItems.Where(x => x.PaymentAmount > Epsilon))
            {
                if (!freshDues.TryGetValue((item.SemesterId, item.ServiceId, item.CategoryId), out var fresh) || item.PaymentAmount > fresh.Outstanding + Epsilon)
                {
                    validationMessage = $"تغير المستحق لخدمة {item.ServiceName}. تم تحديث المبالغ، يرجى المراجعة ثم الحفظ مرة أخرى.";
                    dueItems = BuildDueItems(freshAccounts);
                    return;
                }
            }

            var userId = await _userService.GetUserIdAsync();
            var receiptNumber = await _uow.GetNextReceiptNumberAsync(school!.Id);
            var now = DateTime.UtcNow.GetKsaDateTime();
            var receipt = new Receipt
            {
                SN = receiptNumber,
                ContractId = selectedStudent.ContractId,
                Date = receiptDate.Date,
                PaymentMethodId = paymentMethodId,
                BankName = IsCheque ? bankName?.Trim() : null,
                ChequeNo = IsCheque ? chequeNo?.Trim() : null,
                ChequeDate = IsCheque ? chequeDate?.Date : null,
                CreatedBy = userId,
                CreatedOn = now
            };

            var selectedItems = dueItems.Where(x => x.PaymentAmount > Epsilon).ToList();
            foreach (var item in selectedItems)
            {
                receipt.ReceiptDetails.Add(CreateReceiptDetail(receipt.Id, item.ServiceId, item.CategoryId, item.SemesterId, item.PaymentAmount, userId, now));
                await _uow.StudentAccounts.AddAsync(CreatePaymentAccount(selectedStudent.ContractId, item.ServiceId, item.CategoryId, item.SemesterId, RegularPaymentTransactionTypeId, item.PaymentAmount, receipt.SN, receipt.Date, userId, now));
            }

            if (excessAmount > Epsilon)
            {
                var reference = freshAccounts.FirstOrDefault();
                if (reference is null)
                {
                    var contractDetail = await _uow.StudentContractDetails.FindAsync(x => x.ContractId == selectedStudent.ContractId, ["Service", "Semester"]);
                    if (contractDetail is null)
                    {
                        validationMessage = "لا يمكن تسجيل المبلغ الفائض لعدم وجود خدمة مرتبطة بعقد الطالب.";
                        return;
                    }

                    reference = new StudentAccount
                    {
                        ServiceId = contractDetail.ServiceId,
                        Categoryid = contractDetail.Service.CategoryId,
                        SemesterId = contractDetail.SemesterId
                    };
                }

                receipt.ReceiptDetails.Add(CreateReceiptDetail(receipt.Id, reference.ServiceId, reference.Categoryid, reference.SemesterId, excessAmount, userId, now));
                await _uow.StudentAccounts.AddAsync(CreatePaymentAccount(selectedStudent.ContractId, reference.ServiceId, reference.Categoryid, reference.SemesterId, RegularPaymentTransactionTypeId, excessAmount, receipt.SN, receipt.Date, userId, now));
            }

            // Require every tuition service/semester to be settled after the proposed discounts.
            var tuitionSettled = FullPaymentDiscountCalculator.IsTuitionSettled(freshAccounts, receipt.ReceiptDetails, fullPaymentDiscounts);
            if (tuitionSettled)
                foreach (var discount in fullPaymentDiscounts)
                {
                    discount.Date = receipt.Date;
                    discount.CreatedBy = userId;
                    discount.CreatedOn = now;
                    await _uow.StudentAccounts.AddAsync(discount);
                }

            await _uow.Receipts.AddAsync(receipt);
            if (!await _uow.SaveAsync())
            {
                validationMessage = "تعذر حفظ سند السداد، يرجى المحاولة مرة أخرى.";
                return;
            }

            var method = paymentMethods.First(x => x.Id == paymentMethodId);
            savedReceipt = new SavedReceipt(
                receipt.SN, receipt.Date, method.DisplayName, method.IsCheque, receipt.BankName, receipt.ChequeNo, receipt.ChequeDate,
                selectedStudent.Name, selectedStudent.Code, selectedStudent.LevelName, selectedStudent.ClassName,
                receipt.ReceiptDetails.Sum(x => x.Amount));
            _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], "تم حفظ سند السداد بنجاح.");
            await SendPaymentMessageAsync(savedReceipt.Total);
        }
        catch (Exception ex)
        {
            validationMessage = ex.Message;
            _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
        }
        finally
        {
            await _uow.DiscardChangesAsync(); isSaving = false;
        }
    }

    private async Task SendPaymentMessageAsync(double amount)
    {
        try
        {
            if (school is null || string.IsNullOrWhiteSpace(parent.Mobile))
                throw new InvalidOperationException("رقم جوال ولي الأمر غير متوفر.");
            var message = $"تم سداد {amount.ToString("0.##", CultureInfo.InvariantCulture)} ريال عن {(selectedStudent!.GenderId == 2 ? "الطالبة" : "الطالب")} {selectedStudent.Name}";
            var result = await SmsSender.SendAsync(new SmsSendRequest(school.SmsSender, school.SmsUserName, school.SmsPassword, [parent.Mobile], message));
            if (!result.IsSuccess) throw new InvalidOperationException(result.ErrorMessage);
        }
        catch (Exception)
        {
            _toastService.Notify(NotificationSeverity.Warning, "الرسالة النصية", "تم حفظ السند، ولكن تعذر إرسال الرسالة النصية إلى ولي الأمر.");
        }
    }

    private string? ValidateForm()
    {
        foreach (var item in dueItems)
        {
            item.Error = null;
            if (item.PaymentAmount < -Epsilon || item.PaymentAmount > item.Outstanding + Epsilon)
            {
                item.Error = "لا يمكن سداد مبلغ أكبر من المستحق.";
                return $"مبلغ السداد لخدمة {item.ServiceName} أكبر من المبلغ المستحق.";
            }
        }
        if (paymentMethodId == 0 || paymentMethods.All(x => x.Id != paymentMethodId)) return "يرجى اختيار طريقة الدفع.";
        if (receiptDate == default) return "يرجى تحديد تاريخ السند.";
        if (PaymentTotal <= Epsilon) return "يرجى إدخال مبلغ للسداد.";
        if (excessAmount < 0) return "المبلغ الفائض يجب ألا يكون سالبًا.";
        if (IsCheque && string.IsNullOrWhiteSpace(bankName)) return "يرجى اختيار اسم البنك.";
        if (IsCheque && string.IsNullOrWhiteSpace(chequeNo)) return "يرجى إدخال رقم الشيك.";
        if (IsCheque && chequeDate is null) return "يرجى تحديد تاريخ الشيك.";
        return null;
    }

    private static ReceiptDetail CreateReceiptDetail(Guid receiptId, int serviceId, int categoryId, byte semesterId, double amount, string userId, DateTime now) => new()
    {
        Id = Guid.NewGuid(), ReceiptId = receiptId, ServiceId = serviceId, CategoryId = categoryId, SemesterId = semesterId,
        Amount = Math.Round(amount, 2), CreatedBy = userId, CreatedOn = now
    };

    private static StudentAccount CreatePaymentAccount(Guid contractId, int serviceId, int categoryId, byte semesterId, int transactionTypeId, double amount, int receiptNumber, DateTime transactionDate, string userId, DateTime now) => new()
    {
        ContractId = contractId,
        ServiceId = serviceId,
        Categoryid = categoryId,
        SemesterId = semesterId,
        TransactionTypeId = transactionTypeId,
        RefNo = receiptNumber.ToString(CultureInfo.InvariantCulture),
        Debit = 0,
        Credit = Math.Round(amount, 2),
        Percentage = 0,
        Date = transactionDate.Date, 
        Comments = $"سداد مستحقات بسند رقم {receiptNumber}",
        CreatedBy = userId,
        CreatedOn = now
    };

    private async Task PrintAsync() => await _js.InvokeVoidAsync("APP.printReport", $"Receipt-{savedReceipt?.Number}","portrait");

    private async Task StartAnotherPayment()
    {
        savedReceipt = null;
        receiptDate = DateTime.Today;
        chequeDate = null;
        chequeNo = null;
        bankName = null;
        excessAmount = 0;
        validationMessage = null;
        await LoadAsync();
    }

    private string Localized(string? english, string? arabic) => IsArabic && !string.IsNullOrWhiteSpace(arabic) ? arabic : english ?? arabic ?? string.Empty;
    private static bool IsChequeName(string? english, string? arabic) => $"{english} {arabic}".Contains("شيك", StringComparison.OrdinalIgnoreCase)
        || $"{english} {arabic}".Contains("cheque", StringComparison.OrdinalIgnoreCase)
        || $"{english} {arabic}".Contains("check", StringComparison.OrdinalIgnoreCase);
    private static string Initials(string name) => string.Join(string.Empty, name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x => x[0]));

    private sealed record StudentPaymentCard(Guid Id, Guid ContractId, byte GenderId, string Name, string Code, string LevelName, string ClassName, double TotalDue);
    private sealed record PaymentMethodOption(byte Id, string DisplayName, bool IsCheque);
    private sealed record BankOption(int Id, string DisplayName);
    private sealed record SavedReceipt(int Number, DateTime Date, string PaymentMethod, bool IsCheque, string? BankName, string? ChequeNo,
        DateTime? ChequeDate, string StudentName, string StudentCode, string LevelName, string ClassName, double Total);

    private sealed class DueItem
    {
        public byte SemesterId { get; init; }
        public int ServiceId { get; init; }
        public int CategoryId { get; init; }
        public string SemesterName { get; init; } = string.Empty;
        public string ServiceName { get; init; } = string.Empty;
        public string CategoryName { get; init; } = string.Empty;
        public double Outstanding { get; init; }
        public double PaymentAmount { get; set; }
        public string? Error { get; set; }
    }
}
