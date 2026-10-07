using Core.Entities.Discounts;
using Shared.Enums;

namespace Web.Services
{
    public partial class StudentService
    {
        public const int StudyServiceId = 20;

        private const int PreviousBalanceCategoryId = 10;

        private const byte SemesterStartDateDependencyId = 10;
        private const byte RegistrationDateDependencyId = 20;
        private const byte SiblingCountDependencyId = 30;
        private const byte ClassNumberDependencyId = 40;
        private const byte ManualDependencyId = 50;
       
        public async Task<int> GenerateStudentNumber(IUnitOfWork uow, Guid schoolId)
        {
            var students = await uow.Students.FindAllAsync(x => x.SchoolId == schoolId, x => x.CreatedOn, OrderBy.Descending);
            return students.Any() ? students.First().SN + 1 : 1;
        }

        public async Task<IEnumerable<StudentAccount>> ComputeStudyFee(IUnitOfWork uow, StudentDto model, YearDto registeredForYear, YearDto currentYear, IEnumerable<DiscountDetail> discounts, IEnumerable<SchoolSemester> semesterDetails, string userId)
        {
            var contract = await CreateContract(uow, model, registeredForYear, currentYear, discounts, userId);
            return contract.StudentAccounts;
        }

        public async Task<StudentContract> CreateContract(IUnitOfWork uow, StudentDto model, YearDto registeredForYear, YearDto currentYear, IEnumerable<DiscountDetail> discounts, string userId)
        {
            if (!model.RegistrationDate.HasValue)
                throw new InvalidOperationException("A registration date is required to create the student contract.");

            if (!await uow.SchoolClasses.Existing(x => x.Id == model.ClassId && x.Level.SchoolId == model.SchoolId))
                throw new InvalidOperationException("Please select a valid class for the student's school.");

            var registrationDate = model.RegistrationDate.Value.Date;
            var schoolSemesters = (await uow.SchoolSemesters.FindAllAsync( x => x.SchoolId == model.SchoolId && x.YearId == registeredForYear.Id))
                .GroupBy(x => x.SemesterId)
                .ToDictionary(x => x.Key, x => x.First());
            var selectedSemesters = model.Semesters.Distinct().OrderBy(x => x).ToArray();

            var contract = new StudentContract
            {
                Id = Guid.NewGuid(),
                SN = "-",
                YearId = currentYear.Id,
                ClassId = model.ClassId,
                StartDate = registrationDate,
                EndDate = schoolSemesters.TryGetValue(0, out var fullYear)
                    ? fullYear.EndDate
                    : currentYear.Semesters.FirstOrDefault(x => x.SemesterId == 0)?.EndDate ?? registrationDate,
                RegisteredForYearId = model.RegisteredForYearId,
                StatusId = model.RegisteredForYearId == currentYear.Id ? (byte)10 : (byte)5,
                CreatedBy = userId,
                CreatedOn = DateTime.UtcNow.GetKsaDateTime()
            };

            var services = (await uow.ServicePrices.FindAllAsync(x => x.YearId == registeredForYear.Id && x.ClassId == model.ClassId && x.ServiceId == StudyServiceId,
                new[] { "Service" }))
                .Where(x => selectedSemesters.Contains(x.SemesterId))
                .GroupBy(x => x.SemesterId)
                .ToDictionary(x => x.Key, x => x.First());

            var applicableDiscounts = await GetApplicableDiscounts(uow, model, selectedSemesters, discounts, schoolSemesters, registrationDate);

            foreach (var semesterId in selectedSemesters)
            {
                if (!services.TryGetValue(semesterId, out var servicePrice))
                    continue;

                AddFee(contract, servicePrice, semesterId, userId, contract.CreatedOn);
                var tax = await StudentSubscriptionTax.CreateEntryAsync(uow, model.SchoolId, model.NationalityId, contract.StudentAccounts.Last());
                if (tax is not null) contract.StudentAccounts.Add(tax);
                foreach (var discount in applicableDiscounts.Where(x => x.SemesterId == semesterId))
                {
                    var netFee = StudentAccountDiscountCalculator.CalculateNetServiceFee(contract.StudentAccounts, contract.Id, servicePrice.ServiceId, semesterId);
                    if (netFee <= 0)
                        break;

                    var discountAmount = StudentAccountDiscountCalculator.CalculateDiscountAmount(netFee, (decimal)discount.Detail.Amount, (decimal)discount.Detail.AdditionalFixedAmount);
                    if (discountAmount <= 0)
                        continue;

                    var discountAccount = new StudentAccount
                    {
                        ContractId = contract.Id,
                        ServiceId = servicePrice.ServiceId,
                        SemesterId = semesterId,
                        Categoryid = servicePrice.Service.CategoryId,
                        TransactionTypeId = discount.Detail.Discount.TransactionTypeId,
                        RefNo = StudentDiscountWithdrawal.DiscountReference(discount.Detail.Discount.Id),
                        Debit = 0,
                        Credit = (double)discountAmount,
                        Percentage = (decimal)(discount.Detail.Amount / 100d),
                        Date = contract.StartDate,
                        Comments = $"{discount.Detail.Discount.Name} {discount.Detail.Amount}%",
                        CreatedBy = userId,
                        CreatedOn = contract.CreatedOn
                    };
                    var taxDiscount = StudentSubscriptionTax.CreateDiscountEntry(discountAccount, contract.StudentAccounts);
                    contract.StudentAccounts.Add(discountAccount);
                    if (taxDiscount is not null) contract.StudentAccounts.Add(taxDiscount);

                    if (discount.Detail.Discount.IsShownInContract)
                    {
                        contract.Details.Add(new StudentContractDetail
                        {
                            ServiceId = servicePrice.ServiceId,
                            SemesterId = semesterId,
                            Amount = -(double)discountAmount,
                            Comments = discount.Detail.Comments ?? discount.Detail.Discount.Name
                        });
                    }
                }
            }

            foreach (var account in contract.StudentAccounts)
                account.ContractId = contract.Id;
            return contract;
        }

        public async Task CarryForwardPreviousBalance(IUnitOfWork uow, StudentContract previousContract, StudentContract newContract, string userId)
        {
            var previousAccounts = (await uow.StudentAccounts.FindAllAsync(x => x.ContractId == previousContract.Id)).ToList();
            var balance = Math.Round(previousAccounts.Sum(x => x.Debit - x.Credit), 2);

            if (balance == 0)
                return;

            // A ledger row requires a service and semester. Prefer the new study
            // contract's first fee, and fall back to the previous contract when the
            // rejoining contract has no priced semester.
            var reference = newContract.StudentAccounts.OrderBy(x => x.SemesterId).ThenBy(x => x.ServiceId).FirstOrDefault()
                ?? previousAccounts.OrderByDescending(x => x.Date).ThenByDescending(x => x.CreatedOn).First();

            newContract.StudentAccounts.Add(new StudentAccount
            {
                ContractId = newContract.Id,
                ServiceId = reference.ServiceId,
                SemesterId = reference.SemesterId,
                Categoryid = PreviousBalanceCategoryId,
                TransactionTypeId = StudentAccountDiscountCalculator.ServiceFeeTransactionTypeId,
                RefNo = previousContract.SN,
                Debit = balance > 0 ? balance : 0,
                Credit = balance < 0 ? -balance : 0,
                Percentage = 0,
                Date = newContract.StartDate,
                Comments = "رصيد سابق",
                CreatedBy = userId,
                CreatedOn = newContract.CreatedOn
            });
        }

        public async Task RecalculateContractFinancials(IUnitOfWork uow, Guid contractId, StudentDto model, YearDto registeredForYear, YearDto currentYear, IEnumerable<DiscountDetail> discounts, string userId)
        {
            var contract = await uow.StudentContracts.FindAsync(x => x.Id == contractId);
            if (contract is null)
                throw new InvalidOperationException("The student's current contract could not be found.");

            var recalculated = await CreateContract(uow, model, registeredForYear, currentYear, discounts, userId);
            var existingDetails = await uow.StudentContractDetails.FindAllAsync(x => x.ContractId == contractId && x.ServiceId == StudyServiceId);

            var allAccounts = (await uow.StudentAccounts.FindAllAsync(x => x.ContractId == contractId)).ToList();
            var generatedAccounts = allAccounts.Where(x => x.ServiceId == StudyServiceId
                && x.TransactionTypeId < TransactionTypeIds.ServiceDiscount).ToList();
            var siblingReferences = (await uow.Discounts.FindAllAsync(x => x.TransactionTypeId == TransactionTypeIds.SiblingDiscount))
                .Select(x => $"discount-withdrawal:{x.Id}").ToHashSet();
            generatedAccounts.AddRange(allAccounts.Where(x => x.ServiceId == StudyServiceId
                && x.TransactionTypeId == TransactionTypeIds.DiscountWithdrawal && siblingReferences.Contains(x.RefNo ?? "")));

            recalculated.Id = contractId;
            recalculated.CreatedOn = contract.CreatedOn;
            foreach (var entry in recalculated.StudentAccounts)
                entry.ContractId = contractId;
            var student = await uow.Students.FindAsync(x => x.Id == model.Id);
            student.ParentId = model.ParentId;
            student.SchoolId = model.SchoolId;
            var siblingChanges = await BuildSiblingDiscountChangesAsync(uow, student, recalculated, DateTime.Today, userId, pendingAccounts: allAccounts.Except(generatedAccounts).Concat(recalculated.StudentAccounts));

            uow.StudentContractDetails.DeleteRange(existingDetails);
            uow.StudentAccounts.DeleteRange(generatedAccounts);

            contract.YearId = recalculated.YearId;
            contract.RegisteredForYearId = recalculated.RegisteredForYearId;
            contract.ClassId = recalculated.ClassId;
            contract.StartDate = recalculated.StartDate;
            contract.EndDate = recalculated.EndDate;
            contract.StatusId = recalculated.StatusId;
            contract.UpdatedBy = userId;
            contract.UpdatedOn = DateTime.UtcNow.GetKsaDateTime();
            await uow.StudentContracts.Update(contract);

            foreach (var detail in recalculated.Details)
            {
                detail.ContractId = contractId;
                // The recalculated contract is only a calculation container. Never let EF
                // discover it through a child navigation and insert it as a new contract.
                detail.Contract = null!;
            }

            foreach (var account in recalculated.StudentAccounts)
            {
                account.ContractId = contractId;
                account.Contract = null!;
            }

            if (recalculated.Details.Count > 0)
                await uow.StudentContractDetails.AddRangeAsync(recalculated.Details);

            if (recalculated.StudentAccounts.Count > 0)
                await uow.StudentAccounts.AddRangeAsync(recalculated.StudentAccounts);
            await StageSiblingDiscountChangesAsync(uow, siblingChanges);
        }

        private static void AddFee( StudentContract contract, ServicePrice servicePrice, byte semesterId, string userId, DateTime now)
        {
            contract.Details.Add(new StudentContractDetail
            {
                ServiceId = servicePrice.ServiceId,
                SemesterId = semesterId,
                Amount = servicePrice.Price,
                Comments = servicePrice.Service.NameAr
            });

            contract.StudentAccounts.Add(new StudentAccount
            {
                ContractId = contract.Id,
                ServiceId = servicePrice.ServiceId,
                SemesterId = semesterId,
                Categoryid = servicePrice.Service.CategoryId,
                TransactionTypeId = TransactionTypeIds.AddDues,
                Debit = servicePrice.Price,
                Credit = 0,
                Percentage = 0,
                Comments = "إضافة مستحقات",
                Date = now,
                CreatedBy = userId,
                CreatedOn = now
            });
        }

        public async Task<IReadOnlyCollection<DiscountDetail>> GetApplicableSubscriptionDiscounts(IUnitOfWork uow, Student student, StudentContract contract, int serviceCategoryId, byte semesterId, DateTime subscriptionDate)
        {
            var discountDetails = (await uow.DiscountDetails.FindAllAsync(x => x.SchoolId == student.SchoolId && x.YearId == contract.RegisteredForYearId
                         && (x.SemesterId == semesterId || x.SemesterId == 0) && x.Discount.ServiceCategoryId == serviceCategoryId && x.Discount.ApplicationTiming == DiscountApplicationTiming.OnSubscription
                         && x.Discount.DependencyId != ManualDependencyId,
                    new[] { "Discount" }, x => x.Discount.SN)).ToArray();

            if (discountDetails.Length == 0)
                return Array.Empty<DiscountDetail>();

            var siblingEligible = await IsSiblingDiscountEligibleAsync(uow, new StudentDto
            {
                Id = student.Id, ParentId = student.ParentId, SchoolId = student.SchoolId,
                ClassId = contract.ClassId, RegisteredForYearId = contract.RegisteredForYearId
            });

            var siblingOrder = await uow.Students.CountAsync(x => x.Id != student.Id
                && x.ParentId == student.ParentId
                && x.SchoolId == student.SchoolId
                && x.CurrentContract.RegisteredForYearId == contract.RegisteredForYearId
                && x.CurrentContract.StatusId >= 10
                && x.CurrentContract.StatusId < 30) + 1;
            // Day-based subscription discounts are measured from the beginning of
            // the actual school semester containing the subscription date. The
            // priced semester can differ from that semester when a service is
            // purchased early or late.
            var semester = (await uow.SchoolSemesters.FindAllAsync(x =>
                    x.SchoolId == student.SchoolId
                    && x.YearId == contract.RegisteredForYearId
                    && x.SemesterId > 0
                    && x.StartDate <= subscriptionDate.Date
                    && x.EndDate >= subscriptionDate.Date))
                .OrderByDescending(x => x.StartDate)
                .FirstOrDefault();

            return discountDetails
                .Where(x => x.Discount.TransactionTypeId != TransactionTypeIds.SiblingDiscount || siblingEligible)
                .GroupBy(x => x.DiscountId)
                .Select(group => group
                    .OrderByDescending(x => x.SemesterId == semesterId)
                    .FirstOrDefault(x => IsRegistrationStateInRange(x, semester, subscriptionDate.Date, siblingOrder, contract.ClassId)))
                .Where(x => x is not null)
                .Select(x => x!)
                .OrderBy(x => x.Discount.SN)
                .ThenBy(x => x.DiscountId)
                .ToArray();
        }

        private async Task<IReadOnlyCollection<ApplicableDiscount>> GetApplicableDiscounts(IUnitOfWork uow, StudentDto model, IReadOnlyCollection<byte> selectedSemesters, IEnumerable<DiscountDetail> discounts, IReadOnlyDictionary<byte, SchoolSemester> schoolSemesters, DateTime registrationDate)
        {
            var discountDetails = discounts.Where(x => x.Discount is not null && x.Discount.ApplicationTiming == DiscountApplicationTiming.OnSubscription && x.Discount.DependencyId != ManualDependencyId)
                .OrderBy(x => x.Discount.SN).ThenBy(x => x.DiscountId)
                .ToArray();

            if (discountDetails.Length == 0)
                return Array.Empty<ApplicableDiscount>();

            // Count each other actively enrolled student once, then include the student
            // whose contract is currently being calculated. This also works for rejoining.
            var siblingOrder = await uow.Students.CountAsync(x =>
                x.Id != model.Id
                && x.ParentId == model.ParentId
                && x.SchoolId == model.SchoolId
                && x.CurrentContract.RegisteredForYearId == model.RegisteredForYearId
                && x.CurrentContract.StatusId >= 10
                && x.CurrentContract.StatusId < 30) + 1;

            var result = new List<ApplicableDiscount>();

            var siblingEligible = await IsSiblingDiscountEligibleAsync(uow, model);

            foreach (var semesterId in selectedSemesters)
            {
                foreach (var discountGroup in discountDetails.GroupBy(x => x.DiscountId))
                {
                    if (discountGroup.First().Discount.TransactionTypeId == TransactionTypeIds.SiblingDiscount && !siblingEligible)
                        continue;
                    var matchingDetail = discountGroup
                        .Where(x => x.SemesterId == semesterId || x.SemesterId == 0)
                        .OrderByDescending(x => x.SemesterId == semesterId)
                        .FirstOrDefault(x => IsRegistrationStateInRange(x, schoolSemesters.TryGetValue(semesterId, out var semester) ? semester : null, registrationDate, siblingOrder, model.ClassId));

                    if (matchingDetail is not null)
                        result.Add(new ApplicableDiscount(semesterId, matchingDetail));
                }
            }

            return result;
        }

        public static bool IsRegistrationStateInRange(DiscountDetail detail, SchoolSemester? semester, DateTime registrationDate, int siblingOrder, int classSequence)
        {
            return detail.Discount.DependencyId switch
            {
                SemesterStartDateDependencyId => semester is not null && IsNumberInRange(
                    (registrationDate.Date - semester.StartDate.Date).Days,
                    detail.FromRange,
                    detail.ToRange),
                RegistrationDateDependencyId => IsDateInRange(registrationDate, detail.FromRange, detail.ToRange),
                SiblingCountDependencyId => IsNumberInRange(siblingOrder, detail.FromRange, detail.ToRange),
                ClassNumberDependencyId => IsNumberInRange(classSequence, detail.FromRange, detail.ToRange),
                ManualDependencyId => false,
                0 => true,
                _ => false
            };
        }

        private static bool IsNumberInRange(double value, string? fromText, string? toText)
        {
            var hasFrom = TryParseNumber(fromText, out var from);
            var hasTo = TryParseNumber(toText, out var to);
            return (hasFrom || hasTo) && (!hasFrom || value >= from) && (!hasTo || value <= to);
        }

        private static bool TryParseNumber(string? value, out double result)
        {
            return double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result)
                || double.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result);
        }

        private static bool IsDateInRange(DateTime value, string? fromText, string? toText)
        {
            var hasFrom = TryParseDate(fromText, out var from);
            var hasTo = TryParseDate(toText, out var to);
            return (hasFrom || hasTo) && (!hasFrom || value.Date >= from.Date) && (!hasTo || value.Date <= to.Date);
        }

        private static bool TryParseDate(string? value, out DateTime result)
        {
            var cultures = new[]
            {
                CultureInfo.InvariantCulture,
                CultureInfo.CurrentCulture,
                CultureInfo.GetCultureInfo("en-GB"),
                CultureInfo.GetCultureInfo("ar-SA")
            };

            foreach (var culture in cultures)
            {
                if (DateTime.TryParse(value, culture, DateTimeStyles.AllowWhiteSpaces, out result))
                    return true;
            }

            result = default;
            return false;
        }

        private sealed record ApplicableDiscount(byte SemesterId, DiscountDetail Detail);
    }
}
