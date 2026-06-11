using System.Diagnostics;
using Windows.System;
using WinMensa.Core;

namespace WinMensa.Views
{
    public record MealContainer(Meal Meal, bool Expanded, string FormattedPrice)
    {
        private bool HasRatings => Meal.Ratings is { } r && r.RatingsCount > 0;

        public string FormattedRatingCaption => Meal.Ratings is { } r
            ? (HasRatings ? Strings.Format("RatingCaption", r.AverageRating, r.RatingsCount) : Strings.Get("NoRatings"))
            : string.Empty;

        public string HeaderFormattedRating => Meal.Ratings is { } r
            ? (HasRatings ? Strings.Format("StarRating", r.AverageRating) : Strings.Get("NewMeal"))
            : string.Empty;

        public int AverageRating => Meal.Ratings is { } r
            ? (HasRatings ? (int)Math.Round(r.AverageRating) : -1)
            : 0;

        public string MealTag => $"[{Meal.MealType}]";

        public Visibility ImageControlVisibility =>
            Meal.Images is { Length: > 0 } ? Visibility.Visible : Visibility.Collapsed;
    }

    public partial class MainPage : Page
    {
        private const string CanteenSettingsKey = "SelectedCanteenId";
        private bool _canteenSelectorReady = false;
        private static readonly string SettingsDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinMensa");

        private static string? ReadSetting(string key)
        {
            var path = Path.Combine(SettingsDir, key);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        private static void WriteSetting(string key, string value)
        {
            Directory.CreateDirectory(SettingsDir);
            File.WriteAllText(Path.Combine(SettingsDir, key), value);
        }
        private List<MealImage> _lightboxImages = [];
        private int _lightboxIndex;

        public MainPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            LinesList.ItemClick += OnLineClicked;
            KeyDown += OnPageKeyDown;
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

        private void OnMealImageTapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: MealImage tapped }) return;

            DependencyObject? node = sender as DependencyObject;
            FlipView? flipView = null;
            while (node != null)
            {
                if (node is FlipView fv) { flipView = fv; break; }
                node = VisualTreeHelper.GetParent(node);
            }

            var images = (flipView?.ItemsSource as IEnumerable<MealImage>)?.ToList() ?? [tapped];
            var index = images.FindIndex(img => img.Id == tapped.Id);
            OpenLightbox(images, Math.Max(0, index));
        }

        private void OpenLightbox(List<MealImage> images, int index)
        {
            _lightboxImages = images;
            _lightboxIndex = index;
            UpdateLightboxImage();
            LightboxOverlay.Visibility = Visibility.Visible;
            FadeOverlay(to: 1);
            LightboxCloseButton.Focus(FocusState.Programmatic);
        }

        private void CloseLightbox() => FadeOverlay(to: 0, collapse: true);

        private void UpdateLightboxImage()
        {
            LightboxImage.Source = new BitmapImage(new Uri(_lightboxImages[_lightboxIndex].Url));
            bool many = _lightboxImages.Count > 1;
            LightboxCounter.Text = many ? $"{_lightboxIndex + 1} / {_lightboxImages.Count}" : string.Empty;
            LightboxPrevButton.Visibility = (many && _lightboxIndex > 0) ? Visibility.Visible : Visibility.Collapsed;
            LightboxNextButton.Visibility = (many && _lightboxIndex < _lightboxImages.Count - 1) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void FadeOverlay(double to, bool collapse = false)
        {
            var anim = new DoubleAnimation
            {
                To = to,
                Duration = new Duration(TimeSpan.FromMilliseconds(160)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTarget(anim, LightboxOverlay);
            Storyboard.SetTargetProperty(anim, "Opacity");
            var sb = new Storyboard();
            sb.Children.Add(anim);
            if (collapse)
                sb.Completed += (_, _) => LightboxOverlay.Visibility = Visibility.Collapsed;
            sb.Begin();
        }

        private void OnLightboxBackdropTapped(object sender, TappedRoutedEventArgs e) => CloseLightbox();
        private void OnLightboxContentTapped(object sender, TappedRoutedEventArgs e) => e.Handled = true;
        private void OnLightboxClose(object sender, RoutedEventArgs e) => CloseLightbox();

        private void OnLightboxPrev(object sender, RoutedEventArgs e)
        {
            if (_lightboxIndex > 0) { _lightboxIndex--; UpdateLightboxImage(); }
        }

        private void OnLightboxNext(object sender, RoutedEventArgs e)
        {
            if (_lightboxIndex < _lightboxImages.Count - 1) { _lightboxIndex++; UpdateLightboxImage(); }
        }

        private void OnPageKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (LightboxOverlay.Visibility != Visibility.Visible) return;
            switch (e.Key)
            {
                case VirtualKey.Escape: CloseLightbox(); e.Handled = true; break;
                case VirtualKey.Left: OnLightboxPrev(sender, e); e.Handled = true; break;
                case VirtualKey.Right: OnLightboxNext(sender, e); e.Handled = true; break;
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var canteens = await Query.GetCanteens();
                if (canteens.Length == 0) return;

                _canteenSelectorReady = false;
                CanteenSelector.ItemsSource = canteens;

                var savedId = ReadSetting(CanteenSettingsKey);
                var toSelect = canteens.FirstOrDefault(c => c.Id == savedId) ?? canteens[0];
                CanteenSelector.SelectedItem = toSelect;
                _canteenSelectorReady = true;

                await LoadLinesAsync(toSelect.Id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading canteens: {ex}");

                var content = new StackPanel { Spacing = 12, MaxWidth = 380 };
                content.Children.Add(new TextBlock
                {
                    Text = Strings.Get("ErrorCanteensBody"),
                    TextWrapping = TextWrapping.Wrap,
                });
                content.Children.Add(new TextBlock
                {
                    Text = Strings.Get("ErrorCanteensHint"),
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = 0.7,
                });

                await new ContentDialog
                {
                    Title = Strings.Get("ErrorCanteensTitle"),
                    Content = content,
                    CloseButtonText = Strings.Get("DialogOK"),
                    XamlRoot = XamlRoot,
                }.ShowAsync();
            }
        }

        private async Task LoadLinesAsync(string canteenId)
        {
            try
            {
                MealDetailsList.ItemsSource = null;
                MealDetailsPlaceholderText.Visibility = Visibility.Visible;

                var data = await Query.GetCanteenData(canteenId);
                var lines = data?.Lines?.Where(l => l.Meals.Length > 0).ToArray() ?? [];
                LinesList.ItemsSource = lines;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading canteen data: {ex}");
            }
        }

        private async void OnAboutClicked(object sender, RoutedEventArgs e)
        {
            var content = new StackPanel { Spacing = 12, MaxWidth = 380 };
            content.Children.Add(new TextBlock
            {
                Text = Strings.Get("AboutApiText"),
                TextWrapping = TextWrapping.Wrap,
            });
            content.Children.Add(new HyperlinkButton
            {
                Content = "github.com/kronos-et-al/MensaApp",
                NavigateUri = new Uri("https://github.com/kronos-et-al/MensaApp"),
                Padding = new Thickness(0),
            });
            content.Children.Add(new TextBlock
            {
                Text = Strings.Get("AboutDisclaimer"),
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.7,
            });

            await new ContentDialog
            {
                Title = Strings.Get("AboutTitle"),
                Content = content,
                CloseButtonText = Strings.Get("AboutClose"),
                XamlRoot = XamlRoot,
            }.ShowAsync();
        }

        private async void OnCanteenSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_canteenSelectorReady) return;
            if (CanteenSelector.SelectedItem is not Canteen selected) return;

            WriteSetting(CanteenSettingsKey, selected.Id);
            await LoadLinesAsync(selected.Id);
        }
    }
}
