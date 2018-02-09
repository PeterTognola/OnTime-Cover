namespace OnTime_Cover.extras
{
    public interface IAlertInterface
    {
        void ErrorDialog(string errorMessage);
        void SuccessDialog(string message);
    }
}