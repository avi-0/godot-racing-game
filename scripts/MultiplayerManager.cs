using Godot;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using racingGame;
using JsonSerializer = System.Text.Json.JsonSerializer;

public partial class MultiplayerManager : Node
{
	public static MultiplayerManager Instance;

	
	[Signal]
	public delegate void ConnectedToServerEventHandler();
	
	private const string LOCAL_HOST = "localhost";
	private const int PORT = 1342;
	private const int MAX_PLAYERS = 32;

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
	}
	public override void _Process(double delta)
	{
	}

	private void mprint(string msg)
	{
		GD.Print("MULTIPLAYERMANAGER | " + msg);
	}

	public void TerminateConnection()
	{
		Multiplayer.MultiplayerPeer = null;
		OnServer = false;
	}
	
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
		GameModeController.CurrentGameMode.AddPlayer(id, false, false, id.ToString());
	}

	public void OnClientDisconnected(long id)
	{
		mprint(id + " disconnected");
		GameModeController.CurrentGameMode.DeletePlayer(id);
		ServerInfo.Players.Remove(ServerInfo.Players.Find(@struct => @struct.PlayerId == id));
	}
	//--

	//CLIENT
	public void CreateClient(string ipAddress)
	{
		MultiplayerPeer = new ENetMultiplayerPeer();
		MultiplayerPeer.CreateClient(ipAddress, PORT);
		Multiplayer.MultiplayerPeer = MultiplayerPeer;
		ServerInfo = new ServerInfoStruct();
		PlayerInfo  = new PlayerInfoStruct(MultiplayerPeer.GetUniqueId(), SettingsManager.Instance.Settings.PlayerName);
		
		OnServer = true;
	}
	//--
	
	//server to client rpcs
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = Godot.MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SendServerInfoToClient(string info)
	{
		mprint("Server Info: " + info);
		ServerInfo = ServerInfo.FromJson(info);
		EmitSignalConnectedToServer();

		foreach (PlayerInfoStruct player in ServerInfo.Players)
		{
			GameModeController.CurrentGameMode.AddPlayer(player.PlayerId, false, false, player.PlayerName);
		}

		Rpc(MethodName.SendPlayerInfoToEveryone, PlayerInfo.Json());
	}
	//--
	
	//client to clients rpcs
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = Godot.MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SendPlayerInfoToEveryone(string info)
	{
		PlayerInfoStruct newPlayerInfo = PlayerInfo.FromJson(info);
		mprint("New Player Name: " + newPlayerInfo.PlayerName);
		ServerInfo.Players.Add(newPlayerInfo);
		GameModeController.CurrentGameMode.AddPlayer(newPlayerInfo.PlayerId, false, false, newPlayerInfo.PlayerName);
	}
	//--
}
