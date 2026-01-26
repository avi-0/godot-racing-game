using Godot;
using System;
using racingGame;

public partial class MultiplayerManager : Node
{
	public static MultiplayerManager Instance;
	
	public bool OnServer  = false;
	
	public ENetMultiplayerPeer MultiplayerPeer;
	
	[Signal]
	public delegate void ConnectedToServerEventHandler();
	
	private const string LOCAL_HOST = "localhost";
	private const int PORT = 1342;
	private const int MAX_PLAYERS = 32;

	public const int SERVERINFO_TRACKPATH = 1;

	public const int PLAYERINFO_PLAYERID = 1;
	public const int PLAYERINFO_PLAYERNAME = 2;

	public Godot.Collections.Dictionary<int, string> ServerInfo = new Godot.Collections.Dictionary<int, string>()
	{
		{ SERVERINFO_TRACKPATH, "" },
	};
	
	public Godot.Collections.Dictionary<int, string> PlayerInfo = new Godot.Collections.Dictionary<int, string>()
	{
		{ PLAYERINFO_PLAYERID, "" },
		{ PLAYERINFO_PLAYERNAME, "" },
	};	
	
	public override void _Ready()
	{
		Instance = this;
	}
	public override void _Process(double delta)
	{
	}

	//SERVER
	public void CreateServer(string trackPath)
	{
		ServerInfo[SERVERINFO_TRACKPATH] = trackPath;
		MultiplayerPeer = new ENetMultiplayerPeer();
		MultiplayerPeer.CreateServer(PORT, MAX_PLAYERS);
		Multiplayer.MultiplayerPeer = MultiplayerPeer;
		OnServer = true;
	}

	public void OnClientConnected(long id)
	{
		RpcId(id, MethodName.SendServerInfoToClient, ServerInfo);
	}
	//--

	//CLIENT
	public void CreateClient(string ipAddress)
	{
		MultiplayerPeer = new ENetMultiplayerPeer();
		MultiplayerPeer.CreateClient(ipAddress, PORT);
		Multiplayer.MultiplayerPeer = MultiplayerPeer;
		
		PlayerInfo[PLAYERINFO_PLAYERID] = MultiplayerPeer.GetUniqueId().ToString();
		PlayerInfo[PLAYERINFO_PLAYERNAME] = SettingsManager.Instance.Settings.PlayerName;
		
		OnServer = true;
	}
	//--
	
	//server to client rpcs
	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = Godot.MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SendServerInfoToClient(Godot.Collections.Dictionary<int, string> info)
	{
		ServerInfo = info;
		EmitSignalConnectedToServer();
	}
	//--
	
	//client to clients rpcs
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = Godot.MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SendPlayerInfoToEveryone(Godot.Collections.Dictionary<int, string> info)
	{
		Godot.Collections.Dictionary<int, string> NewPlayerInfo = info;
		GD.Print("New Player Name: " + NewPlayerInfo[PLAYERINFO_PLAYERNAME]);
	}
	//--
}
