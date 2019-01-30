/// <copyright>
///     Rewritten in C# by Yury Kiselev, 2010.
///    
///     Copyright (C) 1996-1997 Id Software, Inc.
///    
///     This program is free software; you can redistribute it and/or modify it under the terms of
///     the GNU General Public License as published by the Free Software Foundation; either version 2
///     of the License, or (at your option) any later version.
///    
///     This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
///     without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
///    
///     See the GNU General Public License for more details.
///    
///     You should have received a copy of the GNU General Public License along with this program; if
///     not, write to the Free Software Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA
///     02111-1307, USA.
/// </copyright>

using System.Drawing;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Input;

namespace SharpQuake
{
    internal static class Input
    {
        public static bool IsMouseActive
        {
            get
            {
                return _IsMouseActive;
            }
        }

        public static Point WindowCenter
        {
            get
            {
                Rectangle bounds = MainForm.Instance.Bounds;
                Point p = bounds.Location;
                p.Offset( bounds.Width / 2, bounds.Height / 2 );
                return p;
            }
        }

        private static Cvar _MouseFilter;
        private static Vector2 _OldMouse;
        private static Vector2 _Mouse;
        private static Vector2 _MouseAccum;
        private static bool _IsMouseActive;
        private static int _MouseButtons;
        private static int _MouseOldButtonState;
        private static bool _MouseActivateToggle;
        private static bool _MouseShowToggle = true;

        public static void Init()
        {
            if( _MouseFilter == null )
            {
                _MouseFilter = new Cvar( "m_filter", "0" );
            }

            _IsMouseActive = ( Mouse.GetState( 0 ).IsConnected != false );
            if( _IsMouseActive )
            {
                _MouseButtons = 3; //TODO: properly upgrade this to 3.0.1
            }
        }

        public static void Shutdown()
        {
            DeactivateMouse();
            ShowMouse();
        }

        public static void Commands()
        {
        }

        public static void ActivateMouse()
        {
            _MouseActivateToggle = true;

            if( Mouse.GetState( 0 ).IsConnected != false )
            {
                Cursor.Position = Input.WindowCenter;

                _IsMouseActive = true;
            }
        }

        public static void DeactivateMouse()
        {
            _MouseActivateToggle = false;
            _IsMouseActive = false;
        }

        public static void HideMouse()
        {
            if( _MouseShowToggle )
            {
                Cursor.Hide();
                _MouseShowToggle = false;
            }
        }

        public static void ShowMouse()
        {
            if( !_MouseShowToggle )
            {
                if( !MainForm.IsFullscreen )
                {
                    Cursor.Show();
                }
                _MouseShowToggle = true;
            }
        }

        public static void Move( usercmd_t cmd )
        {
            if( !MainForm.Instance.Focused )
            {
                return;
            }

            if( MainForm.Instance.WindowState == WindowState.Minimized )
            {
                return;
            }

            MouseMove( cmd );
        }

        public static void ClearStates()
        {
            if( _IsMouseActive )
            {
                _MouseAccum = Vector2.Zero;
                _MouseOldButtonState = 0;
            }
        }

        public static void MouseEvent( int mstate )
        {
            if( _IsMouseActive )
            {
                // perform button actions
                for( int i = 0; i < _MouseButtons; i++ )
                {
                    if( ( mstate & ( 1 << i ) ) != 0 && ( _MouseOldButtonState & ( 1 << i ) ) == 0 )
                    {
                        Key.Event( Key.K_MOUSE1 + i, true );
                    }

                    if( ( mstate & ( 1 << i ) ) == 0 && ( _MouseOldButtonState & ( 1 << i ) ) != 0 )
                    {
                        Key.Event( Key.K_MOUSE1 + i, false );
                    }
                }

                _MouseOldButtonState = mstate;
            }
        }

        private static void MouseMove( usercmd_t cmd )
        {
            if( !_IsMouseActive )
            {
                return;
            }

            Point current_pos = Cursor.Position;
            Point window_center = Input.WindowCenter;

            int mx = (int)( current_pos.X - window_center.X + _MouseAccum.X );
            int my = (int)( current_pos.Y - window_center.Y + _MouseAccum.Y );
            _MouseAccum.X = 0;
            _MouseAccum.Y = 0;

            if( _MouseFilter.Value != 0 )
            {
                _Mouse.X = ( mx + _OldMouse.X ) * 0.5f;
                _Mouse.Y = ( my + _OldMouse.Y ) * 0.5f;
            }
            else
            {
                _Mouse.X = mx;
                _Mouse.Y = my;
            }

            _OldMouse.X = mx;
            _OldMouse.Y = my;

            _Mouse *= Client.Sensitivity;

            // add mouse X/Y movement to cmd
            if( ClientInput.StrafeBtn.IsDown || ( Client.LookStrafe && ClientInput.MLookBtn.IsDown ) )
            {
                cmd.sidemove += Client.MSide * _Mouse.X;
            }
            else
            {
                Client.cl.viewangles.Y -= Client.MYaw * _Mouse.X;
            }

            View.StopPitchDrift();

            Client.cl.viewangles.X += Client.MPitch * _Mouse.Y;

            Client.cl.viewangles.X = MathHelper.Clamp( Client.cl.viewangles.X, -75, 85 );

            // if the mouse has moved, force it to the center, so there's room to move
            if( mx != 0 || my != 0 )
            {
                Cursor.Position = window_center;
            }
        }
    }
}
