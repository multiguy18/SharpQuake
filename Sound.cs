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
using System.Runtime.InteropServices;
using OpenTK;

namespace SharpQuake
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct portable_samplepair_t
    {
        public int left;
        public int right;

        public override string ToString()
        {
            return String.Format( "{{{0}, {1}}}", this.left, this.right );
        }
    }

    internal static partial class Sound
    {
        public static bool IsInitialized
        {
            get
            {
                return _Controller.IsInitialized;
            }
        }

        public static dma_t shm
        {
            get
            {
                return _shm;
            }
        }

        public static float BgmVolume
        {
            get
            {
                return _BgmVolume.Value;
            }
        }

        public static float Volume
        {
            get
            {
                return _Volume.Value;
            }
        }

        public const int DEFAULT_SOUND_PACKET_VOLUME = 255;
        public const float DEFAULT_SOUND_PACKET_ATTENUATION = 1.0f;
        public const int MAX_CHANNELS = 128;
        public const int MAX_DYNAMIC_CHANNELS = 8;

        private const int MAX_SFX = 512;

        private static Cvar _BgmVolume = new Cvar( "bgmvolume", "1", true );
        private static Cvar _Volume = new Cvar( "volume", "0.7", true );
        private static Cvar _NoSound = new Cvar( "nosound", "0" );
        private static Cvar _Precache = new Cvar( "precache", "1" );
        private static Cvar _LoadAs8bit = new Cvar( "loadas8bit", "0" );
        private static Cvar _BgmBuffer = new Cvar( "bgmbuffer", "4096" );
        private static Cvar _AmbientLevel = new Cvar( "ambient_level", "0.3" );
        private static Cvar _AmbientFade = new Cvar( "ambient_fade", "100" );
        private static Cvar _NoExtraUpdate = new Cvar( "snd_noextraupdate", "0" );
        private static Cvar _Show = new Cvar( "snd_show", "0" );
        private static Cvar _MixAhead = new Cvar( "_snd_mixahead", "0.1", true );

        private static ISoundController _Controller = new OpenALController();
        private static bool _IsInitialized;

        private static sfx_t[] _KnownSfx = new sfx_t[MAX_SFX];
        private static int _NumSfx;
        private static sfx_t[] _AmbientSfx = new sfx_t[Ambients.NUM_AMBIENTS];
        private static bool _Ambient = true;
        private static dma_t _shm = new dma_t();

        /* 0 to MAX_DYNAMIC_CHANNELS-1 = normal entity sounds
         *  MAX_DYNAMIC_CHANNELS to MAX_DYNAMIC_CHANNELS + NUM_AMBIENTS -1 = water, etc
         *  MAX_DYNAMIC_CHANNELS + NUM_AMBIENTS to total_channels = static sounds
         */
        private static channel_t[] _Channels = new channel_t[MAX_CHANNELS]; // channels[MAX_CHANNELS]

        private static int _TotalChannels;

        private static float _SoundNominalClipDist = 1000.0f;
        private static Vector3 _ListenerOrigin;
        private static Vector3 _ListenerForward;
        private static Vector3 _ListenerRight;
        private static Vector3 _ListenerUp;

        private static int _SoundTime;
        private static int _PaintedTime;
        private static bool _SoundStarted;
        private static int _SoundBlocked = 0;
        private static int _OldSamplePos;
        private static int _Buffers;
        private static int _PlayHash = 345;
        private static int _PlayVolHash = 543;

        public static void Init()
        {
            Con.Print( "\nSound Initialization\n" );

            if( Common.HasParam( "-nosound" ) )
                return;

            for( int i = 0; i < _Channels.Length; i++ )
                _Channels[i] = new channel_t();

            Cmd.Add( "play", Play );
            Cmd.Add( "playvol", PlayVol );
            Cmd.Add( "stopsound", StopAllSoundsCmd );
            Cmd.Add( "soundlist", SoundList );
            Cmd.Add( "soundinfo", SoundInfo_f );

            _IsInitialized = true;

            Startup();

            InitScaletable();

            _NumSfx = 0;

            Con.Print( "Sound sampling rate: {0}\n", _shm.speed );

            // provides a tick sound until washed clean
            _AmbientSfx[Ambients.AMBIENT_WATER] = PrecacheSound( "ambience/water1.wav" );
            _AmbientSfx[Ambients.AMBIENT_SKY] = PrecacheSound( "ambience/wind2.wav" );

            StopAllSounds( true );
        }

        public static void AmbientOff()
        {
            _Ambient = false;
        }

        public static void AmbientOn()
        {
            _Ambient = true;
        }

        public static void Shutdown()
        {
            if( !_Controller.IsInitialized )
                return;

            if( _shm != null )
                _shm.gamealive = false;

            _Controller.Shutdown();
            _shm = null;
        }

        public static void TouchSound( string sample )
        {
            if( !_Controller.IsInitialized )
                return;

            sfx_t sfx = FindName( sample );
            Cache.Check( sfx.cache );
        }

        public static void ClearBuffer()
        {
            if( !_Controller.IsInitialized || _shm == null || _shm.buffer == null )
                return;

            _Controller.ClearBuffer();
        }

        public static void StaticSound( sfx_t sfx, ref Vector3 origin, float vol, float attenuation )
        {
            if( sfx == null )
                return;

            if( _TotalChannels == MAX_CHANNELS )
            {
                Con.Print( "total_channels == MAX_CHANNELS\n" );
                return;
            }

            channel_t ss = _Channels[_TotalChannels];
            _TotalChannels++;

            sfxcache_t sc = LoadSound( sfx );
            if( sc == null )
                return;

            if( sc.loopstart == -1 )
            {
                Con.Print( "Sound {0} not looped\n", sfx.name );
                return;
            }

            ss.sfx = sfx;
            ss.origin = origin;
            ss.master_vol = (int)vol;
            ss.dist_mult = ( attenuation / 64 ) / _SoundNominalClipDist;
            ss.end = _PaintedTime + sc.length;

            Spatialize( ss );
        }

        public static void StartSound( int entnum, int entchannel, sfx_t sfx, ref Vector3 origin, float fvol, float attenuation )
        {
            if( !_SoundStarted || sfx == null )
                return;

            if( _NoSound.Value != 0 )
                return;

            int vol = (int)( fvol * 255 );

            // pick a channel to play on
            channel_t target_chan = PickChannel( entnum, entchannel );
            if( target_chan == null )
                return;

            // spatialize
            target_chan.origin = origin;
            target_chan.dist_mult = attenuation / _SoundNominalClipDist;
            target_chan.master_vol = vol;
            target_chan.entnum = entnum;
            target_chan.entchannel = entchannel;
            Spatialize( target_chan );

            if( target_chan.leftvol == 0 && target_chan.rightvol == 0 )
                return;  // not audible at all

            // new channel
            sfxcache_t sc = LoadSound( sfx );
            if( sc == null )
            {
                target_chan.sfx = null;
                return;  // couldn't load the sound's data
            }

            target_chan.sfx = sfx;
            target_chan.pos = 0;
            target_chan.end = _PaintedTime + sc.length;

            /* if an identical sound has also been started this frame, offset the pos
             * a bit to keep it from just making the first one louder
             */
            for( int i = Ambients.NUM_AMBIENTS; i < Ambients.NUM_AMBIENTS + MAX_DYNAMIC_CHANNELS; i++ )
            {
                channel_t check = _Channels[i];
                if( check == target_chan )
                    continue;

                if( check.sfx == sfx && check.pos == 0 )
                {
                    int skip = Sys.Random( (int)( 0.1 * _shm.speed ) );
                    if( skip >= target_chan.end )
                        skip = target_chan.end - 1;
                    target_chan.pos += skip;
                    target_chan.end -= skip;
                    break;
                }
            }
        }

        public static void StopSound( int entnum, int entchannel )
        {
            for( int i = 0; i < MAX_DYNAMIC_CHANNELS; i++ )
            {
                if( _Channels[i].entnum == entnum &&
                    _Channels[i].entchannel == entchannel )
                {
                    _Channels[i].end = 0;
                    _Channels[i].sfx = null;
                    return;
                }
            }
        }

        public static sfx_t PrecacheSound( string sample )
        {
            if( !_IsInitialized || _NoSound.Value != 0 )
                return null;

            sfx_t sfx = FindName( sample );

            // cache it in
            if( _Precache.Value != 0 )
                LoadSound( sfx );

            return sfx;
        }

        public static void ClearPrecache()
        {
            // nothing to do
        }

        // Called once each time through the main loop
        public static void Update( ref Vector3 origin, ref Vector3 forward, ref Vector3 right, ref Vector3 up )
        {
            if( !_IsInitialized || ( _SoundBlocked > 0 ) )
                return;

            _ListenerOrigin = origin;
            _ListenerForward = forward;
            _ListenerRight = right;
            _ListenerUp = up;

            // update general area ambient sound sources
            UpdateAmbientSounds();

            channel_t combine = null;

            // update spatialization for static and dynamic sounds
            for( int i = Ambients.NUM_AMBIENTS; i < _TotalChannels; i++ )
            {
                channel_t ch = _Channels[i];
                if( ch.sfx == null )
                    continue;

                Spatialize( ch );  // respatialize channel
                if( ch.leftvol == 0 && ch.rightvol == 0 )
                    continue;

                /* try to combine static sounds with a previous channel of the same
                 * sound effect so we don't mix five torches every frame
                 */
                if( i >= MAX_DYNAMIC_CHANNELS + Ambients.NUM_AMBIENTS )
                {
                    // see if it can just use the last one
                    if( combine != null && combine.sfx == ch.sfx )
                    {
                        combine.leftvol += ch.leftvol;
                        combine.rightvol += ch.rightvol;
                        ch.leftvol = ch.rightvol = 0;
                        continue;
                    }
                    // search for one
                    combine = _Channels[MAX_DYNAMIC_CHANNELS + Ambients.NUM_AMBIENTS];
                    int j;
                    for( j = MAX_DYNAMIC_CHANNELS + Ambients.NUM_AMBIENTS; j < i; j++ )
                    {
                        combine = _Channels[j];
                        if( combine.sfx == ch.sfx )
                            break;
                    }

                    if( j == _TotalChannels )
                    {
                        combine = null;
                    }
                    else
                    {
                        if( combine != ch )
                        {
                            combine.leftvol += ch.leftvol;
                            combine.rightvol += ch.rightvol;
                            ch.leftvol = ch.rightvol = 0;
                        }
                        continue;
                    }
                }
            }

            // debugging output
            if( _Show.Value != 0 )
            {
                int total = 0;
                for( int i = 0; i < _TotalChannels; i++ )
                {
                    channel_t ch = _Channels[i];
                    if( ch.sfx != null && ( ch.leftvol > 0 || ch.rightvol > 0 ) )
                    {
                        total++;
                    }
                }
                Con.Print( "----({0})----\n", total );
            }

            // mix some sound
            Update();
        }

        public static void StopAllSounds( bool clear )
        {
            if( !_Controller.IsInitialized )
                return;

            _TotalChannels = MAX_DYNAMIC_CHANNELS + Ambients.NUM_AMBIENTS; // no statics

            for( int i = 0; i < MAX_CHANNELS; i++ )
                if( _Channels[i].sfx != null )
                    _Channels[i].Clear();

            if( clear )
                ClearBuffer();
        }

        public static void BeginPrecaching()
        {
        }

        public static void EndPrecaching()
        {
        }

        public static void ExtraUpdate()
        {
            if( !_IsInitialized )
                return;
#if _WIN32
         IN_Accumulate ();
#endif

            if( _NoExtraUpdate.Value != 0 )
                return;  // don't pollute timings

            Update();
        }

        public static void LocalSound( string sound )
        {
            if( _NoSound.Value != 0 )
                return;

            if( !_Controller.IsInitialized )
                return;

            sfx_t sfx = PrecacheSound( sound );
            if( sfx == null )
            {
                Con.Print( "S_LocalSound: can't cache {0}\n", sound );
                return;
            }
            StartSound( Client.cl.viewentity, -1, sfx, ref Common.ZeroVector, 1, 1 );
        }

        public static void Startup()
        {
            if( _IsInitialized && !_Controller.IsInitialized )
            {
                _Controller.Init();
                _SoundStarted = _Controller.IsInitialized;
            }
        }

        public static void BlockSound()
        {
            _SoundBlocked++;

            if( _SoundBlocked == 1 )
            {
                _Controller.ClearBuffer();
            }
        }

        public static void UnblockSound()
        {
            _SoundBlocked--;
        }

        private static void Play()
        {
            for( int i = 1; i < Cmd.Argc; i++ )
            {
                string name = Cmd.Argv( i );
                int k = name.IndexOf( '.' );
                if( k == -1 )
                    name += ".wav";

                sfx_t sfx = PrecacheSound( name );
                StartSound( _PlayHash++, 0, sfx, ref _ListenerOrigin, 1.0f, 1.0f );
            }
        }

        private static void PlayVol()
        {
            for( int i = 1; i < Cmd.Argc; i += 2 )
            {
                string name = Cmd.Argv( i );
                int k = name.IndexOf( '.' );
                if( k == -1 )
                    name += ".wav";

                sfx_t sfx = PrecacheSound( name );
                float vol = float.Parse( Cmd.Argv( i + 1 ) );
                StartSound( _PlayVolHash++, 0, sfx, ref _ListenerOrigin, vol, 1.0f );
            }
        }

        private static void SoundList()
        {
            int total = 0;
            for( int i = 0; i < _NumSfx; i++ )
            {
                sfx_t sfx = _KnownSfx[i];
                sfxcache_t sc = (sfxcache_t)Cache.Check( sfx.cache );
                if( sc == null )
                    continue;

                int size = sc.length * sc.width * ( sc.stereo + 1 );
                total += size;
                if( sc.loopstart >= 0 )
                    Con.Print( "L" );
                else
                    Con.Print( " " );
                Con.Print( "({0:d2}b) {1:g6} : {2}\n", sc.width * 8, size, sfx.name );
            }
            Con.Print( "Total resident: {0}\n", total );
        }

        private static void SoundInfo_f()
        {
            if( !_Controller.IsInitialized || _shm == null )
            {
                Con.Print( "sound system not started\n" );
                return;
            }

            Con.Print( "{0:d5} stereo\n", _shm.channels - 1 );
            Con.Print( "{0:d5} samples\n", _shm.samples );
            Con.Print( "{0:d5} samplepos\n", _shm.samplepos );
            Con.Print( "{0:d5} samplebits\n", _shm.samplebits );
            Con.Print( "{0:d5} submission_chunk\n", _shm.submission_chunk );
            Con.Print( "{0:d5} speed\n", _shm.speed );
            Con.Print( "{0:d5} total_channels\n", _TotalChannels );
        }

        private static void StopAllSoundsCmd()
        {
            StopAllSounds( true );
        }

        private static sfx_t FindName( string name )
        {
            if( String.IsNullOrEmpty( name ) )
                Sys.Error( "S_FindName: NULL or empty\n" );

            if( name.Length >= QDef.MAX_QPATH )
                Sys.Error( "Sound name too long: {0}", name );

            // see if already loaded
            for( int i = 0; i < _NumSfx; i++ )
            {
                if( _KnownSfx[i].name == name )
                    return _KnownSfx[i];
            }

            if( _NumSfx == MAX_SFX )
                Sys.Error( "S_FindName: out of sfx_t" );

            sfx_t sfx = _KnownSfx[_NumSfx];
            sfx.name = name;

            _NumSfx++;
            return sfx;
        }

        private static void Spatialize( channel_t ch )
        {
            // anything coming from the view entity will allways be full volume
            if( ch.entnum == Client.cl.viewentity )
            {
                ch.leftvol = ch.master_vol;
                ch.rightvol = ch.master_vol;
                return;
            }

            // calculate stereo seperation and distance attenuation
            sfx_t snd = ch.sfx;
            Vector3 source_vec = ch.origin - _ListenerOrigin;

            float dist = Mathlib.Normalize( ref source_vec ) * ch.dist_mult;
            float dot = Vector3.Dot( _ListenerRight, source_vec );

            float rscale, lscale;
            if( _shm.channels == 1 )
            {
                rscale = 1.0f;
                lscale = 1.0f;
            }
            else
            {
                rscale = 1.0f + dot;
                lscale = 1.0f - dot;
            }

            // add in distance effect
            float scale = ( 1.0f - dist ) * rscale;
            ch.rightvol = (int)( ch.master_vol * scale );
            if( ch.rightvol < 0 )
                ch.rightvol = 0;

            scale = ( 1.0f - dist ) * lscale;
            ch.leftvol = (int)( ch.master_vol * scale );
            if( ch.leftvol < 0 )
                ch.leftvol = 0;
        }

        private static sfxcache_t LoadSound( sfx_t s )
        {
            // see if still in memory
            sfxcache_t sc = (sfxcache_t)Cache.Check( s.cache );
            if( sc != null )
                return sc;

            // load it in
            string namebuffer = "sound/" + s.name;

            byte[] data = Common.LoadFile( namebuffer );
            if( data == null )
            {
                Con.Print( "Couldn't load {0}\n", namebuffer );
                return null;
            }

            wavinfo_t info = GetWavInfo( s.name, data );
            if( info.channels != 1 )
            {
                Con.Print( "{0} is a stereo sample\n", s.name );
                return null;
            }

            float stepscale = info.rate / (float)_shm.speed;
            int len = (int)( info.samples / stepscale );

            len *= info.width * info.channels;

            s.cache = Cache.Alloc( len, s.name );
            if( s.cache == null )
                return null;

            sc = new sfxcache_t();
            sc.length = info.samples;
            sc.loopstart = info.loopstart;
            sc.speed = info.rate;
            sc.width = info.width;
            sc.stereo = info.channels;
            s.cache.data = sc;

            ResampleSfx( s, sc.speed, sc.width, new ByteArraySegment( data, info.dataofs ) );

            return sc;
        }

        private static channel_t PickChannel( int entnum, int entchannel )
        {
            // Check for replacement sound, or find the best one to replace
            int first_to_die = -1;
            int life_left = 0x7fffffff;
            for( int ch_idx = Ambients.NUM_AMBIENTS; ch_idx < Ambients.NUM_AMBIENTS + MAX_DYNAMIC_CHANNELS; ch_idx++ )
            {
                if( entchannel != 0  // channel 0 never overrides
                    && _Channels[ch_idx].entnum == entnum
                    && ( _Channels[ch_idx].entchannel == entchannel || entchannel == -1 ) )
                {
                    // allways override sound from same entity
                    first_to_die = ch_idx;
                    break;
                }

                // don't let monster sounds override player sounds
                if( _Channels[ch_idx].entnum == Client.cl.viewentity && entnum != Client.cl.viewentity && _Channels[ch_idx].sfx != null )
                    continue;

                if( _Channels[ch_idx].end - _PaintedTime < life_left )
                {
                    life_left = _Channels[ch_idx].end - _PaintedTime;
                    first_to_die = ch_idx;
                }
            }

            if( first_to_die == -1 )
                return null;

            if( _Channels[first_to_die].sfx != null )
                _Channels[first_to_die].sfx = null;

            return _Channels[first_to_die];
        }

        private static void UpdateAmbientSounds()
        {
            if( !_Ambient )
                return;

            // calc ambient sound levels
            if( Client.cl.worldmodel == null )
                return;

            mleaf_t l = Mod.PointInLeaf( ref _ListenerOrigin, Client.cl.worldmodel );
            if( l == null || _AmbientLevel.Value == 0 )
            {
                for( int i = 0; i < Ambients.NUM_AMBIENTS; i++ )
                    _Channels[i].sfx = null;
                return;
            }

            for( int i = 0; i < Ambients.NUM_AMBIENTS; i++ )
            {
                channel_t chan = _Channels[i];
                chan.sfx = _AmbientSfx[i];

                float vol = _AmbientLevel.Value * l.ambient_sound_level[i];
                if( vol < 8 )
                    vol = 0;

                // don't adjust volume too fast
                if( chan.master_vol < vol )
                {
                    chan.master_vol += (int)( Host.FrameTime * _AmbientFade.Value );
                    if( chan.master_vol > vol )
                        chan.master_vol = (int)vol;
                }
                else if( chan.master_vol > vol )
                {
                    chan.master_vol -= (int)( Host.FrameTime * _AmbientFade.Value );
                    if( chan.master_vol < vol )
                        chan.master_vol = (int)vol;
                }

                chan.leftvol = chan.rightvol = chan.master_vol;
            }
        }

        private static void Update()
        {
            if( !_SoundStarted || ( _SoundBlocked > 0 ) )
                return;

            // Updates DMA time
            GetSoundTime();

            // check to make sure that we haven't overshot
            if( _PaintedTime < _SoundTime )
                _PaintedTime = _SoundTime;

            // mix ahead of current position
            int endtime = (int)( _SoundTime + _MixAhead.Value * _shm.speed );
            int samps = _shm.samples >> ( _shm.channels - 1 );
            if( endtime - _SoundTime > samps )
                endtime = _SoundTime + samps;

            PaintChannels( endtime );
        }

        private static void GetSoundTime()
        {
            int fullsamples = _shm.samples / _shm.channels;
            int samplepos = _Controller.GetPosition();
            if( samplepos < _OldSamplePos )
            {
                _Buffers++; // buffer wrapped

                if( _PaintedTime > 0x40000000 )
                {
                    // time to chop things off to avoid 32 bit limits
                    _Buffers = 0;
                    _PaintedTime = fullsamples;
                    StopAllSounds( true );
                }
            }
            _OldSamplePos = samplepos;
            _SoundTime = _Buffers * fullsamples + samplepos / _shm.channels;
        }

        static Sound()
        {
            for( int i = 0; i < _KnownSfx.Length; i++ )
                _KnownSfx[i] = new sfx_t();
        }
    }

    internal class sfx_t
    {
        public string name;
        public cache_user_t cache;

        public void Clear()
        {
            this.name = null;
            cache = null;
        }
    }

    internal class sfxcache_t
    {
        public int length;
        public int loopstart;
        public int speed;
        public int width;
        public int stereo;
        public byte[] data;
    }

    internal class dma_t
    {
        public bool gamealive;
        public bool soundalive;
        public bool splitbuffer;
        public int channels;
        public int samples; // mono samples in buffer
        public int submission_chunk; // don't mix less than this #
        public int samplepos; // in mono samples
        public int samplebits;
        public int speed;
        public byte[] buffer;
    }

    [StructLayout( LayoutKind.Sequential )]
    internal class channel_t
    {
        public sfx_t sfx; // sfx number
        public int leftvol; // 0-255 volume
        public int rightvol; // 0-255 volume
        public int end; // end time in global paintsamples
        public int pos; // sample position in sfx
        public int looping; // where to loop, -1 = no looping
        public int entnum; // to allow overriding a specific sound
        public int entchannel;
        public Vector3 origin; // origin of sound effect
        public float dist_mult; // distance multiplier (attenuation/clipK)
        public int master_vol; // 0-255 master volume

        public void Clear()
        {
            sfx = null;
            leftvol = 0;
            rightvol = 0;
            end = 0;
            pos = 0;
            looping = 0;
            entnum = 0;
            entchannel = 0;
            origin = Vector3.Zero;
            dist_mult = 0;
            master_vol = 0;
        }
    }

    internal class wavinfo_t
    {
        public int rate;
        public int width;
        public int channels;
        public int loopstart;
        public int samples;
        public int dataofs; // chunk starts this many bytes from file start
    }

    internal interface ISoundController
    {
        bool IsInitialized
        {
            get;
        }

        void Init();

        void Shutdown();

        void ClearBuffer();

        byte[] LockBuffer();

        void UnlockBuffer( int count );

        int GetPosition();
    }
}
