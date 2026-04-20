using Fusion;

public class GameState : NetworkBehaviour
{
    [Networked] public int CurrentScoreLimit { get; set; }
    [Networked] public int MatchState { get; set; }

    public bool CanValidateGlobalRules()
    {
        return HasStateAuthority;
    }
}