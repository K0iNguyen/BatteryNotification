using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.Windows.System.Power;
using rbnswartz.LinuxIntegration.Notifications;
using System.Threading.Tasks;
using Microsoft.Windows.AppNotifications;
using System.Security.Cryptography.X509Certificates;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BatteryNotification
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        int[] batteryPercentage;
        int batteryIndex;
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void myButton_Click(object sender, RoutedEventArgs e)
        {
            int batteryPercentage = getBatteryPercentage();
            myButton.Content = batteryPercentage.ToString();
            ThrowWindowNotification(batteryPercentage.ToString());
        }

        public int getBatteryPercentage()
        {
            int batteryPercentage = -1;
            //var os = Environment.OSVersion;

            //System.Runtime.InteropServices.RuntimeInformation.OSDescription ==
            if (System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Windows"))
            {
                batteryPercentage = PowerManager.RemainingChargePercent;
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Linux"))
            {
                bool read = false;
                int batNUM = 0;
                while (!read)
                {
                    try
                    {
                        string addressString = "/~/sys/class/power_supply/";
                        addressString += "BAT" + batNUM.ToString() + "/capacity";
                        StreamReader file = new(addressString);
                        read = true;
                        batteryPercentage = Int32.Parse(file.ReadLine());
                    }
                    catch (DirectoryNotFoundException e)
                    {
                        batNUM ++;
                        if (batNUM == 10)
                        {
                            read = true;
                        }
                    }//End catch
                }//End while
            }//End else if

            return batteryPercentage;
        }


        public void ThrowWindowNotification(string batteryPercentage)
        {
            var builder = new AppNotificationBuilder()
                .AddArgument("conversationId", "9813")

                .AddText("You have reached " + batteryPercentage + "%")

                .SetAudioUri(new Uri("ms-appx:///Sound.mp3"));

            var notificationManager = AppNotificationManager.Default;
            notificationManager.Show(builder.BuildNotification());

        }

        public async void throwLinuxNotification()
        {
            NotificationManager manager = new NotificationManager("Battery Status");
            var summary = "Battery Percentage Status";
            var body = "Battery Percentage reached ";
            body += batteryPercentage[batteryIndex].ToString();
            await manager.ShowNotificationAsync(summary, body, expiration: 5000);
        }



    }
}
