namespace Web.Services;

public sealed class StudentWelcomeMessageService(ISmsSender smsSender)
{
    public async Task<bool> SendAsync(IUnitOfWork uow, Student student)
    {
        try
        {
            var school = await uow.Schools.FindAsync(x => x.Id == student.SchoolId);
            var parent = await uow.Parents.FindAsync(x => x.Id == student.ParentId);
            if (school is null || string.IsNullOrWhiteSpace(parent?.Mobile))
                return false;

            var schoolName = string.IsNullOrWhiteSpace(school.NameAr) ? school.Name : school.NameAr;
            var child = student.GenderId == 2 ? "بإبنتكم" : "بإبنكم";
            var message = $"نشكركم لثقتكم بنا, ونرحب {child} {student.Name} في {schoolName}";
            var result = await smsSender.SendAsync(new SmsSendRequest(
                school.SmsSender, school.SmsUserName, school.SmsPassword, [parent.Mobile], message));
            return result.IsSuccess;
        }
        catch (Exception)
        {
            // Notification failures must not turn a completed registration into a failed save.
            return false;
        }
    }
}
