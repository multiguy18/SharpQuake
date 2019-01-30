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
using OpenTK;

namespace SharpQuake
{
    internal enum server_state_t
    {
        Loading,
        Active
    }

    internal static partial class Server
    {
        public static server_t sv
        {
            get
            {
                return _Server;
            }
        }

        public static server_static_t svs
        {
            get
            {
                return _ServerStatic;
            }
        }

        public static bool IsActive
        {
            get
            {
                return _Server.active;
            }
        }

        public static float Gravity
        {
            get
            {
                return _Gravity.Value;
            }
        }

        public static bool IsLoading
        {
            get
            {
                return _Server.state == server_state_t.Loading;
            }
        }

        public static float Aim
        {
            get
            {
                return _Aim.Value;
            }
        }

        public const int NUM_PING_TIMES = 16;
        public const int NUM_SPAWN_PARMS = 16;

        private static Cvar _Friction;
        private static Cvar _EdgeFriction;
        private static Cvar _StopSpeed;
        private static Cvar _Gravity;
        private static Cvar _MaxVelocity;
        private static Cvar _NoStep;
        private static Cvar _MaxSpeed;
        private static Cvar _Accelerate;
        private static Cvar _Aim;
        private static Cvar _IdealPitchScale;

        private static server_t _Server;
        private static server_static_t _ServerStatic;

        private static string[] _LocalModels = new string[QDef.MAX_MODELS]; // inline model names for precache

        public static edict_t EdictNum( int n )
        {
            if( n < 0 || n >= _Server.max_edicts )
            {
                Sys.Error( "EDICT_NUM: bad number {0}", n );
            }

            return _Server.edicts[n];
        }

        /* Either finds a free edict, or allocates a new one.
         * Try to avoid reusing an entity that was recently freed, because it
         * can cause the client to think the entity morphed into something else
         * instead of being removed and recreated, which can cause interpolated
         * angles and bad trails.
         */

        public static edict_t AllocEdict()
        {
            edict_t e;
            int i;
            for( i = svs.maxclients + 1; i < sv.num_edicts; i++ )
            {
                e = EdictNum( i );

                // the first couple seconds of server time can involve a lot of freeing and
                // allocating, so relax the replacement policy
                if( e.free && ( e.freetime < 2 || sv.time - e.freetime > 0.5 ) )
                {
                    e.Clear();
                    return e;
                }
            }

            if( i == QDef.MAX_EDICTS )
            {
                Sys.Error( "ED_Alloc: no free edicts" );
            }

            sv.num_edicts++;
            e = EdictNum( i );
            e.Clear();

            return e;
        }

        // FIXME: walk all entities and NULL out references to this entity
        public static void FreeEdict( edict_t ed )
        {
            UnlinkEdict( ed ); // unlink from world bsp

            ed.free = true;
            ed.v.model = 0;
            ed.v.takedamage = 0;
            ed.v.modelindex = 0;
            ed.v.colormap = 0;
            ed.v.skin = 0;
            ed.v.frame = 0;
            ed.v.origin = default( v3f );
            ed.v.angles = default( v3f );
            ed.v.nextthink = -1;
            ed.v.solid = 0;

            ed.freetime = (float)sv.time;
        }

        public static int EdictToProg( edict_t e )
        {
            return Array.IndexOf( _Server.edicts, e ); // todo: optimize this
        }

        // Offset in bytes!
        public static edict_t ProgToEdict( int e )
        {
            if( e < 0 || e > sv.edicts.Length )
            {
                Sys.Error( "ProgToEdict: Bad prog!" );
            }

            return sv.edicts[e];
        }

        public static int NumForEdict( edict_t e )
        {
            int i = Array.IndexOf( sv.edicts, e ); // todo: optimize this

            if( i < 0 )
            {
                Sys.Error( "NUM_FOR_EDICT: bad pointer" );
            }

            return i;
        }

        static Server()
        {
            _Server = new server_t();
            _ServerStatic = new server_static_t();
        }
    }

    internal static class Movetypes
    {
        public const int MOVETYPE_NONE = 0; // never moves
        public const int MOVETYPE_ANGLENOCLIP = 1;
        public const int MOVETYPE_ANGLECLIP = 2;
        public const int MOVETYPE_WALK = 3; // gravity
        public const int MOVETYPE_STEP = 4; // gravity, special edge handling
        public const int MOVETYPE_FLY = 5;
        public const int MOVETYPE_TOSS = 6; // gravity
        public const int MOVETYPE_PUSH = 7; // no clip to world, push and crush
        public const int MOVETYPE_NOCLIP = 8;
        public const int MOVETYPE_FLYMISSILE = 9; // extra size to monsters
        public const int MOVETYPE_BOUNCE = 10;
#if QUAKE2
        public const int MOVETYPE_BOUNCEMISSILE = 11;  // bounce w/o gravity
        public const int MOVETYPE_FOLLOW = 12;  // track movement of aiment
#endif
    }

    internal static class Solids
    {
        public const int SOLID_NOT = 0; // no interaction with other objects
        public const int SOLID_TRIGGER = 1; // touch on edge, but not blocking
        public const int SOLID_BBOX = 2; // touch on edge, block
        public const int SOLID_SLIDEBOX = 3; // touch on edge, but not an onground
        public const int SOLID_BSP = 4; // bsp clip, touch on edge, block
    }

    internal static class DeadFlags
    {
        public const int DEAD_NO = 0;
        public const int DEAD_DYING = 1;
        public const int DEAD_DEAD = 2;
    }

    internal static class Damages
    {
        public const int DAMAGE_NO = 0;
        public const int DAMAGE_YES = 1;
        public const int DAMAGE_AIM = 2;
    }

    internal static class EdictFlags
    {
        public const int FL_FLY = 1;
        public const int FL_SWIM = 2;
        public const int FL_CONVEYOR = 4;
        public const int FL_CLIENT = 8;
        public const int FL_INWATER = 16;
        public const int FL_MONSTER = 32;
        public const int FL_GODMODE = 64;
        public const int FL_NOTARGET = 128;
        public const int FL_ITEM = 256;
        public const int FL_ONGROUND = 512;
        public const int FL_PARTIALGROUND = 1024; // not all corners are valid
        public const int FL_WATERJUMP = 2048; // player jumping out of water
        public const int FL_JUMPRELEASED = 4096; // for jump debouncing

#if QUAKE2
        public const int FL_FLASHLIGHT = 8192;
        public const int FL_ARCHIVE_OVERRIDE = 1048576;
#endif
    }

    internal static class SpawnFlags
    {
        public const int SPAWNFLAG_NOT_EASY = 256;
        public const int SPAWNFLAG_NOT_MEDIUM = 512;
        public const int SPAWNFLAG_NOT_HARD = 1024;
        public const int SPAWNFLAG_NOT_DEATHMATCH = 2048;
    }

    internal class areanode_t
    {
        public int axis; // -1 = leaf node
        public float dist;
        public areanode_t[] children;
        public link_t trigger_edicts;
        public link_t solid_edicts;

        public void Clear()
        {
            axis = 0;
            dist = 0;
            children[0] = null;
            children[1] = null;
            trigger_edicts.ClearToNulls();
            solid_edicts.ClearToNulls();
        }

        public areanode_t()
        {
            children = new areanode_t[2];
            trigger_edicts = new link_t( this );
            solid_edicts = new link_t( this );
        }
    }

    internal class server_static_t
    {
        public int maxclients;
        public int maxclientslimit;
        public client_t[] clients;
        public int serverflags; // episode completion information
        public bool changelevel_issued; // cleared when at SV_SpawnServer
    }

    internal class server_t
    {
        public bool active; // false if only a net client
        public bool paused;
        public bool loadgame; // handle connections specially
        public double time;
        public int lastcheck; // used by PF_checkclient
        public double lastchecktime;
        public string name; // map name
        public string modelname; // maps/<name>.bsp, for model_precache[0]
        public model_t worldmodel;
        public string[] model_precache; // NULL terminated
        public model_t[] models;
        public string[] sound_precache; // NULL terminated
        public string[] lightstyles;
        public int num_edicts;
        public int max_edicts;
        public edict_t[] edicts;
        /* ^ can NOT be array indexed, because edict_t is variable sized, but can
         * be used to reference the world ent
        */
        public server_state_t state; // some actions are only valid during load

        public MsgWriter datagram;
        public MsgWriter reliable_datagram; // copied to all clients at end of frame
        public MsgWriter signon;

        public void Clear()
        {
            active = false;
            paused = false;
            loadgame = false;
            time = 0;
            lastcheck = 0;
            lastchecktime = 0;
            name = null;
            modelname = null;
            worldmodel = null;
            Array.Clear( model_precache, 0, model_precache.Length );
            Array.Clear( models, 0, models.Length );
            Array.Clear( sound_precache, 0, sound_precache.Length );
            Array.Clear( lightstyles, 0, lightstyles.Length );
            num_edicts = 0;
            max_edicts = 0;
            edicts = null;
            state = 0;
            datagram.Clear();
            reliable_datagram.Clear();
            signon.Clear();
        }

        public server_t()
        {
            model_precache = new string[QDef.MAX_MODELS];
            models = new model_t[QDef.MAX_MODELS];
            sound_precache = new string[QDef.MAX_SOUNDS];
            lightstyles = new string[QDef.MAX_LIGHTSTYLES];
            datagram = new MsgWriter( QDef.MAX_DATAGRAM );
            reliable_datagram = new MsgWriter( QDef.MAX_DATAGRAM );
            signon = new MsgWriter( 8192 );
        }
    }

    internal class client_t
    {
        public bool active; // false = client is free
        public bool spawned; // false = don't send datagrams
        public bool dropasap; // has been told to go to another level
        public bool privileged; // can execute any host command
        public bool sendsignon; // only valid before spawned

        public double last_message; // reliable messages must be sent

        public qsocket_t netconnection; // communications handle

        public usercmd_t cmd; // movement
        public Vector3 wishdir; // intended motion calced from cmd

        public MsgWriter message;

        public edict_t edict; // EDICT_NUM(clientnum+1)
        public string name; // for printing to other people
        public int colors;

        public float[] ping_times;
        public int num_pings;

        // spawn parms are carried from level to level
        public float[] spawn_parms;

        // client known data for deltas
        public int old_frags;

        public void Clear()
        {
            active = false;
            spawned = false;
            dropasap = false;
            privileged = false;
            sendsignon = false;
            last_message = 0;
            netconnection = null;
            cmd.Clear();
            wishdir = Vector3.Zero;
            message.Clear();
            edict = null;
            name = null;
            colors = 0;
            Array.Clear( ping_times, 0, ping_times.Length );
            num_pings = 0;
            Array.Clear( spawn_parms, 0, spawn_parms.Length );
            old_frags = 0;
        }

        public client_t()
        {
            ping_times = new float[Server.NUM_PING_TIMES];
            spawn_parms = new float[Server.NUM_SPAWN_PARMS];
            message = new MsgWriter( QDef.MAX_MSGLEN );
        }
    }
}
