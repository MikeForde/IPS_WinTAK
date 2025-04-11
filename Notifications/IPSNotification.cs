using ipswintakplugin.Common;

using System;
using System.ComponentModel.Composition;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using WinTak.Framework.Notifications;

namespace ipswintakplugin.Notifications
{
    [Export(typeof(Notification))]
    public class IPSNotification : Notification
    {
        public IPSNotification() 
        {
            base.Type = ULIDGenerator.GenerateULID();
            this.Key = ULIDGenerator.GenerateULID();
        }
        public override string GetHeader()
        {
            return "IPSNotification";
        }
        public override ImageSource GetHeaderIcon()
        {
            BitmapImage bitmapImage = new BitmapImage();

            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri("pack://application:,,,/ipswintakplugin;component/assets/hw_notification_icon.png");
            bitmapImage.EndInit();

            return bitmapImage;
        }
    }
}
