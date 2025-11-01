using Godot;

namespace racingGame;

public partial class UiSoundPlayer : Node
{
	public static UiSoundPlayer Singleton;

	private AudioStreamPlaybackPolyphonic _playback;

	[Export] public AudioStream BlockPlacedSound;

	[Export] public AudioStreamPlayer Player;

	public override void _Ready()
	{
		Singleton = this;

		var stream = new AudioStreamPolyphonic();
		stream.Polyphony = 32;

		Player.Stream = stream;
		Player.Play();

		_playback = Player.GetStreamPlayback() as AudioStreamPlaybackPolyphonic;
	}

	public void PlayBlockPlaced(float pitchScale = 1f)
	{
		_playback.PlayStream(BlockPlacedSound, volumeDb: -3f, pitchScale: pitchScale);
	}
}