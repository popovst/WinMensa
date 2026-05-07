using System.Diagnostics;
using Windows.System;
using WinMensa.Core;

namespace WinMensa.Views
{
    public record MealContainer(Meal Meal, bool Expanded, string FormattedPrice);

    public partial class MainPage : Page
    {
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
