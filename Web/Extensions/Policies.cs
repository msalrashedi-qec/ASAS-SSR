using Microsoft.AspNetCore.Authorization;

namespace Web.Extensions
{
    public static class Policies
    {
        public static class SystemAdmin
        {
            public const string Manage = "SystemAdmin.Manage";
        }
        public static class Parents
        {
            public const string Create = "Permissions.Parent.Create";
            public const string Edit = "Permissions.Parent.Edit";
        }

        public static class Students
        {
            public const string Create = "Permissions.Student.Create";
            public const string Edit = "Permissions.Student.Edit";
            public const string Withdraw = "Permissions.Student.Withdraw";
        }

        public static class StudentDocuments
        {
            public const string Add = "Permissions.StudentDocument.Add";
            public const string View = "Permissions.StudentDocument.View";
            public const string Delete = "Permissions.StudentDocument.Delete";
        }

        public static class Subscriptions
        {
            public const string Add = "Permissions.Subscription.Add";
            public const string Remove = "Permissions.Subscription.Remove";
        }

        public static class StudentAssignmentInClassRooms
        {
            public const string Manage = "Permissions.StudentAssignmentInClassRoom.Manage";
        }
        public static class StudentAssignmentInBuses
        {
            public const string Manage = "Permissions.StudentAssignmentInBus.Manage";
        }

        public static class ParentReceipts
        {
            public const string View = "Permissions.ParentReceipt.View";
        }

        public static class DebitReceipts
        {
            public const string Create = "Permissions.DebitReceipt.Create";
            public const string Edit = "Permissions.DebitReceipt.Edit";
            public const string Cancel = "Permissions.DebitReceipt.Cancel";
            public const string SelectDate = "Permissions.DebitReceipt.SelectDate";
        }

        public static class RefundReceipts
        {
            public const string Create = "Permissions.RefundReceipt.Create";
            public const string Edit = "Permissions.RefundReceipt.Edit";
            public const string Cancel = "Permissions.RefundReceipt.Cancel";
            public const string SelectDate = "Permissions.RefundReceipt.SelectDate";
        }

        public static class Requests
        {
            public const string Submit = "Permissions.Request.Submit";
            public const string Approve = "Permissions.Request.Approve";

            public const string SubmitLegalCase = "Permissions.Request.SubmitLegalCase";
            public const string ViewLegalCases = "Permissions.Request.ViewLegalCases";
        }

        public static class SMS
        {
            public const string Send = "Permissions.SMS.Send";
        }

        public static class Services
        {
            public const string Manage = "Permissions.Service.Manage";
        }

        public static class StudentAccounts
        {
            public const string View = "Permissions.StudentAccount.View";
        }

        public static class StudentContracts
        {
            public const string View = "Permissions.StudentContract.View";
        }
        public static class Reports
        {
            public const string View = "Reports.View";
            public const string ViewDashboard = "Reports.ViewDashboard";
        }


       

        // CENTRALIZED LIST
        public static List<string> AllPermissions => new()
        {
            SystemAdmin.Manage,
            Parents.Create,
            Parents.Edit,

            Students.Create,
            Students.Edit,
            Students.Withdraw,

            StudentDocuments.Add,
            StudentDocuments.View,
            StudentDocuments.Delete,

            Subscriptions.Add,
            Subscriptions.Remove,
            StudentAssignmentInClassRooms.Manage,
            StudentAssignmentInBuses.Manage,

            ParentReceipts.View,
            DebitReceipts.Create,
            DebitReceipts.Edit,
            DebitReceipts.Cancel,
            RefundReceipts.Create,
            RefundReceipts.Edit,
            RefundReceipts.Cancel,
            DebitReceipts.SelectDate,
            RefundReceipts.SelectDate,

            Requests.Submit,
            Requests.Approve,
            Requests.SubmitLegalCase,
            Requests.ViewLegalCases,

            SMS.Send,
            Services.Manage,

            StudentAccounts.View,
            StudentContracts.View,

            Reports.View,
            Reports.ViewDashboard,
        };

        public static void AddPolicies(AuthorizationOptions options)
        {
            foreach (var permission in AllPermissions)
            {
                options.AddPolicy(permission, policy =>
                    policy.RequireClaim("Permission", permission));
            }
        }
    }
}
