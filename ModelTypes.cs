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

using System.Runtime.InteropServices;
using OpenTK;

namespace SharpQuake
{
    // d*_t structures are on-disk representations
    // m*_t structures are in-memory

    // in memory representation
    internal struct mvertex_t
    {
        public Vector3 position;
    }

    internal struct medge_t
    {
        public ushort[] v;
    }

    internal struct mspriteframedesc_t
    {
        public spriteframetype_t type;
        public object frameptr;
    }

    internal struct maliasframedesc_t
    {
        public static int SizeInBytes = Marshal.SizeOf( typeof( maliasframedesc_t ) );
        public int firstpose;
        public int numposes;
        public float interval;
        public trivertx_t bboxmin;
        public trivertx_t bboxmax;
        public string name;

        public void Init()
        {
            this.bboxmin.Init();
            this.bboxmax.Init();
        }
    }

    internal enum modtype_t
    {
        mod_brush, mod_sprite, mod_alias
    }

    internal enum synctype_t
    {
        ST_SYNC = 0,
        ST_RAND
    }

    internal enum aliasframetype_t
    {
        ALIAS_SINGLE = 0,
        ALIAS_GROUP
    }

    internal enum aliasskintype_t
    {
        ALIAS_SKIN_SINGLE = 0,
        ALIAS_SKIN_GROUP
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct mdl_t
    {
        public int ident;
        public int version;
        public v3f scale;
        public v3f scale_origin;
        public float boundingradius;
        public v3f eyeposition;
        public int numskins;
        public int skinwidth;
        public int skinheight;
        public int numverts;
        public int numtris;
        public int numframes;
        public synctype_t synctype;
        public int flags;
        public float size;

        public static readonly int SizeInBytes = Marshal.SizeOf( typeof( mdl_t ) );
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct stvert_t
    {
        public int onseam;
        public int s;
        public int t;

        public static int SizeInBytes = Marshal.SizeOf( typeof( stvert_t ) );
    }

    // TODO: could be shorts
    [StructLayout( LayoutKind.Sequential )]
    internal struct dtriangle_t
    {
        public int facesfront;

        [MarshalAs( UnmanagedType.ByValArray, ArraySubType = UnmanagedType.I4, SizeConst = 3 )]
        public int[] vertindex;

        public static int SizeInBytes = Marshal.SizeOf( typeof( dtriangle_t ) );
    }

    // This mirrors trivert_t in trilib.h, is present so Quake knows how to load this data
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct trivertx_t
    {
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
        public byte[] v;

        public byte lightnormalindex;

        public static int SizeInBytes = Marshal.SizeOf( typeof( trivertx_t ) );

        // Call only for manually created instances
        public void Init()
        {
            if( this.v == null )
                this.v = new byte[3];
        }
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct daliasframe_t
    {
        public trivertx_t bboxmin; // lightnormal isn't used
        public trivertx_t bboxmax; // lightnormal isn't used

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 16 )]
        public byte[] name; // frame name from grabbing

        public static int SizeInBytes = Marshal.SizeOf( typeof( daliasframe_t ) );
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct daliasgroup_t
    {
        public int numframes;
        public trivertx_t bboxmin; // lightnormal isn't used
        public trivertx_t bboxmax; // lightnormal isn't used

        public static int SizeInBytes = Marshal.SizeOf( typeof( daliasgroup_t ) );
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct daliasskingroup_t
    {
        public int numskins;

        public static int SizeInBytes = Marshal.SizeOf( typeof( daliasskingroup_t ) );
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct daliasinterval_t
    {
        public float interval;

        public static int SizeInBytes = Marshal.SizeOf( typeof( daliasinterval_t ) );
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct daliasskininterval_t
    {
        public float interval;

        public static int SizeInBytes = Marshal.SizeOf( typeof( daliasskininterval_t ) );
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct daliasframetype_t
    {
        public aliasframetype_t type;
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct daliasskintype_t
    {
        public aliasskintype_t type;

        public static int SizeInBytes = Marshal.SizeOf( typeof( daliasskintype_t ) );
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct dsprite_t
    {
        public int ident;
        public int version;
        public int type;
        public float boundingradius;
        public int width;
        public int height;
        public int numframes;
        public float beamlength;
        public synctype_t synctype;

        public static int SizeInBytes = Marshal.SizeOf( typeof( dsprite_t ) );
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct dspriteframe_t
    {
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 2 )]
        public int[] origin;

        public int width;
        public int height;

        public static int SizeInBytes = Marshal.SizeOf( typeof( dspriteframe_t ) );
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct dspritegroup_t
    {
        public int numframes;

        public static int SizeInBytes = Marshal.SizeOf( typeof( dspritegroup_t ) );
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct dspriteinterval_t
    {
        public float interval;

        public static int SizeInBytes = Marshal.SizeOf( typeof( dspriteinterval_t ) );
    }

    internal enum spriteframetype_t
    {
        SPR_SINGLE = 0, SPR_GROUP
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct dspriteframetype_t
    {
        public spriteframetype_t type;
    }

    // entity effects
    internal static class EntityEffects
    {
        public static int EF_BRIGHTFIELD = 1;
        public static int EF_MUZZLEFLASH = 2;
        public static int EF_BRIGHTLIGHT = 4;
        public static int EF_DIMLIGHT = 8;
#if QUAKE2
        public static int EF_DARKLIGHT = 16;
        public static int EF_DARKFIELD = 32;
        public static int EF_LIGHT = 64;
        public static int EF_NODRAW = 128;
#endif
    }

    internal static class Side
    {
        public const int SIDE_FRONT = 0;
        public const int SIDE_BACK = 1;
        public const int SIDE_ON = 2;
    }

    internal static class Surf
    {
        public const int SURF_PLANEBACK = 2;
        public const int SURF_DRAWSKY = 4;
        public const int SURF_DRAWSPRITE = 8;
        public const int SURF_DRAWTURB = 0x10;
        public const int SURF_DRAWTILED = 0x20;
        public const int SURF_DRAWBACKGROUND = 0x40;
        public const int SURF_UNDERWATER = 0x80;
    }

    internal static class EF
    {
        public const int EF_ROCKET = 1; // leave a trail
        public const int EF_GRENADE = 2; // leave a trail
        public const int EF_GIB = 4; // leave a trail
        public const int EF_ROTATE = 8; // rotate (bonus items)
        public const int EF_TRACER = 16; // green split trail
        public const int EF_ZOMGIB = 32; // small blood trail
        public const int EF_TRACER2 = 64; // orange split trail + rotate
        public const int EF_TRACER3 = 128; // purple trail
    }

    internal static class SPR
    {
        public const int SPR_VP_PARALLEL_UPRIGHT = 0;
        public const int SPR_FACING_UPRIGHT = 1;
        public const int SPR_VP_PARALLEL = 2;
        public const int SPR_ORIENTED = 3;
        public const int SPR_VP_PARALLEL_ORIENTED = 4;
    }

    internal class mplane_t
    {
        public Vector3 normal;
        public float dist;
        public byte type;   // for texture axis selection and fast side tests
        public byte signbits;  // signx + signy<<1 + signz<<1
    }

    /* Uze:
     * WARNING: texture_t changed!!!
     * in original Quake texture_t and it's data where allocated as one hunk
     * texture_t* ptex = Alloc_Hunk(sizeof(texture_t) + size_of_mip_level_0 + ...)
     * ptex->offset[0] = sizeof(texture_t)
     * ptex->offset[1] = ptex->offset[0] + size_of_mip_level_0 and so on
     * now there is field <pixels> and all offsets are just indices in this byte array
     */

    internal class texture_t
    {
        public string name;
        public uint width, height;
        public int gl_texturenum;
        public msurface_t texturechain; // for gl_texsort drawing
        public int anim_total;    // total tenths in sequence ( 0 = no)
        public int anim_min, anim_max;  // time for this frame min <=time< max
        public texture_t anim_next;  // in the animation sequence
        public texture_t alternate_anims; // bmodels in frmae 1 use these
        public int[] offsets; // four mip maps stored
        public byte[] pixels; // added by Uze

        public texture_t()
        {
            offsets = new int[BspFile.MIPLEVELS];
        }
    }

    internal class mtexinfo_t
    {
        public Vector4[] vecs;
        public float mipadjust;
        public texture_t texture;
        public int flags;

        public mtexinfo_t()
        {
            vecs = new Vector4[2];
        }
    }

    internal class glpoly_t
    {
        public glpoly_t next;
        public glpoly_t chain;
        public int numverts;
        public int flags;   // for SURF_UNDERWATER

        public float[][] verts;// variable sized (xyz s1t1 s2t2)

        public void Clear()
        {
            this.next = null;
            this.chain = null;
            this.numverts = 0;
            this.flags = 0;
            this.verts = null;
        }

        public void AllocVerts( int count )
        {
            this.numverts = count;
            this.verts = new float[count][];
            for( int i = 0; i < count; i++ )
                this.verts[i] = new float[Mod.VERTEXSIZE];
        }
    }

    internal class msurface_t
    {
        public int visframe;  // should be drawn when node is crossed

        public mplane_t plane;
        public int flags;

        public int firstedge; // look up in model->surfedges[], negative numbers
        public int numedges; // are backwards edges

        public short[] texturemins;
        public short[] extents;

        public int light_s, light_t; // gl lightmap coordinates

        public glpoly_t polys; // multiple if warped
        public msurface_t texturechain;

        public mtexinfo_t texinfo;

        // lighting info
        public int dlightframe;

        public int dlightbits;

        public int lightmaptexturenum;
        public byte[] styles;
        public int[] cached_light; // values currently used in lightmap
        public bool cached_dlight; // true if dynamic light in cache

        /// <summary>
        /// Use in pair with sampleofs field!!!
        /// </summary>
        public byte[] sample_base;

        public int sampleofs; // added by Uze. In original Quake samples = loadmodel->lightdata + offset;
        // now samples = loadmodel->lightdata;

        public msurface_t()
        {
            texturemins = new short[2];
            extents = new short[2];
            styles = new byte[BspFile.MAXLIGHTMAPS];
            cached_light = new int[BspFile.MAXLIGHTMAPS];
            // samples is allocated when needed
        }
    }

    // commmon part of mnode_t and mleaf_t
    internal class mnodebase_t
    {
        public int contents;  // 0 for mnode_t and negative for mleaf_t
        public int visframe;  // node needs to be traversed if current
        public Vector3 mins;
        public Vector3 maxs;
        public mnode_t parent;
    }

    internal class mnode_t : mnodebase_t
    {
        // node specific
        public mplane_t plane;

        public mnodebase_t[] children;

        public ushort firstsurface;
        public ushort numsurfaces;

        public mnode_t()
        {
            this.children = new mnodebase_t[2];
        }
    }

    internal class mleaf_t : mnodebase_t
    {
        // leaf specific
        /// <summary>
        /// Use in pair with visofs!
        /// </summary>
        public byte[] compressed_vis;

        public int visofs; // added by Uze
        public efrag_t efrags;
        public msurface_t[] marksurfaces;
        public int firstmarksurface;
        public int nummarksurfaces;
        public byte[] ambient_sound_level;

        public mleaf_t()
        {
            this.ambient_sound_level = new byte[Ambients.NUM_AMBIENTS];
        }
    }

    internal class hull_t
    {
        public dclipnode_t[] clipnodes;
        public mplane_t[] planes;
        public int firstclipnode;
        public int lastclipnode;
        public Vector3 clip_mins;
        public Vector3 clip_maxs;

        public void Clear()
        {
            this.clipnodes = null;
            this.planes = null;
            this.firstclipnode = 0;
            this.lastclipnode = 0;
            this.clip_mins = Vector3.Zero;
            this.clip_maxs = Vector3.Zero;
        }

        public void CopyFrom( hull_t src )
        {
            this.clipnodes = src.clipnodes;
            this.planes = src.planes;
            this.firstclipnode = src.firstclipnode;
            this.lastclipnode = src.lastclipnode;
            this.clip_mins = src.clip_mins;
            this.clip_maxs = src.clip_maxs;
        }
    }

    // FIXME: shorten these?
    internal class mspriteframe_t
    {
        public int width;
        public int height;
        public float up;
        public float down;
        public float left;
        public float right;
        public int gl_texturenum;
    }

    internal class mspritegroup_t
    {
        public int numframes;
        public float[] intervals;
        public mspriteframe_t[] frames;
    }

    internal class msprite_t
    {
        public int type;
        public int maxwidth;
        public int maxheight;
        public int numframes;
        public float beamlength;  // remove?
        public mspriteframedesc_t[] frames;
    }

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal class aliashdr_t
    {
        public int ident;
        public int version;
        public Vector3 scale;
        public Vector3 scale_origin;
        public float boundingradius;
        public Vector3 eyeposition;
        public int numskins;
        public int skinwidth;
        public int skinheight;
        public int numverts;
        public int numtris;
        public int numframes;
        public synctype_t synctype;
        public int flags;
        public float size;

        public int numposes;
        public int poseverts;
        public trivertx_t[] posedata;
        public int[] commands; // gl command list with embedded s/t

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = ( Mod.MAX_SKINS * 4 ) )]
        public int[,] gl_texturenum;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = Mod.MAX_SKINS )]
        public object[] texels; // only for player skins

        public maliasframedesc_t[] frames; // variable sized

        public static int SizeInBytes = Marshal.SizeOf( typeof( aliashdr_t ) );

        public aliashdr_t()
        {
            this.gl_texturenum = new int[Mod.MAX_SKINS, 4];
            this.texels = new object[Mod.MAX_SKINS];
        }
    }

    //===================================================================

    // modtype_t;
    internal class model_t
    {
        public string name;
        public bool needload; // bmodels and sprites don't cache normally

        public modtype_t type;
        public int numframes;
        public synctype_t synctype;

        public int flags;

        // volume occupied by the model graphics
        public Vector3 mins, maxs;

        public float radius;

        // solid volume for clipping
        public bool clipbox;

        public Vector3 clipmins, clipmaxs;

        // brush model
        public int firstmodelsurface, nummodelsurfaces;

        public int numsubmodels;
        public dmodel_t[] submodels;

        public int numplanes;
        public mplane_t[] planes;

        public int numleafs;  // number of visible leafs, not counting 0
        public mleaf_t[] leafs;

        public int numvertexes;
        public mvertex_t[] vertexes;

        public int numedges;
        public medge_t[] edges;

        public int numnodes;
        public mnode_t[] nodes;

        public int numtexinfo;
        public mtexinfo_t[] texinfo;

        public int numsurfaces;
        public msurface_t[] surfaces;

        public int numsurfedges;
        public int[] surfedges;

        public int numclipnodes;
        public dclipnode_t[] clipnodes;

        public int nummarksurfaces;
        public msurface_t[] marksurfaces;

        public hull_t[] hulls;

        public int numtextures;
        public texture_t[] textures;

        public byte[] visdata;
        public byte[] lightdata;
        public string entities;

        // additional model data
        public cache_user_t cache; // only access through Mod_Extradata

        public void Clear()
        {
            this.name = null;
            this.needload = false;
            this.type = 0;
            this.numframes = 0;
            this.synctype = 0;
            this.flags = 0;
            this.mins = Vector3.Zero;
            this.maxs = Vector3.Zero;
            this.radius = 0;
            this.clipbox = false;
            this.clipmins = Vector3.Zero;
            this.clipmaxs = Vector3.Zero;
            this.firstmodelsurface = 0;
            this.nummodelsurfaces = 0;

            this.numsubmodels = 0;
            this.submodels = null;

            this.numplanes = 0;
            this.planes = null;

            this.numleafs = 0;
            this.leafs = null;

            this.numvertexes = 0;
            this.vertexes = null;

            this.numedges = 0;
            this.edges = null;

            this.numnodes = 0;
            this.nodes = null;

            this.numtexinfo = 0;
            this.texinfo = null;

            this.numsurfaces = 0;
            this.surfaces = null;

            this.numsurfedges = 0;
            this.surfedges = null;

            this.numclipnodes = 0;
            this.clipnodes = null;

            this.nummarksurfaces = 0;
            this.marksurfaces = null;

            foreach( hull_t h in this.hulls )
                h.Clear();

            this.numtextures = 0;
            this.textures = null;

            this.visdata = null;
            this.lightdata = null;
            this.entities = null;

            this.cache = null;
        }

        public void CopyFrom( model_t src )
        {
            this.name = src.name;
            this.needload = src.needload;
            this.type = src.type;
            this.numframes = src.numframes;
            this.synctype = src.synctype;
            this.flags = src.flags;
            this.mins = src.mins;
            this.maxs = src.maxs;
            this.radius = src.radius;
            this.clipbox = src.clipbox;
            this.clipmins = src.clipmins;
            this.clipmaxs = src.clipmaxs;
            this.firstmodelsurface = src.firstmodelsurface;
            this.nummodelsurfaces = src.nummodelsurfaces;

            this.numsubmodels = src.numsubmodels;
            this.submodels = src.submodels;

            this.numplanes = src.numplanes;
            this.planes = src.planes;

            this.numleafs = src.numleafs;
            this.leafs = src.leafs;

            this.numvertexes = src.numvertexes;
            this.vertexes = src.vertexes;

            this.numedges = src.numedges;
            this.edges = src.edges;

            this.numnodes = src.numnodes;
            this.nodes = src.nodes;

            this.numtexinfo = src.numtexinfo;
            this.texinfo = src.texinfo;

            this.numsurfaces = src.numsurfaces;
            this.surfaces = src.surfaces;

            this.numsurfedges = src.numsurfedges;
            this.surfedges = src.surfedges;

            this.numclipnodes = src.numclipnodes;
            this.clipnodes = src.clipnodes;

            this.nummarksurfaces = src.nummarksurfaces;
            this.marksurfaces = src.marksurfaces;

            for( int i = 0; i < src.hulls.Length; i++ )
            {
                this.hulls[i].CopyFrom( src.hulls[i] );
            }

            this.numtextures = src.numtextures;
            this.textures = src.textures;

            this.visdata = src.visdata;
            this.lightdata = src.lightdata;
            this.entities = src.entities;

            this.cache = src.cache;
        }

        public model_t()
        {
            this.hulls = new hull_t[BspFile.MAX_MAP_HULLS];

            for( int i = 0; i < this.hulls.Length; i++ )
                this.hulls[i] = new hull_t();
        }
    }

    /*********************************************************
     * This file must be identical in the modelgen directory *
     * and in the Quake directory, because it's used to      *
     * pass data from one to the other via model files.      *
     *********************************************************/
}
