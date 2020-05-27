using System;
using System.Windows.Forms;

namespace HighlightInput
{
    class MouseOverlay : IDisposable
    {
        #region Tweakables
        readonly int c_mouseRadius = 35;
        readonly float c_mouseInitialOpacity = 0.4f;
        readonly double c_mouseFadeMillis = TimeSpan.FromSeconds(0.4).TotalMilliseconds;
        readonly float c_mouseBorderWidth = 0f;
        #endregion

        #region State
        private System.Drawing.Point m_mousePos;
        private DateTime m_mouseDownTime = DateTime.MinValue;
        private bool m_mouseIsDown;
        private GameOverlay.Windows.GraphicsWindow m_overlay;
        #endregion

        public MouseOverlay()
        {
            //window must be big enough to accomodate the circle + border, add a little extra to help with clipping
            int windowSize = (int)((c_mouseRadius + c_mouseBorderWidth) * 2.1f);
            m_overlay = new GameOverlay.Windows.GraphicsWindow(0, 0, windowSize, windowSize);
            m_overlay.DrawGraphics += DrawGraphics;
            m_overlay.Create();
            m_overlay.IsTopmost = true;
            m_overlay.Show();
        }

        public void MouseDown(MouseEventArgs args)
        {
            m_mousePos.X = args.X;
            m_mousePos.Y = args.Y;
            m_mouseDownTime = DateTime.Now;
            m_mouseIsDown = true;
        }

        public void MouseMove(MouseEventArgs args)
        {
            //if clicked, update the position
            if(m_mouseIsDown)
            {
                m_mousePos.X = args.X;
                m_mousePos.Y = args.Y;
            }
        }

        public void MouseUp(MouseEventArgs args)
        {
            m_mouseIsDown = false;
        }

        public void Dispose()
        {
            m_overlay.Dispose();
        }

        private void DrawGraphics(object sender, GameOverlay.Windows.DrawGraphicsEventArgs e)
        {
            var window = sender as GameOverlay.Windows.GraphicsWindow;

            //if down, reset the time to keep it at max opacity
            if (m_mouseIsDown)
            {
                m_mouseDownTime = DateTime.Now;
            }

            double millisElapsed = DateTime.Now.Subtract(m_mouseDownTime).TotalMilliseconds;
            if (millisElapsed > c_mouseFadeMillis)
            {
                return;
            }

            //centre overlay on the click
            window.Move(m_mousePos.X - window.Width / 2, m_mousePos.Y - window.Height / 2);

            e.Graphics.ClearScene();

            float mouseAlpha = c_mouseInitialOpacity * (1.0f - (float)(millisElapsed / c_mouseFadeMillis));
            e.Graphics.FillCircle(e.Graphics.CreateSolidBrush(1, 1, 0, mouseAlpha), new GameOverlay.Drawing.Circle(window.Width / 2, window.Height / 2, c_mouseRadius));

            //if there's a border, render just outside the radius
            if (c_mouseBorderWidth > 0)
            {
                e.Graphics.DrawCircle(e.Graphics.CreateSolidBrush(0, 0, 0, mouseAlpha), new GameOverlay.Drawing.Circle(window.Width / 2, window.Height / 2, c_mouseRadius + c_mouseBorderWidth / 2.0f), c_mouseBorderWidth);
            }
        }
    }
}