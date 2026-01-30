using racingGame;

namespace racingGame.data;

public class CartopiaPlayer(long playerId)
{
    public long PlayerId { get; init; } = playerId;
    public int Type {get; set;} = GameModeUtils.PLAYER_EMPTY;
    public int State { get; set; } = GameModeUtils.PLAYER_STATE_NONE;
    public string PlayerName { get; set; } = "";
}