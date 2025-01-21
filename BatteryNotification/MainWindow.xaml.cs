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
using Windows.System;
using Microsoft.Extensions.Logging;
using ABI.System.Collections.Generic;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BatteryNotification
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        int[] batteryPercentage = [0,0,0,0,0,0,0,0,0,0];
        string cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BatteryNotification\\settingCache.txt");

        /// <summary>
        /// Debug Logger operation
        /// </summary>
        static ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole().AddDebug().SetMinimumLevel(LogLevel.Debug));
        ILogger logger = factory.CreateLogger<MainWindow>();

        public MainWindow()
        {
            this.InitializeComponent();
            AppWindow.Resize(new Windows.Graphics.SizeInt32(650, 250));
            Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BatteryNotification"));
            logger.LogDebug(cachePath);
            PowerManager.RemainingChargePercentChanged += CheckBatteryPercentage;
        }

        private void testNotification(object sender, RoutedEventArgs e)
        {
            int batteryCurrentPercentage = getCurrentBatteryPercentage();
            ThrowNotification(batteryPercentage[0].ToString());
            writeMemory();
        }

        public void writeMemory()
        {
            try
            {
                StreamWriter sw = new(cachePath);
                sw.WriteLine(PercentTextBox.Text);
                logger.LogDebug("Written to cache file");
                sw.Close();
            }
            catch (Exception e)
            {
                    logger.LogDebug("Can't write to cache file");
            }
            finally
            {
                logger.LogDebug("Finish Writing");
            }
        }

        public void readMemory() 
        {
            try
            {
                string tempText;
                StreamReader sr = new(cachePath);
                tempText = sr.ReadLine();
                if (!(tempText == "")) 
                {
                    PercentTextBox.Text = tempText;
                }
                sr.Close();
            }
            catch (Exception e)
            {
                logger.LogDebug("Can't find cache file");
            }
            finally
            {
                logger.LogDebug("Finish Reading");
            }
        }

        public void CheckBatteryPercentage(object sender, object e)
        {
            for (int i = 0; i < batteryPercentage.Length; i++)
            {
                if (getCurrentBatteryPercentage() == batteryPercentage[i])
                {
                    ThrowNotification(batteryPercentage[i].ToString());
                }
            }
        }

        public void getBatteryPercentage(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == (VirtualKey)13)
            {
                string[] stringList = PercentTextBox.Text.Split(',');
                if (stringList.Length > 10)
                {
                    logger.LogDebug("stringList longer than 10");
                    return;
                }
                for (int i = 0; i < stringList.Length; i++)
                {
                    int singularPercent;
                    if (Int32.TryParse(stringList[i], out singularPercent))
                    {
                        batteryPercentage[i] = singularPercent;
                        logger.LogDebug("Added {singularPercent}", singularPercent);
                    }
                    else
                    {
                        logger.LogDebug("Can't add value");
                    }
                }
            }
        }

        public int getCurrentBatteryPercentage()
        {
            int batteryPercentage = -1;
            //var os = Environment.OSVersion;

            //System.Runtime.InteropServices.RuntimeInformation.OSDescription == Get OS Description
            if (System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Windows"))
            {
                logger.LogDebug("System is Windows");
                batteryPercentage = PowerManager.RemainingChargePercent;
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Linux"))
            {
                logger.LogDebug("System is Linux");
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

        public void ThrowNotification(string batteryPercentage)
        {
            if (System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Windows"))
            {
                ThrowWindowNotification(batteryPercentage);
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Linux"))
            {
                throwLinuxNotification(batteryPercentage);
            }
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

        public async void throwLinuxNotification(string batteryPercentage)
        {
            NotificationManager manager = new NotificationManager("BatteryNotification");
            var summary = "Current Battery Percentage Status";
            var body = "You have reached " + batteryPercentage + "%";
            await manager.ShowNotificationAsync(summary, body, expiration: 5000);
        }
    }
}
