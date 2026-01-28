using System;
using System.Text;
using Godot;
using Newtonsoft.Json;
using racingGame.data;

namespace racingGame;

public static class GameModeUtils
{
	private const string SavePbPath = "user://userdata.mdat";
	private const string SaveGhostPath = "user://ghosts.mdat";

	//Player Types
	public const int PLAYER_LOCAL = 1;
	public const int PLAYER_LOCAL_SPLITSCREEN = 2;
	public const int PLAYER_ONLINE = 3;
	
	public const int MEDAL_NONE = 0;
	public const int MEDAL_BRONZE = 1;
	public const int MEDAL_SILVER = 2;
	public const int MEDAL_GOLD = 3;
	public const int MEDAL_AUTHOR = 4;
	public const int MEDAL_MAX = 4;

	public static readonly string[] MEDAL_NAME = {
		"No Medal",
		"Bronze Medal",
		"Silver Medal",
		"Gold Medal",
		"Author Medal",
	};
	
	public static void TimeAttack()
	{
		GameModeController.CurrentGameMode = new GameModeTimeAttack();
	}

	public static string FormatRaceTime(TimeSpan raceTime)
		=> $"{raceTime:mm}:{raceTime:ss}.{raceTime:fff}";
	
	public static string FormatRaceTime(int raceTimeMS)
		=> FormatRaceTime(new TimeSpan(0, 0, 0, 0, raceTimeMS));

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
		return MEDAL_NAME[GetMedalIdFromTime(timeMs, atMs)];
	}

	public static int GetMedalIdFromTime(int timeMs, int atMs)
	{
		if (timeMs < atMs)
		{
			return MEDAL_AUTHOR;
		}
		if (timeMs < GetGoldFromAt(atMs))
		{
			return MEDAL_GOLD;
		}
		if (timeMs < GetSilverFromAt(atMs))
		{
			return MEDAL_SILVER;
		}
		if (timeMs < GetBronzeFromAt(atMs))
		{
			return MEDAL_BRONZE;
		}

		return MEDAL_NONE;
	}

	public static void SaveUserPb(TimeSpan time, string trackUid)
	{
		if (time == TimeSpan.Zero || trackUid == "0") return;

		var config = new ConfigFile();
		config.LoadEncrypted(SavePbPath, "sosal?".Sha256Buffer());
		config.SetValue("PBS", trackUid, time.TotalMilliseconds);
		config.SaveEncrypted(SavePbPath, "sosal?".Sha256Buffer());
	}

	public static void SaveUserGhost(Ghost ghost, string trackUid)
	{
		if (ghost.Empty) return;
		
		var config = new ConfigFile();
		config.LoadEncrypted(SaveGhostPath, "mimimi".Sha256Buffer());
		config.SetValue("Ghosts", trackUid, Convert.ToBase64String(Encoding.Default.GetBytes(JsonConvert.SerializeObject(ghost, Formatting.Indented, Jz.Settings))));
		config.SaveEncrypted(SaveGhostPath, "mimimi".Sha256Buffer());
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

	public static Ghost LoadUserGhost(string trackUid)
	{
		var config = new ConfigFile();
		var err = config.LoadEncrypted(SaveGhostPath, "mimimi".Sha256Buffer());
		if (err == Error.Ok)
		{
			string ghostString = (string)config.GetValue("Ghosts", trackUid, 0);
			if (ghostString.Length > 1)
			{
				string json = Encoding.Default.GetString(Convert.FromBase64String(ghostString));

				return JsonConvert.DeserializeObject<Ghost>(json);
			}
		}

		return new Ghost();
	}
	//----//

	public static void RestartPlayer(long PlayerId)
	{
		GameModeController.CurrentGameMode.RestartPlayer(PlayerId);
		
		if (MultiplayerManager.Instance.OnServer && !MultiplayerManager.Instance.IsServer())
		{
			MultiplayerManager.Instance.ClientRequestRestart();
		}		
	}
}