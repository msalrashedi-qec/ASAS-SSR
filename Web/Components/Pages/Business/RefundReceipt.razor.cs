using System.Xml.Linq;
using RefundReceiptEntity = Core.Entities.Business.RefundReceipt;

namespace Web.Components.Pages.Business;

public partial class RefundReceipt
{
    private const int RefundTransactionTypeId = TransactionTypeIds.FeeRefund;
    private const double Epsilon = 0.005;

    [Parameter] public Guid ParentId { get; set; }
    [Parameter] public Guid? RefundReceiptId { get; set; }

    private Parent? parent;
    private School? school;
    private List<StudentCard> students = [];
    private List<CreditItem> creditItems = [];
    private List<PaymentMethodOption> paymentMethods = [];
    private List<BankOption> banks = [];
    private Guid? selectedStudentId;
    private StudentCard? selectedStudent;
    private DateTime receiptDate = DateTime.Today;
    private DateTime? chequeDate;
    private byte paymentMethodId;
    private string? bankName;
    private string? chequeNo;
    private bool isLoading;
    private bool isSaving;
    private string? validationMessage;
    private SavedRefund? savedReceipt;
    private Guid? savedRefundId;

    private bool IsArabic => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
    private bool IsCheque => paymentMethods.FirstOrDefault(x => x.Id == paymentMethodId)?.IsCheque == true;
    private double RefundTotal => Math.Round(creditItems.Sum(x => Math.Max(0, x.RefundAmount)), 2);
    private string SchoolName => Localized(school?.Name, school?.NameAr);
    private string CompanyName => Localized(school?.Company?.Name, school?.Company?.NameAr);
    private string? ReceiptFooterImage => school?.Company?.ReceiptFooter;
    private string RefundAmountInWords => savedReceipt is null ? string.Empty : new ToWord(savedReceipt.Total, new CurrencyInfo(CurrencyInfo.Currencies.SaudiArabia)).ConvertToArabic();

    protected override async Task OnParametersSetAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        isLoading = true;
        try
        {
            await _appStateService.InitializeAsync();
            if (_appStateService.SchoolId == Guid.Empty)
            {
                _navigator.NavigateTo("select-school");
                return;
            }

            parent = await _uow.Parents.FindAsync(x => x.Id == ParentId, new[] { "Gender" });
            school = await _uow.Schools.FindAsync(x => x.Id == _appStateService.SchoolId, ["Company"]);
            if (parent is null || school is null) return;

            var studentEntities = (await _uow.Students.FindAllAsync(
                x => x.ParentId == ParentId && x.SchoolId == _appStateService.SchoolId,
                ["CurrentContract", "CurrentContract.Class", "CurrentContract.Class.Level"]))
                .OrderBy(x => x.SN).ToList();
            var contractIds = studentEntities.Where(x => x.CurrentContractId != Guid.Empty).Select(x => x.CurrentContractId).ToArray();
            var accounts = contractIds.Length == 0 ? [] : (await _uow.StudentAccounts.FindAllAsync(x => contractIds.Contains(x.ContractId))).ToList();
            var refundable = accounts.GroupBy(x => x.ContractId).ToDictionary(x => x.Key,
                x => Math.Round(x.GroupBy(a => new { a.SemesterId, a.ServiceId, a.Categoryid }).Sum(g => Math.Max(0, g.Sum(a => a.Credit - a.Debit))), 2));

            students = studentEntities.Select(x => new StudentCard(x.Id, x.CurrentContractId,
                $"{x.Name} {x.FatherName}".Trim(), $"{school.Code}-{x.SN:D5}",
                Localized(x.CurrentContract?.Class?.Level?.Name, x.CurrentContract?.Class?.Level?.NameAr),
                Localized(x.CurrentContract?.Class?.Name, x.CurrentContract?.Class?.NameAr),
                refundable.GetValueOrDefault(x.CurrentContractId))).ToList();

            paymentMethods = (await _uow.PaymentMethods.GetAllAsync()).OrderBy(x => x.Id)
                .Select(x => new PaymentMethodOption(x.Id, Localized(x.Name, x.NameAr), IsChequeName(x.Name, x.NameAr))).ToList();
            banks = (await _uow.Banks.GetAllAsync()).OrderBy(x => Localized(x.Name, x.NameAr))
                .Select(x => new BankOption(Localized(x.Name, x.NameAr))).ToList();

            if (RefundReceiptId.HasValue)
            {
                await LoadSavedRefundAsync(RefundReceiptId.Value);
                return;
            }

            if (paymentMethods.Count > 0) paymentMethodId = paymentMethods[0].Id;
            var first = students.FirstOrDefault(x => x.Refundable > Epsilon) ?? students.FirstOrDefault();
            if (first is not null) await SelectStudentAsync(first.Id);
        }
        catch (Exception ex)
        {
            parent = null;
            _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
        }
        finally { isLoading = false; }
    }

    private async Task LoadSavedRefundAsync(Guid refundReceiptId)
    {
        var receipt = await _uow.RefundReceipts.FindAsync(x => x.Id == refundReceiptId,
            ["Contract.Student.Parent", "Contract.Student.School", "Contract.Class.Level", "PaymentMethod"]);
        if (receipt is null || receipt.Contract.Student.ParentId != ParentId || receipt.Contract.Student.SchoolId != _appStateService.SchoolId)
        {
            parent = null;
            return;
        }

        var details = (await _uow.RefundReceiptDetails.FindAllAsync(x => x.ReceiptId == refundReceiptId,
            ["Semester", "Service", "Category"])).OrderBy(x => x.SemesterId).ThenBy(x => x.CategoryId).ThenBy(x => x.ServiceId).ToList();
        var student = receipt.Contract.Student;
        savedRefundId = receipt.Id;
        savedReceipt = new SavedRefund(receipt.SN, receipt.Date, Localized(receipt.PaymentMethod.Name, receipt.PaymentMethod.NameAr),
            IsChequeName(receipt.PaymentMethod.Name, receipt.PaymentMethod.NameAr), receipt.BankName, receipt.ChequeNo, receipt.ChequeDate,
            $"{student.Name} {student.FatherName}".Trim(), details.Sum(x => x.Amount),
            details.Select(x => new SavedRefundItem(Localized(x.Semester.Name, x.Semester.NameAr), Localized(x.Service.Name, x.Service.NameAr), Localized(x.Category.Name, x.Category.NameAr), x.Amount)).ToList());
    }

    private async Task SelectStudentAsync(Guid studentId)
    {
        selectedStudentId = studentId;
        selectedStudent = students.FirstOrDefault(x => x.Id == studentId);
        creditItems = [];
        validationMessage = null;
        if (selectedStudent is null || selectedStudent.ContractId == Guid.Empty) return;
        creditItems = BuildCreditItems(await LoadContractAccountsAsync(selectedStudent.ContractId));
    }

    private async Task<List<StudentAccount>> LoadContractAccountsAsync(Guid contractId) =>
        (await _uow.StudentAccounts.FindAllAsync(x => x.ContractId == contractId, ["Semester", "Service", "Category"]))
        .OrderBy(x => x.SemesterId).ThenBy(x => x.Categoryid).ThenBy(x => x.ServiceId).ToList();

    private List<CreditItem> BuildCreditItems(IEnumerable<StudentAccount> accounts) => accounts
        .GroupBy(x => new { x.SemesterId, x.ServiceId, x.Categoryid })
        .Select(group =>
        {
            var first = group.First();
            return new CreditItem
            {
                SemesterId = group.Key.SemesterId, ServiceId = group.Key.ServiceId, CategoryId = group.Key.Categoryid,
                SemesterName = Localized(first.Semester.Name, first.Semester.NameAr), ServiceName = Localized(first.Service.Name, first.Service.NameAr),
                CategoryName = Localized(first.Category.Name, first.Category.NameAr), Available = Math.Round(group.Sum(x => x.Credit - x.Debit), 2)
            };
        }).Where(x => x.Available > Epsilon).OrderBy(x => x.SemesterId).ThenBy(x => x.CategoryName).ThenBy(x => x.ServiceName).ToList();

    private void PaymentMethodChanged(object? _)
    {
        validationMessage = null;
        if (IsCheque) return;
        bankName = null; chequeNo = null; chequeDate = null;
    }

    private void AllocationChanged(CreditItem item)
    {
        item.Error = null;
        if (item.RefundAmount < 0) item.RefundAmount = 0;
        if (item.RefundAmount > item.Available + Epsilon)
        {
            item.RefundAmount = item.Available;
            item.Error = "لا يمكن استرداد مبلغ أكبر من الرصيد الدائن.";
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
            var freshCredits = BuildCreditItems(freshAccounts).ToDictionary(x => (x.SemesterId, x.ServiceId, x.CategoryId));
            var selectedItems = creditItems.Where(x => x.RefundAmount > Epsilon).ToList();
            foreach (var item in selectedItems)
            {
                if (!freshCredits.TryGetValue((item.SemesterId, item.ServiceId, item.CategoryId), out var fresh) || item.RefundAmount > fresh.Available + Epsilon)
                {
                    validationMessage = $"تغير الرصيد الدائن لخدمة {item.ServiceName}. تم تحديث المبالغ، يرجى المراجعة ثم الحفظ مرة أخرى.";
                    creditItems = BuildCreditItems(freshAccounts);
                    return;
                }
            }

            var userId = await _userService.GetUserIdAsync();
            var receiptNumber = await _uow.GetNextRefundReceiptNumberAsync(school!.Id);
            var now = DateTime.UtcNow.GetKsaDateTime();
            var receipt = new RefundReceiptEntity
            {
                Id = Guid.NewGuid(), SN = receiptNumber, ContractId = selectedStudent.ContractId, Date = receiptDate.Date,
                PaymentMethodId = paymentMethodId, BankName = IsCheque ? bankName?.Trim() : null,
                ChequeNo = IsCheque ? chequeNo?.Trim() : null, ChequeDate = IsCheque ? chequeDate?.Date : null,
                CreatedBy = userId, CreatedOn = now
            };
            await _uow.RefundReceipts.AddAsync(receipt);

            foreach (var item in selectedItems)
            {
                var amount = Math.Round(item.RefundAmount, 2);
                await _uow.RefundReceiptDetails.AddAsync(new RefundReceiptDetail
                {
                    Id = Guid.NewGuid(), ReceiptId = receipt.Id, ServiceId = item.ServiceId, CategoryId = item.CategoryId,
                    SemesterId = item.SemesterId, Amount = amount, CreatedBy = userId, CreatedOn = now
                });
                await _uow.StudentAccounts.AddAsync(new StudentAccount
                {
                    ContractId = selectedStudent.ContractId,
                    ServiceId = item.ServiceId,
                    Categoryid = item.CategoryId,
                    SemesterId = item.SemesterId,
                    TransactionTypeId = RefundTransactionTypeId,
                    RefNo = receipt.SN.ToString(CultureInfo.InvariantCulture),
                    Debit = amount,
                    Credit = 0,
                    Percentage = 0,
                    Date = receipt.Date,
                    Comments = $"سند صرف رقم {receipt.SN}",
                    CreatedBy = userId,
                    CreatedOn = now
                });
            }

            if (!await _uow.SaveAsync())
            {
                validationMessage = "تعذر حفظ سند الاسترداد، يرجى المحاولة مرة أخرى.";
                return;
            }

            var method = paymentMethods.First(x => x.Id == paymentMethodId);
            savedRefundId = receipt.Id;
            savedReceipt = new SavedRefund(receipt.SN, receipt.Date, method.DisplayName, method.IsCheque, receipt.BankName, receipt.ChequeNo, receipt.ChequeDate,
                selectedStudent.Name, selectedItems.Sum(x => x.RefundAmount), selectedItems.Select(x => new SavedRefundItem(x.SemesterName, x.ServiceName, x.CategoryName, x.RefundAmount)).ToList());
            _toastService.Notify(NotificationSeverity.Success, _localizer["Save"], "تم حفظ سند الاسترداد بنجاح.");
        }
        catch (Exception ex)
        {
            validationMessage = ex.Message;
            _toastService.Notify(NotificationSeverity.Error, _localizer["Error"], ex.Message);
        }
        finally { await _uow.DiscardChangesAsync(); isSaving = false; }
    }

    private string? ValidateForm()
    {
        foreach (var item in creditItems)
        {
            item.Error = null;
            if (item.RefundAmount < -Epsilon || item.RefundAmount > item.Available + Epsilon)
            {
                item.Error = "المبلغ يتجاوز الرصيد الدائن.";
                return $"مبلغ الاسترداد لخدمة {item.ServiceName} يتجاوز الرصيد الدائن.";
            }
        }
        if (paymentMethods.All(x => x.Id != paymentMethodId)) return "يرجى اختيار طريقة الصرف.";
        if (receiptDate == default) return "يرجى تحديد تاريخ السند.";
        if (RefundTotal <= Epsilon) return "يرجى إدخال مبلغ للاسترداد.";
        if (IsCheque && string.IsNullOrWhiteSpace(bankName)) return "يرجى اختيار اسم البنك.";
        if (IsCheque && string.IsNullOrWhiteSpace(chequeNo)) return "يرجى إدخال رقم الشيك.";
        if (IsCheque && chequeDate is null) return "يرجى تحديد تاريخ الشيك.";
        return null;
    }

    private async Task PrintAsync() => await _js.InvokeVoidAsync("APP.printReport", $"Refund-{savedReceipt?.Number}", "portrait");

    private async Task StartAnotherRefundAsync()
    {
        savedRefundId = null;
        savedReceipt = null; receiptDate = DateTime.Today; chequeDate = null; chequeNo = null; bankName = null; validationMessage = null;
        await LoadAsync();
    }

    private string Localized(string? english, string? arabic) => IsArabic && !string.IsNullOrWhiteSpace(arabic) ? arabic : english ?? arabic ?? string.Empty;
    private static bool IsChequeName(string? english, string? arabic) => $"{english} {arabic}".Contains("شيك", StringComparison.OrdinalIgnoreCase) || $"{english} {arabic}".Contains("cheque", StringComparison.OrdinalIgnoreCase) || $"{english} {arabic}".Contains("check", StringComparison.OrdinalIgnoreCase);
    private static string Initials(string name) => string.Join(string.Empty, name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x => x[0]));

    private sealed record StudentCard(Guid Id, Guid ContractId, string Name, string Code, string LevelName, string ClassName, double Refundable);
    private sealed record PaymentMethodOption(byte Id, string DisplayName, bool IsCheque);
    private sealed record BankOption(string DisplayName);
    private sealed record SavedRefundItem(string Semester, string Service, string Category, double Amount);
    private sealed record SavedRefund(int Number, DateTime Date, string PaymentMethod, bool IsCheque, string? BankName, string? ChequeNo, DateTime? ChequeDate, string StudentName, double Total, List<SavedRefundItem> Items);

    private sealed class CreditItem
    {
        public byte SemesterId { get; init; }
        public int ServiceId { get; init; }
        public int CategoryId { get; init; }
        public string SemesterName { get; init; } = string.Empty;
        public string ServiceName { get; init; } = string.Empty;
        public string CategoryName { get; init; } = string.Empty;
        public double Available { get; init; }
        public double RefundAmount { get; set; }
        public string? Error { get; set; }
    }
}
