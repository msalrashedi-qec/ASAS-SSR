namespace Web.Components.Pages.Business;

public partial class ReceiptEdit
{
    private const int RegularPaymentTransactionTypeId = TransactionTypeIds.DuesPayment;

    [Parameter] public Guid ParentId { get; set; }
    [Parameter] public Guid ReceiptId { get; set; }

    private Receipt? receipt;
    private decimal originalTotal;
    private string parentName = string.Empty;
    private string studentName = string.Empty;
    private DateTime receiptDate;
    private byte paymentMethodId;
    private string? bankName;
    private string? chequeNo;
    private DateTime? chequeDate;
    private List<PaymentMethodItem> paymentMethods = [];
    private List<BankItem> banks = [];
    private List<DetailItem> details = [];
    private bool isLoading;
    private bool isSaving;
    private string? validationMessage;

    private bool IsCheque => paymentMethods.FirstOrDefault(x => x.Id == paymentMethodId)?.IsCheque == true;

    protected override async Task OnParametersSetAsync()
    {
        isLoading = true;
        try
        {
            await _appStateService.InitializeAsync();
            receipt = await _uow.Receipts.FindAsync(x => x.Id == ReceiptId,
                ["Contract.Student.Parent", "Contract.Student.School", "PaymentMethod", "ReceiptDetails.Semester", "ReceiptDetails.Service", "ReceiptDetails.Category"]);

            if (receipt is null || receipt.Contract.Student.ParentId != ParentId || receipt.Contract.Student.SchoolId != _appStateService.SchoolId)
            {
                receipt = null;
                return;
            }

            paymentMethods = (await _uow.PaymentMethods.GetAllAsync()).OrderBy(x => x.Id)
                .Select(x => new PaymentMethodItem(x.Id, Localized(x.Name, x.NameAr), IsChequeName(x.Name, x.NameAr))).ToList();
            banks = (await _uow.Banks.GetAllAsync()).OrderBy(x => Localized(x.Name, x.NameAr))
                .Select(x => new BankItem(Localized(x.Name, x.NameAr))).ToList();

            parentName = receipt.Contract.Student.Parent.Name;
            studentName = $"{receipt.Contract.Student.Name} {receipt.Contract.Student.FatherName}".Trim();
            receiptDate = receipt.Date;
            paymentMethodId = receipt.PaymentMethodId;
            bankName = receipt.BankName;
            chequeNo = receipt.ChequeNo;
            chequeDate = receipt.ChequeDate;
            originalTotal = receipt.ReceiptDetails.Sum(x => (decimal)x.Amount);
            details = BuildDetails(receipt, await LoadAccountsAsync(receipt.ContractId));

        }
        catch (Exception ex)
        {
            receipt = null;
            _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
        }
        finally { isLoading = false; }
    }

    private void PaymentMethodChanged(object? _)
    {
        validationMessage = null;
        if (IsCheque) return;
        bankName = null;
        chequeNo = null;
        chequeDate = null;
    }

    private async Task SaveAsync()
    {
        if (receipt is null || isSaving) return;
        validationMessage = Validate();
        if (validationMessage is not null) return;

        isSaving = true;
        try
        {
            var current = await _uow.Receipts.FindAsync(x => x.Id == ReceiptId && x.Contract.Student.ParentId == ParentId
                && x.Contract.Student.SchoolId == _appStateService.SchoolId, ["ReceiptDetails", "Contract"]);
            if (current is null)
            {
                validationMessage = "لم يعد السند موجودًا.";
                return;
            }

            if (current.ReceiptDetails.Sum(x => (decimal)x.Amount) != originalTotal)
            {
                validationMessage = "تغيرت قيمة السند. يرجى إعادة تحميل الصفحة.";
                return;
            }
            var ledger = await LoadAccountsAsync(current.ContractId);
            var accounts = ledger.Where(x => x.TransactionTypeId == RegularPaymentTransactionTypeId
                && x.RefNo == current.SN.ToString(CultureInfo.InvariantCulture)).ToList();
            if (!ReceiptAllocation.MatchesLedger(current.ReceiptDetails.Select(x => (x.SemesterId, x.ServiceId, x.CategoryId, x.Amount)),
                accounts.Select(x => (x.SemesterId, x.ServiceId, x.Categoryid, x.Credit))))
            {
                validationMessage = "قيود حساب الطالب لا تطابق بنود السند. يرجى مراجعتها قبل التعديل.";
                return;
            }
            var available = BuildDetails(current, ledger).ToDictionary(x => (x.SemesterId, x.ServiceId, x.CategoryId));
            foreach (var item in details.Where(x => x.Amount > 0))
                if (!available.TryGetValue((item.SemesterId, item.ServiceId, item.CategoryId), out var due)
                    || (decimal)item.Amount > due.Available)
                {
                    validationMessage = $"المبلغ الموزع على خدمة {item.Service} يتجاوز الرصيد المتاح. يرجى مراجعة التوزيع.";
                    return;
                }
            var beforePayment = ledger.Except(accounts).ToList();
            var proposedDetails = details.Where(x => x.Amount > 0).Select(x => new ReceiptDetail
            {
                ReceiptId = current.Id, SemesterId = x.SemesterId, ServiceId = x.ServiceId,
                CategoryId = x.CategoryId, Amount = x.Amount
            }).ToList();
            var afterPayment = beforePayment.Concat(proposedDetails.Select(x => new StudentAccount
            {
                ContractId = current.ContractId, SemesterId = x.SemesterId, ServiceId = x.ServiceId,
                Categoryid = x.CategoryId, Credit = x.Amount, TransactionTypeId = RegularPaymentTransactionTypeId
            })).ToList();
            var discountsToRemove = FullPaymentDiscountCalculator.GetDiscountsToRemove(afterPayment, current.ContractId);
            afterPayment = afterPayment.Except(discountsToRemove).ToList();
            var discounts = await PaymentDiscountService.CalculateAsync(_uow, _appStateService.SchoolId, current.Contract, ParentId, receiptDate, afterPayment);
            var userId = await _userService.GetUserIdAsync();
            var now = DateTime.UtcNow.GetKsaDateTime();

            current.Contract = null!;
            current.Date = receiptDate.Date;
            current.PaymentMethodId = paymentMethodId;
            current.BankName = IsCheque ? bankName?.Trim() : null;
            current.ChequeNo = IsCheque ? chequeNo?.Trim() : null;
            current.ChequeDate = IsCheque ? chequeDate?.Date : null;
            current.UpdatedBy = userId;
            current.UpdatedOn = now;
            await _uow.Receipts.Update(current);

            foreach (var oldDetail in current.ReceiptDetails.ToList())
                await _uow.ReceiptDetails.Delete(oldDetail);
            foreach (var account in accounts.Concat(discountsToRemove))
            {
                account.Semester = null!;
                account.Service = null!;
                account.Category = null!;
                await _uow.StudentAccounts.Delete(account);
            }
            foreach (var entity in proposedDetails)
            {
                entity.CreatedBy = userId;
                entity.CreatedOn = now;
                await _uow.ReceiptDetails.AddAsync(entity);
                await _uow.StudentAccounts.AddAsync(new StudentAccount
                {
                    ContractId = current.ContractId,
                    SemesterId = entity.SemesterId, 
                    ServiceId = entity.ServiceId,
                    Categoryid = entity.CategoryId,
                    Credit = entity.Amount,
                    Date = receiptDate.Date,
                    TransactionTypeId = RegularPaymentTransactionTypeId,
                    RefNo = current.SN.ToString(CultureInfo.InvariantCulture),
                    Comments = $"سداد مستحقات بسند رقم {current.SN}",
                    CreatedBy = userId,
                    CreatedOn = now
                });
            }
            foreach (var discount in discounts)
            {
                discount.Date = receiptDate.Date;
                discount.CreatedBy = userId;
                discount.CreatedOn = now;
                await _uow.StudentAccounts.AddAsync(discount);
            }

            if (!await _uow.SaveAsync())
            {
                validationMessage = "تعذر حفظ تعديلات السند، يرجى المحاولة مرة أخرى.";
                return;
            }

            _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], "تم تعديل سند القبض بنجاح.");
            _navigator.NavigateTo($"/parent-receipts/{ParentId}");
        }
        catch (Exception ex)
        {
            validationMessage = ex.Message;
            _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
        }
        finally { await _uow.DiscardChangesAsync(); isSaving = false; }
    }

    private string? Validate()
    {
        if (receiptDate == default) return "يرجى تحديد تاريخ السند.";
        if (paymentMethods.All(x => x.Id != paymentMethodId)) return "يرجى اختيار طريقة الدفع.";
        if (!ReceiptAllocation.IsValid(details.Select(x => x.Amount), originalTotal))
            return "يجب إدخال مبالغ غير سالبة، وأن يساوي إجمالي السداد قيمة السند الأصلية تمامًا.";
        if (IsCheque && string.IsNullOrWhiteSpace(bankName)) return "يرجى اختيار اسم البنك.";
        if (IsCheque && string.IsNullOrWhiteSpace(chequeNo)) return "يرجى إدخال رقم الشيك.";
        if (IsCheque && chequeDate is null) return "يرجى تحديد تاريخ الشيك.";
        return null;
    }

    private async Task<List<StudentAccount>> LoadAccountsAsync(Guid contractId) =>
        (await _uow.StudentAccounts.FindAllAsync(x => x.ContractId == contractId, ["Semester", "Service", "Category"])).ToList();

    private List<DetailItem> BuildDetails(Receipt source, List<StudentAccount> ledger)
    {
        var reference = source.SN.ToString(CultureInfo.InvariantCulture);
        var original = source.ReceiptDetails.GroupBy(x => (x.SemesterId, x.ServiceId, x.CategoryId))
            .ToDictionary(x => x.Key, x => x.Sum(d => (decimal)d.Amount));
        return ledger.GroupBy(x => (x.SemesterId, x.ServiceId, x.Categoryid)).Select(group =>
        {
            var first = group.First();
            var amount = original.GetValueOrDefault(group.Key);
            var due = decimal.Round(group.Where(x => x.TransactionTypeId != RegularPaymentTransactionTypeId || x.RefNo != reference)
                .Sum(x => (decimal)x.Debit - (decimal)x.Credit), 2);
            return new DetailItem( first.SemesterId, first.ServiceId, first.Categoryid,
                Localized(first.Semester.Name, first.Semester.NameAr), Localized(first.Service.Name, first.Service.NameAr),
                Localized(first.Category.Name, first.Category.NameAr), (double)amount)
            { Available = Math.Max(amount, due) };
        }).Where(x => x.Available > 0 || x.Amount > 0)
            .OrderBy(x => x.SemesterId).ThenBy(x => x.CategoryId).ThenBy(x => x.ServiceId).ToList();
    }

    private string Localized(string? english, string? arabic) => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" && !string.IsNullOrWhiteSpace(arabic) ? arabic : english ?? arabic ?? string.Empty;
    private static bool IsChequeName(string? english, string? arabic) => $"{english} {arabic}".Contains("شيك", StringComparison.OrdinalIgnoreCase) || $"{english} {arabic}".Contains("cheque", StringComparison.OrdinalIgnoreCase) || $"{english} {arabic}".Contains("check", StringComparison.OrdinalIgnoreCase);

    private sealed record PaymentMethodItem(byte Id, string Name, bool IsCheque);
    private sealed record BankItem(string Name);
    private sealed class DetailItem( byte semesterId, int serviceId, int categoryId, string semester, string service, string category, double amount)
    {
        public byte SemesterId { get; } = semesterId;
        public int ServiceId { get; } = serviceId;
        public int CategoryId { get; } = categoryId;
        public string Semester { get; } = semester;
        public string Service { get; } = service;
        public string Category { get; } = category;
        public double Amount { get; set; } = amount;
        public decimal Available { get; init; }
    }
}
