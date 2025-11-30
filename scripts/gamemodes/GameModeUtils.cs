using System;
using Godot;

namespace racingGame;

public static class GameModeUtils
{
	private const string SavePbPath = "user://userdata.mdat";
	
	public static void TimeAttack()
	{
		GameModeController.CurrentGameMode = new GameModeTimeAttack();
	}

	public static string FormatRaceTime(TimeSpan raceTime)
		=> $"{raceTime:mm}:{raceTime:ss}.{raceTime:fff}";

	public static string FormatPbTime(TimeSpan raceTime)
		=> $"PB: {raceTime:mm}:{raceTime:ss}.{raceTime:fff}";

	public static string FormatCheckPointCount(int current, int total)
	{
		if (total == 0)
			return "";
		
		return current + "/" + total;
	}

	public static string FormatLapsCount(int current, int total)
	{
		if (total == 0)
			return "";
		
		return $"Lap {current + 1}/{total}";
	}

	public static string FormatTrackInfo(string trackName, string authorName)
	{
		if (trackName != "")
			return $"{trackName} by {authorName}";

		return "";
	}
	
	public static int GetGoldFromAt(int ms)
	{
		return Mathf.FloorToInt(ms * 1.2);
	}

	public static int GetSilverFromAt(int ms)
	{
		return Mathf.FloorToInt(ms * 1.6);
	}

	public static int GetBronzeFromAt(int ms)
	{
		return Mathf.FloorToInt(ms * 2.0);
	}

	public static string GetMedalFromTime(int timeMs, int atMs)
	{
		if (timeMs < atMs)
		{
			return "Diamond Medal";
		}
		if (timeMs < GetGoldFromAt(atMs))
		{
			return "Gold Medal";
		}
		if (timeMs < GetSilverFromAt(atMs))
		{
			return "Silver Medal";
		}
		if (timeMs < GetBronzeFromAt(atMs))
		{
			return "Bronze Medal";
		}
		
		return "No Medal";
	}

	public static void SaveUserPb(TimeSpan time, string trackUid)
	{
		if (time == TimeSpan.Zero || trackUid == "0") return;

		var config = new ConfigFile();
		config.LoadEncrypted(SavePbPath, "sosal?".Sha256Buffer());
		config.SetValue("PBS", trackUid, time.TotalMilliseconds);
		config.SaveEncrypted(SavePbPath, "sosal?".Sha256Buffer());
	}

	public static TimeSpan LoadUserPb(string trackUid)
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