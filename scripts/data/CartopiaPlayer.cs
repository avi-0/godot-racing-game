using System.Text.Json.Serialization;
using racingGame;

namespace racingGame.data;

public class CartopiaPlayer(long playerId)
{
    public long PlayerId { get; init; } = playerId;
    public int Type {get; set;} = GameModeUtils.PLAYER_EMPTY;
    public int State { get; set; } = GameModeUtils.PLAYER_STATE_NONE;
    public string PlayerName { get; set; } = "";
    
    [JsonIgnore] public Car PlayerCar => CarManager.Instance.GetPlayerCarById(PlayerId);
    [JsonIgnore] public Car PlayerGhostCar { get; set; }
}