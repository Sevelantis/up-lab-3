using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace lab3
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BluetoothRadio adapter = null;
        private BluetoothDeviceInfo device = null;
        private readonly BackgroundWorker worker = new BackgroundWorker();
        public MainWindow()
        {
            InitializeComponent();
            init();
        }

        void init()
        {
            var adapters = BluetoothRadio.AllRadios;
            AdaptersComboBox.ItemsSource = adapters;

            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
        }

        private void SearchDevicesButton_Click(object sender, RoutedEventArgs e)
        {
            string searchingString = "Searching ...";
            SearchDevicesButton.Content = searchingString;
            if (AdaptersComboBox.SelectedIndex < 0)
            {
                MessageBox.Show("You didnt choose adapter!");
                return;
            }
            worker.RunWorkerAsync();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            EventHandler<BluetoothWin32AuthenticationEventArgs> authHandler = new EventHandler<BluetoothWin32AuthenticationEventArgs>(handleAuthRequests);
            BluetoothWin32Authentication authenticator = new BluetoothWin32Authentication(authHandler);

            if (adapter == null || device == null)
            {
                MessageBox.Show("You didnt choose adapter or device!");
                return;
            }


            if (BluetoothSecurity.PairRequest(device.DeviceAddress, null))
            {
                ConnectButton.IsEnabled = false;
                DisconnectButton.IsEnabled = true;
                SendFileButton.IsEnabled = true;

                MessageBox.Show("Connected successfully with " + device.DeviceName);
                return;
            }
            MessageBox.Show("Couldn't connect to " + device.DeviceName);
        }

        private void SendFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileChoice = new OpenFileDialog();
            fileChoice.RestoreDirectory = true;

            if (fileChoice.ShowDialog() == true)
            {
                string fileName = fileChoice.FileName;
                ObexWebRequest request = new ObexWebRequest(
                    new Uri("obex://" + device.DeviceAddress + "/" + fileName)
                    );
                request.ReadFile(fileName);
                ObexWebResponse response = (ObexWebResponse)request.GetResponse();
                response.Close();
                MessageBox.Show("Result message:\n" + response.StatusCode.ToString());
            }
            else
            {
                MessageBox.Show("You didn't choose a file");
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (adapter == null || device == null)
            {
                MessageBox.Show("You didn't choose an adapter and a device");
                return;
            }
            if (BluetoothSecurity.RemoveDevice(device.DeviceAddress))
            {
                ConnectButton.IsEnabled = true;
                DisconnectButton.IsEnabled = false;
                SendFileButton.IsEnabled = false;
                MessageBox.Show("Disconnected successfully");
            }
            else
            {
                MessageBox.Show("Couldn't disconnect");
            }
        }

        private void handleAuthRequests(object sender, BluetoothWin32AuthenticationEventArgs e)
        {
            switch (e.AuthenticationMethod)
            {
                case BluetoothAuthenticationMethod.Legacy:
                    MessageBox.Show("Legacy Authentication");
                    break;

                case BluetoothAuthenticationMethod.OutOfBand:
                    MessageBox.Show("Out of Band Authentication");
                    break;

                case BluetoothAuthenticationMethod.NumericComparison:
                    if (e.JustWorksNumericComparison == true)
                    {
                        MessageBox.Show("Just Works Numeric Comparison");
                    }
                    else
                    {
                        MessageBox.Show("Show User Numeric Comparison");
                        if (MessageBox.Show(e.NumberOrPasskeyAsString, "Pair Device", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            e.Confirm = true;
                        }
                        else
                        {
                            e.Confirm = false;
                        }
                    }
                    break;

                case BluetoothAuthenticationMethod.PasskeyNotification:
                    MessageBox.Show("Passkey Notification");
                    break;

                case BluetoothAuthenticationMethod.Passkey:
                    MessageBox.Show("Passkey");
                    break;

                default:
                    MessageBox.Show("Event handled in some unknown way");
                    break;

            }
        }

        private void AdaptersComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            adapter = (BluetoothRadio)AdaptersComboBox.SelectedItem;
        }

        private void DevicesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            device = (BluetoothDeviceInfo)DevicesComboBox.SelectedItem;
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            DevicesComboBox.ItemsSource = (System.Collections.IEnumerable)e.Result;
            SearchDevicesButton.Content = "Search Devices";
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BluetoothEndPoint endPoint = new BluetoothEndPoint(adapter.LocalAddress, BluetoothService.SerialPort);
            BluetoothClient client = new BluetoothClient(endPoint);
            var devices = client.DiscoverDevices();

            e.Result = devices;
        }
    }
}
