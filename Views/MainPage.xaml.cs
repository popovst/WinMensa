using System.Diagnostics;
using WinMensa.Core;

namespace WinMensa.Views
{
    public record MealContainer(Meal Meal, bool Expanded, string FormattedPrice);

    /// <summary>
    /// A simple page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public partial class MainPage : Page
    {
        private readonly Image _lightboxImage = new() { Stretch = Stretch.Uniform, MaxHeight = 700, MaxWidth = 900 };
        private ContentDialog? _lightbox;

        public MainPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            LinesList.ItemClick += OnLineClicked;
        }

        private void OnLineClicked(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Line line)
            {
                MealDetailsPlaceholderText.Visibility = Visibility.Collapsed;

                var meals = line.Meals.Select(m =>
                    new MealContainer(m, false, (m.Price.Student / 100.0).ToString("0.00") + "€")
                ).ToList();
                MealDetailsList.ItemsSource = meals;
            }
        }

        private async void OnMealImageTapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: string url }) return;

            _lightbox ??= new ContentDialog
            {
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Color.FromArgb(220, 0, 0, 0)),
                CloseButtonText = "Close",
                Content = _lightboxImage
            };

            _lightboxImage.Source = new BitmapImage(new Uri(url));
            _lightbox.XamlRoot = XamlRoot;
            await _lightbox.ShowAsync();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = await Query.GetCanteenData();
                var lines = data?.Lines?.Where(l => l.Meals.Length > 0).ToArray() ?? [];
                LinesList.ItemsSource = lines ?? [];
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading data: {ex}");
            }
        }
    }
}
