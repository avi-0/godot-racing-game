using Godot;

namespace racingGame;

public partial class UiSoundPlayer : Node
{
	public static UiSoundPlayer Singleton;

	private AudioStreamPlaybackPolyphonic _playback;

	[Export(PropertyHint.ResourceType, "UiSound")]
	public UiSound BlockErasedSound;

	[Export(PropertyHint.ResourceType, "UiSound")]
	public UiSound BlockPlacedSound;

	[Export(PropertyHint.ResourceType, "UiSound")]
	public UiSound CheckpointCollectedSound;

	[Export(PropertyHint.ResourceType, "UiSound")]
	public UiSound LapFinishedSound;

	[Export] public AudioStreamPlayer Player;

	[Export(PropertyHint.ResourceType, "UiSound")]
	public UiSound RaceCountDownSound;

	[Export(PropertyHint.ResourceType, "UiSound")]
	public UiSound RaceStartSound;
	
	[Export(PropertyHint.ResourceType, "UiSound")]
	public UiSound CheckpointCollectedWorseTimeSound;
	
	[Export(PropertyHint.ResourceType, "UiSound")]
	public UiSound RespawnSound1;
	
	[Export(PropertyHint.ResourceType, "UiSound")]
	public UiSound RespawnSound2;

	public override void _Ready()
	{
		Singleton = this;

		var stream = new AudioStreamPolyphonic();
		stream.Polyphony = 32;

		Player.Stream = stream;
		Player.Play();

		_playback = Player.GetStreamPlayback() as AudioStreamPlaybackPolyphonic;
	}

	public void Play(UiSound sound)
	{
		_playback.PlayStream(sound.AudioStream, volumeDb: sound.VolumeDb, pitchScale: sound.PitchScale);
	}
}