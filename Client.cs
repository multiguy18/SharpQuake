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
    internal struct lightstyle_t
    {
        public string map;
    }

    internal enum cactive_t
    {
        ca_dedicated,   // a dedicated server with no ability to start a client
        ca_disconnected,  // full screen console with no connection
        ca_connected  // valid netcon, talking to a server
    }

    internal struct usercmd_t
    {
        public Vector3 viewangles;

        // intended velocities
        public float forwardmove;

        public float sidemove;
        public float upmove;

        public void Clear()
        {
            viewangles = Vector3.Zero;
            forwardmove = 0;
            sidemove = 0;
            upmove = 0;
        }
    }

    internal struct kbutton_t
    {
        public bool IsDown
        {
            get
            {
                return ( state & 1 ) != 0;
            }
        }

        public int down0; // key nums holding this button down
        public int down1;
        public int state; // low bit is down state
    }

    internal static partial class Client
    {
        public static client_static_t cls
        {
            get
            {
                return _Static;
            }
        }

        public static client_state_t cl
        {
            get
            {
                return _State;
            }
        }

        public static entity_t[] Entities
        {
            get
            {
                return _Entities;
            }
        }

        public static entity_t ViewEntity
        {
            get
            {
                return _Entities[_State.viewentity];
            }
        }

        public static entity_t ViewEnt
        {
            get
            {
                return _State.viewent;
            }
        }

        public static float ForwardSpeed
        {
            get
            {
                return _ForwardSpeed.Value;
            }
        }

        public static bool LookSpring
        {
            get
            {
                return ( _LookSpring.Value != 0 );
            }
        }

        public static bool LookStrafe
        {
            get
            {
                return ( _LookStrafe.Value != 0 );
            }
        }

        public static dlight_t[] DLights
        {
            get
            {
                return _DLights;
            }
        }

        public static lightstyle_t[] LightStyle
        {
            get
            {
                return _LightStyle;
            }
        }

        public static entity_t[] VisEdicts
        {
            get
            {
                return _VisEdicts;
            }
        }

        public static float Sensitivity
        {
            get
            {
                return _Sensitivity.Value;
            }
        }

        public static float MSide
        {
            get
            {
                return _MSide.Value;
            }
        }

        public static float MYaw
        {
            get
            {
                return _MYaw.Value;
            }
        }

        public static float MPitch
        {
            get
            {
                return _MPitch.Value;
            }
        }

        public static float MForward
        {
            get
            {
                return _MForward.Value;
            }
        }

        public static string Name
        {
            get
            {
                return _Name.String;
            }
        }

        public static float Color
        {
            get
            {
                return _Color.Value;
            }
        }

        public const int SIGNONS = 4; // signon messages to receive before connected
        public const int MAX_DLIGHTS = 32;
        public const int MAX_BEAMS = 24;
        public const int MAX_EFRAGS = 640;
        public const int MAX_MAPSTRING = 2048;
        public const int MAX_DEMOS = 8;
        public const int MAX_DEMONAME = 16;
        public const int MAX_VISEDICTS = 256;

        public static int NumVisEdicts;
        private const int MAX_TEMP_ENTITIES = 64; // lightning bolts, etc
        private const int MAX_STATIC_ENTITIES = 128; // torches, etc

        private static client_static_t _Static = new client_static_t();
        private static client_state_t _State = new client_state_t();

        private static efrag_t[] _EFrags = new efrag_t[MAX_EFRAGS];
        private static entity_t[] _Entities = new entity_t[QDef.MAX_EDICTS];
        private static entity_t[] _StaticEntities = new entity_t[MAX_STATIC_ENTITIES];
        private static lightstyle_t[] _LightStyle = new lightstyle_t[QDef.MAX_LIGHTSTYLES];
        private static dlight_t[] _DLights = new dlight_t[MAX_DLIGHTS];

        private static Cvar _Name;
        private static Cvar _Color;
        private static Cvar _ShowNet; // can be 0, 1, or 2
        private static Cvar _NoLerp;
        private static Cvar _LookSpring;
        private static Cvar _LookStrafe;
        private static Cvar _Sensitivity;
        private static Cvar _MPitch;
        private static Cvar _MYaw;
        private static Cvar _MForward;
        private static Cvar _MSide;
        private static Cvar _UpSpeed;
        private static Cvar _ForwardSpeed;
        private static Cvar _BackSpeed;
        private static Cvar _SideSpeed;
        private static Cvar _MoveSpeedKey;
        private static Cvar _YawSpeed;
        private static Cvar _PitchSpeed;
        private static Cvar _AngleSpeedKey;

        private static entity_t[] _VisEdicts = new entity_t[MAX_VISEDICTS];
    }

    internal static class ColorShift
    {
        public const int CSHIFT_CONTENTS = 0;
        public const int CSHIFT_DAMAGE = 1;
        public const int CSHIFT_BONUS = 2;
        public const int CSHIFT_POWERUP = 3;
        public const int NUM_CSHIFTS = 4;
    }

    internal class scoreboard_t
    {
        public string name;
        public int frags;
        public int colors; // two 4 bit fields
        public byte[] translations;

        public scoreboard_t()
        {
            translations = new byte[Vid.VID_GRADES * 256];
        }
    }

    internal class cshift_t
    {
        public int[] destcolor;
        public int percent; // 0-256

        public void Clear()
        {
            destcolor[0] = 0;
            destcolor[1] = 0;
            destcolor[2] = 0;
            percent = 0;
        }

        public cshift_t()
        {
            destcolor = new int[3];
        }

        public cshift_t( int[] destColor, int percent )
        {
            if( destColor.Length != 3 )
            {
                throw new ArgumentException( "destColor must have length of 3 elements!" );
            }
            destcolor = destColor;
            this.percent = percent;
        }
    }

    internal class dlight_t
    {
        public Vector3 origin;
        public float radius;
        public float die; // stop lighting after this time
        public float decay; // drop this each second
        public float minlight; // don't add when contributing less
        public int key;

        public void Clear()
        {
            origin = Vector3.Zero;
            radius = 0;
            die = 0;
            decay = 0;
            minlight = 0;
            key = 0;
        }
    }

    internal class beam_t
    {
        public int entity;
        public model_t model;
        public float endtime;
        public Vector3 start;
        public Vector3 end;

        public void Clear()
        {
            entity = 0;
            model = null;
            endtime = 0;
            start = Vector3.Zero;
            end = Vector3.Zero;
        }
    }

    internal class client_static_t
    {
        public cactive_t state;

        // personalization data sent to server
        public string mapstring;

        public string spawnparms; // to restart a level

        // demo loop control
        public int demonum; // -1 = don't play demos

        public string[] demos; // when not playing

        // demo recording info must be here, because record is started before entering a map (and
        // clearing client_state_t)
        public bool demorecording;

        public bool demoplayback;
        public bool timedemo;
        public int forcetrack; // -1 = use normal cd track
        public IDisposable demofile;
        public int td_lastframe; // to meter out one message a frame
        public int td_startframe; // host_framecount at start
        public float td_starttime; // realtime at second frame of timedemo

        // connection information
        public int signon; // 0 to SIGNONS

        public qsocket_t netcon;
        public MsgWriter message; // writing buffer to send to server

        public client_static_t()
        {
            demos = new string[Client.MAX_DEMOS];
            message = new MsgWriter( 1024 ); // like in Client_Init()
        }
    }

    /* the client_state_t structure is wiped completely at every server signon
     */

    internal class client_state_t
    {
        public int movemessages; // since connecting to this server
        public usercmd_t cmd; // last command sent to the server

        // information for local display
        public int[] stats; // health, etc

        public int items; // inventory bit flags
        public float[] item_gettime; // cl.time of aquiring item, for blinking
        public float faceanimtime; // use anim frame if cl.time < this

        public cshift_t[] cshifts; // color shifts for damage, powerups
        public cshift_t[] prev_cshifts;  // and content types

        // the client maintains its own idea of view angles, which are sent to the server each frame.
        // The server sets punchangle when the view is temporarliy offset, and an angle reset
        // commands at the start of each level and after teleporting.
        public Vector3[] mviewangles; // during demo playback viewangles is lerped

        // ^ between these v
        public Vector3 viewangles;

        public Vector3[] mvelocity; // update by server, used for lean+bob

        // (0 is newest)
        public Vector3 velocity; // lerped between mvelocity[0] and [1]

        public Vector3 punchangle; // temporary offset

        // pitch drifting vars
        public float idealpitch;

        public float pitchvel;
        public bool nodrift;
        public float driftmove;
        public double laststop;

        public float viewheight;
        public float crouch; // local amount for smoothing stepups

        public bool paused; // send over by server
        public bool onground;
        public bool inwater;

        public int intermission; // don't change view angle, full screen, etc
        public int completed_time; // latched at intermission start

        public double[] mtime; // the timestamp of last two messages
        public double time; // clients view of time, should be between

        // servertime and oldservertime to generate a lerp point for other data
        public double oldtime; // previous cl.time, time-oldtime is used to decay light values and smooth step ups

        public float last_received_message; // (realtime) for net trouble icon

        /* information that is static for the entire time connected to a server
        */
        public model_t[] model_precache;
        public sfx_t[] sound_precache;

        public string levelname; // for display on solo scoreboard
        public int viewentity;
        public int maxclients;
        public int gametype;

        // refresh related state
        public model_t worldmodel;

        public efrag_t free_efrags; // first free efrag in list
        public int num_entities; // held in cl_entities array
        public int num_statics; // held in cl_staticentities array
        public entity_t viewent; // the gun model

        public int cdtrack, looptrack; // cd audio

        // frag scoreboard
        public scoreboard_t[] scores;

        public bool HasItems( int item )
        {
            return ( items & item ) == item;
        }

        public void Clear()
        {
            movemessages = 0;
            cmd.Clear();
            Array.Clear( stats, 0, stats.Length );
            items = 0;
            Array.Clear( item_gettime, 0, item_gettime.Length );
            faceanimtime = 0;

            foreach( cshift_t cs in cshifts )
            {
                cs.Clear();
            }

            foreach( cshift_t cs in prev_cshifts )
            {
                cs.Clear();
            }

            mviewangles[0] = Vector3.Zero;
            mviewangles[1] = Vector3.Zero;
            viewangles = Vector3.Zero;
            mvelocity[0] = Vector3.Zero;
            mvelocity[1] = Vector3.Zero;
            velocity = Vector3.Zero;
            punchangle = Vector3.Zero;

            idealpitch = 0;
            pitchvel = 0;
            nodrift = false;
            driftmove = 0;
            laststop = 0;

            viewheight = 0;
            crouch = 0;

            paused = false;
            onground = false;
            inwater = false;

            intermission = 0;
            completed_time = 0;

            mtime[0] = 0;
            mtime[1] = 0;
            time = 0;
            oldtime = 0;
            last_received_message = 0;

            Array.Clear( model_precache, 0, model_precache.Length );
            Array.Clear( sound_precache, 0, sound_precache.Length );

            levelname = null;
            viewentity = 0;
            maxclients = 0;
            gametype = 0;

            worldmodel = null;
            free_efrags = null;
            num_entities = 0;
            num_statics = 0;
            viewent.Clear();

            cdtrack = 0;
            looptrack = 0;

            scores = null;
        }

        public client_state_t()
        {
            stats = new int[QStats.MAX_CL_STATS];
            item_gettime = new float[32];

            cshifts = new cshift_t[ColorShift.NUM_CSHIFTS];
            for( int i = 0; i < ColorShift.NUM_CSHIFTS; i++ )
            {
                cshifts[i] = new cshift_t();
            }

            prev_cshifts = new cshift_t[ColorShift.NUM_CSHIFTS];
            for( int i = 0; i < ColorShift.NUM_CSHIFTS; i++ )
            {
                prev_cshifts[i] = new cshift_t();
            }

            mviewangles = new Vector3[2];
            mvelocity = new Vector3[2];
            mtime = new double[2];
            model_precache = new model_t[QDef.MAX_MODELS];
            sound_precache = new sfx_t[QDef.MAX_SOUNDS];
            viewent = new entity_t();
        }
    }
}
