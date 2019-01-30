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
using System.Runtime.InteropServices;
using func_t = System.Int32;

using string_t = System.Int32;

namespace SharpQuake
{
    internal delegate void builtin_t();

    [StructLayout( LayoutKind.Explicit, Size = 12, Pack = 1 )]
    internal unsafe struct eval_t
    {
        [FieldOffset( 0 )]
        public int _string;

        [FieldOffset( 0 )]
        public float _float;

        [FieldOffset( 0 )]
        public fixed float vector[3];

        [FieldOffset( 0 )]
        public int function;

        [FieldOffset( 0 )]
        public int _int;

        [FieldOffset( 0 )]
        public int edict;
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct dedict_t
    {
        public bool free;
        public int dummy1, dummy2;

        public int num_leafs;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = Progs.MAX_ENT_LEAFS )]
        public short[] leafnums;

        public entity_state_t baseline;

        public float freetime; // sv.time when the object was freed
        public entvars_t v; // C exported fields from progs, other fields from progs come immediately after

        public static int SizeInBytes = Marshal.SizeOf( typeof( dedict_t ) );
    }

    internal enum etype_t
    {
        ev_void, ev_string, ev_float, ev_vector, ev_entity, ev_field, ev_function, ev_pointer
    }

    internal enum OP
    {
        OP_DONE,
        OP_MUL_F,
        OP_MUL_V,
        OP_MUL_FV,
        OP_MUL_VF,
        OP_DIV_F,
        OP_ADD_F,
        OP_ADD_V,
        OP_SUB_F,
        OP_SUB_V,

        OP_EQ_F,
        OP_EQ_V,
        OP_EQ_S,
        OP_EQ_E,
        OP_EQ_FNC,

        OP_NE_F,
        OP_NE_V,
        OP_NE_S,
        OP_NE_E,
        OP_NE_FNC,

        OP_LE,
        OP_GE,
        OP_LT,
        OP_GT,

        OP_LOAD_F,
        OP_LOAD_V,
        OP_LOAD_S,
        OP_LOAD_ENT,
        OP_LOAD_FLD,
        OP_LOAD_FNC,

        OP_ADDRESS,

        OP_STORE_F,
        OP_STORE_V,
        OP_STORE_S,
        OP_STORE_ENT,
        OP_STORE_FLD,
        OP_STORE_FNC,

        OP_STOREP_F,
        OP_STOREP_V,
        OP_STOREP_S,
        OP_STOREP_ENT,
        OP_STOREP_FLD,
        OP_STOREP_FNC,

        OP_RETURN,
        OP_NOT_F,
        OP_NOT_V,
        OP_NOT_S,
        OP_NOT_ENT,
        OP_NOT_FNC,
        OP_IF,
        OP_IFNOT,
        OP_CALL0,
        OP_CALL1,
        OP_CALL2,
        OP_CALL3,
        OP_CALL4,
        OP_CALL5,
        OP_CALL6,
        OP_CALL7,
        OP_CALL8,
        OP_STATE,
        OP_GOTO,
        OP_AND,
        OP_OR,

        OP_BITAND,
        OP_BITOR
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct dstatement_t
    {
        public ushort op;
        public short a, b, c;

        public static int SizeInBytes = Marshal.SizeOf( typeof( dstatement_t ) );

        public void SwapBytes()
        {
            op = (ushort)Common.LittleShort( (short)op );
            a = Common.LittleShort( a );
            b = Common.LittleShort( b );
            c = Common.LittleShort( c );
        }
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct v3f
    {
        public float x, y, z;

        public bool IsEmpty
        {
            get
            {
                return ( x == 0 ) && ( y == 0 ) && ( z == 0 );
            }
        }
    }

    [StructLayout( LayoutKind.Explicit, Size = ( 4 * 28 ) )]
    internal struct pad_int28
    {
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct entvars_t
    {
        public float modelindex;
        public v3f absmin;
        public v3f absmax;
        public float ltime;
        public float movetype;
        public float solid;
        public v3f origin;
        public v3f oldorigin;
        public v3f velocity;
        public v3f angles;
        public v3f avelocity;
        public v3f punchangle;
        public string_t classname;
        public string_t model;
        public float frame;
        public float skin;
        public float effects;
        public v3f mins;
        public v3f maxs;
        public v3f size;
        public func_t touch;
        public func_t use;
        public func_t think;
        public func_t blocked;
        public float nextthink;
        public int groundentity;
        public float health;
        public float frags;
        public float weapon;
        public string_t weaponmodel;
        public float weaponframe;
        public float currentammo;
        public float ammo_shells;
        public float ammo_nails;
        public float ammo_rockets;
        public float ammo_cells;
        public float items;
        public float takedamage;
        public int chain;
        public float deadflag;
        public v3f view_ofs;
        public float button0;
        public float button1;
        public float button2;
        public float impulse;
        public float fixangle;
        public v3f v_angle;
        public float idealpitch;
        public string_t netname;
        public int enemy;
        public float flags;
        public float colormap;
        public float team;
        public float max_health;
        public float teleport_time;
        public float armortype;
        public float armorvalue;
        public float waterlevel;
        public float watertype;
        public float ideal_yaw;
        public float yaw_speed;
        public int aiment;
        public int goalentity;
        public float spawnflags;
        public string_t target;
        public string_t targetname;
        public float dmg_take;
        public float dmg_save;
        public int dmg_inflictor;
        public int owner;
        public v3f movedir;
        public string_t message;
        public float sounds;
        public string_t noise;
        public string_t noise1;
        public string_t noise2;
        public string_t noise3;

        public static int SizeInBytes = Marshal.SizeOf( typeof( entvars_t ) );
    }

    internal static partial class Progs
    {
        public const int DEF_SAVEGLOBAL = ( 1 << 15 );
        public const int MAX_PARMS = 8;
        public const int MAX_ENT_LEAFS = 16;

        private const int PROG_VERSION = 6;
        private const int PROGHEADER_CRC = 5927;
    }

    internal static class OFS
    {
        public const int OFS_NULL = 0;
        public const int OFS_RETURN = 1;
        public const int OFS_PARM0 = 4; // leave 3 ofs for each parm to hold vectors
        public const int OFS_PARM1 = 7;
        public const int OFS_PARM2 = 10;
        public const int OFS_PARM3 = 13;
        public const int OFS_PARM4 = 16;
        public const int OFS_PARM5 = 19;
        public const int OFS_PARM6 = 22;
        public const int OFS_PARM7 = 25;
        public const int RESERVED_OFS = 28;
    }

    internal class edict_t
    {
        public bool free;
        public link_t area; // linked to a division node or leaf

        public int num_leafs;
        public short[] leafnums;

        public entity_state_t baseline;

        public float freetime; // sv.time when the object was freed
        public entvars_t v; // C exported fields from progs
        public float[] fields; // other fields from progs

        public void Clear()
        {
            v = default( entvars_t );
            if( fields != null )
            {
                Array.Clear( fields, 0, fields.Length );
            }

            free = false;
        }

        public bool IsV( int offset, out int correctedOffset )
        {
            if( offset < ( entvars_t.SizeInBytes >> 2 ) )
            {
                correctedOffset = offset;
                return true;
            }
            correctedOffset = offset - ( entvars_t.SizeInBytes >> 2 );
            return false;
        }

        public unsafe void LoadInt( int offset, eval_t* result )
        {
            if( IsV( offset, out int offset1 ) )
            {
                fixed ( void* pv = &v )
                {
                    eval_t* a = (eval_t*)( (int*)pv + offset1 );
                    result->_int = a->_int;
                }
            }
            else
            {
                fixed ( void* pv = fields )
                {
                    eval_t* a = (eval_t*)( (int*)pv + offset1 );
                    result->_int = a->_int;
                }
            }
        }

        public unsafe void StoreInt( int offset, eval_t* value )
        {
            if( IsV( offset, out int offset1 ) )
            {
                fixed ( void* pv = &v )
                {
                    eval_t* a = (eval_t*)( (int*)pv + offset1 );
                    a->_int = value->_int;
                }
            }
            else
            {
                fixed ( void* pv = fields )
                {
                    eval_t* a = (eval_t*)( (int*)pv + offset1 );
                    a->_int = value->_int;
                }
            }
        }

        public unsafe void LoadVector( int offset, eval_t* result )
        {
            if( IsV( offset, out int offset1 ) )
            {
                fixed ( void* pv = &v )
                {
                    eval_t* a = (eval_t*)( (int*)pv + offset1 );
                    result->vector[0] = a->vector[0];
                    result->vector[1] = a->vector[1];
                    result->vector[2] = a->vector[2];
                }
            }
            else
            {
                fixed ( void* pf = fields )
                {
                    eval_t* a = (eval_t*)( (int*)pf + offset1 );
                    result->vector[0] = a->vector[0];
                    result->vector[1] = a->vector[1];
                    result->vector[2] = a->vector[2];
                }
            }
        }

        public unsafe void StoreVector( int offset, eval_t* value )
        {
            if( IsV( offset, out int offset1 ) )
            {
                fixed ( void* pv = &v )
                {
                    eval_t* a = (eval_t*)( (int*)pv + offset1 );
                    a->vector[0] = value->vector[0];
                    a->vector[1] = value->vector[1];
                    a->vector[2] = value->vector[2];
                }
            }
            else
            {
                fixed ( void* pf = fields )
                {
                    eval_t* a = (eval_t*)( (int*)pf + offset1 );
                    a->vector[0] = value->vector[0];
                    a->vector[1] = value->vector[1];
                    a->vector[2] = value->vector[2];
                }
            }
        }

        public unsafe int GetInt( int offset )
        {
            int result;
            if( IsV( offset, out int offset1 ) )
            {
                fixed ( void* pv = &v )
                {
                    eval_t* a = (eval_t*)( (int*)pv + offset1 );
                    result = a->_int;
                }
            }
            else
            {
                fixed ( void* pv = fields )
                {
                    eval_t* a = (eval_t*)( (int*)pv + offset1 );
                    result = a->_int;
                }
            }
            return result;
        }

        public unsafe float GetFloat( int offset )
        {
            float result;
            if( IsV( offset, out int offset1 ) )
            {
                fixed ( void* pv = &v )
                {
                    eval_t* a = (eval_t*)( (float*)pv + offset1 );
                    result = a->_float;
                }
            }
            else
            {
                fixed ( void* pv = fields )
                {
                    eval_t* a = (eval_t*)( (float*)pv + offset1 );
                    result = a->_float;
                }
            }
            return result;
        }

        public unsafe void SetFloat( int offset, float value )
        {
            if( IsV( offset, out int offset1 ) )
            {
                fixed ( void* pv = &v )
                {
                    eval_t* a = (eval_t*)( (float*)pv + offset1 );
                    a->_float = value;
                }
            }
            else
            {
                fixed ( void* pv = fields )
                {
                    eval_t* a = (eval_t*)( (float*)pv + offset1 );
                    a->_float = value;
                }
            }
        }

        public edict_t()
        {
            area = new link_t( this );
            leafnums = new short[Progs.MAX_ENT_LEAFS];
            fields = new float[( Progs.EdictSize - entvars_t.SizeInBytes ) >> 2];
        }
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal class ddef_t
    {
        public ushort type;  // if DEF_SAVEGLOBGAL bit is set

        // the variable needs to be saved in savegames
        public ushort ofs;

        public int s_name;

        public static int SizeInBytes = Marshal.SizeOf( typeof( ddef_t ) );

        public void SwapBytes()
        {
            type = (ushort)Common.LittleShort( (short)type );
            ofs = (ushort)Common.LittleShort( (short)ofs );
            s_name = Common.LittleLong( s_name );
        }
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal class dfunction_t
    {
        public int first_statement; // negative numbers are builtins
        public int parm_start;
        public int locals;    // total ints of parms + locals

        public int profile;  // runtime

        public int s_name;
        public int s_file;   // source file defined in

        public int numparms;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = Progs.MAX_PARMS )]
        public byte[] parm_size;

        public static int SizeInBytes = Marshal.SizeOf( typeof( dfunction_t ) );

        public string FileName
        {
            get
            {
                return Progs.GetString( s_file );
            }
        }

        public string Name
        {
            get
            {
                return Progs.GetString( s_name );
            }
        }

        public void SwapBytes()
        {
            first_statement = Common.LittleLong( first_statement );
            parm_start = Common.LittleLong( parm_start );
            locals = Common.LittleLong( locals );
            s_name = Common.LittleLong( s_name );
            s_file = Common.LittleLong( s_file );
            numparms = Common.LittleLong( numparms );
        }

        public override string ToString()
        {
            return string.Format( "{{{0}: {1}()}}", FileName, Name );
        }
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal class dprograms_t
    {
        public int version;
        public int crc;   // check of header file

        public int ofs_statements;
        public int numstatements; // statement 0 is an error

        public int ofs_globaldefs;
        public int numglobaldefs;

        public int ofs_fielddefs;
        public int numfielddefs;

        public int ofs_functions;
        public int numfunctions; // function 0 is an empty

        public int ofs_strings;
        public int numstrings;  // first string is a null string

        public int ofs_globals;
        public int numglobals;

        public int entityfields;

        public static int SizeInBytes = Marshal.SizeOf( typeof( dprograms_t ) );

        public void SwapBytes()
        {
            version = Common.LittleLong( version );
            crc = Common.LittleLong( crc );
            ofs_statements = Common.LittleLong( ofs_statements );
            numstatements = Common.LittleLong( numstatements );
            ofs_globaldefs = Common.LittleLong( ofs_globaldefs );
            numglobaldefs = Common.LittleLong( numglobaldefs );
            ofs_fielddefs = Common.LittleLong( ofs_fielddefs );
            numfielddefs = Common.LittleLong( numfielddefs );
            ofs_functions = Common.LittleLong( ofs_functions );
            numfunctions = Common.LittleLong( numfunctions );
            ofs_strings = Common.LittleLong( ofs_strings );
            numstrings = Common.LittleLong( numstrings );
            ofs_globals = Common.LittleLong( ofs_globals );
            numglobals = Common.LittleLong( numglobals );
            entityfields = Common.LittleLong( entityfields );
        }
    }

    //=================================================================
    // QuakeC compiler generated data from progdefs.q1
    //=================================================================

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal class globalvars_t
    {
        private readonly pad_int28 pad;
        public int self;
        public int other;
        public int world;
        public float time;
        public float frametime;
        public float force_retouch;
        public string_t mapname;
        public float deathmatch;
        public float coop;
        public float teamplay;
        public float serverflags;
        public float total_secrets;
        public float total_monsters;
        public float found_secrets;
        public float killed_monsters;
        public float parm1;
        public float parm2;
        public float parm3;
        public float parm4;
        public float parm5;
        public float parm6;
        public float parm7;
        public float parm8;
        public float parm9;
        public float parm10;
        public float parm11;
        public float parm12;
        public float parm13;
        public float parm14;
        public float parm15;
        public float parm16;
        public v3f v_forward;
        public v3f v_up;
        public v3f v_right;
        public float trace_allsolid;
        public float trace_startsolid;
        public float trace_fraction;
        public v3f trace_endpos;
        public v3f trace_plane_normal;
        public float trace_plane_dist;
        public int trace_ent;
        public float trace_inopen;
        public float trace_inwater;
        public int msg_entity;
        public func_t main;
        public func_t StartFrame;
        public func_t PlayerPreThink;
        public func_t PlayerPostThink;
        public func_t ClientKill;
        public func_t ClientConnect;
        public func_t PutClientInServer;
        public func_t ClientDisconnect;
        public func_t SetNewParms;
        public func_t SetChangeParms;

        public static int SizeInBytes = Marshal.SizeOf( typeof( globalvars_t ) );

        public void SetParams( float[] src )
        {
            if( src.Length < Server.NUM_SPAWN_PARMS )
            {
                throw new ArgumentException( string.Format( "There must be {0} parameters!", Server.NUM_SPAWN_PARMS ) );
            }

            parm1 = src[0];
            parm2 = src[1];
            parm3 = src[2];
            parm4 = src[3];
            parm5 = src[4];
            parm6 = src[5];
            parm7 = src[6];
            parm8 = src[7];
            parm9 = src[8];
            parm10 = src[9];
            parm11 = src[10];
            parm12 = src[11];
            parm13 = src[12];
            parm14 = src[13];
            parm15 = src[14];
            parm16 = src[15];
        }
    }
}
