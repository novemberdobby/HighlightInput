using System;
using System.Windows.Forms;
using MKB = MouseKeyboardActivityMonitor;

namespace HighlightInput
{
    class Program
    {
        static void Main(string[] args)
        {
            var globalHooker = new MKB.WinApi.GlobalHooker();

            var msListener = new MKB.MouseHookListener(globalHooker);
            msListener.MouseClick += (object sender, MouseEventArgs eventArgs) =>
            {
                Console.WriteLine(eventArgs.Button);
            };

            var kbListener = new MKB.KeyboardHookListener(globalHooker);
            kbListener.KeyDown += (object sender, KeyEventArgs eventArgs) =>
            {
                Console.WriteLine(eventArgs.KeyCode);
            };

            msListener.Start();
            kbListener.Start();

            Application.Run();

            msListener.Stop();
            kbListener.Stop();
        }
    }
}
