using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GameOverlay.Windows;

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
        private object m_lock = new object();
        private System.Drawing.Point m_position;
        private DateTime m_downTime = DateTime.MinValue;
        private bool m_isDown;
        private MouseButtons m_button;

        private bool m_scrollDir;
        private DateTime m_scrollTime = DateTime.MinValue;

        private GameOverlay.Windows.GraphicsWindow m_overlay;
        GameOverlay.Drawing.Image m_img_wheelup;
        GameOverlay.Drawing.Image m_img_wheeldn;
        #endregion

        public MouseOverlay()
        {
            //window must be big enough to accomodate the circle + border, add a little extra to help with clipping
            int windowSize = (int)((c_radius + c_borderWidth) * 4);
            m_overlay = new GameOverlay.Windows.GraphicsWindow(0, 0, windowSize, windowSize, new GameOverlay.Drawing.Graphics() { PerPrimitiveAntiAliasing = true, TextAntiAliasing = true });
            m_overlay.SetupGraphics += SetupGraphics;
            m_overlay.DrawGraphics += DrawGraphics;
            m_overlay.Create();
            m_overlay.IsTopmost = true;
            m_overlay.Show();
        }

        private void SetupGraphics(object sender, SetupGraphicsEventArgs e)
        {
            m_img_wheelup = e.Graphics.CreateImage("Images\\m_wheel_up.png");
            m_img_wheeldn = e.Graphics.CreateImage("Images\\m_wheel_dn.png");
        }

        public void MouseDown(MouseEventArgs args)
        {
            lock (m_lock)
            {
                m_button = args.Button;
                m_position.X = args.X;
                m_position.Y = args.Y;
                m_downTime = DateTime.Now;
                m_isDown = true;
            }
        }

        public void MouseMove(MouseEventArgs args)
        {
            lock (m_lock)
            {
                //if clicked, update the position
                if (m_isDown)
                {
                    m_position.X = args.X;
                    m_position.Y = args.Y;
                }
            }
        }

        public void MouseUp(MouseEventArgs args)
        {
            lock (m_lock)
            {
                m_isDown = false;
            }
        }

        public void MouseWheel(MouseEventArgs args)
        {
            lock (m_lock)
            {
                m_scrollDir = args.Delta > 0;
                m_scrollTime = DateTime.Now;

                m_position.X = args.X;
                m_position.Y = args.Y;
            }
        }
        
        private void DrawGraphics(object sender, GameOverlay.Windows.DrawGraphicsEventArgs e)
        {
            lock (m_lock)
            {
                e.Graphics.ClearScene();

                //centre overlay on the mouse
                m_overlay.Move(m_position.X - m_overlay.Width / 2, m_position.Y - m_overlay.Height / 2);
                var middle = new GameOverlay.Drawing.Point(m_overlay.Width / 2, m_overlay.Height / 2);

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
                if (millisElapsed <= c_growMillis + c_fadeMillis)
                {
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
                    e.Graphics.FillCircle(e.Graphics.CreateSolidBrush(curColour.R, curColour.G, curColour.B, alpha), new GameOverlay.Drawing.Circle(middle.X, middle.Y, radius));

                    //if there's a border, render just outside the radius
                    if (c_borderWidth > 0)
                    {
                        e.Graphics.DrawCircle(e.Graphics.CreateSolidBrush(0, 0, 0, alpha), new GameOverlay.Drawing.Circle(middle.X, middle.Y, radius + c_borderWidth / 2.0f), c_borderWidth * (radius / c_radius));
                    }
                }

                //draw scroll indicator
                float scrollFadeDelay = 300;
                float scrollFadeTime = 1000;

                double scrollMillisElapsed = DateTime.Now.Subtract(m_scrollTime).TotalMilliseconds;
                if (scrollMillisElapsed > 50 && scrollMillisElapsed < scrollFadeDelay + scrollFadeTime)
                {
                    float alpha = 1.0f;
                    if (scrollMillisElapsed >= scrollFadeDelay)
                    {
                        alpha = 1.0f - (float)((scrollMillisElapsed - 50 - scrollFadeDelay) / scrollFadeTime);
                    }

                    int xOffset = 40;
                    e.Graphics.DrawImage(m_scrollDir ? m_img_wheelup : m_img_wheeldn, middle.X + xOffset, middle.Y - m_img_wheelup.Height / 2, alpha);
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
    }
}