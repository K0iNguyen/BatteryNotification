using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Windows.System.Power;
using rbnswartz.LinuxIntegration.Notifications;
using System.Threading.Tasks;
using Windows.System;
using Microsoft.Extensions.Logging;
using ABI.System.Collections.Generic;
using System.Threading;

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
        bool isNotificationRegistered = false;
        /// <summary>
        /// Debug Logger operation
        /// </summary>
        static ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole().AddDebug().SetMinimumLevel(LogLevel.Debug));
        ILogger logger = factory.CreateLogger<MainWindow>();

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "Battery Notification";
            AppWindow.Resize(new Windows.Graphics.SizeInt32(650, 250));
            Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BatteryNotification"));
            logger.LogDebug(cachePath);
            loadStartup();
            //readMemory(cachePath);
            initializedNotification();
            PowerManager.RemainingChargePercentChanged += CheckBatteryPercentage;
        }

        ~MainWindow()
        {
            UnRegisterNotification();
        }

        public void initializedNotification()
        {
            if (!isNotificationRegistered)
            {
                AppNotificationManager notificationManager = AppNotificationManager.Default;

                notificationManager.NotificationInvoked += OnNotificationInvoked;

                notificationManager.Register();

                isNotificationRegistered = true;
            }
        }

        public void UnRegisterNotification()
        {
            if (isNotificationRegistered)
            {
                AppNotificationManager.Default.NotificationInvoked -= OnNotificationInvoked;
                AppNotificationManager.Default.Unregister();
                isNotificationRegistered = false;
            }
        }

        private void testNotification(object sender, RoutedEventArgs e)
        {
            int batteryCurrentPercentage = getCurrentBatteryPercentage();
            logger.LogDebug("The current battery percentage is {batteryCurrentPercentage}", batteryCurrentPercentage);
            ThrowNotification("0");
            //writeMemory(cachePath);
        }

        private void loadStartup()
        {
            if (readMemory(cachePath))
            {
                loadBatteryPercent();
            }
        }

        public bool writeMemory(string cachePath)
        {
            try
            {
                StreamWriter sw = new(cachePath);
                sw.WriteLine(PercentTextBox.Text);
                logger.LogDebug("Written to cache file");
                sw.Close();
                return true;
            }
            catch (Exception e)
            {
                logger.LogDebug("Can't write to cache file");
                return false;
            }
            finally
            {
                logger.LogDebug("Finish Writing");
            }
        }

        public bool readMemory(string cachePath) 
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
                return true;
            }
            catch (Exception e)
            {
                logger.LogDebug("Can't find cache file");
                return false;
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

        public void getBatteryPercentKeyPressed(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == (VirtualKey)13)
            {
                writeMemory(cachePath);
                loadBatteryPercent();
            }
        }

        public void loadBatteryPercent()
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
                logger.LogDebug("System is Windows");
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

        public void OnNotificationInvoked(object sender, AppNotificationActivatedEventArgs notificationActivatedEventArgs)
        {
            logger.LogDebug("Notification Invoked");
        }

        public void ThrowWindowNotification(string batteryPercentage)
        {
            var builder = new AppNotificationBuilder()
                .AddText("You have reached " + batteryPercentage + "%")
                .SetScenario(AppNotificationScenario.Urgent);

            AppNotificationManager.Default.Show(builder.BuildNotification());
        
            logger.LogDebug("Notification shown");
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
