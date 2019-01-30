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

using OpenTK;

namespace SharpQuake
{
    internal struct plane_t
    {
        public Vector3 normal;
        public float dist;
    }

    internal static class Move
    {
        public const int MOVE_NORMAL = 0;
        public const int MOVE_NOMONSTERS = 1;
        public const int MOVE_MISSILE = 2;
    }

    internal class trace_t
    {
        public bool allsolid; // if true, plane is not valid
        public bool startsolid;	// if true, the initial point was in a solid area
        public bool inopen, inwater;
        public float fraction; // time completed, 1.0 = didn't hit anything
        public Vector3 endpos; // final position
        public plane_t plane; // surface normal at impact
        public edict_t ent; // entity the surface is on

        public void CopyFrom( trace_t src )
        {
            allsolid = src.allsolid;
            startsolid = src.startsolid;
            inopen = src.inopen;
            inwater = src.inwater;
            fraction = src.fraction;
            endpos = src.endpos;
            plane = src.plane;
            ent = src.ent;
        }
    }
}
