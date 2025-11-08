using System;
using Godot;

namespace racingGame;

public class GameModeUtils
{
	private const string SavePbPath = "user://userdata.mdat";
	private int _startTime = -1;
	
	public void TimeAttack()
	{
		GameModeController.CurrentGameMode = new GameModeTimeAttack();
	}

	public void UpdateLocalRaceTime(TimeSpan raceTime)
	{
		GameManager.Singleton.TimeLabel.Text =
			raceTime.ToString("mm") + ":" + raceTime.ToString("ss") + "." + raceTime.ToString("fff");
	}

	public void UpdateLocalPb(TimeSpan newPb)
	{
		GameManager.Singleton.PbLabel.Text =
			"PB: " + newPb.ToString("mm") + ":" + newPb.ToString("ss") + "." + newPb.ToString("fff");
	}

	public void OpenFinishWindow(TimeSpan finishTime, bool isPb, bool isEditor)
	{
		GameManager.Singleton.FinishTimeLabel.Text = "Race Time: " + finishTime.ToString("mm") + ":" +
		                                             finishTime.ToString("ss") + "." + finishTime.ToString("fff");
		if (isPb)
		{
			if (!isEditor)
				GameManager.Singleton.FinishTimeLabel.Text += "\nPersonal Best!!!";
			else
				GameManager.Singleton.FinishTimeLabel.Text += "\nNew Author Time!!!";
		}

		if (!isEditor)
		{
			var at = GameManager.Singleton.Track.Options.AuthorTime;
			if (finishTime.TotalMilliseconds <= at)
				GameManager.Singleton.FinishTimeLabel.Text += "\nDiamond Medal!!!!";
			else if (finishTime.TotalMilliseconds <= GetGoldFromAt(at))
				GameManager.Singleton.FinishTimeLabel.Text += "\nGold Medal!!!";
			else if (finishTime.TotalMilliseconds <= GetSilverFromAt(at))
				GameManager.Singleton.FinishTimeLabel.Text += "\nSilver Medal!!";
			else if (finishTime.TotalMilliseconds <= GetBronzeFromAt(at))
				GameManager.Singleton.FinishTimeLabel.Text += "\nBronze Medal!";
		}

		GameManager.Singleton.FinishPanel.Show();
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	public void SetStartTimer(int time, bool playSound = true)
	{
		if (_startTime != time)
		{
			_startTime = time;

			if (playSound)
			{
				if (time == 0)
					UiSoundPlayer.Singleton.RaceStartSound.Play();
				else
					UiSoundPlayer.Singleton.RaceCountDownSound.Play();
			}
		}

		if (time > 0)
		{
			GameManager.Singleton.StartTimerLabel.Show();
			GameManager.Singleton.StartTimerLabel.Text = time.ToString();
		}
		else
		{
			GameManager.Singleton.StartTimerLabel.Hide();
		}
	}

	public void SetCheckPointCount(int current, int total)
	{
		if (total == 0)
			GameManager.Singleton.CheckPointLabel.Text = "";
		else
			GameManager.Singleton.CheckPointLabel.Text = current + "/" + total;
	}

	public void SetLapsCount(int current, int total)
	{
		if (total == 0)
			GameManager.Singleton.LapsLabel.Text = "";
		else
			GameManager.Singleton.LapsLabel.Text = $"Lap {current + 1}/{total}";
	}

	public void SetTrackInfo(string trackName, string authorName)
	{
		if (trackName != "")
			GameManager.Singleton.TrackInfoLabel.Text = trackName + " by " + authorName;
		else
			GameManager.Singleton.TrackInfoLabel.Text = "";
	}

	public void UnloadLocalStats()
	{
		GameManager.Singleton.PbLabel.Text = "PB: ";
		UpdateLocalRaceTime(TimeSpan.Zero);
		SetStartTimer(0, false);
		SetCheckPointCount(0, 0);
		SetLapsCount(0, 0);
		SetTrackInfo("", "");
	}

	public string TakePreviewScreenshot()
	{
		GameManager.Singleton.SetGameUiVisiblity(false);
		Image img = GameManager.Singleton.GetViewport().GetTexture().GetImage();
		GameManager.Singleton.SetGameUiVisiblity(true);
		
		img.Resize(1280, 720, Image.Interpolation.Bilinear);
		return Marshalls.RawToBase64(img.SaveJpgToBuffer());
	}

	//TRACK INFO
	public int GetGoldFromAt(int ms)
	{
		return Mathf.FloorToInt(ms * 1.2);
	}

	public int GetSilverFromAt(int ms)
	{
		return Mathf.FloorToInt(ms * 1.6);
	}

	public int GetBronzeFromAt(int ms)
	{
		return Mathf.FloorToInt(ms * 2.0);
	}

	public void SaveUserPb(TimeSpan time, string trackUid)
	{
		if (time == TimeSpan.Zero || trackUid == "0") return;

		var config = new ConfigFile();
		config.LoadEncrypted(SavePbPath, "sosal?".Sha256Buffer());
		config.SetValue("PBS", trackUid, time.TotalMilliseconds);
		config.SaveEncrypted(SavePbPath, "sosal?".Sha256Buffer());
	}

	public TimeSpan LoadUserPb(string trackUid)
	{
		var config = new ConfigFile();
		var err = config.LoadEncrypted(SavePbPath, "sosal?".Sha256Buffer());
		if (err == Error.Ok)
		{
			var ms = (int)config.GetValue("PBS", trackUid, 0);
			if (ms != 0) return TimeSpan.FromMilliseconds(ms);
		}

		return TimeSpan.Zero;
	}
	//----//
}