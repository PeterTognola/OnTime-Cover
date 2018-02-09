using OnTime_Cover.extras;

namespace OnTime_Cover
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel(new AlertService(), new EmailService());
        }
    }
}
