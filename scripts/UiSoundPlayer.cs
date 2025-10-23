using Godot;

namespace racingGame;

public partial class UiSoundPlayer : Node
{
	public static UiSoundPlayer __Instance;

	[Export] public AudioStreamPlayer Player;

	[Export] public AudioStream BlockPlacedSound;

	private AudioStreamPlaybackPolyphonic _playback;

	public override void _Ready()
	{
		__Instance = this;

		var stream = new AudioStreamPolyphonic();
		stream.Polyphony = 32;

		Player.Stream = stream;
		Player.Play();

		_playback = Player.GetStreamPlayback() as AudioStreamPlaybackPolyphonic;
	}

	public void PlayBlockPlaced()
	{
		_playback.PlayStream(BlockPlacedSound, volumeDb: -3f);
	}
}
