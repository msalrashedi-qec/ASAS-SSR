namespace Shared.Enums;

public enum DiscountApplicationTiming : byte
{
    OnSubscription = 10,
    OnPayment = 20,
    OnWithdrawal = 30,
    OnRequestApproval = 40,
}
