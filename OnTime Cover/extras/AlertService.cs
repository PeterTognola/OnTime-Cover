using System.Windows;

namespace OnTime_Cover.extras
{
    class AlertService : IAlertInterface
    {
        public void ErrorDialog(string errorMessage)
        {
            //todo Add logging...
            MessageBox.Show(errorMessage);
        }

        public void SuccessDialog(string message)
        {
            MessageBox.Show(message);
        }
    }
}
