using System.Collections.Generic;
using Godot;

namespace SwordInTheForest.Core;

/// <summary>Autoload "Sfx": fire-and-forget sound effects by name, and the music track.</summary>
public partial class Sfx : Node
{
    private const string SoundDir = "res://assets/third_party/ninja_adventure/sounds";
    private const int Voices = 10;

    private readonly List<AudioStreamPlayer> _voices = new();
    private readonly Dictionary<string, AudioStream> _streams = new();
    private readonly RandomNumberGenerator _rng = new();
    private AudioStreamPlayer? _music;
    private int _next;

    public static Sfx Instance { get; private set; } = null!;

    /// <summary>How often each sound hook fired; scenarios check these.</summary>
    public Dictionary<string, int> Played { get; } = new();

    public override void _EnterTree() => Instance = this;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        for (var i = 0; i < Voices; i++)
        {
            var p = new AudioStreamPlayer();
            AddChild(p);
            _voices.Add(p);
        }
    }

    /// <summary>Plays a sound from the pack by short name, e.g. "deflect". Pitch varies a little.</summary>
    public static void Play(string name, float pitchJitter = 0.05f, float volumeScale = 1f) => Instance?.PlayInternal(name, pitchJitter, volumeScale);

    private void PlayInternal(string name, float pitchJitter, float volumeScale)
    {
        Played[name] = Played.TryGetValue(name, out var n) ? n + 1 : 1;
        if (!_streams.TryGetValue(name, out var stream))
        {
            stream = GD.Load<AudioStream>($"{SoundDir}/{name}.wav");
            _streams[name] = stream;
        }
        var voice = _voices[_next];
        _next = (_next + 1) % _voices.Count;
        voice.Stream = stream;
        voice.PitchScale = 1f + _rng.RandfRange(-pitchJitter, pitchJitter);
        voice.VolumeDb = Mathf.LinearToDb(Mathf.Max(0.0001f, Tunables.Feel.SfxVolume * volumeScale));
        voice.Play();
    }

    public void PlayMusic(string path)
    {
        if (_music == null)
        {
            _music = new AudioStreamPlayer();
            AddChild(_music);
        }
        var stream = GD.Load<AudioStream>(path);
        if (stream is AudioStreamOggVorbis ogg) ogg.Loop = true;
        if (_music.Stream == stream && _music.Playing) return;
        _music.Stream = stream;
        _music.VolumeDb = Mathf.LinearToDb(Mathf.Max(0.0001f, Tunables.Feel.MusicVolume));
        _music.Play();
    }

    public override void _Process(double delta)
    {
        if (_music != null) _music.VolumeDb = Mathf.LinearToDb(Mathf.Max(0.0001f, Tunables.Feel.MusicVolume));
    }
}
