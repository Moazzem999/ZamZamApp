namespace ZamZamApp
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnSayHelloClicked(object sender, EventArgs e)
        {
            string name = NameEntry.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageLabel.Text = "Please enter your name.";
                return;
            }

            MessageLabel.Text = $"Hello, {name}! Welcome to .NET MAUI.";
        }
    }
}
