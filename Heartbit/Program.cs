using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using ANT_Managed_Library;
using AntPlus.Profiles.HeartRate;
using AntPlus.Types;


namespace HelloAnt
{
    class Program
    {
        //Azure IoT Connection String 
        static string DeviceConnectionString = "";
        
        static DeviceClient Client = null;
        static TwinCollection reportedProperties = new TwinCollection();
        static CancellationTokenSource cts;
        static void Main(string[] args)
        {
            try
            {
                InitClient();
              //  SendDeviceProperties();

                cts = new CancellationTokenSource();
                SendTelemetryAsync(cts.Token);

                Console.WriteLine(">>Wait for settings update...");
                Client.SetDesiredPropertyUpdateCallbackAsync(HandleSettingChanged, null).Wait();
                Console.ReadKey();
                cts.Cancel();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        public static void InitClient()
        {
            try
            {
                Console.WriteLine(">>Connecting to hub....");
                Client = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Mqtt);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

      /*  public static async void SendDeviceProperties()
        {
            try
            {
                Console.WriteLine("Sending device properties:");
                Random random = new Random();
                TwinCollection telemetryConfig = new TwinCollection();
                reportedProperties["DeviceProperty"] = random.Next(1, 6);
                Console.WriteLine(JsonConvert.SerializeObject(reportedProperties));

                await Client.UpdateReportedPropertiesAsync(reportedProperties);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }
        */

        private static async void SendTelemetryAsync(CancellationToken token)
        {
            try
            {
                //ANT Part
                 byte USER_RADIOFREQ = 57; // RF Frequency + 2400 MHz
                //ANTPLUS KEY
                 byte[] USER_NETWORK_KEY = { 0xB9, 0xA5, 0x21, 0xFB, 0xBD, 0x72, 0xC3, 0x45 }; // key
                 byte USER_NETWORK_NUM = 0;
                //Use USB dongle to connect ANT+ device
                ANT_Device USB_Dongle;
                USB_Dongle = new ANT_Device();
                USB_Dongle.ResetSystem();
                USB_Dongle.setNetworkKey(USER_NETWORK_NUM, USER_NETWORK_KEY);
                ANT_Channel Channel0 = USB_Dongle.getChannel(0);
                Network AntPlusNetwork = new Network(USER_NETWORK_NUM, USER_NETWORK_KEY, USER_RADIOFREQ);
                HeartRateDisplay HR = new HeartRateDisplay(Channel0, AntPlusNetwork);
                HR.TurnOn();
                Console.WriteLine(">>ANT+ Tuen on...");

                while (true)
                {
                    byte currentHeartbeat = HR.HeartRate;
                    var telemetryDataPoint = new
                    {
                        heartbeat = currentHeartbeat,
                    };
                    var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                    var message = new Message(Encoding.ASCII.GetBytes(messageString));
                    token.ThrowIfCancellationRequested();
                    await Client.SendEventAsync(message);

                    Console.WriteLine("{0} > Sending heartbeat signal : {1}", DateTime.Now, messageString);
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Intentional shutdown: {0}", ex.Message);
            }
        }

        private static async Task HandleSettingChanged(TwinCollection desiredProperties, object userContext)
        {
            try
            {
                Console.WriteLine("Received settings change...");
                Console.WriteLine(JsonConvert.SerializeObject(desiredProperties));

                string setting = "ANT";
                if (desiredProperties.Contains(setting))
                {
                    // Act on setting change, then
                    AcknowledgeSettingChange(desiredProperties, setting);
                }
               
                await Client.UpdateReportedPropertiesAsync(reportedProperties);
            }

            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        private static void AcknowledgeSettingChange(TwinCollection desiredProperties, string setting)
        {
            reportedProperties[setting] = new
            {
                value = desiredProperties[setting]["value"],
                status = "completed",
                desiredVersion = desiredProperties["$version"],
                message = "Processed"
            };
        }
    }

}
           