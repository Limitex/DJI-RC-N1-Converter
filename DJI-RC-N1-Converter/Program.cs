using System.Diagnostics;

namespace DJI_RC_N1_Converter
{
    internal static class Program
    {
        static MainForm? mainForm;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            mainForm = new MainForm();

            Disconnected();

            using (var backgroundProgram = new BackgroundProgram())
            {
                backgroundProgram.ComPortConnected += (sender, portName) => Connected(portName);
                backgroundProgram.ComPortDisconnected += (sender, portName) => Disconnected(portName);
                Application.Run();
            }
        }

        static void Connected(string? portName)
        {
            Debug.WriteLine($"RC-N1 (RC231) is connected! ({portName})");
            mainForm?.UpdateContextMenu(menu =>
            {
                menu.Items[0].Text = $"RC-N1 (RC231) is connected! ({portName})";
            });
        }

        static void Disconnected(string? portName = null)
        {
            Debug.WriteLine($"Waiting for RC-N1 (RC231) connection...");
            mainForm?.UpdateContextMenu(menu =>
            {
                menu.Items[0].Text = "Waiting for RC-N1 (RC231) connection...";
            });
        }
    }
}