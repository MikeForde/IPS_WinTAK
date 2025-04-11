using System;
using System.IO;
using System.Windows;
using MapEngine.Interop.Util;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace ipswintakplugin
{
    public partial class WebViewWindow : Window
    {
        public WebViewWindow(string data, bool isHtmlContent = false)
        {
            InitializeComponent();
            InitializeWebView(data, isHtmlContent);
        }

        private async void InitializeWebView(string data, bool isHtmlContent)
        {
            try
            {
                string userDataFolder = Path.Combine(Path.GetTempPath(), "MyPlugin_WebView2UserData");
                CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await WebViewControl.EnsureCoreWebView2Async(env);

                // Subscribe to the WebMessageReceived event to receive messages from the webpage.
                //WebViewControl.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                if (isHtmlContent)
                {
                    // Directly load the HTML string.
                    WebViewControl.CoreWebView2.NavigateToString(data);
                }
                else
                {
                    // Treat the data as a URL.
                    WebViewControl.CoreWebView2.Navigate(data);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error initializing WebView2: " + ex.Message);
            }
        }

        ///// <summary>
        ///// Event handler to receive messages from the webpage.
        ///// </summary>
        //private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        //{
        //    try
        //    {
        //        // Retrieve the message from the webpage.
        //        string message = e.TryGetWebMessageAsString();
        //        // Log and display the received message. Customize as needed.
        //        Log.i("WebViewWindow", "Received message from webpage: " + message);
        //        MessageBox.Show("Received message: " + message);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error processing WebMessage: " + ex.Message);
        //    }
        //}
    }
}


//using System;
//using System.IO;
//using System.Windows;
//using MapEngine.Interop.Util;
//using Microsoft.Web.WebView2.Core;
//using Microsoft.Web.WebView2.Wpf;

//namespace ipswintakplugin
//{
//    public partial class WebViewWindow : Window
//    {
//        // If isHtmlContent is true, then 'data' is assumed to be full HTML; otherwise, it's a URL.
//        public WebViewWindow(string data, bool isHtmlContent = false)
//        {
//            InitializeComponent();
//            InitializeWebView(data, isHtmlContent);
//        }

//        private async void InitializeWebView(string data, bool isHtmlContent)
//        {
//            try
//            {
//                string userDataFolder = Path.Combine(Path.GetTempPath(), "MyPlugin_WebView2UserData");
//                CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
//                await WebViewControl.EnsureCoreWebView2Async(env);

//                // Subscribe to the WebMessageReceived event to receive messages from the webpage.
//                WebViewControl.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

//                if (isHtmlContent)
//                {
//                    // Directly load the HTML string.
//                    WebViewControl.CoreWebView2.NavigateToString(data);
//                }
//                else
//                {
//                    // Treat the data as a URL.
//                    WebViewControl.CoreWebView2.Navigate(data);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show("Error initializing WebView2: " + ex.Message);
//            }
//        }

//        /// <summary>
//        /// Event handler to receive messages from the webpage.
//        /// </summary>
//        private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
//        {
//            try
//            {
//                // Retrieve the message from the webpage.
//                string message = e.TryGetWebMessageAsString();
//                // Log and display the received message. Customize as needed.
//                Log.i("WebViewWindow", "Received message from webpage: " + message);
//                MessageBox.Show("Received message: " + message);
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show("Error processing WebMessage: " + ex.Message);
//            }
//        }

//    }
//}
