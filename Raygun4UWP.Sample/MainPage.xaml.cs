﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Raygun4UWP.Sample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        int count = 0;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await RaygunClient.Current.SendAsync(new Exception("Test exception"));
        }

        private void ButtonBreadcrumb_Click(object sender, RoutedEventArgs e)
        {
            var breadcrumb = new RaygunBreadcrumb
            {
                Message = "Test breadcrumb: " + count,
                Category = "Test category",
                CustomData = new Dictionary<string, object>
                {
                    { "Key", "Value" }
                }
            };

            count++;

            RaygunClient.Current.RecordBreadcrumb(breadcrumb);
        }
    }
}
