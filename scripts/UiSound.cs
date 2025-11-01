using Godot;

namespace racingGame;

[GlobalClass]
public partial class UiSound : Resource
{
	[Export] public AudioStream AudioStream;

	[Export] public float PitchScale = 1.0f;

	[Export(PropertyHint.None, "suffix:db")]
	public float VolumeDb;

	public void Play()
	{
		UiSoundPlayer.Singleton.Play(this);
	}
}