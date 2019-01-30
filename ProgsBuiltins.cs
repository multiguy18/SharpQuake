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
using System.Text;
using OpenTK;

namespace SharpQuake
{
    internal static class QBuiltins
    {
        public static int Count
        {
            get
            {
                return _Builtin.Length;
            }
        }

        private static MsgWriter WriteDest
        {
            get
            {
                int dest = (int)GetFloat( OFS.OFS_PARM0 );
                switch( dest )
                {
                    case MSG_BROADCAST:
                        return Server.sv.datagram;

                    case MSG_ONE:
                        edict_t ent = Server.ProgToEdict( Progs.GlobalStruct.msg_entity );
                        int entnum = Server.NumForEdict( ent );
                        if( entnum < 1 || entnum > Server.svs.maxclients )
                        {
                            Progs.RunError( "WriteDest: not a client" );
                        }

                        return Server.svs.clients[entnum - 1].message;

                    case MSG_ALL:
                        return Server.sv.reliable_datagram;

                    case MSG_INIT:
                        return Server.sv.signon;

                    default:
                        Progs.RunError( "WriteDest: bad destination" );
                        break;
                }

                return null;
            }
        }

        private const int MAX_CHECK = 16;

        private const int MSG_BROADCAST = 0; // unreliable to all
        private const int MSG_ONE = 1;  // reliable to one (msg_entity)
        private const int MSG_ALL = 2;  // reliable to all
        private const int MSG_INIT = 3;  // write to the init string

        private static builtin_t[] _Builtin = new builtin_t[]
        {
            PF_Fixme,
            PF_makevectors,
            PF_setorigin,
            PF_setmodel,
            PF_setsize,
            PF_Fixme,
            PF_break,
            PF_random,
            PF_sound,
            PF_normalize,
            PF_error,
            PF_objerror,
            PF_vlen,
            PF_vectoyaw,
            PF_Spawn,
            PF_Remove,
            PF_traceline,
            PF_checkclient,
            PF_Find,
            PF_precache_sound,
            PF_precache_model,
            PF_stuffcmd,
            PF_findradius,
            PF_bprint,
            PF_sprint,
            PF_dprint,
            PF_ftos,
            PF_vtos,
            PF_coredump,
            PF_traceon,
            PF_traceoff,
            PF_eprint,
            PF_walkmove,
            PF_Fixme,
            PF_droptofloor,
            PF_lightstyle,
            PF_rint,
            PF_floor,
            PF_ceil,
            PF_Fixme,
            PF_checkbottom,
            PF_pointcontents,
            PF_Fixme,
            PF_fabs,
            PF_aim,
            PF_cvar,
            PF_localcmd,
            PF_nextent,
            PF_particle,
            PF_changeyaw,
            PF_Fixme,
            PF_vectoangles,

            PF_WriteByte,
            PF_WriteChar,
            PF_WriteShort,
            PF_WriteLong,
            PF_WriteCoord,
            PF_WriteAngle,
            PF_WriteString,
            PF_WriteEntity,

            PF_Fixme,
            PF_Fixme,
            PF_Fixme,
            PF_Fixme,
            PF_Fixme,
            PF_Fixme,
            PF_Fixme,

            Server.MoveToGoal,
            PF_precache_file,
            PF_makestatic,

            PF_changelevel,
            PF_Fixme,

            PF_cvar_set,
            PF_centerprint,

            PF_ambientsound,

            PF_precache_model,
            PF_precache_sound,  // precache_sound2 is different only for qcc
            PF_precache_file,

            PF_setspawnparms
        };

        private static readonly byte[] _CheckPvs = new byte[BspFile.MAX_MAP_LEAFS / 8]; // checkpvs

        private static int _TempString = -1;

        private static int _InVisCount;
        private static int _NotVisCount;

        public static void Execute( int num )
        {
            _Builtin[num]();
        }

        public static void ClearState()
        {
            _TempString = -1;
        }

        public static unsafe void ReturnEdict( edict_t e )
        {
            int prog = Server.EdictToProg( e );
            ReturnInt( prog );
        }

        public static unsafe void ReturnInt( int value )
        {
            int* ptr = (int*)Progs.GlobalStructAddr;
            ptr[OFS.OFS_RETURN] = value;
        }

        public static unsafe void ReturnFloat( float value )
        {
            float* ptr = (float*)Progs.GlobalStructAddr;
            ptr[OFS.OFS_RETURN] = value;
        }

        public static unsafe void ReturnVector( ref v3f value )
        {
            float* ptr = (float*)Progs.GlobalStructAddr;
            ptr[OFS.OFS_RETURN + 0] = value.x;
            ptr[OFS.OFS_RETURN + 1] = value.y;
            ptr[OFS.OFS_RETURN + 2] = value.z;
        }

        public static unsafe void ReturnVector( ref Vector3 value )
        {
            float* ptr = (float*)Progs.GlobalStructAddr;
            ptr[OFS.OFS_RETURN + 0] = value.X;
            ptr[OFS.OFS_RETURN + 1] = value.Y;
            ptr[OFS.OFS_RETURN + 2] = value.Z;
        }

        public static unsafe string GetString( int parm )
        {
            int* ptr = (int*)Progs.GlobalStructAddr;
            return Progs.GetString( ptr[parm] );
        }

        public static unsafe int GetInt( int parm )
        {
            int* ptr = (int*)Progs.GlobalStructAddr;
            return ptr[parm];
        }

        public static unsafe float GetFloat( int parm )
        {
            float* ptr = (float*)Progs.GlobalStructAddr;
            return ptr[parm];
        }

        public static unsafe float* GetVector( int parm )
        {
            float* ptr = (float*)Progs.GlobalStructAddr;
            return &ptr[parm];
        }

        public static unsafe edict_t GetEdict( int parm )
        {
            int* ptr = (int*)Progs.GlobalStructAddr;
            edict_t ed = Server.ProgToEdict( ptr[parm] );
            return ed;
        }

        public static void PF_changeyaw()
        {
            edict_t ent = Server.ProgToEdict( Progs.GlobalStruct.self );
            float current = Mathlib.AngleMod( ent.v.angles.y );
            float ideal = ent.v.ideal_yaw;
            float speed = ent.v.yaw_speed;

            if( current == ideal )
            {
                return;
            }

            float move = ideal - current;
            if( ideal > current )
            {
                if( move >= 180 )
                {
                    move = move - 360;
                }
            }
            else
            {
                if( move <= -180 )
                {
                    move = move + 360;
                }
            }
            if( move > 0 )
            {
                if( move > speed )
                {
                    move = speed;
                }
            }
            else
            {
                if( move < -speed )
                {
                    move = -speed;
                }
            }

            ent.v.angles.y = Mathlib.AngleMod( current + move );
        }

        private static int SetTempString( string value )
        {
            if( _TempString == -1 )
            {
                _TempString = Progs.NewString( value );
            }
            else
            {
                Progs.SetString( _TempString, value );
            }
            return _TempString;
        }

        private static string PF_VarString( int first )
        {
            StringBuilder sb = new StringBuilder( 256 );
            for( int i = first; i < Progs.Argc; i++ )
            {
                sb.Append( GetString( OFS.OFS_PARM0 + i * 3 ) );
            }
            return sb.ToString();
        }

        private static unsafe void Copy( float* src, ref v3f dest )
        {
            dest.x = src[0];
            dest.y = src[1];
            dest.z = src[2];
        }

        private static unsafe void Copy( float* src, out Vector3 dest )
        {
            dest.X = src[0];
            dest.Y = src[1];
            dest.Z = src[2];
        }

        private static void PF_error()
        {
            string s = PF_VarString( 0 );
            Con.Print( "======SERVER ERROR in {0}:\n{1}\n",
                Progs.GetString( Progs.xFunction.s_name ), s );
            edict_t ed = Server.ProgToEdict( Progs.GlobalStruct.self );
            Progs.Print( ed );
            Host.Error( "Program error" );
        }

        private static void PF_objerror()
        {
            string s = PF_VarString( 0 );
            Con.Print( "======OBJECT ERROR in {0}:\n{1}\n",
                GetString( Progs.xFunction.s_name ), s );
            edict_t ed = Server.ProgToEdict( Progs.GlobalStruct.self );
            Progs.Print( ed );
            Server.FreeEdict( ed );
            Host.Error( "Program error" );
        }

        private static unsafe void PF_makevectors()
        {
            float* av = GetVector( OFS.OFS_PARM0 );
            Vector3 a = new Vector3( av[0], av[1], av[2] );
            Mathlib.AngleVectors( ref a, out Vector3 fw, out Vector3 right, out Vector3 up );
            Mathlib.Copy( ref fw, out Progs.GlobalStruct.v_forward );
            Mathlib.Copy( ref right, out Progs.GlobalStruct.v_right );
            Mathlib.Copy( ref up, out Progs.GlobalStruct.v_up );
        }

        /* This is the only valid way to move an object without using the physics of the world (setting velocity and waiting).
         * Directly changing origin will not set internal links correctly, so clipping would be messed up.
         * This should be called when an object is spawned, and then only if it is teleported.
         * setorigin (entity, origin)
         */

        private static unsafe void PF_setorigin()
        {
            edict_t e = GetEdict( OFS.OFS_PARM0 );
            float* org = GetVector( OFS.OFS_PARM1 );
            Copy( org, ref e.v.origin );

            Server.LinkEdict( e, false );
        }

        private static void SetMinMaxSize( edict_t e, ref Vector3 min, ref Vector3 max, bool rotate )
        {
            if( min.X > max.X || min.Y > max.Y || min.Z > max.Z )
            {
                Progs.RunError( "backwards mins/maxs" );
            }

            rotate = false;  // FIXME: implement rotation properly again

            Vector3 rmin = min, rmax = max;
            /* if( !rotate )
             {
             }
             else
             {
                 // find min / max for rotations
                 angles = e.v.angles;

                 a = angles[1] / 180 * M_PI;

                 xvector[0] = cos(a);
                 xvector[1] = sin(a);
                 yvector[0] = -sin(a);
                 yvector[1] = cos(a);

                 VectorCopy(min, bounds[0]);
                 VectorCopy(max, bounds[1]);

                 rmin[0] = rmin[1] = rmin[2] = 9999;
                 rmax[0] = rmax[1] = rmax[2] = -9999;

                 for (i = 0; i <= 1; i++)
                 {
                     base[0] = bounds[i][0];
                     for (j = 0; j <= 1; j++)
                     {
                         base[1] = bounds[j][1];
                         for (k = 0; k <= 1; k++)
                         {
                             base[2] = bounds[k][2];

                             // transform the point
                             transformed[0] = xvector[0] * base[0] + yvector[0] * base[1];
                             transformed[1] = xvector[1] * base[0] + yvector[1] * base[1];
                             transformed[2] = base[2];

                             for (l = 0; l < 3; l++)
                             {
                                 if (transformed[l] < rmin[l])
                                     rmin[l] = transformed[l];
                                 if (transformed[l] > rmax[l])
                                     rmax[l] = transformed[l];
                             }
                         }
                     }
                 }
             }*/

            // set derived values
            Mathlib.Copy( ref rmin, out e.v.mins );
            Mathlib.Copy( ref rmax, out e.v.maxs );
            Vector3 s = max - min;
            Mathlib.Copy( ref s, out e.v.size );

            Server.LinkEdict( e, false );
        }

        private static unsafe void PF_setsize()
        {
            edict_t e = GetEdict( OFS.OFS_PARM0 );
            float* min = GetVector( OFS.OFS_PARM1 );
            float* max = GetVector( OFS.OFS_PARM2 );
            Copy( min, out Vector3 vmin );
            Copy( max, out Vector3 vmax );
            SetMinMaxSize( e, ref vmin, ref vmax, false );
        }

        private static void PF_setmodel()
        {
            edict_t e = GetEdict( OFS.OFS_PARM0 );
            int m_idx = GetInt( OFS.OFS_PARM1 );
            string m = Progs.GetString( m_idx );

            // check to see if model was properly precached
            for( int i = 0; i < Server.sv.model_precache.Length; i++ )
            {
                string check = Server.sv.model_precache[i];

                if( check == null )
                {
                    break;
                }

                if( check == m )
                {
                    e.v.model = m_idx;
                    e.v.modelindex = i;

                    model_t mod = Server.sv.models[(int)e.v.modelindex];

                    if( mod != null )
                    {
                        SetMinMaxSize( e, ref mod.mins, ref mod.maxs, true );
                    }
                    else
                    {
                        SetMinMaxSize( e, ref Common.ZeroVector, ref Common.ZeroVector, true );
                    }

                    return;
                }
            }

            Progs.RunError( "no precache: {0}\n", m );
        }

        private static void PF_bprint()
        {
            string s = PF_VarString( 0 );
            Server.BroadcastPrint( s );
        }

        private static void PF_sprint()
        {
            int entnum = Server.NumForEdict( GetEdict( OFS.OFS_PARM0 ) );
            string s = PF_VarString( 1 );

            if( entnum < 1 || entnum > Server.svs.maxclients )
            {
                Con.Print( "tried to sprint to a non-client\n" );
                return;
            }

            client_t client = Server.svs.clients[entnum - 1];

            client.message.WriteChar( Protocol.svc_print );
            client.message.WriteString( s );
        }

        private static void PF_centerprint()
        {
            int entnum = Server.NumForEdict( GetEdict( OFS.OFS_PARM0 ) );
            string s = PF_VarString( 1 );

            if( entnum < 1 || entnum > Server.svs.maxclients )
            {
                Con.Print( "tried to centerprint to a non-client\n" );
                return;
            }

            client_t client = Server.svs.clients[entnum - 1];

            client.message.WriteChar( Protocol.svc_centerprint );
            client.message.WriteString( s );
        }

        private static unsafe void PF_normalize()
        {
            float* value1 = GetVector( OFS.OFS_PARM0 );
            Copy( value1, out Vector3 tmp );
            Mathlib.Normalize( ref tmp );

            ReturnVector( ref tmp );
        }

        private static unsafe void PF_vlen()
        {
            float* v = GetVector( OFS.OFS_PARM0 );
            float result = (float)Math.Sqrt( v[0] * v[0] + v[1] * v[1] + v[2] * v[2] );

            ReturnFloat( result );
        }

        private static unsafe void PF_vectoyaw()
        {
            float* value1 = GetVector( OFS.OFS_PARM0 );
            float yaw;
            if( value1[1] == 0 && value1[0] == 0 )
            {
                yaw = 0;
            }
            else
            {
                yaw = (int)( Math.Atan2( value1[1], value1[0] ) * 180 / Math.PI );
                if( yaw < 0 )
                {
                    yaw += 360;
                }
            }

            ReturnFloat( yaw );
        }

        private static unsafe void PF_vectoangles()
        {
            float yaw, pitch, forward;
            float* value1 = GetVector( OFS.OFS_PARM0 );

            if( value1[1] == 0 && value1[0] == 0 )
            {
                yaw = 0;
                if( value1[2] > 0 )
                {
                    pitch = 90;
                }
                else
                {
                    pitch = 270;
                }
            }
            else
            {
                yaw = (int)( Math.Atan2( value1[1], value1[0] ) * 180 / Math.PI );
                if( yaw < 0 )
                {
                    yaw += 360;
                }

                forward = (float)Math.Sqrt( value1[0] * value1[0] + value1[1] * value1[1] );
                pitch = (int)( Math.Atan2( value1[2], forward ) * 180 / Math.PI );
                if( pitch < 0 )
                {
                    pitch += 360;
                }
            }

            Vector3 result = new Vector3( pitch, yaw, 0 );
            ReturnVector( ref result );
        }

        private static void PF_random()
        {
            float num = ( Sys.Random() & 0x7fff ) / ( (float)0x7fff );
            ReturnFloat( num );
        }

        private static unsafe void PF_particle()
        {
            float* org = GetVector( OFS.OFS_PARM0 );
            float* dir = GetVector( OFS.OFS_PARM1 );
            float color = GetFloat( OFS.OFS_PARM2 );
            float count = GetFloat( OFS.OFS_PARM3 );
            Copy( org, out Vector3 vorg );
            Copy( dir, out Vector3 vdir );
            Server.StartParticle( ref vorg, ref vdir, (int)color, (int)count );
        }

        private static unsafe void PF_ambientsound()
        {
            float* pos = GetVector( OFS.OFS_PARM0 );
            string samp = GetString( OFS.OFS_PARM1 );
            float vol = GetFloat( OFS.OFS_PARM2 );
            float attenuation = GetFloat( OFS.OFS_PARM3 );

            // check to see if samp was properly precached
            for( int i = 0; i < Server.sv.sound_precache.Length; i++ )
            {
                if( Server.sv.sound_precache[i] == null )
                {
                    break;
                }

                if( samp == Server.sv.sound_precache[i] )
                {
                    // add an svc_spawnambient command to the level signon packet
                    MsgWriter msg = Server.sv.signon;

                    msg.WriteByte( Protocol.svc_spawnstaticsound );
                    for( int i2 = 0; i2 < 3; i2++ )
                    {
                        msg.WriteCoord( pos[i2] );
                    }

                    msg.WriteByte( i );

                    msg.WriteByte( (int)( vol * 255 ) );
                    msg.WriteByte( (int)( attenuation * 64 ) );

                    return;
                }
            }

            Con.Print( "no precache: {0}\n", samp );
        }

        /* Each entity can have eight independant sound sources, like voice,
         * weapon, feet, etc.
         *
         * Channel 0 is an auto-allocate channel, the others override anything
         * allready running on that entity/channel pair.
         *
         * An attenuation of 0 will play full volume everywhere in the level.
         * Larger attenuations will drop off.
         */

        private static void PF_sound()
        {
            edict_t entity = GetEdict( OFS.OFS_PARM0 );
            int channel = (int)GetFloat( OFS.OFS_PARM1 );
            string sample = GetString( OFS.OFS_PARM2 );
            int volume = (int)( GetFloat( OFS.OFS_PARM3 ) * 255 );
            float attenuation = GetFloat( OFS.OFS_PARM4 );

            Server.StartSound( entity, channel, sample, volume, attenuation );
        }

        private static void PF_break()
        {
            Con.Print( "break statement\n" );
            //*(int *)-4 = 0; // dump to debugger
        }

        /* Used for use tracing and shot targeting
         * Traces are blocked by bbox and exact bsp entityes, and also slide box entities
         * if the tryents flag is set.
         */

        private static unsafe void PF_traceline()
        {
            float* v1 = GetVector( OFS.OFS_PARM0 );
            float* v2 = GetVector( OFS.OFS_PARM1 );
            int nomonsters = (int)GetFloat( OFS.OFS_PARM2 );
            edict_t ent = GetEdict( OFS.OFS_PARM3 );
            Copy( v1, out Vector3 vec1 );
            Copy( v2, out Vector3 vec2 );
            trace_t trace = Server.Move( ref vec1, ref Common.ZeroVector, ref Common.ZeroVector, ref vec2, nomonsters, ent );

            Progs.GlobalStruct.trace_allsolid = trace.allsolid ? 1 : 0;
            Progs.GlobalStruct.trace_startsolid = trace.startsolid ? 1 : 0;
            Progs.GlobalStruct.trace_fraction = trace.fraction;
            Progs.GlobalStruct.trace_inwater = trace.inwater ? 1 : 0;
            Progs.GlobalStruct.trace_inopen = trace.inopen ? 1 : 0;
            Mathlib.Copy( ref trace.endpos, out Progs.GlobalStruct.trace_endpos );
            Mathlib.Copy( ref trace.plane.normal, out Progs.GlobalStruct.trace_plane_normal );
            Progs.GlobalStruct.trace_plane_dist = trace.plane.dist;
            if( trace.ent != null )
            {
                Progs.GlobalStruct.trace_ent = Server.EdictToProg( trace.ent );
            }
            else
            {
                Progs.GlobalStruct.trace_ent = Server.EdictToProg( Server.sv.edicts[0] );
            }
        }

        private static int PF_newcheckclient( int check )
        {
            // cycle to the next one

            if( check < 1 )
            {
                check = 1;
            }

            if( check > Server.svs.maxclients )
            {
                check = Server.svs.maxclients;
            }

            int i = check + 1;
            if( check == Server.svs.maxclients )
            {
                i = 1;
            }

            edict_t ent;
            for( ; ; i++ )
            {
                if( i == Server.svs.maxclients + 1 )
                {
                    i = 1;
                }

                ent = Server.EdictNum( i );

                if( i == check )
                {
                    break; // didn't find anything else
                }

                if( ent.free )
                {
                    continue;
                }

                if( ent.v.health <= 0 )
                {
                    continue;
                }

                if( ( (int)ent.v.flags & EdictFlags.FL_NOTARGET ) != 0 )
                {
                    continue;
                }

                // anything that is a client, or has a client as an enemy
                break;
            }

            // get the PVS for the entity
            Vector3 org = Common.ToVector( ref ent.v.origin ) + Common.ToVector( ref ent.v.view_ofs );
            mleaf_t leaf = Mod.PointInLeaf( ref org, Server.sv.worldmodel );
            byte[] pvs = Mod.LeafPVS( leaf, Server.sv.worldmodel );
            Buffer.BlockCopy( pvs, 0, _CheckPvs, 0, pvs.Length );

            return i;
        }

        /* Returns a client (or object that has a client enemy) that would be a valid target.
         *
         * If there are more than one valid options, they are cycled each frame
         *
         * If (self.origin + self.viewofs) is not in the PVS of the current target,
         * it is not returned at all.
         */

        private static void PF_checkclient()
        {
            // find a new check if on a new frame
            if( Server.sv.time - Server.sv.lastchecktime >= 0.1 )
            {
                Server.sv.lastcheck = PF_newcheckclient( Server.sv.lastcheck );
                Server.sv.lastchecktime = Server.sv.time;
            }

            // return check if it might be visible
            edict_t ent = Server.EdictNum( Server.sv.lastcheck );
            if( ent.free || ent.v.health <= 0 )
            {
                ReturnEdict( Server.sv.edicts[0] );
                return;
            }

            // if current entity can't possibly see the check entity, return 0
            edict_t self = Server.ProgToEdict( Progs.GlobalStruct.self );
            Vector3 view = Common.ToVector( ref self.v.origin ) + Common.ToVector( ref self.v.view_ofs );
            mleaf_t leaf = Mod.PointInLeaf( ref view, Server.sv.worldmodel );
            int l = Array.IndexOf( Server.sv.worldmodel.leafs, leaf ) - 1;
            if( ( l < 0 ) || ( _CheckPvs[l >> 3] & ( 1 << ( l & 7 ) ) ) == 0 )
            {
                _NotVisCount++;
                ReturnEdict( Server.sv.edicts[0] );
                return;
            }

            // might be able to see it
            _InVisCount++;
            ReturnEdict( ent );
        }

        //============================================================================

        private static void PF_stuffcmd()
        {
            int entnum = Server.NumForEdict( GetEdict( OFS.OFS_PARM0 ) );
            if( entnum < 1 || entnum > Server.svs.maxclients )
            {
                Progs.RunError( "Parm 0 not a client" );
            }

            string str = GetString( OFS.OFS_PARM1 );

            client_t old = Host.HostClient;
            Host.HostClient = Server.svs.clients[entnum - 1];
            Host.ClientCommands( "{0}", str );
            Host.HostClient = old;
        }

        private static void PF_localcmd()
        {
            string cmd = GetString( OFS.OFS_PARM0 );
            Cbuf.AddText( cmd );
        }

        private static void PF_cvar()
        {
            string str = GetString( OFS.OFS_PARM0 );
            ReturnFloat( Cvar.GetValue( str ) );
        }

        private static void PF_cvar_set()
        {
            Cvar.Set( GetString( OFS.OFS_PARM0 ), GetString( OFS.OFS_PARM1 ) );
        }

        // Returns a chain of entities that have origins within a spherical area
        private static unsafe void PF_findradius()
        {
            edict_t chain = Server.sv.edicts[0];

            float* org = GetVector( OFS.OFS_PARM0 );
            float rad = GetFloat( OFS.OFS_PARM1 );

            Copy( org, out Vector3 vorg );

            for( int i = 1; i < Server.sv.num_edicts; i++ )
            {
                edict_t ent = Server.sv.edicts[i];
                if( ent.free )
                {
                    continue;
                }

                if( ent.v.solid == Solids.SOLID_NOT )
                {
                    continue;
                }

                Vector3 v = vorg - ( Common.ToVector( ref ent.v.origin ) +
                    ( Common.ToVector( ref ent.v.mins ) + Common.ToVector( ref ent.v.maxs ) ) * 0.5f );
                if( v.Length > rad )
                {
                    continue;
                }

                ent.v.chain = Server.EdictToProg( chain );
                chain = ent;
            }

            ReturnEdict( chain );
        }

        private static void PF_dprint()
        {
            Con.DPrint( PF_VarString( 0 ) );
        }

        private static void PF_ftos()
        {
            float v = GetFloat( OFS.OFS_PARM0 );

            if( v == (int)v )
            {
                SetTempString( string.Format( "{0}", (int)v ) );
            }
            else
            {
                SetTempString( string.Format( "{0:F1}", v ) );
            }

            ReturnInt( _TempString );
        }

        private static void PF_fabs()
        {
            float v = GetFloat( OFS.OFS_PARM0 );
            ReturnFloat( Math.Abs( v ) );
        }

        private static unsafe void PF_vtos()
        {
            float* v = GetVector( OFS.OFS_PARM0 );
            SetTempString( string.Format( "'{0,5:F1} {1,5:F1} {2,5:F1}'", v[0], v[1], v[2] ) );
            ReturnInt( _TempString );
        }

        private static void PF_Spawn()
        {
            edict_t ed = Server.AllocEdict();
            ReturnEdict( ed );
        }

        private static void PF_Remove()
        {
            edict_t ed = GetEdict( OFS.OFS_PARM0 );
            Server.FreeEdict( ed );
        }

        private static void PF_Find()
        {
            int e = GetInt( OFS.OFS_PARM0 );
            int f = GetInt( OFS.OFS_PARM1 );
            string s = GetString( OFS.OFS_PARM2 );
            if( s == null )
            {
                Progs.RunError( "PF_Find: bad search string" );
            }

            for( e++; e < Server.sv.num_edicts; e++ )
            {
                edict_t ed = Server.EdictNum( e );
                if( ed.free )
                {
                    continue;
                }

                string t = Progs.GetString( ed.GetInt( f ) );
                if( string.IsNullOrEmpty( t ) )
                {
                    continue;
                }

                if( t == s )
                {
                    ReturnEdict( ed );
                    return;
                }
            }

            ReturnEdict( Server.sv.edicts[0] );
        }

        private static void CheckEmptyString( string s )
        {
            if( s == null || s.Length == 0 || s[0] <= ' ' )
            {
                Progs.RunError( "Bad string" );
            }
        }

        private static void PF_precache_file()
        {
            // precache_file is only used to copy files with qcc, it does nothing
            ReturnInt( GetInt( OFS.OFS_PARM0 ) );
        }

        private static void PF_precache_sound()
        {
            if( !Server.IsLoading )
            {
                Progs.RunError( "PF_Precache_*: Precache can only be done in spawn functions" );
            }

            string s = GetString( OFS.OFS_PARM0 );
            ReturnInt( GetInt( OFS.OFS_PARM0 ) );
            CheckEmptyString( s );

            for( int i = 0; i < QDef.MAX_SOUNDS; i++ )
            {
                if( Server.sv.sound_precache[i] == null )
                {
                    Server.sv.sound_precache[i] = s;
                    return;
                }
                if( Server.sv.sound_precache[i] == s )
                {
                    return;
                }
            }
            Progs.RunError( "PF_precache_sound: overflow" );
        }

        private static void PF_precache_model()
        {
            if( !Server.IsLoading )
            {
                Progs.RunError( "PF_Precache_*: Precache can only be done in spawn functions" );
            }

            string s = GetString( OFS.OFS_PARM0 );
            ReturnInt( GetInt( OFS.OFS_PARM0 ) );
            CheckEmptyString( s );

            for( int i = 0; i < QDef.MAX_MODELS; i++ )
            {
                if( Server.sv.model_precache[i] == null )
                {
                    Server.sv.model_precache[i] = s;
                    Server.sv.models[i] = Mod.ForName( s, true );
                    return;
                }
                if( Server.sv.model_precache[i] == s )
                {
                    return;
                }
            }
            Progs.RunError( "PF_precache_model: overflow" );
        }

        private static void PF_coredump()
        {
            Progs.PrintEdicts();
        }

        private static void PF_traceon()
        {
            Progs.Trace = true;
        }

        private static void PF_traceoff()
        {
            Progs.Trace = false;
        }

        private static void PF_eprint()
        {
            Progs.PrintNum( Server.NumForEdict( GetEdict( OFS.OFS_PARM0 ) ) );
        }

        private static void PF_walkmove()
        {
            edict_t ent = Server.ProgToEdict( Progs.GlobalStruct.self );
            float yaw = GetFloat( OFS.OFS_PARM0 );
            float dist = GetFloat( OFS.OFS_PARM1 );

            if( ( (int)ent.v.flags & ( EdictFlags.FL_ONGROUND | EdictFlags.FL_FLY | EdictFlags.FL_SWIM ) ) == 0 )
            {
                ReturnFloat( 0 );
                return;
            }

            yaw = (float)( yaw * Math.PI * 2.0 / 360.0 );

            v3f move;
            move.x = (float)Math.Cos( yaw ) * dist;
            move.y = (float)Math.Sin( yaw ) * dist;
            move.z = 0;

            // save program state, because SV_movestep may call other progs
            dfunction_t oldf = Progs.xFunction;
            int oldself = Progs.GlobalStruct.self;

            ReturnFloat( Server.MoveStep( ent, ref move, true ) ? 1 : 0 );

            // restore program state
            Progs.xFunction = oldf;
            Progs.GlobalStruct.self = oldself;
        }

        private static void PF_droptofloor()
        {
            edict_t ent = Server.ProgToEdict( Progs.GlobalStruct.self );
            Mathlib.Copy( ref ent.v.origin, out Vector3 org );
            Mathlib.Copy( ref ent.v.mins, out Vector3 mins );
            Mathlib.Copy( ref ent.v.maxs, out Vector3 maxs );
            Vector3 end = org;
            end.Z -= 256;

            trace_t trace = Server.Move( ref org, ref mins, ref maxs, ref end, 0, ent );

            if( trace.fraction == 1 || trace.allsolid )
            {
                ReturnFloat( 0 );
            }
            else
            {
                Mathlib.Copy( ref trace.endpos, out ent.v.origin );
                Server.LinkEdict( ent, false );
                ent.v.flags = (int)ent.v.flags | EdictFlags.FL_ONGROUND;
                ent.v.groundentity = Server.EdictToProg( trace.ent );
                ReturnFloat( 1 );
            }
        }

        private static void PF_lightstyle()
        {
            int style = (int)GetFloat( OFS.OFS_PARM0 );
            string val = GetString( OFS.OFS_PARM1 );

            // change the string in sv
            Server.sv.lightstyles[style] = val;

            // send message to all clients on this server
            if( !Server.IsActive )
            {
                return;
            }

            for( int j = 0; j < Server.svs.maxclients; j++ )
            {
                client_t client = Server.svs.clients[j];
                if( client.active || client.spawned )
                {
                    client.message.WriteChar( Protocol.svc_lightstyle );
                    client.message.WriteChar( style );
                    client.message.WriteString( val );
                }
            }
        }

        private static void PF_rint()
        {
            float f = GetFloat( OFS.OFS_PARM0 );
            if( f > 0 )
            {
                ReturnFloat( (int)( f + 0.5 ) );
            }
            else
            {
                ReturnFloat( (int)( f - 0.5 ) );
            }
        }

        private static void PF_floor()
        {
            ReturnFloat( (float)Math.Floor( GetFloat( OFS.OFS_PARM0 ) ) );
        }

        private static void PF_ceil()
        {
            ReturnFloat( (float)Math.Ceiling( GetFloat( OFS.OFS_PARM0 ) ) );
        }

        private static void PF_checkbottom()
        {
            edict_t ent = GetEdict( OFS.OFS_PARM0 );
            ReturnFloat( Server.CheckBottom( ent ) ? 1 : 0 );
        }

        private static unsafe void PF_pointcontents()
        {
            float* v = GetVector( OFS.OFS_PARM0 );
            Copy( v, out Vector3 tmp );
            ReturnFloat( Server.PointContents( ref tmp ) );
        }

        private static void PF_nextent()
        {
            int i = Server.NumForEdict( GetEdict( OFS.OFS_PARM0 ) );
            while( true )
            {
                i++;
                if( i == Server.sv.num_edicts )
                {
                    ReturnEdict( Server.sv.edicts[0] );
                    return;
                }
                edict_t ent = Server.EdictNum( i );
                if( !ent.free )
                {
                    ReturnEdict( ent );
                    return;
                }
            }
        }

        // Pick a vector for the player to shoot along
        private static void PF_aim()
        {
            edict_t ent = GetEdict( OFS.OFS_PARM0 );
            float speed = GetFloat( OFS.OFS_PARM1 );

            Vector3 start = Common.ToVector( ref ent.v.origin );
            start.Z += 20;

            // try sending a trace straight
            Mathlib.Copy( ref Progs.GlobalStruct.v_forward, out Vector3 dir );
            Vector3 end = start + dir * 2048;
            trace_t tr = Server.Move( ref start, ref Common.ZeroVector, ref Common.ZeroVector, ref end, 0, ent );
            if( tr.ent != null && tr.ent.v.takedamage == Damages.DAMAGE_AIM &&
                ( Host.TeamPlay == 0 || ent.v.team <= 0 || ent.v.team != tr.ent.v.team ) )
            {
                ReturnVector( ref Progs.GlobalStruct.v_forward );
                return;
            }

            // try all possible entities
            Vector3 bestdir = dir;
            float bestdist = Server.Aim;
            edict_t bestent = null;

            for( int i = 1; i < Server.sv.num_edicts; i++ )
            {
                edict_t check = Server.sv.edicts[i];
                if( check.v.takedamage != Damages.DAMAGE_AIM )
                {
                    continue;
                }

                if( check == ent )
                {
                    continue;
                }

                if( Host.TeamPlay != 0 && ent.v.team > 0 && ent.v.team == check.v.team )
                {
                    continue; // don't aim at teammate
                }

                Mathlib.VectorAdd( ref check.v.mins, ref check.v.maxs, out v3f tmp );
                Mathlib.VectorMA( ref check.v.origin, 0.5f, ref tmp, out tmp );
                Mathlib.Copy( ref tmp, out end );

                dir = end - start;
                Mathlib.Normalize( ref dir );
                float dist = Vector3.Dot( dir, Common.ToVector( ref Progs.GlobalStruct.v_forward ) );
                if( dist < bestdist )
                {
                    continue; // to far to turn
                }

                tr = Server.Move( ref start, ref Common.ZeroVector, ref Common.ZeroVector, ref end, 0, ent );
                if( tr.ent == check )
                { // can shoot at this one
                    bestdist = dist;
                    bestent = check;
                }
            }

            if( bestent != null )
            {
                Mathlib.VectorSubtract( ref bestent.v.origin, ref ent.v.origin, out v3f dir2 );
                float dist = Mathlib.DotProduct( ref dir2, ref Progs.GlobalStruct.v_forward );
                Mathlib.VectorScale( ref Progs.GlobalStruct.v_forward, dist, out v3f end2 );
                end2.z = dir2.z;
                Mathlib.Normalize( ref end2 );
                ReturnVector( ref end2 );
            }
            else
            {
                ReturnVector( ref bestdir );
            }
        }

        private static void PF_WriteByte()
        {
            WriteDest.WriteByte( (int)GetFloat( OFS.OFS_PARM1 ) );
        }

        private static void PF_WriteChar()
        {
            WriteDest.WriteChar( (int)GetFloat( OFS.OFS_PARM1 ) );
        }

        private static void PF_WriteShort()
        {
            WriteDest.WriteShort( (int)GetFloat( OFS.OFS_PARM1 ) );
        }

        private static void PF_WriteLong()
        {
            WriteDest.WriteLong( (int)GetFloat( OFS.OFS_PARM1 ) );
        }

        private static void PF_WriteAngle()
        {
            WriteDest.WriteAngle( GetFloat( OFS.OFS_PARM1 ) );
        }

        private static void PF_WriteCoord()
        {
            WriteDest.WriteCoord( GetFloat( OFS.OFS_PARM1 ) );
        }

        private static void PF_WriteString()
        {
            WriteDest.WriteString( GetString( OFS.OFS_PARM1 ) );
        }

        private static void PF_WriteEntity()
        {
            WriteDest.WriteShort( Server.NumForEdict( GetEdict( OFS.OFS_PARM1 ) ) );
        }

        private static void PF_makestatic()
        {
            edict_t ent = GetEdict( OFS.OFS_PARM0 );
            MsgWriter msg = Server.sv.signon;

            msg.WriteByte( Protocol.svc_spawnstatic );
            msg.WriteByte( Server.ModelIndex( Progs.GetString( ent.v.model ) ) );
            msg.WriteByte( (int)ent.v.frame );
            msg.WriteByte( (int)ent.v.colormap );
            msg.WriteByte( (int)ent.v.skin );
            for( int i = 0; i < 3; i++ )
            {
                msg.WriteCoord( Mathlib.Comp( ref ent.v.origin, i ) );
                msg.WriteAngle( Mathlib.Comp( ref ent.v.angles, i ) );
            }

            // throw the entity away now
            Server.FreeEdict( ent );
        }

        private static void PF_setspawnparms()
        {
            edict_t ent = GetEdict( OFS.OFS_PARM0 );
            int i = Server.NumForEdict( ent );
            if( i < 1 || i > Server.svs.maxclients )
            {
                Progs.RunError( "Entity is not a client" );
            }

            // copy spawn parms out of the client_t
            client_t client = Server.svs.clients[i - 1];

            Progs.GlobalStruct.SetParams( client.spawn_parms );
        }

        private static void PF_changelevel()
        {
            // make sure we don't issue two changelevels
            if( Server.svs.changelevel_issued )
            {
                return;
            }

            Server.svs.changelevel_issued = true;

            string s = GetString( OFS.OFS_PARM0 );
            Cbuf.AddText( string.Format( "changelevel {0}\n", s ) );
        }

        private static void PF_Fixme()
        {
            Progs.RunError( "unimplemented bulitin" );
        }
    }
}
