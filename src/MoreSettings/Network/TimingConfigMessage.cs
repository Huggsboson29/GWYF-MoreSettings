using Mirror;

namespace MoreSettings.Network;

/// <summary>
/// Mirror network message that carries the host's active timing profile to clients.
/// Reader and Writer delegates are registered in <see cref="LobbyVisibility.RegisterMessageDelegates"/>.
/// </summary>
public struct TimingConfigMessage : NetworkMessage
{
    public bool IsVanilla;
    public string ProfileName;
    public float DayDurationSeconds;
    public int DaysBeforeQuota;
    public long StartingQuota;
    public float CatchUpFactor;
    public int QuotaScalingModeValue;
    public int QuotaMultiplierCount;
}
