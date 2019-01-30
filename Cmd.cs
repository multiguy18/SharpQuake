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

using System.Collections.Generic;
using System.Text;

namespace SharpQuake
{
    internal delegate void xcommand_t(); // typedef void (*xcommand_t) (void);

    // Command execution takes a string, breaks it into tokens, then searches for a command or
    // variable that matches the first token.
    //
    // Commands can come from three sources, but the handler functions may choose to dissallow the
    // action or forward it to a remote server if the source is not apropriate.

    internal enum cmd_source_t
    {
        src_client, // came in over a net connection as a clc_stringcmd

        // host_client will be valid during this state.
        src_command // from the command buffer
    }

    internal static class Cmd
    {
        public static cmd_source_t Source
        {
            get
            {
                return _Source;
            }
        }

        public static int Argc
        {
            get
            {
                return _Argc;
            }
        }

        public static string Args
        {
            get
            {
                return _Args;
            }
        }

        internal static bool Wait
        {
            get
            {
                return _Wait;
            }
            set
            {
                _Wait = value;
            }
        }

        private const int MAX_ALIAS_NAME = 32;
        private const int MAX_ARGS = 80;

        private static cmd_source_t _Source;
        private static Dictionary<string, string> _Aliases;
        private static Dictionary<string, xcommand_t> _Functions;
        private static int _Argc;
        private static string[] _Argv;
        private static string _Args;
        private static bool _Wait;

        public static void Init()
        {
            // register our commands
            Add( "stuffcmds", StuffCmds_f );
            Add( "exec", Exec_f );
            Add( "echo", Echo_f );
            Add( "alias", Alias_f );
            Add( "cmd", ForwardToServer );
        }

        public static void Add( string name, xcommand_t function )
        {
            // ??? because hunk allocation would get stomped
            if( Host.IsInitialized )
            {
                Sys.Error( "Cmd.Add after host initialized!" );
            }

            // fail if the command is a variable name
            if( Cvar.Exists( name ) )
            {
                Con.Print( "Cmd.Add: {0} already defined as a var!\n", name );
                return;
            }

            // fail if the command already exists
            if( Exists( name ) )
            {
                Con.Print( "Cmd.Add: {0} already defined!\n", name );
                return;
            }

            _Functions.Add( name, function );
        }

        public static string[] Complete( string partial )
        {
            if( string.IsNullOrEmpty( partial ) )
            {
                return null;
            }

            List<string> result = new List<string>();
            foreach( string cmd in _Functions.Keys )
            {
                if( cmd.StartsWith( partial ) )
                {
                    result.Add( cmd );
                }
            }
            return ( result.Count > 0 ? result.ToArray() : null );
        }

        public static string Argv( int arg )
        {
            if( arg < 0 || arg >= _Argc )
            {
                return string.Empty;
            }

            return _Argv[arg];
        }

        public static bool Exists( string name )
        {
            return ( Find( name ) != null );
        }

        public static void TokenizeString( string text )
        {
            // clear the args from the last string
            _Argc = 0;
            _Args = null;
            _Argv = null;

            List<string> argv = new List<string>( MAX_ARGS );
            while( !string.IsNullOrEmpty( text ) )
            {
                if( _Argc == 1 )
                {
                    _Args = text;
                }

                text = Common.Parse( text );

                if( string.IsNullOrEmpty( Common.Token ) )
                {
                    break;
                }

                if( _Argc < MAX_ARGS )
                {
                    argv.Add( Common.Token );
                    _Argc++;
                }
            }
            _Argv = argv.ToArray();
        }

        // FIXME: lookupnoadd the token to speed search?
        public static void ExecuteString( string text, cmd_source_t src )
        {
            _Source = src;

            TokenizeString( text );

            // execute the command line
            if( _Argc <= 0 )
            {
                return;  // no tokens
            }

            // check functions
            xcommand_t handler = Find( _Argv[0] ); // must search with comparison like Q_strcasecmp()
            if( handler != null )
            {
                handler();
            }
            else
            {
                // check alias
                string alias = FindAlias( _Argv[0] ); // must search with compare func like Q_strcasecmp
                if( !string.IsNullOrEmpty( alias ) )
                {
                    Cbuf.InsertText( alias );
                }
                else
                {
                    // check cvars
                    if( !Cvar.Command() )
                    {
                        Con.Print( "Unknown command \"{0}\"\n", _Argv[0] );
                    }
                }
            }
        }

        public static void ForwardToServer()
        {
            if( Client.cls.state != cactive_t.ca_connected )
            {
                Con.Print( "Can't \"{0}\", not connected\n", Cmd.Argv( 0 ) );
                return;
            }

            if( Client.cls.demoplayback )
            {
                return; // not really connected
            }

            MsgWriter writer = Client.cls.message;
            writer.WriteByte( Protocol.clc_stringcmd );
            if( !Cmd.Argv( 0 ).Equals( "cmd" ) )
            {
                writer.Print( Cmd.Argv( 0 ) + " " );
            }
            if( Cmd.Argc > 1 )
            {
                writer.Print( Cmd.Args );
            }
            else
            {
                writer.Print( "\n" );
            }
        }

        public static string JoinArgv()
        {
            return string.Join( " ", _Argv );
        }

        private static xcommand_t Find( string name )
        {
            _Functions.TryGetValue( name, out xcommand_t result );
            return result;
        }

        private static string FindAlias( string name )
        {
            _Aliases.TryGetValue( name, out string result );
            return result;
        }

        private static void StuffCmds_f()
        {
            if( _Argc != 1 )
            {
                Con.Print( "stuffcmds : execute command line parameters\n" );
                return;
            }

            // build the combined string to parse from
            StringBuilder sb = new StringBuilder( 1024 );
            for( int i = 1; i < _Argc; i++ )
            {
                if( !string.IsNullOrEmpty( _Argv[i] ) )
                {
                    sb.Append( _Argv[i] );
                    if( i + 1 < _Argc )
                    {
                        sb.Append( " " );
                    }
                }
            }

            // pull out the commands
            string text = sb.ToString();
            sb.Length = 0;

            for( int i = 0; i < text.Length; i++ )
            {
                if( text[i] == '+' )
                {
                    i++;

                    int j = i;
                    while( ( j < text.Length ) && ( text[j] != '+' ) && ( text[j] != '-' ) )
                    {
                        j++;
                    }

                    sb.Append( text.Substring( i, j - i + 1 ) );
                    sb.AppendLine();
                    i = j - 1;
                }
            }

            if( sb.Length > 0 )
            {
                Cbuf.InsertText( sb.ToString() );
            }
        }

        private static void Exec_f()
        {
            if( _Argc != 2 )
            {
                Con.Print( "exec <filename> : execute a script file\n" );
                return;
            }

            byte[] bytes = Common.LoadFile( _Argv[1] );
            if( bytes == null )
            {
                Con.Print( "couldn't exec {0}\n", _Argv[1] );
                return;
            }
            string script = Encoding.ASCII.GetString( bytes );
            Con.Print( "execing {0}\n", _Argv[1] );
            Cbuf.InsertText( script );
        }

        // Just prints the rest of the line to the console
        private static void Echo_f()
        {
            for( int i = 1; i < _Argc; i++ )
            {
                Con.Print( "{0} ", _Argv[i] );
            }
            Con.Print( "\n" );
        }

        // Creates a new command that executes a command string (possibly ; seperated)
        private static void Alias_f()
        {
            if( _Argc == 1 )
            {
                Con.Print( "Current alias commands:\n" );
                foreach( KeyValuePair<string, string> alias in _Aliases )
                {
                    Con.Print( "{0} : {1}\n", alias.Key, alias.Value );
                }
                return;
            }

            string name = _Argv[1];
            if( name.Length >= MAX_ALIAS_NAME )
            {
                Con.Print( "Alias name is too long\n" );
                return;
            }

            // copy the rest of the command line
            StringBuilder sb = new StringBuilder( 1024 );
            for( int i = 2; i < _Argc; i++ )
            {
                sb.Append( _Argv[i] );
                if( i + 1 < _Argc )
                {
                    sb.Append( " " );
                }
            }
            sb.AppendLine();
            _Aliases[name] = sb.ToString();
        }

        static Cmd()
        {
            _Aliases = new Dictionary<string, string>();
            _Functions = new Dictionary<string, xcommand_t>();
        }
    }

    /* Any number of commands can be added in a frame, from several different sources.
     * Most commands come from either keybindings or console line input, but remote
     * servers can also send across commands and entire text files can be execed.
     *
     * The + command line options are also added to the command buffer.
     *
     * The game starts with a Cbuf_AddText ("exec quake.rc\n"); Cbuf_Execute ();
     */

    internal static class Cbuf
    {
        private static StringBuilder _Buf;
        private static bool _Wait;

        public static void Init()
        {
            Cmd.Add( "wait", Cmd_Wait_f );
        }

        public static void AddText( string text )
        {
            if( string.IsNullOrEmpty( text ) )
            {
                return;
            }

            int len = text.Length;
            if( _Buf.Length + len > _Buf.Capacity )
            {
                Con.Print( "Cbuf.AddText: overflow!\n" );
            }
            else
            {
                _Buf.Append( text );
            }
        }

        // FIXME: actually change the command buffer to do less copying
        public static void InsertText( string text )
        {
            _Buf.Insert( 0, text );
        }

        public static void Execute()
        {
            while( _Buf.Length > 0 )
            {
                string text = _Buf.ToString();

                // find a \n or ; line break
                int quotes = 0, i;
                for( i = 0; i < text.Length; i++ )
                {
                    if( text[i] == '"' )
                    {
                        quotes++;
                    }

                    if( ( ( quotes & 1 ) == 0 ) && ( text[i] == ';' ) )
                    {
                        break;  // don't break if inside a quoted string
                    }

                    if( text[i] == '\n' )
                    {
                        break;
                    }
                }

                string line = text.Substring( 0, i ).TrimEnd( '\n', ';' );

                /* delete the text from the command buffer and move remaining commands down
                 * this is necessary because commands (exec, alias) can insert data at the
                 * beginning of the text buffer
                 */

                if( i == _Buf.Length )
                {
                    _Buf.Length = 0;
                }
                else
                {
                    _Buf.Remove( 0, i + 1 );
                }

                // execute the command line
                if( !string.IsNullOrEmpty( line ) )
                {
                    Cmd.ExecuteString( line, cmd_source_t.src_command );

                    if( _Wait )
                    {
                        // skip out while text still remains in buffer, leaving it for next frame
                        _Wait = false;
                        break;
                    }
                }
            }
        }

        // Delays execution of the command buffer until the next frame
        public static void Cmd_Wait_f()
        {
            _Wait = true;
        }

        static Cbuf()
        {
            _Buf = new StringBuilder( 8192 ); // space for commands and script files
        }
    }
}
