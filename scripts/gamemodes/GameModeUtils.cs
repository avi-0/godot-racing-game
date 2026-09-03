using System;
using System.Text;
using System.Threading;
using Godot;
using Newtonsoft.Json;
using racingGame.data;

namespace racingGame;

public static class GameModeUtils
{
	private const string SavePbPath = "user://userdata.mdat";
	private const string SaveGhostPath = "user://ghosts.mdat";

	//Player Types
	public const int PLAYER_EMPTY = 0;
	public const int PLAYER_LOCAL = 1;
	public const int PLAYER_LOCAL_SPLITSCREEN = 2;
	public const int PLAYER_ONLINE = 3;
	public const int PLAYER_BOT = 4;
	//--
	
	//Player States
	public const int PLAYER_STATE_NONE = 0;
	public const int PLAYER_STATE_CONNECTING = 1;
	public const int PLAYER_STATE_LOADING = 2;
	public const int PLAYER_STATE_SPECTATING = 3;
	public const int PLAYER_STATE_PRESTART = 4;
	public const int PLAYER_STATE_PLAYING = 5;
	public const int PLAYER_STATE_AFTERFINISH = 6;
	public const int PLAYER_STATE_DEAD = 7;
	//--
	
	//Track Types
	public const int TRACK_TYPE_NONE = 0;
	public const int TRACK_TYPE_RACE = 1;
	public const int TRACK_TYPE_ARENA = 2;
	public const int TRACK_TYPE_RING = 3;
	public const int TRACK_TYPE_ADVENTURE = 4;
	
	public static readonly string[] TRACK_TYPE_NAME = 
	{
		"None",
		"Race",
		"Arena",
		"Ring",
		"Adventure",
	};
	//--
	
	//Gamemodes
	public const int GAMEMODE_NONE = 0;
	public const int GAMEMODE_TIMEATTACK = 1;
	public const int GAMEMODE_ROUNDS = 2;
	public const int GAMEMODE_CHASE = 3;
	public const int GAMEMODE_DERBY = 4;
	public const int GAMEMODE_ADVENTURE = 5;

	public static readonly string[] GAMEMODE_NAME =
	{
		"None",
		"Time Attack",
		"Rounds",
		"Chase",
		"Derby",
		"Adventure",
	};
	//--
	
	//Medals
	public const int MEDAL_NONE = 0;
	public const int MEDAL_BRONZE = 1;
	public const int MEDAL_SILVER = 2;
	public const int MEDAL_GOLD = 3;
	public const int MEDAL_AUTHOR = 4;
	public const int MEDAL_MAX = 4;

	public static readonly string[] MEDAL_NAME = 
	{
		"No Medal",
		"Bronze Medal",
		"Silver Medal",
		"Gold Medal",
		"Author Medal",
	};
	//--

	public static void LaunchGameMode(int type)
	{
		GameModeController.CurrentGameModeType = type;
		switch (type)
		{
			case GAMEMODE_TIMEATTACK:
				GameModeController.CurrentGameMode = new GameModeTimeAttack();
				break;
		}
	}

	public static bool GameModeSupportsTrackType(int gamemode, int track)
	{
		if (track == TRACK_TYPE_RACE && (gamemode == GAMEMODE_TIMEATTACK || gamemode == GAMEMODE_ROUNDS)) { return true; }
		if (track == TRACK_TYPE_ARENA && (gamemode == GAMEMODE_DERBY || gamemode == GAMEMODE_CHASE)) { return true; }
		if (track == TRACK_TYPE_RING && (gamemode == GAMEMODE_DERBY)) { return true; }
		if (track == TRACK_TYPE_ADVENTURE && (gamemode == GAMEMODE_ADVENTURE)) { return true; }
		
		return false;
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

	public static string FormatTimeDiff(TimeSpan newTime, TimeSpan oldTime)
	{
		double diff = newTime.TotalMilliseconds - oldTime.TotalMilliseconds;
		string diffText = (diff / 1000).ToString("0.000");
		if (diff > 0)
		{
			diffText = "+" + diffText;
		}
		return diffText;
	}
	
	public static int GetGoldFromAt(int ms)
	{
		return Mathf.FloorToInt(ms * 1.2);
	}

	public static int GetSilverFromAt(int ms)
	{
		return Mathf.FloorToInt(ms * 1.45);
	}

	public static int GetBronzeFromAt(int ms)
	{
		return Mathf.FloorToInt(ms * 1.8);
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
	
	public static void SaveUserFastestLap(TimeSpan time, string trackUid)
	{
		if (time == TimeSpan.Zero || trackUid == "0") return;

		var config = new ConfigFile();
		config.LoadEncrypted(SavePbPath, "sosal?".Sha256Buffer());
		config.SetValue("FASTESTLAPS", trackUid, time.TotalMilliseconds);
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
	
	public static TimeSpan LoadUserFastestLap(string trackUid)
	{
		var config = new ConfigFile();
		var err = config.LoadEncrypted(SavePbPath, "sosal?".Sha256Buffer());
		if (err == Error.Ok)
		{
			var ms = (int)config.GetValue("FASTESTLAPS", trackUid, 0);
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

	public static void RestartPlayer(long PlayerId)
	{
		GameModeController.CurrentGameMode.RestartPlayer(PlayerId);
		
		if (MultiplayerManager.Instance.OnServer && !MultiplayerManager.Instance.IsServer())
		{
			MultiplayerManager.Instance.ClientRequestRestart();
		}		
	}
}