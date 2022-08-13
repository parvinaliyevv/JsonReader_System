namespace JsonReader.Views;

public partial class MainView : Window
{
    public MainView()
    {
        InitializeComponent();

        DataContext = new MainViewModel();
    }

    private void AppClose_ButtonClicked(object sender, RoutedEventArgs e) => Close();

    private void DragWindow_MouseDown(object sender, MouseButtonEventArgs e) => DragMove();
}
