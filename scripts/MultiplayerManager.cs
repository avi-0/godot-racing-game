using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using racingGame;

public partial class MultiplayerManager : Node
{
	public static MultiplayerManager Instance;

	
	[Signal]
	public delegate void ConnectionAttemptEndedEventHandler();
	
	private const string LOCAL_HOST = "localhost";
	private const int PORT = 1342;
	private const int MAX_PLAYERS = 32;

	public const int HOST_ID = 1;
	public const int CONNECTION_STATUS_CONNECTED = 0;
	public const int CONNECTION_STATUS_FAILED = 1;
	public const int CONNECTION_STATUS_DISCONNECTED = 2;

	public int LastConnectionAttemptStatus;
	
	public struct ServerInfoStruct
	{
		public bool IsDedicated = false;
		public string TrackPath;
		public List<PlayerInfoStruct> Players = new List<PlayerInfoStruct>();

		public ServerInfoStruct(string trackPath)
		{
			this.TrackPath = trackPath;
		}

		public string Json()
		{
			return JsonConvert.SerializeObject(this);
		}
		public ServerInfoStruct FromJson(string json)
		{
			return JsonConvert.DeserializeObject<ServerInfoStruct>(json);
		}
	}
	public struct PlayerInfoStruct
	{
		public string PlayerName;
		public long PlayerId;

		public PlayerInfoStruct(long playerId, string playerName)
		{
			this.PlayerId = playerId;
			this.PlayerName = playerName;
		}

		public string Json()
		{
			return JsonConvert.SerializeObject(this);
		}
		public PlayerInfoStruct FromJson(string json)
		{
			return JsonConvert.DeserializeObject<PlayerInfoStruct>(json);
		}
	}
	
	public ServerInfoStruct ServerInfo;
	public PlayerInfoStruct PlayerInfo;	
	public ENetMultiplayerPeer MultiplayerPeer;
	
	public bool OnServer = false;
	
	public override void _Ready()
	{
		Instance = this;

		Multiplayer.ConnectedToServer += ClientConnectionStatusSuccess;
		Multiplayer.ConnectionFailed += ClientConnectionStatusFailed;
		Multiplayer.ServerDisconnected += ClientConnectionStatusDisconnected;
	}
	public override void _Process(double delta)
	{
	}

	private void mprint(string msg)
	{
		GD.Print("MULTIPLAYERMANAGER | " + msg);
	}

	//SHARED
	public void TerminateConnection()
	{
		MultiplayerPeer = null;
		Multiplayer.MultiplayerPeer = null;
		OnServer = false;
		GameManager.Instance.Stop();
	}

	public void RemoveClient(long id)
	{
		GameModeController.CurrentGameMode.DeletePlayer(id);
		ServerInfo.Players.Remove(ServerInfo.Players.Find(@struct => @struct.PlayerId == id));
	}
	
	public bool IsServer()
	{
		return Multiplayer.IsServer();
	}
	//--
	
	//SERVER
	public void CreateServer(string trackPath)
	{
		ServerInfo = new ServerInfoStruct(trackPath);
		MultiplayerPeer = new ENetMultiplayerPeer();
		MultiplayerPeer.CreateServer(PORT, MAX_PLAYERS);
		Multiplayer.MultiplayerPeer = MultiplayerPeer;
		Multiplayer.PeerConnected += OnClientConnected;
		Multiplayer.PeerDisconnected += OnClientDisconnected;
		
		PlayerInfo  = new PlayerInfoStruct(MultiplayerPeer.GetUniqueId(), SettingsManager.Instance.Settings.PlayerName);
		ServerInfo.Players.Add(PlayerInfo);
		
		mprint("Host Server Info: " + ServerInfo.Json());
		
		OnServer = true;
	}

	public void OnClientConnected(long id)
	{
		mprint(id + " connected");
		RpcId(id, MethodName.SendServerInfoToClient, ServerInfo.Json());
	}

	public void OnClientDisconnected(long id)
	{
		mprint(id + " disconnected");
		Rpc(MethodName.SendClientDisconnectedInfoToOtherClients, id);
		RemoveClient(id);
	}

	public void UpdateGameModeInfo()
	{
		if (Multiplayer.IsServer())
		{
			Rpc(MethodName.SendGameModeInfoToClients, GameModeController.CurrentGameMode.GetGameModeInfoJson());
		}
	}
	//--

	//CLIENT
	public void CreateClient(string ipAddress)
	{
		MultiplayerPeer = new ENetMultiplayerPeer();
		Error error = MultiplayerPeer.CreateClient(ipAddress, PORT);

		if (error != Error.Ok)
		{
			ClientConnectionStatusFailed();
			return;
		}
		
		Multiplayer.MultiplayerPeer = MultiplayerPeer;
		ServerInfo = new ServerInfoStruct();
		PlayerInfo = new PlayerInfoStruct(MultiplayerPeer.GetUniqueId(), SettingsManager.Instance.Settings.PlayerName);
	}
	
	public void ClientRequestRestart()
	{
		RpcId(HOST_ID, MethodName.SendRespawnRequestToServer, MultiplayerPeer.GetUniqueId());
	}

	public void ClientConnectionStatusSuccess()
	{
		LastConnectionAttemptStatus = CONNECTION_STATUS_CONNECTED;
	}

	public void ClientConnectionStatusFailed()
	{
		TerminateConnection(); 
		LastConnectionAttemptStatus = CONNECTION_STATUS_FAILED; 
		GameManager.Instance.ShowMessage("Failed to connect to server");
		EmitSignalConnectionAttemptEnded();
	}

	public void ClientConnectionStatusDisconnected()
	{
		TerminateConnection(); 
		LastConnectionAttemptStatus = CONNECTION_STATUS_DISCONNECTED; 
		GameManager.Instance.ShowMessage("Server disconnected");
		EmitSignalConnectionAttemptEnded();
	}
	//--
	
	//server to client rpcs
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = Godot.MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SendServerInfoToClient(string info)
	{
		mprint("Server Info: " + info);
		ServerInfo = ServerInfo.FromJson(info);

		OnServer = true;
		EmitSignalConnectionAttemptEnded();

		foreach (PlayerInfoStruct player in ServerInfo.Players)
		{
			GameModeController.CurrentGameMode.AddPlayer(player.PlayerId, GameModeUtils.PLAYER_ONLINE, player.PlayerName);
		}

		Rpc(MethodName.SendPlayerInfoToEveryone, PlayerInfo.Json());
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = Godot.MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
	private void SendGameModeInfoToClients(string info)
	{
		if (GameModeController.CurrentGameMode.Running())
		{
			GameModeController.CurrentGameMode.LoadGameModeInfoJson(info);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = Godot.MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SendClientDisconnectedInfoToOtherClients(long id)
	{
		RemoveClient(id);
	}
	//--
	
	//client to clients rpcs
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = Godot.MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SendPlayerInfoToEveryone(string info)
	{
		PlayerInfoStruct newPlayerInfo = PlayerInfo.FromJson(info);
		mprint("New Player Name: " + newPlayerInfo.PlayerName);
		ServerInfo.Players.Add(newPlayerInfo);
		GameModeController.CurrentGameMode.AddPlayer(newPlayerInfo.PlayerId, GameModeUtils.PLAYER_ONLINE, newPlayerInfo.PlayerName);
	}
	//--
	
	//client to server rpcs
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = Godot.MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SendRespawnRequestToServer(long id)
	{
		if (Multiplayer.IsServer())
		{
			GameModeController.CurrentGameMode.RestartPlayer(id);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false,
		TransferMode = Godot.MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SendPlayerDisconnectedToServer(long id)
	{
		if (Multiplayer.IsServer())
		{
			MultiplayerPeer.DisconnectPeer((int)id);
		}
	}
	//--
}
