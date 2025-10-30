using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using racingGame;

public partial class GameMode : Node
{
	public static GameModeBase CurrentGameMode;
	public static GameModeUtils Utils;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Utils = new GameModeUtils();

		Utils.TimeAttack();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (CurrentGameMode.Running())
		{
			CurrentGameMode.Tick();
		}
	}
}

public interface GameModeBase
{
	bool Running();
	void Running(bool running);
	void Tick();
	void InitTrack(Node3D TrackNode);
	int SpawnPlayer(bool LocalPlayer, Car PlayerCar);
	void RespawnPlayer(int PlayerID);
	void PlayerAttemptFinish(int PlayerID);
	void KillGame();
}

public class GameModeUtils
{
	public void TimeAttack()
	{
		GameMode.CurrentGameMode = new GameModeTimeAttack();
	}

	public void UpdateLocalRaceTime(TimeSpan RaceTime)
	{
		GameManager.Singleton.TimeLabel.Text = RaceTime.ToString("mm") + ":" + RaceTime.ToString("ss") + "." + RaceTime.ToString("fff");
	}

	public void UpdateLocalPB(TimeSpan NewPB)
	{
		GameManager.Singleton.PBLabel.Text = "PB: " + NewPB.ToString("mm") + ":" + NewPB.ToString("ss") + "." + NewPB.ToString("fff");
	}	

	public void OpenFinishWindow(TimeSpan FinishTime, bool IsPB)
	{
		GameManager.Singleton.FinishTimeLabel.Text = "Race Time: " + FinishTime.ToString("mm") + ":" + FinishTime.ToString("ss") + "." + FinishTime.ToString("fff");
		if (IsPB)
		{
			GameManager.Singleton.FinishTimeLabel.Text += "\nPersonal Best!!!";
		}

		GameManager.Singleton.FinishPanel.Show();
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	public void UnloadLocalPB()
	{
		GameManager.Singleton.PBLabel.Text = "PB: ";
	}
}

//-------------- TIME ATTACK --------------\\
public class GameModeTimeAttack : GameModeBase
{
	private IList<TAPlayer> Players;
	private TAMap CurrentTrack;

	private bool _running = false;
	public void Running(bool running) {this._running = running;}
	public bool Running() {return _running;}

	public void Tick()
	{
		for (int PlayerID = 0; PlayerID < Players.Count; PlayerID++)
		{
			var Player = Players[PlayerID];

			if (Player.InGame && Player.RaceStartTime != null)
			{
				Player.CurrentRaceTime = DateTime.Now.Subtract(Player.RaceStartTime);
				if (Player.LocalPlayer)
				{
					GameMode.Utils.UpdateLocalRaceTime(Player.CurrentRaceTime);
				}
			}

			Players[PlayerID] = Player;
		}	
	}

	public void InitTrack(Node3D TrackNode)
	{
		CurrentTrack = new TAMap(TrackNode);
		Players = new List<TAPlayer>();


	}

	public int SpawnPlayer(bool LocalPlayer, Car PlayerCar)
	{
		var PlayerID = Players.Count;
		Players.Add(new TAPlayer(PlayerID, LocalPlayer, PlayerCar));
		var Player = Players[PlayerID];

		Player.RaceStartTime = DateTime.Now;
	
		Players[PlayerID] = Player;
		return PlayerID;
	}

	public void RespawnPlayer(int PlayerID)
	{
		var Player = Players[PlayerID];

		Player.RaceStartTime = DateTime.Now;
		Player.InGame = true;

		Players[PlayerID] = Player;
	}

	public void PlayerAttemptFinish(int PlayerID)
	{
		if (Players[PlayerID].InGame && Players[PlayerID].CheckPointsCollected == CurrentTrack.CheckPointCount)
		{
			PlayerFinished(PlayerID);
		}
	}	

	public void KillGame()
	{
		_running = false;
		Players = null;	
		GameMode.Utils.UnloadLocalPB();
	}

	private void PlayerFinished(int PlayerID)
	{
		var Player = Players[PlayerID];

		Player.InGame = false;
		Player.PlayerCar.AcceptsInputs = false;


		var IsPB = false;
		if (Player.PBTime.TotalMilliseconds == 0 || TimeSpan.Compare(Player.PBTime, Player.CurrentRaceTime) == 1)
		{
			IsPB = true;

			Player.PBTime = Player.CurrentRaceTime;
			if (Player.LocalPlayer)
			{
				GameMode.Utils.UpdateLocalPB(Player.PBTime);
			}
		}
		GameMode.Utils.OpenFinishWindow(Player.CurrentRaceTime, IsPB);

		Players[PlayerID] = Player;
	}
}

public struct TAPlayer
{
	public int PlayerID {get; init;}

	public bool LocalPlayer {get; set;} = false;
	public bool InGame {get; set;} = true;

	public int CheckPointsCollected {get; set;} = 0;
	public int LapsDone {get; set;} = 0;

	public DateTime RaceStartTime {get; set;}
	public TimeSpan CurrentRaceTime {get; set;}
	public TimeSpan PBTime {get; set;}

	public Car PlayerCar {get; set;}

	public TAPlayer(int PlayerID, bool LocalPlayer, Car PlayerCar)
	{
		this.PlayerID = PlayerID;
		this.LocalPlayer = LocalPlayer;
		this.PlayerCar = PlayerCar;
	}
}

public struct TAMap
{
	public Node3D TrackNode {get; init;}
	public int CheckPointCount {get; set;} = 0;

	public TAMap(Node3D TrackNode)
	{
		this.TrackNode = TrackNode;
	}
}
//-------------- TIME ATTACK --------------\\
