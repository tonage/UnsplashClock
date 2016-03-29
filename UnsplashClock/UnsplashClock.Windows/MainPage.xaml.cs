﻿using System;
using Windows.Storage;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace UnsplashClock
{
    public sealed partial class MainPage : Page
    {
        private const string LastFileName = "last.jpg";

        public static bool IsLoadingBackground { get; set; }

        public MainPage()
        {
            InitializeComponent();

            SettingsHelper.LoadSettings();

            DateTimeTimerOnTick(null, null);
            var dateTimeTimer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 100) };
            dateTimeTimer.Tick += DateTimeTimerOnTick;
            dateTimeTimer.Start();

            SetLastSavedImage(LastFileName);

            BackgroundTimerOnTick(this, null);
            var backgroundTimer = new DispatcherTimer {Interval = new TimeSpan(0, 0, 1) };
            backgroundTimer.Tick += BackgroundTimerOnTick;
            backgroundTimer.Start();

            SettingsPane.GetForCurrentView().CommandsRequested += (sender, args) =>
            {
                var settingsFlyout = new SettingsFlyout1();
                var setting = new SettingsCommand("ClockSettings", "Settings", handler => settingsFlyout.Show());
                args.Request.ApplicationCommands.Add(setting);
            };

            SettingsPane.GetForCurrentView().CommandsRequested += (sender, args) =>
            {
                var settingsFlyout = new SettingsFlyout2();
                var setting = new SettingsCommand("WorldClock", "World Clock", handler => settingsFlyout.Show());
                args.Request.ApplicationCommands.Add(setting);
            };

            SettingsHelper.OnThemeChanged += theme => { ChangeImage(UriHelper.GetUri(theme, SettingsHelper.UpdateInterval), true); };
        }

        private async void SetLastSavedImage(string fileName)
        {
            var storageFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync(fileName);
            if (storageFile == null)
                return;

            ChangeImage(new Uri(storageFile.Path), false);
        }

        private void DateTimeTimerOnTick(object sender, object o)
        {
            TimeText.Text = DateTime.Now.ToString(SettingsHelper.LongTimeFormat ? "HH:mm" : "t");
            DateText.Text = DateTime.Now.ToString("D");

            if (SettingsHelper.WorldTime)
            {
                Clock1Panel.Visibility  = Clock2Panel.Visibility = Clock3Panel.Visibility = Visibility.Visible;

                Clock1Name.Text = SettingsHelper.Clock1Name;
                Clock1Text.Text = SettingsHelper.Clock1TimeZone.ConvertTime(new DateTimeOffset(DateTime.Now)).ToString(SettingsHelper.LongTimeFormat ? "HH:mm" : "t");
                Clock2Name.Text = SettingsHelper.Clock2Name;
                Clock2Text.Text = SettingsHelper.Clock2TimeZone.ConvertTime(new DateTimeOffset(DateTime.Now)).ToString(SettingsHelper.LongTimeFormat ? "HH:mm" : "t");
                Clock3Name.Text = SettingsHelper.Clock3Name;
                Clock3Text.Text = SettingsHelper.Clock3TimeZone.ConvertTime(new DateTimeOffset(DateTime.Now)).ToString(SettingsHelper.LongTimeFormat ? "HH:mm" : "t");
            }
            else
            {
                Clock1Panel.Visibility = Clock2Panel.Visibility = Clock3Panel.Visibility = Visibility.Collapsed;
            }
        }

        private void BackgroundTimerOnTick(object sender, object o)
        {
            var diff = DateTime.UtcNow - SettingsHelper.LastBackgroundChange;
            bool forceChange = false;

            switch (SettingsHelper.UpdateInterval)
            {
                case "minute":
                    if (diff.TotalMinutes < 1)
                        return;
                    break;

                case "hour":
                    if (diff.TotalHours < 1)
                        return;
                    if (diff.TotalHours > 3)
                    {
                        forceChange = true;
                        break;
                    }
                    break;

                case "day":
                    if (diff.TotalDays < 1)
                        return;
                    break;
            }

            if (IsLoadingBackground && !forceChange)
                return;

            ChangeImage(UriHelper.GetUri(SettingsHelper.Theme, SettingsHelper.UpdateInterval), true);
        }

        private async void ChangeImage(Uri uri, bool saveLastChangedTime)
        {
            IsLoadingBackground = true;
            ProgressRing.IsActive = true;
            ImageSource source;
            try
            {
                if(!uri.IsFile)
                    source = await Downloader.DownloadImage(uri, LastFileName);
                else
                    source = new BitmapImage(uri);
            }
            catch
            {
                return;
            }

            Staging.Source = source;

            ImageFadeOut.Completed += (s, e) =>
            {
                BackImage.Source = source;
                ImageFadeIn.Begin();
            };
            ImageFadeOut.Begin();

            ProgressRing.IsActive = false;
            if (!saveLastChangedTime)
            {
                IsLoadingBackground = false;
                return;
            }
            
            SettingsHelper.LastBackgroundChange = DateTime.UtcNow;
            IsLoadingBackground = false;
        }

        private void MainPage_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ChangeImage(UriHelper.GetUri(SettingsHelper.Theme, SettingsHelper.UpdateInterval), true);
        }
    }
}
