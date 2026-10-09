using System;
using System.Linq;
using Godot;

namespace racingGame;

public partial class GameModeController : Node
{
	public static IGameMode CurrentGameMode;

	public static int CurrentGameModeType;
	
	public static bool IsHost;

	public static ulong TicksPerSecond = 0;
	public static DateTime TPSSecond = DateTime.MinValue;
	public static int TPSViolationsInARow = 0;
	
	public override void _Ready()
	{
		GameModeUtils.LaunchGameMode(GameModeUtils.GAMEMODE_TIMEATTACK);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (CurrentGameMode.Running())
		{
			CurrentGameMode.Tick();
			
			if (MultiplayerManager.Instance.OnServer)
			{
				MultiplayerManager.Instance.UpdateGameModeInfo();
			}
			
			//anti speedhack
			TicksPerSecond++;
			if (TPSSecond == DateTime.MinValue || (DateTime.Now-TPSSecond).TotalSeconds > 1)
			{
				if (TPSSecond != DateTime.MinValue && (TicksPerSecond < 125 || TicksPerSecond > 131))
				{
					GD.Print("TPS violated with " + TicksPerSecond);
					
					TPSViolationsInARow++;
					if (TPSViolationsInARow > 2)
					{
						CurrentGameMode.RestartPlayer(MultiplayerManager.HOST_ID);
						TPSViolationsInARow = 1;
					}
				}
				else
				{
					TPSViolationsInARow = 0;
				}
				
				TicksPerSecond = 0;
				TPSSecond = DateTime.Now;
			}
			//--
		}
		else
		{
			TicksPerSecond = 0;
			TPSSecond = DateTime.MinValue;
		}
	}

	public static void InitGameMode(bool Host)
	{
		IsHost = Host;
		CurrentGameMode.InitGameMode();
	}

	public static void LoadMap(Track track)
	{
		track.ResetPhysBlocks(false);

		CurrentGameMode.InitTrack(track);
	}

	public static void UnloadMap(Track track)
	{
		track.ResetPhysBlocks(true);
		
		CurrentGameMode.KillGame();
	}
}