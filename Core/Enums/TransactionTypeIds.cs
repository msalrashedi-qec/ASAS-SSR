namespace Core.Enums;

public static class TransactionTypeIds
{
    public const int AddDues = 100;
    public const int MarketingDiscount = 200;
    public const int PromotionalDiscount = 201;
    public const int LateRegistrationDiscount = 300;
    public const int SiblingDiscount = 400;
    public const int EmployeeChildrenDiscount = 500;
    public const int FullPaymentDiscount = 600;
    public const int EarlyPaymentDiscount = 700;
    public const int SpecialDiscount = 701;
    public const int FinancialSettlement = 800;
    public const int FullPaymentSettlement = 810;
    public const int ServiceDiscount = 900;
    public const int DiscountWithdrawal = 1000;
    public const int DuesWithdrawal = 1100;
    public const int WithdrawalPenalty = 1200;
    public const int DuesPayment = 1300;
    public const int FeeRefund = 1400;

    public static readonly HashSet<int> DiscountIds =
    [
        MarketingDiscount,
        PromotionalDiscount,
        LateRegistrationDiscount,
        SiblingDiscount,
        EmployeeChildrenDiscount,
        FullPaymentDiscount,
        EarlyPaymentDiscount,
        SpecialDiscount,
        ServiceDiscount,
        DiscountWithdrawal
    ];
}
