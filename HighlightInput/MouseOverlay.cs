using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HighlightInput
{
    class MouseOverlay : IDisposable
    {
        #region Tweakables
        readonly int c_radius = 35;
        readonly float c_initialOpacity = 0.5f;
        readonly double c_fadeMillis = TimeSpan.FromSeconds(0.5).TotalMilliseconds;
        readonly double c_growMillis = TimeSpan.FromSeconds(0.1).TotalMilliseconds;
        readonly float c_borderWidth = 4f;

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
            m_overlay = new GameOverlay.Windows.GraphicsWindow(0, 0, windowSize, windowSize, new GameOverlay.Drawing.Graphics() { PerPrimitiveAntiAliasing = true, TextAntiAliasing = true });
            m_overlay.DrawGraphics += DrawGraphics;
            m_overlay.FPS = 60;
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
            if (m_isDown)
            {
                m_position.X = args.X;
                m_position.Y = args.Y;
            }
        }

        public void MouseUp(MouseEventArgs args)
        {
            m_isDown = false;
        }
        
        private void DrawGraphics(object sender, GameOverlay.Windows.DrawGraphicsEventArgs e)
        {
            e.Graphics.ClearScene();

            //if down, reset the time to keep it at max opacity
            if (m_isDown)
            {
                if(c_growMillis > 0)
                {
                    //if grow is enabled, only reset to max grow size - not initial click size
                    double elapsed = DateTime.Now.Subtract(m_downTime).TotalMilliseconds;
                    if(elapsed > c_growMillis)
                    {
                        m_downTime = DateTime.Now.Subtract(TimeSpan.FromMilliseconds(c_growMillis));
                    }
                }
                else
                {
                    m_downTime = DateTime.Now;
                }
            }

            double millisElapsed = DateTime.Now.Subtract(m_downTime).TotalMilliseconds;
            if (millisElapsed > c_growMillis + c_fadeMillis)
            {
                return;
            }

            //centre overlay on the click
            m_overlay.Move(m_position.X - m_overlay.Width / 2, m_position.Y - m_overlay.Height / 2);

            float alpha = c_initialOpacity;
            float radius = c_radius;

            if (c_growMillis > 0 && millisElapsed <= c_growMillis) //in grow stage
            {
                radius *= (float)(millisElapsed / c_growMillis);
            }
            else //past grow stage
            {
                alpha = c_initialOpacity * (1.0f - (float)((millisElapsed - c_growMillis) / c_fadeMillis));
            }

            GameOverlay.Drawing.Color curColour;
            if (!c_colours.TryGetValue(m_button, out curColour))
            {
                curColour = c_colours[MouseButtons.Left];
            }
            e.Graphics.FillCircle(e.Graphics.CreateSolidBrush(curColour.R, curColour.G, curColour.B, alpha), new GameOverlay.Drawing.Circle(m_overlay.Width / 2, m_overlay.Height / 2, radius));

            //if there's a border, render just outside the radius
            if (c_borderWidth > 0)
            {
                e.Graphics.DrawCircle(e.Graphics.CreateSolidBrush(0, 0, 0, alpha), new GameOverlay.Drawing.Circle(m_overlay.Width / 2, m_overlay.Height / 2, radius + c_borderWidth / 2.0f), c_borderWidth * (radius / c_radius));
            }
        }

        public void Dispose()
        {
            m_overlay.Dispose();
        }
    }
}