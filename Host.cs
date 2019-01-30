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
using System.IO;
using System.Text;

namespace SharpQuake
{
    internal static partial class Host
    {
        public static quakeparms_t Params
        {
            get
            {
                return _Params;
            }
        }

        public static bool IsDedicated
        {
            get
            {
                return false;
            }
        }

        public static bool IsInitialized
        {
            get
            {
                return _IsInitialized;
            }
        }

        public static double Time
        {
            get
            {
                return _Time;
            }
        }

        public static int FrameCount
        {
            get
            {
                return _FrameCount;
            }
        }

        public static bool IsDeveloper
        {
            get
            {
                return ( _Developer != null ? _Developer.Value != 0 : false );
            }
        }

        public static byte[] ColorMap
        {
            get
            {
                return _ColorMap;
            }
        }

        public static float TeamPlay
        {
            get
            {
                return _Teamplay.Value;
            }
        }

        public static byte[] BasePal
        {
            get
            {
                return _BasePal;
            }
        }

        public static bool IsCoop
        {
            get
            {
                return _Coop.Value != 0;
            }
        }

        public static float Skill
        {
            get
            {
                return _Skill.Value;
            }
        }

        public static float Deathmatch
        {
            get
            {
                return _Deathmatch.Value;
            }
        }

        public static int ClientNum
        {
            get
            {
                return Array.IndexOf( Server.svs.clients, Host.HostClient );
            }
        }

        public static float FragLimit
        {
            get
            {
                return _FragLimit.Value;
            }
        }

        public static float TimeLimit
        {
            get
            {
                return _TimeLimit.Value;
            }
        }

        public static BinaryReader VcrReader
        {
            get
            {
                return _VcrReader;
            }
        }

        public static BinaryWriter VcrWriter
        {
            get
            {
                return _VcrWriter;
            }
        }

        public static double FrameTime;
        public static double RealTime;
        public static int CurrentSkill;
        public static bool NoClipAngleHack;
        public static client_t HostClient;
        private const int VCR_SIGNATURE = 0x56435231;
        private const int SAVEGAME_VERSION = 5;

        private static quakeparms_t _Params;

        private static Cvar _Sys_TickRate;
        private static Cvar _Developer;
        private static Cvar _FrameRate; // set for slow motion
        private static Cvar _Speeds; // set for running times
        private static Cvar _ServerProfile;
        private static Cvar _FragLimit;
        private static Cvar _TimeLimit;
        private static Cvar _Teamplay;
        private static Cvar _SameLevel;
        private static Cvar _NoExit;
        private static Cvar _Skill; // 0 - 3
        private static Cvar _Deathmatch; // 0, 1, or 2
        private static Cvar _Coop; // 0 or 1
        private static Cvar _Pausable;
        private static Cvar _Temp1;

        private static bool _IsInitialized; // true if into command execution
        private static int _FrameCount; // incremented every frame, never reset
        private static byte[] _BasePal;
        private static byte[] _ColorMap;

        private static double _Time; // host_time

        // not bounded in any way, changed at start of every frame, never reset
        private static double _OldRealTime; // last frame run

        private static BinaryReader _VcrReader; // vcrFile
        private static BinaryWriter _VcrWriter; // vcrFile

        private static double _TimeTotal;
        private static int _TimeCount;
        private static double _Time1 = 0;
        private static double _Time2 = 0;
        private static double _Time3 = 0;

        private static int _ShutdownDepth;
        private static int _ErrorDepth;

        public static void ClearMemory()
        {
            Con.DPrint( "Clearing memory\n" );

            Mod.ClearAll();
            Client.cls.signon = 0;
            Server.sv.Clear();
            Client.cl.Clear();
        }

        public static void ServerFrame()
        {
            // run the world state
            Progs.GlobalStruct.frametime = (float)Host.FrameTime;

            // set the time and clear the general datagram
            Server.ClearDatagram();

            // check for new clients
            Server.CheckForNewClients();

            // read client messages
            Server.RunClients();

            // move things around and think always pause in single player if in console or menus
            if( !Server.sv.paused && ( Server.svs.maxclients > 1 || Key.Destination == keydest_t.key_game ) )
            {
                Server.Physics();
            }

            // send all messages to the clients
            Server.SendClientMessages();
        }

        public static void Init( quakeparms_t parms )
        {
            _Params = parms;

            Cache.Init( 1024 * 1024 * 16 ); // debug
            Cbuf.Init();
            Cmd.Init();
            View.Init();
            Chase.Init();
            InitVCR( parms );
            Common.Init( parms.basedir, parms.argv );
            InitLocal();
            Wad.LoadWadFile( "gfx.wad" );
            Key.Init();
            Con.Init();
            Menu.Init();
            Progs.Init();
            Mod.Init();
            Net.Init();
            Server.Init();

            Render.InitTextures(); // needed even for dedicated servers

            if( Client.cls.state != cactive_t.ca_dedicated )
            {
                _BasePal = Common.LoadFile( "gfx/palette.lmp" );
                if( _BasePal == null )
                {
                    Sys.Error( "Couldn't load gfx/palette.lmp" );
                }

                _ColorMap = Common.LoadFile( "gfx/colormap.lmp" );
                if( _ColorMap == null )
                {
                    Sys.Error( "Couldn't load gfx/colormap.lmp" );
                }

                // on non win32, mouse comes before video for security reasons
                Input.Init();
                Vid.Init( _BasePal );
                Drawer.Init();
                Scr.Init();
                Render.Init();
                Sound.Init();
                CDAudio.Init();
                Sbar.Init();
                Client.Init();
            }

            Cbuf.InsertText( "exec quake.rc\n" );

            _IsInitialized = true;

            Con.DPrint( "========Quake Initialized=========\n" );
        }

        public static void Shutdown()
        {
            _ShutdownDepth++;
            try
            {
                if( _ShutdownDepth > 1 )
                {
                    return;
                }

                // keep Con_Printf from trying to update the screen
                Scr.IsDisabledForLoading = true;

                WriteConfiguration();

                CDAudio.Shutdown();
                Net.Shutdown();
                Sound.Shutdown();
                Input.Shutdown();

                if( _VcrWriter != null )
                {
                    Con.Print( "Closing vcrfile.\n" );
                    _VcrWriter.Close();
                    _VcrWriter = null;
                }
                if( _VcrReader != null )
                {
                    Con.Print( "Closing vcrfile.\n" );
                    _VcrReader.Close();
                    _VcrReader = null;
                }

                if( Client.cls.state != cactive_t.ca_dedicated )
                {
                    Vid.Shutdown();
                }

                Con.Shutdown();
            }
            finally
            {
                _ShutdownDepth--;
            }
        }

        public static void Error( string error, params object[] args )
        {
            _ErrorDepth++;
            try
            {
                if( _ErrorDepth > 1 )
                {
                    Sys.Error( "Host_Error: recursively entered. " + error, args );
                }

                Scr.EndLoadingPlaque(); // reenable screen updates

                string message = ( args.Length > 0 ? string.Format( error, args ) : error );
                Con.Print( "Host_Error: {0}\n", message );

                if( Server.sv.active )
                {
                    ShutdownServer( false );
                }

                if( Client.cls.state == cactive_t.ca_dedicated )
                {
                    Sys.Error( "Host_Error: {0}\n", message ); // dedicated servers exit
                }

                Client.Disconnect();
                Client.cls.demonum = -1;

                throw new EndGameException();
            }
            finally
            {
                _ErrorDepth--;
            }
        }

        public static void EndGame( string message, params object[] args )
        {
            string str = string.Format( message, args );
            Con.DPrint( "Host_EndGame: {0}\n", str );

            if( Server.IsActive )
            {
                Host.ShutdownServer( false );
            }

            if( Client.cls.state == cactive_t.ca_dedicated )
            {
                Sys.Error( "Host_EndGame: {0}\n", str ); // dedicated servers exit
            }

            if( Client.cls.demonum != -1 )
            {
                Client.NextDemo();
            }
            else
            {
                Client.Disconnect();
            }

            throw new EndGameException();
        }

        public static void Frame( double time )
        {
            if( _ServerProfile.Value == 0 )
            {
                InternalFrame( time );
                return;
            }

            double time1 = Sys.GetFloatTime();
            InternalFrame( time );
            double time2 = Sys.GetFloatTime();

            _TimeTotal += time2 - time1;
            _TimeCount++;

            if( _TimeCount < 1000 )
            {
                return;
            }

            int m = (int)( _TimeTotal * 1000 / _TimeCount );
            _TimeCount = 0;
            _TimeTotal = 0;
            int c = 0;
            foreach( client_t cl in Server.svs.clients )
            {
                if( cl.active )
                {
                    c++;
                }
            }

            Con.Print( "serverprofile: {0,2:d} clients {1,2:d} msec\n", c, m );
        }

        public static void ClientCommands( string fmt, params object[] args )
        {
            string tmp = string.Format( fmt, args );
            HostClient.message.WriteByte( Protocol.svc_stufftext );
            HostClient.message.WriteString( tmp );
        }

        // This only happens at the end of a game, not between levels
        public static void ShutdownServer( bool crash )
        {
            if( !Server.IsActive )
            {
                return;
            }

            Server.sv.active = false;

            // stop all client sounds immediately
            if( Client.cls.state == cactive_t.ca_connected )
            {
                Client.Disconnect();
            }

            // flush any pending messages - like the score!!!
            double start = Sys.GetFloatTime();
            int count;
            do
            {
                count = 0;
                for( int i = 0; i < Server.svs.maxclients; i++ )
                {
                    HostClient = Server.svs.clients[i];
                    if( HostClient.active && !HostClient.message.IsEmpty )
                    {
                        if( Net.CanSendMessage( HostClient.netconnection ) )
                        {
                            Net.SendMessage( HostClient.netconnection, HostClient.message );
                            HostClient.message.Clear();
                        }
                        else
                        {
                            Net.GetMessage( HostClient.netconnection );
                            count++;
                        }
                    }
                }
                if( ( Sys.GetFloatTime() - start ) > 3.0 )
                {
                    break;
                }
            }
            while( count > 0 );

            // make sure all the clients know we're disconnecting
            MsgWriter writer = new MsgWriter( 4 );
            writer.WriteByte( Protocol.svc_disconnect );
            count = Net.SendToAll( writer, 5 );
            if( count != 0 )
            {
                Con.Print( "Host_ShutdownServer: NET_SendToAll failed for {0} clients\n", count );
            }

            for( int i = 0; i < Server.svs.maxclients; i++ )
            {
                HostClient = Server.svs.clients[i];
                if( HostClient.active )
                {
                    Server.DropClient( crash );
                }
            }

            // clear structures
            Server.sv.Clear();
            for( int i = 0; i < Server.svs.clients.Length; i++ )
            {
                Server.svs.clients[i].Clear();
            }
        }

        private static void WriteConfiguration()
        {
            // dedicated servers initialize the host but don't parse and set the config.cfg cvars
            if( _IsInitialized & !Host.IsDedicated )
            {
                string path = Path.Combine( Common.GameDir, "config.cfg" );
                using( FileStream fs = Sys.FileOpenWrite( path, true ) )
                {
                    if( fs != null )
                    {
                        Key.WriteBindings( fs );
                        Cvar.WriteVariables( fs );
                    }
                }
            }
        }

        private static void InitLocal()
        {
            InitCommands();

            if( _Sys_TickRate == null )
            {
                _Sys_TickRate = new Cvar( "sys_ticrate", "0.05" );
                _Developer = new Cvar( "developer", "0" );
                _FrameRate = new Cvar( "host_framerate", "0" ); // set for slow motion
                _Speeds = new Cvar( "host_speeds", "0" ); // set for running times
                _ServerProfile = new Cvar( "serverprofile", "0" );
                _FragLimit = new Cvar( "fraglimit", "0", false, true );
                _TimeLimit = new Cvar( "timelimit", "0", false, true );
                _Teamplay = new Cvar( "teamplay", "0", false, true );
                _SameLevel = new Cvar( "samelevel", "0" );
                _NoExit = new Cvar( "noexit", "0", false, true );
                _Skill = new Cvar( "skill", "1" ); // 0 - 3
                _Deathmatch = new Cvar( "deathmatch", "0" ); // 0, 1, or 2
                _Coop = new Cvar( "coop", "0" ); // 0 or 1
                _Pausable = new Cvar( "pausable", "1" );
                _Temp1 = new Cvar( "temp1", "0" );
            }

            FindMaxClients();

            _Time = 1.0; // so a think at time 0 won't get called
        }

        private static void FindMaxClients()
        {
            server_static_t svs = Server.svs;
            client_static_t cls = Client.cls;

            svs.maxclients = 1;

            int i = Common.CheckParm( "-dedicated" );
            if( i > 0 )
            {
                cls.state = cactive_t.ca_dedicated;
                if( i != ( Common.Argc - 1 ) )
                {
                    svs.maxclients = Common.atoi( Common.Argv( i + 1 ) );
                }
                else
                {
                    svs.maxclients = 8;
                }
            }
            else
            {
                cls.state = cactive_t.ca_disconnected;
            }

            i = Common.CheckParm( "-listen" );
            if( i > 0 )
            {
                if( cls.state == cactive_t.ca_dedicated )
                {
                    Sys.Error( "Only one of -dedicated or -listen can be specified" );
                }

                if( i != ( Common.Argc - 1 ) )
                {
                    svs.maxclients = Common.atoi( Common.Argv( i + 1 ) );
                }
                else
                {
                    svs.maxclients = 8;
                }
            }
            if( svs.maxclients < 1 )
            {
                svs.maxclients = 8;
            }
            else if( svs.maxclients > QDef.MAX_SCOREBOARD )
            {
                svs.maxclients = QDef.MAX_SCOREBOARD;
            }

            svs.maxclientslimit = svs.maxclients;
            if( svs.maxclientslimit < 4 )
            {
                svs.maxclientslimit = 4;
            }

            svs.clients = new client_t[svs.maxclientslimit];
            for( i = 0; i < svs.clients.Length; i++ )
            {
                svs.clients[i] = new client_t();
            }

            if( svs.maxclients > 1 )
            {
                Cvar.Set( "deathmatch", 1.0f );
            }
            else
            {
                Cvar.Set( "deathmatch", 0.0f );
            }
        }

        private static void InitVCR( quakeparms_t parms )
        {
            if( Common.HasParam( "-playback" ) )
            {
                if( Common.Argc != 2 )
                {
                    Sys.Error( "No other parameters allowed with -playback\n" );
                }

                Stream file = Sys.FileOpenRead( "quake.vcr" );
                if( file == null )
                {
                    Sys.Error( "playback file not found\n" );
                }

                _VcrReader = new BinaryReader( file, Encoding.ASCII );
                int signature = _VcrReader.ReadInt32();
                if( signature != Host.VCR_SIGNATURE )
                {
                    Sys.Error( "Invalid signature in vcr file\n" );
                }

                int argc = _VcrReader.ReadInt32();
                string[] argv = new string[argc + 1];
                argv[0] = parms.argv[0];

                for( int i = 1; i < argv.Length; i++ )
                {
                    argv[i] = Sys.ReadString( _VcrReader );
                }
                Common.Args = argv;
                parms.argv = argv;
            }

            int n = Common.CheckParm( "-record" );
            if( n != 0 )
            {
                Stream file = Sys.FileOpenWrite( "quake.vcr" );
                _VcrWriter = new BinaryWriter( file, Encoding.ASCII );

                _VcrWriter.Write( VCR_SIGNATURE );
                _VcrWriter.Write( Common.Argc - 1 );
                for( int i = 1; i < Common.Argc; i++ )
                {
                    if( i == n )
                    {
                        Sys.WriteString( _VcrWriter, "-playback" );
                        continue;
                    }
                    Sys.WriteString( _VcrWriter, Common.Argv( i ) );
                }
            }
        }

        private static void InternalFrame( double time )
        {
            // keep the random time dependent
            Sys.Random();

            // decide the simulation time
            if( !FilterTime( time ) )
            {
                return;   // don't run too fast, or packets will flood out
            }

            // get new key events
            Sys.SendKeyEvents();

            // allow mice or other external controllers to add commands
            Input.Commands();

            // process console commands
            Cbuf.Execute();

            Net.Poll();

            // if running the server locally, make intentions now
            if( Server.sv.active )
            {
                Client.SendCmd();
            }

            /*************************
             * server operations
             */

            // check for commands typed to the host
            GetConsoleCommands();

            if( Server.sv.active )
            {
                ServerFrame();
            }

            /*************************
             * client operations
             */

            // if running the server remotely, send intentions now after the incoming messages have
            // been read
            if( !Server.sv.active )
            {
                Client.SendCmd();
            }

            _Time += FrameTime;

            // fetch results from server
            if( Client.cls.state == cactive_t.ca_connected )
            {
                Client.ReadFromServer();
            }

            // update video
            if( _Speeds.Value != 0 )
            {
                _Time1 = Sys.GetFloatTime();
            }

            Scr.UpdateScreen();

            if( _Speeds.Value != 0 )
            {
                _Time2 = Sys.GetFloatTime();
            }

            // update audio
            if( Client.cls.signon == Client.SIGNONS )
            {
                Sound.Update( ref Render.Origin, ref Render.ViewPn, ref Render.ViewRight, ref Render.ViewUp );
                Client.DecayLights();
            }
            else
            {
                Sound.Update( ref Common.ZeroVector, ref Common.ZeroVector, ref Common.ZeroVector, ref Common.ZeroVector );
            }

            CDAudio.Update();

            if( _Speeds.Value != 0 )
            {
                int pass1 = (int)( ( _Time1 - _Time3 ) * 1000 );
                _Time3 = Sys.GetFloatTime();
                int pass2 = (int)( ( _Time2 - _Time1 ) * 1000 );
                int pass3 = (int)( ( _Time3 - _Time2 ) * 1000 );
                Con.Print( "{0,3} tot {1,3} server {2,3} gfx {3,3} snd\n", pass1 + pass2 + pass3, pass1, pass2, pass3 );
            }

            _FrameCount++;
        }

        private static bool FilterTime( double time )
        {
            Host.RealTime += time;

            if( !Client.cls.timedemo && RealTime - _OldRealTime < 1.0 / 72.0 )
            {
                return false; // framerate is too high
            }

            FrameTime = RealTime - _OldRealTime;
            _OldRealTime = RealTime;

            if( _FrameRate.Value > 0 )
            {
                FrameTime = _FrameRate.Value;
            }
            else
            {
                // don't allow really long or short frames
                if( FrameTime > 0.1 )
                {
                    FrameTime = 0.1;
                }

                if( FrameTime < 0.001 )
                {
                    FrameTime = 0.001;
                }
            }

            return true;
        }

        // Add them exactly as if they had been typed at the console
        private static void GetConsoleCommands()
        {
            while( true )
            {
                string cmd = Sys.ConsoleInput();
                if( string.IsNullOrEmpty( cmd ) )
                {
                    break;
                }

                Cbuf.AddText( cmd );
            }
        }
    }
}
