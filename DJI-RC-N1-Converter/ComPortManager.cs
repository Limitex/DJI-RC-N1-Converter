using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace DJI_RC_N1_Converter
{
    public class ComPortManager : IDisposable
    {
        public event EventHandler<string?>? ComPortAdded;
        public event EventHandler<string?>? ComPortRemoved;

        private readonly ManagementEventWatcher comAddEventWatcher;
        private readonly ManagementEventWatcher comRemoveEventWatcher;

        public ComPortManager()
        {
            var queryAdd = new WqlEventQuery(
                "SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_SerialPort'");
            comAddEventWatcher = new ManagementEventWatcher(queryAdd);
            comAddEventWatcher.EventArrived += ComAddEventArrived;

            var queryRemove = new WqlEventQuery(
                "SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_SerialPort'");
            comRemoveEventWatcher = new ManagementEventWatcher(queryRemove);
            comRemoveEventWatcher.EventArrived += ComRemoveEventArrived;

            comAddEventWatcher.Start();
            comRemoveEventWatcher.Start();
        }

        public void Dispose()
        {
            comAddEventWatcher.Stop();
            comAddEventWatcher.Dispose();
            comRemoveEventWatcher.Stop();
            comRemoveEventWatcher.Dispose();
        }

        public void CheckExistingComPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            foreach (var port in ports)
                ComPortAdded?.Invoke(this, port);
        }

        private void ComAddEventArrived(object sender, EventArrivedEventArgs e)
        {
            var instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            ComPortAdded?.Invoke(this, instance["DeviceID"].ToString());
        }

        private void ComRemoveEventArrived(object sender, EventArrivedEventArgs e)
        {
            var instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            ComPortRemoved?.Invoke(this, instance["DeviceID"].ToString());
        }
    }
}
