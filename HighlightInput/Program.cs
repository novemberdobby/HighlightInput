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

            using (var mouseOverlay = new MouseOverlay())
            using (var kbOverlay = new KeyboardOverlay())
            using (var msListener = new MKB.MouseHookListener(globalHooker))
            using (var kbListener = new MKB.KeyboardHookListener(globalHooker))
            {
                msListener.MouseDown += (sender, eventArgs) => mouseOverlay.MouseDown(eventArgs);
                msListener.MouseMove += (sender, eventArgs) => mouseOverlay.MouseMove(eventArgs);
                msListener.MouseUp += (sender, eventArgs) => mouseOverlay.MouseUp(eventArgs);
                msListener.MouseWheel += (sender, eventArgs) => mouseOverlay.MouseWheel(eventArgs);

                kbListener.KeyDown += (sender, eventArgs) => kbOverlay.KeyDown(eventArgs);

                msListener.Start();
                kbListener.Start();

                Application.Run();

                msListener.Stop();
                kbListener.Stop();
            }
        }
    }
}