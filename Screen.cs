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

using System;
using System.Drawing;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SharpQuake
{
    internal static partial class Scr
    {
        public static viddef_t vid
        {
            get
            {
                return _VidDef;
            }
        }

        public static Cvar ViewSize
        {
            get
            {
                return _ViewSize;
            }
        }

        public static float ConCurrent
        {
            get
            {
                return _ConCurrent;
            }
        }

        public static bool CopyEverithing
        {
            get
            {
                return _CopyEverything;
            }
            set
            {
                _CopyEverything = value;
            }
        }

        public static bool IsDisabledForLoading;
        public static bool BlockDrawing = false;
        public static bool SkipUpdate;
        public static bool FullSbarDraw;
        public static bool IsPermedia;

        // only the refresh window will be updated unless these variables are flagged
        public static bool CopyTop;

        public static int ClearNotify;
        public static int glX;
        public static int glY;
        public static int glWidth;
        public static int glHeight;
        public static float CenterTimeOff;
        public static int FullUpdate;
        private static viddef_t _VidDef = new viddef_t();
        private static vrect_t _VRect;
        private static bool _DrawLoading;
        private static double _DisabledTime;
        private static bool _DrawDialog;
        private static bool _IsInitialized;
        private static bool _InUpdate;
        private static glpic_t _Ram;
        private static glpic_t _Net;
        private static glpic_t _Turtle;
        private static int _TurtleCount; // static count from SCR_DrawTurtle()
        private static bool _CopyEverything;

        private static float _ConCurrent;
        private static float _ConLines;  // lines of console to display
        private static int _ClearConsole;
        private static float _OldScreenSize;
        private static float _OldFov;
        private static int _CenterLines;
        private static int _EraseLines;
        private static float _CenterTimeStart; // for slow victory printing
        private static string _CenterString;

        private static Cvar _ViewSize;
        private static Cvar _Fov;
        private static Cvar _ConSpeed;
        private static Cvar _CenterTime;
        private static Cvar _ShowRam;
        private static Cvar _ShowTurtle;
        private static Cvar _ShowPause;
        private static Cvar _PrintSpeed;
        private static Cvar _glTripleBuffer;

        private static string _NotifyString;
        private static bool _IsMouseWindowed; // windowed_mouse (don't confuse with _windowed_mouse cvar)

        public static void Init()
        {
            if( _ViewSize == null )
            {
                _ViewSize = new Cvar( "viewsize", "100", true );
                _Fov = new Cvar( "fov", "90" );
                _ConSpeed = new Cvar( "scr_conspeed", "3000" );
                _CenterTime = new Cvar( "scr_centertime", "2" );
                _ShowRam = new Cvar( "showram", "1" );
                _ShowTurtle = new Cvar( "showturtle", "0" );
                _ShowPause = new Cvar( "showpause", "1" );
                _PrintSpeed = new Cvar( "scr_printspeed", "8" );
                _glTripleBuffer = new Cvar( "gl_triplebuffer", "1", true );
            }

            // register our commands
            Cmd.Add( "screenshot", ScreenShot_f );
            Cmd.Add( "sizeup", SizeUp_f );
            Cmd.Add( "sizedown", SizeDown_f );

            _Ram = Drawer.PicFromWad( "ram" );
            _Net = Drawer.PicFromWad( "net" );
            _Turtle = Drawer.PicFromWad( "turtle" );

            if( Common.HasParam( "-fullsbar" ) )
            {
                FullSbarDraw = true;
            }

            _IsInitialized = true;
        }

        /* This is called every frame, and can also be called explicitly to flush
         * text to the screen.
         *
         * WARNING: be very careful calling this from elsewhere, because the refresh
         * needs almost the entire 256k of stack space!
         */

        public static void UpdateScreen()
        {
            if( BlockDrawing || !_IsInitialized || _InUpdate )
            {
                return;
            }

            _InUpdate = true;
            try
            {
                if( MainForm.Instance != null )
                {
                    if( ( MainForm.Instance.VSync == VSyncMode.On ) != Vid.Wait )
                    {
                        MainForm.Instance.VSync = ( Vid.Wait ? VSyncMode.On : VSyncMode.Off );
                    }
                }

                _VidDef.numpages = 2 + (int)_glTripleBuffer.Value;

                CopyTop = false;
                _CopyEverything = false;

                if( IsDisabledForLoading )
                {
                    if( ( Host.RealTime - _DisabledTime ) > 60 )
                    {
                        IsDisabledForLoading = false;
                        Con.Print( "Load failed.\n" );
                    }
                    else
                    {
                        return;
                    }
                }

                if( !Con.IsInitialized )
                {
                    return; // not initialized yet
                }

                BeginRendering();

                // determine size of refresh window
                if( _OldFov != _Fov.Value )
                {
                    _OldFov = _Fov.Value;
                    _VidDef.recalc_refdef = true;
                }

                if( _OldScreenSize != _ViewSize.Value )
                {
                    _OldScreenSize = _ViewSize.Value;
                    _VidDef.recalc_refdef = true;
                }

                if( _VidDef.recalc_refdef )
                {
                    CalcRefdef();
                }

                // do 3D refresh drawing, and then update the screen
                SetUpToDrawConsole();

                View.RenderView();

                Set2D();

                // draw any areas not covered by the refresh
                Scr.TileClear();

                if( _DrawDialog )
                {
                    Sbar.Draw();
                    Drawer.FadeScreen();
                    DrawNotifyString();
                    _CopyEverything = true;
                }
                else if( _DrawLoading )
                {
                    DrawLoading();
                    Sbar.Draw();
                }
                else if( Client.cl.intermission == 1 && Key.Destination == keydest_t.key_game )
                {
                    Sbar.IntermissionOverlay();
                }
                else if( Client.cl.intermission == 2 && Key.Destination == keydest_t.key_game )
                {
                    Sbar.FinaleOverlay();
                    CheckDrawCenterString();
                }
                else
                {
                    if( View.Crosshair > 0 )
                    {
                        Drawer.DrawCharacter( _VRect.x + _VRect.width / 2, _VRect.y + _VRect.height / 2, '+' );
                    }

                    DrawRam();
                    DrawNet();
                    DrawTurtle();
                    DrawPause();
                    CheckDrawCenterString();
                    Sbar.Draw();
                    DrawConsole();
                    Menu.Draw();
                }

                View.UpdatePalette();
                EndRendering();
            }
            finally
            {
                _InUpdate = false;
            }
        }

        public static void EndRendering()
        {
            MainForm form = MainForm.Instance;
            if( form == null )
            {
                return;
            }

            if( !SkipUpdate || BlockDrawing )
            {
                form.SwapBuffers();
            }

            // handle the mouse state
            if( !Vid.WindowedMouse )
            {
                if( _IsMouseWindowed )
                {
                    Input.DeactivateMouse();
                    Input.ShowMouse();
                    _IsMouseWindowed = false;
                }
            }
            else
            {
                _IsMouseWindowed = true;
                if( Key.Destination == keydest_t.key_game && !Input.IsMouseActive &&
                    Client.cls.state != cactive_t.ca_disconnected )
                {
                    Input.ActivateMouse();
                    Input.HideMouse();
                }
                else if( Input.IsMouseActive && Key.Destination != keydest_t.key_game )
                {
                    Input.DeactivateMouse();
                    Input.ShowMouse();
                }
            }

            if( FullSbarDraw )
            {
                Sbar.Changed();
            }
        }

        // Called for important messages that should stay in the center of the screen for a few moments
        public static void CenterPrint( string str )
        {
            _CenterString = str;
            CenterTimeOff = _CenterTime.Value;
            _CenterTimeStart = (float)Client.cl.time;

            // count the number of lines for centering
            _CenterLines = 1;
            foreach( char c in _CenterString )
            {
                if( c == '\n' )
                {
                    _CenterLines++;
                }
            }
        }

        public static void EndLoadingPlaque()
        {
            Scr.IsDisabledForLoading = false;
            Scr.FullUpdate = 0;
            Con.ClearNotify();
        }

        public static void BeginLoadingPlaque()
        {
            Sound.StopAllSounds( true );

            if( Client.cls.state != cactive_t.ca_connected )
            {
                return;
            }

            if( Client.cls.signon != Client.SIGNONS )
            {
                return;
            }

            // redraw with no console and the loading plaque
            Con.ClearNotify();
            CenterTimeOff = 0;
            _ConCurrent = 0;

            _DrawLoading = true;
            Scr.FullUpdate = 0;
            Sbar.Changed();
            UpdateScreen();
            _DrawLoading = false;

            Scr.IsDisabledForLoading = true;
            _DisabledTime = Host.RealTime;
            Scr.FullUpdate = 0;
        }

        // Displays a text string in the center of the screen and waits for a Y or N keypress.
        public static bool ModalMessage( string text )
        {
            if( Client.cls.state == cactive_t.ca_dedicated )
            {
                return true;
            }

            _NotifyString = text;

            // draw a fresh screen
            Scr.FullUpdate = 0;
            _DrawDialog = true;
            UpdateScreen();
            _DrawDialog = false;

            Sound.ClearBuffer();  // so dma doesn't loop current sound

            do
            {
                Key.KeyCount = -1;  // wait for a key down and up
                Sys.SendKeyEvents();
            } while( Key.LastPress != 'y' && Key.LastPress != 'n' && Key.LastPress != Key.K_ESCAPE );

            Scr.FullUpdate = 0;
            UpdateScreen();

            return ( Key.LastPress == 'y' );
        }

        private static void SizeUp_f()
        {
            Cvar.Set( "viewsize", _ViewSize.Value + 10 );
            _VidDef.recalc_refdef = true;
        }

        private static void SizeDown_f()
        {
            Cvar.Set( "viewsize", _ViewSize.Value - 10 );
            _VidDef.recalc_refdef = true;
        }

        private static void ScreenShot_f()
        {
            // find a file name to save it to
            string path = null;
            int i;
            for( i = 0; i <= 999; i++ )
            {
                path = Path.Combine( Common.GameDir, string.Format( "quake{0:D3}.tga", i ) );
                if( Sys.GetFileTime( path ) == DateTime.MinValue )
                {
                    break; // file doesn't exist
                }
            }
            if( i == 100 )
            {
                Con.Print( "SCR_ScreenShot_f: Couldn't create a file\n" );
                return;
            }

            FileStream fs = Sys.FileOpenWrite( path, true );
            if( fs == null )
            {
                Con.Print( "SCR_ScreenShot_f: Couldn't create a file\n" );
                return;
            }
            using( BinaryWriter writer = new BinaryWriter( fs ) )
            {
                // Write tga header (18 bytes)
                writer.Write( (ushort)0 );
                writer.Write( (byte)2 ); // uncompressed type
                writer.Write( (byte)0 );
                writer.Write( (uint)0 );
                writer.Write( (uint)0 );
                writer.Write( (byte)( glWidth & 0xff ) );
                writer.Write( (byte)( glWidth >> 8 ) );
                writer.Write( (byte)( glHeight & 0xff ) );
                writer.Write( (byte)( glHeight >> 8 ) );
                writer.Write( (byte)24 ); // pixel size
                writer.Write( (ushort)0 );

                byte[] buffer = new byte[glWidth * glHeight * 3];
                GL.ReadPixels( glX, glY, glWidth, glHeight, PixelFormat.Rgb, PixelType.UnsignedByte, buffer );

                // swap 012 to 102
                int c = glWidth * glHeight * 3;
                for( i = 0; i < c; i += 3 )
                {
                    byte temp = buffer[i + 0];
                    buffer[i + 0] = buffer[i + 1];
                    buffer[i + 1] = temp;
                }
                writer.Write( buffer, 0, buffer.Length );
            }
            Con.Print( "Wrote {0}\n", Path.GetFileName( path ) );
        }

        private static void BeginRendering()
        {
            glX = 0;
            glY = 0;
            glWidth = 0;
            glHeight = 0;

            INativeWindow window = MainForm.Instance;
            if( window != null )
            {
                Size size = window.ClientSize;
                glWidth = size.Width;
                glHeight = size.Height;
            }
        }

        private static void CalcRefdef()
        {
            Scr.FullUpdate = 0; // force a background redraw
            _VidDef.recalc_refdef = false;

            // force the status bar to redraw
            Sbar.Changed();

            // bound viewsize
            if( _ViewSize.Value < 30 )
            {
                Cvar.Set( "viewsize", "30" );
            }

            if( _ViewSize.Value > 120 )
            {
                Cvar.Set( "viewsize", "120" );
            }

            // bound field of view
            if( _Fov.Value < 10 )
            {
                Cvar.Set( "fov", "10" );
            }

            if( _Fov.Value > 170 )
            {
                Cvar.Set( "fov", "170" );
            }

            // intermission is always full screen
            float size;
            if( Client.cl.intermission > 0 )
            {
                size = 120;
            }
            else
            {
                size = _ViewSize.Value;
            }

            if( size >= 120 )
            {
                Sbar.Lines = 0; // no status bar at all
            }
            else if( size >= 110 )
            {
                Sbar.Lines = 24; // no inventory
            }
            else
            {
                Sbar.Lines = 24 + 16 + 8;
            }

            bool full = false;
            if( _ViewSize.Value >= 100.0 )
            {
                full = true;
                size = 100.0f;
            }
            else
            {
                size = _ViewSize.Value;
            }

            if( Client.cl.intermission > 0 )
            {
                full = true;
                size = 100;
                Sbar.Lines = 0;
            }
            size /= 100.0f;

            int h = _VidDef.height - Sbar.Lines;

            refdef_t rdef = Render.RefDef;
            rdef.vrect.width = (int)( _VidDef.width * size );
            if( rdef.vrect.width < 96 )
            {
                size = 96.0f / rdef.vrect.width;
                rdef.vrect.width = 96;  // min for icons
            }

            rdef.vrect.height = (int)( _VidDef.height * size );
            if( rdef.vrect.height > _VidDef.height - Sbar.Lines )
            {
                rdef.vrect.height = _VidDef.height - Sbar.Lines;
            }

            if( rdef.vrect.height > _VidDef.height )
            {
                rdef.vrect.height = _VidDef.height;
            }

            rdef.vrect.x = ( _VidDef.width - rdef.vrect.width ) / 2;
            if( full )
            {
                rdef.vrect.y = 0;
            }
            else
            {
                rdef.vrect.y = ( h - rdef.vrect.height ) / 2;
            }

            rdef.fov_x = _Fov.Value;
            rdef.fov_y = CalcFov( rdef.fov_x, rdef.vrect.width, rdef.vrect.height );

            _VRect = rdef.vrect;
        }

        private static float CalcFov( float fov_x, float width, float height )
        {
            if( fov_x < 1 || fov_x > 179 )
            {
                Sys.Error( "Bad fov: {0}", fov_x );
            }

            double x = width / Math.Tan( fov_x / 360.0 * Math.PI );
            double a = Math.Atan( height / x );
            a = a * 360.0 / Math.PI;
            return (float)a;
        }

        private static void SetUpToDrawConsole()
        {
            Con.CheckResize();

            if( _DrawLoading )
            {
                return; // never a console with loading plaque
            }

            // decide on the height of the console
            Con.ForcedUp = ( Client.cl.worldmodel == null ) || ( Client.cls.signon != Client.SIGNONS );

            if( Con.ForcedUp )
            {
                _ConLines = _VidDef.height; // full screen
                _ConCurrent = _ConLines;
            }
            else if( Key.Destination == keydest_t.key_console )
            {
                _ConLines = _VidDef.height / 2; // half screen
            }
            else
            {
                _ConLines = 0; // none visible
            }

            if( _ConLines < _ConCurrent )
            {
                _ConCurrent -= (int)( _ConSpeed.Value * Host.FrameTime );
                if( _ConLines > _ConCurrent )
                {
                    _ConCurrent = _ConLines;
                }
            }
            else if( _ConLines > _ConCurrent )
            {
                _ConCurrent += (int)( _ConSpeed.Value * Host.FrameTime );
                if( _ConLines < _ConCurrent )
                {
                    _ConCurrent = _ConLines;
                }
            }

            if( _ClearConsole++ < _VidDef.numpages )
            {
                Sbar.Changed();
            }
            else if( ClearNotify++ < _VidDef.numpages )
            {
            }
            else
            {
                Con.NotifyLines = 0;
            }
        }

        private static void TileClear()
        {
            refdef_t rdef = Render.RefDef;
            if( rdef.vrect.x > 0 )
            {
                // left
                Drawer.TileClear( 0, 0, rdef.vrect.x, _VidDef.height - Sbar.Lines );
                // right
                Drawer.TileClear( rdef.vrect.x + rdef.vrect.width, 0,
                    _VidDef.width - rdef.vrect.x + rdef.vrect.width,
                    _VidDef.height - Sbar.Lines );
            }
            if( rdef.vrect.y > 0 )
            {
                // top
                Drawer.TileClear( rdef.vrect.x, 0, rdef.vrect.x + rdef.vrect.width, rdef.vrect.y );
                // bottom
                Drawer.TileClear( rdef.vrect.x, rdef.vrect.y + rdef.vrect.height,
                    rdef.vrect.width, _VidDef.height - Sbar.Lines - ( rdef.vrect.height + rdef.vrect.y ) );
            }
        }

        private static void DrawNotifyString()
        {
            int offset = 0;
            int y = (int)( Scr.vid.height * 0.35 );

            do
            {
                int end = _NotifyString.IndexOf( '\n', offset );
                if( end == -1 )
                {
                    end = _NotifyString.Length;
                }

                if( end - offset > 40 )
                {
                    end = offset + 40;
                }

                int length = end - offset;
                if( length > 0 )
                {
                    int x = ( vid.width - length * 8 ) / 2;
                    for( int j = 0; j < length; j++, x += 8 )
                    {
                        Drawer.DrawCharacter( x, y, _NotifyString[offset + j] );
                    }

                    y += 8;
                }
                offset = end + 1;
            } while( offset < _NotifyString.Length );
        }

        private static void DrawLoading()
        {
            if( !_DrawLoading )
            {
                return;
            }

            glpic_t pic = Drawer.CachePic( "gfx/loading.lmp" );
            Drawer.DrawPic( ( vid.width - pic.width ) / 2, ( vid.height - 48 - pic.height ) / 2, pic );
        }

        private static void CheckDrawCenterString()
        {
            CopyTop = true;
            if( _CenterLines > _EraseLines )
            {
                _EraseLines = _CenterLines;
            }

            CenterTimeOff -= (float)Host.FrameTime;

            if( CenterTimeOff <= 0 && Client.cl.intermission == 0 )
            {
                return;
            }

            if( Key.Destination != keydest_t.key_game )
            {
                return;
            }

            DrawCenterString();
        }

        private static void DrawRam()
        {
            if( _ShowRam.Value == 0 )
            {
                return;
            }

            if( !Render.CacheTrash )
            {
                return;
            }

            Drawer.DrawPic( _VRect.x + 32, _VRect.y, _Ram );
        }

        private static void DrawTurtle()
        {
            if( _ShowTurtle.Value == 0 )
            {
                return;
            }

            if( Host.FrameTime < 0.1 )
            {
                _TurtleCount = 0;
                return;
            }

            _TurtleCount++;
            if( _TurtleCount < 3 )
            {
                return;
            }

            Drawer.DrawPic( _VRect.x, _VRect.y, _Turtle );
        }

        private static void DrawNet()
        {
            if( Host.RealTime - Client.cl.last_received_message < 0.3 )
            {
                return;
            }

            if( Client.cls.demoplayback )
            {
                return;
            }

            Drawer.DrawPic( _VRect.x + 64, _VRect.y, _Net );
        }

        private static void DrawPause()
        {
            if( _ShowPause.Value == 0 ) // turn off for screenshots
            {
                return;
            }

            if( !Client.cl.paused )
            {
                return;
            }

            glpic_t pic = Drawer.CachePic( "gfx/pause.lmp" );
            Drawer.DrawPic( ( vid.width - pic.width ) / 2, ( vid.height - 48 - pic.height ) / 2, pic );
        }

        private static void DrawConsole()
        {
            if( _ConCurrent > 0 )
            {
                _CopyEverything = true;
                Con.Draw( (int)_ConCurrent, true );
                _ClearConsole = 0;
            }
            else if( Key.Destination == keydest_t.key_game ||
                Key.Destination == keydest_t.key_message )
            {
                Con.DrawNotify(); // only draw notify in game
            }
        }

        private static void DrawCenterString()
        {
            int remaining;

            // the finale prints the characters one at a time
            if( Client.cl.intermission > 0 )
            {
                remaining = (int)( _PrintSpeed.Value * ( Client.cl.time - _CenterTimeStart ) );
            }
            else
            {
                remaining = 9999;
            }

            int y = 48;
            if( _CenterLines <= 4 )
            {
                y = (int)( _VidDef.height * 0.35 );
            }

            string[] lines = _CenterString.Split( '\n' );
            for( int i = 0; i < lines.Length; i++ )
            {
                string line = lines[i].TrimEnd( '\r' );
                int x = ( vid.width - line.Length * 8 ) / 2;

                for( int j = 0; j < line.Length; j++, x += 8 )
                {
                    Drawer.DrawCharacter( x, y, line[j] );
                    if( remaining-- <= 0 )
                    {
                        return;
                    }
                }
                y += 8;
            }
        }
    }
}
