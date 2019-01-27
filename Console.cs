/// <copyright>
///
/// Rewritten in C# by Yury Kiselev, 2010.
///
/// Copyright (C) 1996-1997 Id Software, Inc.
///
/// This program is free software; you can redistribute it and/or
/// modify it under the terms of the GNU General Public License
/// as published by the Free Software Foundation; either version 2
/// of the License, or (at your option) any later version.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
///
/// See the GNU General Public License for more details.
///
/// You should have received a copy of the GNU General Public License
/// along with this program; if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
/// </copyright>

using System;
using System.IO;
using System.Text;

namespace SharpQuake
{
    internal static class Con
    {
        public static bool IsInitialized
        {
            get
            {
                return _IsInitialized;
            }
        }

        public static bool ForcedUp
        {
            get
            {
                return _ForcedUp;
            }
            set
            {
                _ForcedUp = value;
            }
        }

        public static int NotifyLines
        {
            get
            {
                return _NotifyLines;
            }
            set
            {
                _NotifyLines = value;
            }
        }

        public static int TotalLines
        {
            get
            {
                return _TotalLines;
            }
        }

        public static int BackScroll;
        private const string LOG_FILE_NAME = "qconsole.log";

        private const int CON_TEXTSIZE = 16384;
        private const int NUM_CON_TIMES = 4;

        private static char[] _Text = new char[CON_TEXTSIZE];
        private static int _VisLines;
        private static int _TotalLines; // total lines in console scrollback

        // lines up from bottom to display
        private static int _Current; // where next message will be printed

        private static int _X; // offset in current line for next print
        private static int _CR;
        private static double[] _Times = new double[NUM_CON_TIMES]; // realtime time the line was generated

        // for transparent notify lines
        private static int _LineWidth;

        private static bool _DebugLog;
        private static bool _IsInitialized;
        private static bool _ForcedUp;// because no entities to refresh
        private static int _NotifyLines;// scan lines to clear for notify lines
        private static Cvar _NotifyTime;//seconds
        private static float _CursorSpeed = 4;
        private static FileStream _Log;

        public static void CheckResize()
        {
            int width = ( Scr.vid.width >> 3 ) - 2;
            if( width == _LineWidth )
                return;

            if( width < 1 ) // video hasn't been initialized yet
            {
                width = 38;
                _LineWidth = width;
                _TotalLines = CON_TEXTSIZE / _LineWidth;
                Common.FillArray( _Text, ' ' );
            }
            else
            {
                int oldwidth = _LineWidth;
                _LineWidth = width;
                int oldtotallines = _TotalLines;
                _TotalLines = CON_TEXTSIZE / _LineWidth;
                int numlines = oldtotallines;

                if( _TotalLines < numlines )
                    numlines = _TotalLines;

                int numchars = oldwidth;

                if( _LineWidth < numchars )
                    numchars = _LineWidth;

                char[] tmp = _Text;
                _Text = new char[CON_TEXTSIZE];
                Common.FillArray( _Text, ' ' );

                for( int i = 0; i < numlines; i++ )
                {
                    for( int j = 0; j < numchars; j++ )
                    {
                        _Text[( _TotalLines - 1 - i ) * _LineWidth + j] = tmp[( ( _Current - i + oldtotallines ) %
                                      oldtotallines ) * oldwidth + j];
                    }
                }

                ClearNotify();
            }

            BackScroll = 0;
            _Current = _TotalLines - 1;
        }

        public static void Init()
        {
            _DebugLog = ( Common.CheckParm( "-condebug" ) > 0 );
            if( _DebugLog )
            {
                string path = Path.Combine( Common.GameDir, LOG_FILE_NAME );
                if( File.Exists( path ) )
                    File.Delete( path );

                _Log = new FileStream( path, FileMode.Create, FileAccess.Write, FileShare.Read );
            }

            _LineWidth = -1;
            CheckResize();

            Con.Print( "Console initialized.\n" );

            // register our commands
            if( _NotifyTime == null )
            {
                _NotifyTime = new Cvar( "con_notifytime", "3" );
            }

            Cmd.Add( "toggleconsole", ToggleConsole_f );
            Cmd.Add( "messagemode", MessageMode_f );
            Cmd.Add( "messagemode2", MessageMode2_f );
            Cmd.Add( "clear", Clear_f );

            _IsInitialized = true;
        }

        public static void Draw( int lines, bool drawinput )
        {
            if( lines <= 0 )
                return;

            // draw the background
            Drawer.DrawConsoleBackground( lines );

            // draw the text
            _VisLines = lines;

            int rows = ( lines - 16 ) >> 3; // rows of text to draw
            int y = lines - 16 - ( rows << 3 ); // may start slightly negative

            for( int i = _Current - rows + 1; i <= _Current; i++, y += 8 )
            {
                int j = i - BackScroll;
                if( j < 0 )
                    j = 0;

                int offset = ( j % _TotalLines ) * _LineWidth;

                for( int x = 0; x < _LineWidth; x++ )
                    Drawer.DrawCharacter( ( x + 1 ) << 3, y, _Text[offset + x] );
            }

            // draw the input prompt, user text, and cursor if desired
            if( drawinput )
                DrawInput();
        }

        public static void Print( string fmt, params object[] args )
        {
            string msg = ( args.Length > 0 ? String.Format( fmt, args ) : fmt );

            // log all messages to file
            if( _DebugLog )
                DebugLog( msg );

            if( !_IsInitialized )
                return;

            if( Client.cls.state == cactive_t.ca_dedicated )
                return;  // no graphics mode

            // write it to the scrollable buffer
            Print( msg );

            // update the screen if the console is displayed
            if( Client.cls.signon != Client.SIGNONS && !Scr.IsDisabledForLoading )
                Scr.UpdateScreen();
        }

        public static void Shutdown()
        {
            if( _Log != null )
            {
                _Log.Flush();
                _Log.Dispose();
                _Log = null;
            }
        }

        public static void DPrint( string fmt, params object[] args )
        {
            if( Host.IsDeveloper )
                Print( fmt, args );
        }

        public static void SafePrint( string fmt, params object[] args )
        {
            bool temp = Scr.IsDisabledForLoading;
            Scr.IsDisabledForLoading = true;
            Print( fmt, args );
            Scr.IsDisabledForLoading = temp;
        }

        public static void DrawNotify()
        {
            int v = 0;
            for( int i = _Current - NUM_CON_TIMES + 1; i <= _Current; i++ )
            {
                if( i < 0 )
                    continue;
                double time = _Times[i % NUM_CON_TIMES];
                if( time == 0 )
                    continue;
                time = Host.RealTime - time;
                if( time > _NotifyTime.Value )
                    continue;

                int textOffset = ( i % _TotalLines ) * _LineWidth;

                Scr.ClearNotify = 0;
                Scr.CopyTop = true;

                for( int x = 0; x < _LineWidth; x++ )
                    Drawer.DrawCharacter( ( x + 1 ) << 3, v, _Text[textOffset + x] );

                v += 8;
            }

            if( Key.Destination == keydest_t.key_message )
            {
                Scr.ClearNotify = 0;
                Scr.CopyTop = true;

                int x = 0;

                Drawer.DrawString( 8, v, "say:" );
                string chat = Key.ChatBuffer;
                for( ; x < chat.Length; x++ )
                {
                    Drawer.DrawCharacter( ( x + 5 ) << 3, v, chat[x] );
                }
                Drawer.DrawCharacter( ( x + 5 ) << 3, v, 10 + ( (int)( Host.RealTime * _CursorSpeed ) & 1 ) );
                v += 8;
            }

            if( v > _NotifyLines )
                _NotifyLines = v;
        }

        public static void ClearNotify()
        {
            for( int i = 0; i < NUM_CON_TIMES; i++ )
                _Times[i] = 0;
        }

        public static void ToggleConsole_f()
        {
            if( Key.Destination == keydest_t.key_console )
            {
                if( Client.cls.state == cactive_t.ca_connected )
                {
                    Key.Destination = keydest_t.key_game;
                    Key.Lines[Key.EditLine][1] = '\0'; // clear any typing
                    Key.LinePos = 1;
                }
                else
                {
                    MenuBase.MainMenu.Show();
                }
            }
            else
                Key.Destination = keydest_t.key_console;

            Scr.EndLoadingPlaque();
            Array.Clear( _Times, 0, _Times.Length );
        }

        private static void DebugLog( string msg )
        {
            if( _Log != null )
            {
                byte[] tmp = Encoding.UTF8.GetBytes( msg );
                _Log.Write( tmp, 0, tmp.Length );
            }
        }

        private static void Print( string txt )
        {
            if( String.IsNullOrEmpty( txt ) )
                return;

            int mask, offset = 0;

            BackScroll = 0;

            if( txt.StartsWith( ( (char)1 ).ToString() ) )
            {
                mask = 128; // go to colored text
                Sound.LocalSound( "misc/talk.wav" ); // play talk wav
                offset++;
            }
            else if( txt.StartsWith( ( (char)2 ).ToString() ) )
            {
                mask = 128; // go to colored text
                offset++;
            }
            else
                mask = 0;

            while( offset < txt.Length )
            {
                char c = txt[offset];

                int l;
                // count word length
                for( l = 0; l < _LineWidth && offset + l < txt.Length; l++ )
                {
                    if( txt[offset + l] <= ' ' )
                        break;
                }

                // word wrap
                if( l != _LineWidth && ( _X + l > _LineWidth ) )
                    _X = 0;

                offset++;

                if( _CR != 0 )
                {
                    _Current--;
                    _CR = 0;
                }

                if( _X == 0 )
                {
                    LineFeed();
                    // mark time for transparent overlay
                    if( _Current >= 0 )
                        _Times[_Current % NUM_CON_TIMES] = Host.RealTime; // realtime
                }

                switch( c )
                {
                    case '\n':
                        _X = 0;
                        break;

                    case '\r':
                        _X = 0;
                        _CR = 1;
                        break;

                    default:    // display character and advance
                        int y = _Current % _TotalLines;
                        _Text[y * _LineWidth + _X] = (char)( c | mask );
                        _X++;
                        if( _X >= _LineWidth )
                            _X = 0;
                        break;
                }
            }
        }

        private static void Clear_f()
        {
            Common.FillArray( _Text, ' ' );
        }

        private static void MessageMode_f()
        {
            Key.Destination = keydest_t.key_message;
            Key.TeamMessage = false;
        }

        private static void MessageMode2_f()
        {
            Key.Destination = keydest_t.key_message;
            Key.TeamMessage = true;
        }

        private static void LineFeed()
        {
            _X = 0;
            _Current++;

            for( int i = 0; i < _LineWidth; i++ )
            {
                _Text[( _Current % _TotalLines ) * _LineWidth + i] = ' ';
            }
        }

        private static void DrawInput()
        {
            if( Key.Destination != keydest_t.key_console && !_ForcedUp )
                return;  // don't draw anything

            // add the cursor frame
            Key.Lines[Key.EditLine][Key.LinePos] = (char)( 10 + ( (int)( Host.RealTime * _CursorSpeed ) & 1 ) );

            // fill out remainder with spaces
            for( int i = Key.LinePos + 1; i < _LineWidth; i++ )
                Key.Lines[Key.EditLine][i] = ' ';

            // prestep if horizontally scrolling
            int offset = 0;
            if( Key.LinePos >= _LineWidth )
                offset = 1 + Key.LinePos - _LineWidth;

            // draw it
            int y = _VisLines - 16;

            for( int i = 0; i < _LineWidth; i++ )
                Drawer.DrawCharacter( ( i + 1 ) << 3, _VisLines - 16, Key.Lines[Key.EditLine][offset + i] );

            // remove cursor
            Key.Lines[Key.EditLine][Key.LinePos] = '\0';
        }
    }
}
