using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HighlightInput
{
    class MouseOverlay : IDisposable
    {
        #region Tweakables
        readonly int c_radius = 35;
        readonly float c_initialOpacity = 0.4f;
        readonly double c_fadeMillis = TimeSpan.FromSeconds(0.4).TotalMilliseconds;
        readonly float c_borderWidth = 0f;

        readonly Dictionary<MouseButtons, GameOverlay.Drawing.Color> c_colours = new Dictionary<MouseButtons, GameOverlay.Drawing.Color>()
        {
            { MouseButtons.Left, new GameOverlay.Drawing.Color(1f, 1f, 0f) },
            { MouseButtons.Right, new GameOverlay.Drawing.Color(0.7f, 0f, 0f) },
            { MouseButtons.Middle, new GameOverlay.Drawing.Color(0f, 0.5f, 1) },
        };
        #endregion

        #region State
        private System.Drawing.Point m_position;
        private DateTime m_downTime = DateTime.MinValue;
        private bool m_isDown;
        private MouseButtons m_button;

        private GameOverlay.Windows.GraphicsWindow m_overlay;
        #endregion

        public MouseOverlay()
        {
            //window must be big enough to accomodate the circle + border, add a little extra to help with clipping
            int windowSize = (int)((c_radius + c_borderWidth) * 2.1f);
            m_overlay = new GameOverlay.Windows.GraphicsWindow(0, 0, windowSize, windowSize);
            m_overlay.DrawGraphics += DrawGraphics;
            m_overlay.Create();
            m_overlay.IsTopmost = true;
            m_overlay.Show();
        }

        public void MouseDown(MouseEventArgs args)
        {
            m_button = args.Button;
            m_position.X = args.X;
            m_position.Y = args.Y;
            m_downTime = DateTime.Now;
            m_isDown = true;
        }

        public void MouseMove(MouseEventArgs args)
        {
            //if clicked, update the position
            if(m_isDown)
            {
                m_position.X = args.X;
                m_position.Y = args.Y;
            }
        }

        public void MouseUp(MouseEventArgs args)
        {
            m_isDown = false;
        }

        public void Dispose()
        {
            m_overlay.Dispose();
        }

        private void DrawGraphics(object sender, GameOverlay.Windows.DrawGraphicsEventArgs e)
        {
            var window = sender as GameOverlay.Windows.GraphicsWindow;

            //if down, reset the time to keep it at max opacity
            if (m_isDown)
            {
                m_downTime = DateTime.Now;
            }

            double millisElapsed = DateTime.Now.Subtract(m_downTime).TotalMilliseconds;
            if (millisElapsed > c_fadeMillis)
            {
                return;
            }

            //centre overlay on the click
            window.Move(m_position.X - window.Width / 2, m_position.Y - window.Height / 2);

            e.Graphics.ClearScene();

            GameOverlay.Drawing.Color curColour;
            if(!c_colours.TryGetValue(m_button, out curColour))
            {
                curColour = c_colours[MouseButtons.Left];
            }

            float mouseAlpha = c_initialOpacity * (1.0f - (float)(millisElapsed / c_fadeMillis));
            e.Graphics.FillCircle(e.Graphics.CreateSolidBrush(curColour.R, curColour.G, curColour.B, mouseAlpha), new GameOverlay.Drawing.Circle(window.Width / 2, window.Height / 2, c_radius));

            //if there's a border, render just outside the radius
            if (c_borderWidth > 0)
            {
                e.Graphics.DrawCircle(e.Graphics.CreateSolidBrush(0, 0, 0, mouseAlpha), new GameOverlay.Drawing.Circle(window.Width / 2, window.Height / 2, c_radius + c_borderWidth / 2.0f), c_borderWidth);
            }
        }
    }
}