namespace racingGame.data;

public class TrackOptions
{
	public string Uid = "0";
	public string Name = "New Track";
	public string AuthorName = "Anonymous";
	public string TrackType = "TimeAttack";
	public string CarType = "thedriftcar.tscn";
	public string TrackBase = "grass";
	public string Message = "";
	public int Laps = 0;
	public int AuthorTime = 0;
	public int StartDayTime = 10;
	public string PreviewImage = "";
	public bool Fog = false;
	public bool Rain = false;
	public CameraPositionData PreviewCameraPosition = new();
}