using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HighlightInput
{
    class KeyboardOverlay : IDisposable
    {
        #region Tweakables
        readonly double c_fadeMillis = TimeSpan.FromSeconds(1).TotalMilliseconds;
        readonly double c_fadeStartDelay = TimeSpan.FromSeconds(1).TotalMilliseconds;

        readonly float c_percentFromBottom = 0.2f;
        readonly int c_paddingX = 20;
        readonly int c_paddingY = 10;
        readonly int c_border = 6;

        readonly Dictionary<Keys, string> c_keyReplacements = new Dictionary<Keys, string>()
        {
            { Keys.Oem8, "`" },
            { Keys.D0, "0" }, { Keys.D1, "1" }, { Keys.D2, "2" }, { Keys.D3, "3" }, { Keys.D4, "4" }, { Keys.D5, "5" }, { Keys.D6, "6" }, { Keys.D7, "7" }, { Keys.D8, "8" }, { Keys.D9, "9" },
            { Keys.Back, "Backspace" },
            { Keys.OemMinus, "-" }, { Keys.Oemplus, "+" },
        };
        #endregion

        #region State
        private object m_lock = new object();
        private GameOverlay.Windows.GraphicsWindow m_overlay;
        private GameOverlay.Windows.WindowBounds m_desktopBounds;

        private DateTime m_shownTime = DateTime.MinValue;
        private string m_text;
        #endregion

        public KeyboardOverlay()
        {
            GameOverlay.Drawing.Rectangle rect = GetDesktopRect();
            m_overlay = new GameOverlay.Windows.GraphicsWindow((int)rect.Left, (int)rect.Top, (int)(rect.Right - rect.Left), (int)(rect.Bottom - rect.Top),
                new GameOverlay.Drawing.Graphics() { PerPrimitiveAntiAliasing = true, TextAntiAliasing = true });

            m_overlay.DrawGraphics += DrawGraphics;
            m_overlay.Create();
            
            m_overlay.IsTopmost = true;
            m_overlay.Show();
        }

        //TODO: re-check desktop size occasionally & call m_overlay.Resize with this rect
        private GameOverlay.Drawing.Rectangle GetDesktopRect()
        {
            lock (m_lock)
            {
                var desktop = GameOverlay.Windows.WindowHelper.GetDesktopWindow();

                if (desktop == IntPtr.Zero)
                {
                    throw new Exception("Unable to get the desktop window");
                }

                if (!GameOverlay.Windows.WindowHelper.GetWindowBounds(desktop, out m_desktopBounds))
                {
                    throw new Exception("Unable to get the desktop bounds");
                }

                int middleX = m_desktopBounds.Right / 2;
                int middleY = (int)(m_desktopBounds.Bottom * (1.0f - c_percentFromBottom));

                //define a max size for the overlay, but what the user sees will be much smaller (measures the text)
                int maxWidth = 1000;
                int maxHeight = 200;

                return GameOverlay.Drawing.Rectangle.Create(middleX - maxWidth / 2, middleY, maxWidth, maxHeight);
            }
        }

        public void KeyDown(KeyEventArgs args)
        {
            lock(m_lock)
            {
                //exit shortcut: Ctrl Alt Escape
                if(args.Control && args.Alt && args.KeyCode == Keys.Escape)
                {
                    args.Handled = true;
                    Application.Exit();
                }

                string str = args.KeyCode.ToString();

                //do we have a nice alias?
                if(c_keyReplacements.ContainsKey(args.KeyCode))
                {
                    str = c_keyReplacements[args.KeyCode];
                }

                //don't show any modifiers on their own
                //TODO: stop combinations of modifiers showing without actual keys
                if (args.Modifiers == Keys.None && (str.Contains("Shift") || str.Contains("Control") || str.Contains("Menu")))
                {
                    return;
                }

                m_text = args.Modifiers == 0 ? str : $"{args.Modifiers} + {str}";
                m_shownTime = DateTime.Now;
            }
        }
        
        //TODO: hook setup & destroy graphics properly
        private void DrawGraphics(object sender, GameOverlay.Windows.DrawGraphicsEventArgs e)
        {
            lock (m_lock)
            {
                e.Graphics.ClearScene();
                
                if (!string.IsNullOrEmpty(m_text))
                {
                    GameOverlay.Drawing.Font font = e.Graphics.CreateFont("Calibri", 50, true);
                    GameOverlay.Drawing.Point textSize = e.Graphics.MeasureString(font, m_text);

                    int middleX = m_overlay.Width / 2;
                    int middleY = m_overlay.Height / 2;

                    double millisElapsed = DateTime.Now.Subtract(m_shownTime).TotalMilliseconds;
                    if (millisElapsed > c_fadeStartDelay + c_fadeMillis)
                    {
                        return;
                    }

                    //'flash' by not displaying immediately on keyboard input, makes it easier to see when different keys are pressed
                    if(millisElapsed < 30)
                    {
                        return;
                    }

                    float alpha = 1.0f;
                    if(millisElapsed > c_fadeStartDelay)
                    {
                        alpha = 1.0f - (float)((millisElapsed - c_fadeStartDelay) / c_fadeMillis);
                    }

                    var backColour = e.Graphics.CreateSolidBrush(0.6f, 0.6f, 0.6f, 0.85f * alpha);
                    var fontColour = e.Graphics.CreateSolidBrush(0f, 0f, 0f, alpha);

                    var rect = GameOverlay.Drawing.Rectangle.Create((int)(middleX - textSize.X / 2), (int)(middleY - textSize.Y / 2), (int)textSize.X, (int)textSize.Y);
                    Inflate(ref rect, c_paddingX, c_paddingY);
                    
                    e.Graphics.FillRectangle(backColour, rect);
                    if(c_border > 0)
                    {
                        Inflate(ref rect, c_border / 2, c_border / 2);
                        e.Graphics.DrawRectangle(fontColour, rect, c_border);
                    }

                    e.Graphics.DrawText(font, fontColour, middleX - textSize.X / 2, middleY - textSize.Y / 2, m_text);
                }
            }
        }

        public void Dispose()
        {
            lock (m_lock)
            {
                m_overlay.Dispose();
            }
        }

        private void Inflate(ref GameOverlay.Drawing.Rectangle rect, int inflateX, int inflateY)
        {
            rect.Left -= inflateX;
            rect.Top -= inflateY;
            rect.Right += inflateX;
            rect.Bottom += inflateY;
        }
    }
}
