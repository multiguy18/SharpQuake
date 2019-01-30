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
    internal static class Chase
    {
        public static bool IsActive
        {
            get
            {
                return ( _Active.Value != 0 );
            }
        }

        private static Cvar _Back;
        private static Cvar _Up;
        private static Cvar _Right;
        private static Cvar _Active;
        private static Vector3 _Dest;

        public static void Init()
        {
            if( _Back == null )
            {
                _Back = new Cvar( "chase_back", "100" );
                _Up = new Cvar( "chase_up", "16" );
                _Right = new Cvar( "chase_right", "0" );
                _Active = new Cvar( "chase_active", "0" );
            }
        }

        public static void Update()
        {
            Mathlib.AngleVectors( ref Client.cl.viewangles, out Vector3 forward, out Vector3 right, out Vector3 up );

            // calc exact destination
            _Dest = Render.RefDef.vieworg - forward * _Back.Value - right * _Right.Value;
            _Dest.Z = Render.RefDef.vieworg.Z + _Up.Value;

            // find the spot the player is looking at
            Vector3 dest = Render.RefDef.vieworg + forward * 4096;

            TraceLine( ref Render.RefDef.vieworg, ref dest, out Vector3 stop );

            // calculate pitch to look at the same spot from camera
            stop -= Render.RefDef.vieworg;
            Vector3.Dot( ref stop, ref forward, out float dist );
            if( dist < 1 )
            {
                dist = 1;
            }

            Render.RefDef.viewangles.X = (float)( -Math.Atan( stop.Z / dist ) / Math.PI * 180.0 );

            // move towards destination
            Render.RefDef.vieworg = _Dest;
        }

        private static void TraceLine( ref Vector3 start, ref Vector3 end, out Vector3 impact )
        {
            trace_t trace = new trace_t();

            Server.RecursiveHullCheck( Client.cl.worldmodel.hulls[0], 0, 0, 1, ref start, ref end, trace );

            impact = trace.endpos;
        }
    }
}
