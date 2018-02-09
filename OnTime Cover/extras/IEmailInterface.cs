namespace OnTime_Cover.extras
{
    public interface IEmailInterface
    {
        bool SendEmail(string to, string from, string @subject, string @body);
    }
}