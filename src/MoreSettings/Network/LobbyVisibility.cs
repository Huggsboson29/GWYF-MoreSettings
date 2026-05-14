using System;
using BepInEx.Logging;
using Mirror;
using MoreSettings.Models;
using MoreSettings.Runtime;

namespace MoreSettings.Network;

/// <summary>
/// Registers Mirror reader/writer delegates for <see cref="TimingConfigMessage"/> and
/// handles broadcasting the active timing profile from host to clients when a session starts.
/// </summary>
public static class LobbyVisibility
{
    private static ManualLogSource? _log;
    private static SessionTimingState? _clientState;

    public static void Initialize(ManualLogSource log) => _log = log;

    public static SessionTimingState? GetVisibleState()
    {
        if (NetworkServer.active)
        {
            return TimingCoordinator.CurrentState;
        }

        if (NetworkClient.active)
        {
            return _clientState;
        }

        return TimingCoordinator.CurrentState;
    }

    public static void ClearClientState() => _clientState = null;

    /// <summary>
    /// Must be called from <c>PluginMain.Awake</c> before any network activity.
    /// Registers custom Reader/Writer delegates and the client-side message handler.
    /// </summary>
    public static void RegisterMessageDelegates()
    {
        Writer<TimingConfigMessage>.write = static (writer, msg) =>
        {
            writer.WriteBool(msg.IsVanilla);
            writer.WriteString(msg.ProfileName ?? string.Empty);
            writer.WriteFloat(msg.DayDurationSeconds);
            writer.WriteInt(msg.DaysBeforeQuota);
            writer.WriteLong(msg.StartingQuota);
            writer.WriteFloat(msg.CatchUpFactor);
            writer.WriteInt(msg.QuotaScalingModeValue);
            writer.WriteInt(msg.QuotaMultiplierCount);
        };

        Reader<TimingConfigMessage>.read = static reader => new TimingConfigMessage
        {
            IsVanilla = reader.ReadBool(),
            ProfileName = reader.ReadString(),
            DayDurationSeconds = reader.ReadFloat(),
            DaysBeforeQuota = reader.ReadInt(),
            StartingQuota = reader.ReadLong(),
            CatchUpFactor = reader.ReadFloat(),
            QuotaScalingModeValue = reader.ReadInt(),
            QuotaMultiplierCount = reader.ReadInt(),
        };

        NetworkClient.RegisterHandler<TimingConfigMessage>(OnTimingConfigReceived);
    }

    /// <summary>
    /// Sends the current timing state to all connected clients.
    /// No-op if not running as server or if no state has been applied yet.
    /// </summary>
    public static void BroadcastCurrentState()
    {
        if (!NetworkServer.active) return;

        var state = TimingCoordinator.CurrentState;
        if (state == null) return;

        NetworkServer.SendToAll(BuildMessage(state));

        _log?.LogDebug(
            $"[LobbyVisibility] Broadcast timing profile '{state.ProfileName}' to all clients.");
    }

    /// <summary>
    /// Sends the current timing state to a single client connection.
    /// Used when a client becomes scene-ready after the server start broadcast.
    /// </summary>
    public static void SendToClient(NetworkConnectionToClient conn)
    {
        if (!NetworkServer.active) return;

        var state = TimingCoordinator.CurrentState;
        if (state == null) return;

        conn.Send(BuildMessage(state));
    }

    private static TimingConfigMessage BuildMessage(Models.SessionTimingState state) =>
        new()
        {
            IsVanilla = state.IsVanilla,
            ProfileName = state.ProfileName,
            DayDurationSeconds = state.DayDurationSeconds,
            DaysBeforeQuota = state.DaysBeforeQuota,
            StartingQuota = state.StartingQuota,
            CatchUpFactor = state.CatchUpFactor,
            QuotaScalingModeValue = (int)state.QuotaScalingMode,
            QuotaMultiplierCount = state.QuotaMultiplierCount,
        };

    private static void OnTimingConfigReceived(TimingConfigMessage msg)
    {
        // Host already knows its own config; only log on pure clients.
        if (NetworkServer.activeHost) return;

        _clientState = new SessionTimingState(
            "TimingConfigMessage",
            string.IsNullOrWhiteSpace(msg.ProfileName) ? "Vanilla" : msg.ProfileName,
            msg.IsVanilla,
            msg.DayDurationSeconds,
            msg.DaysBeforeQuota,
            msg.StartingQuota,
            msg.CatchUpFactor,
            ResolveQuotaScalingMode(msg.QuotaScalingModeValue),
            msg.QuotaMultiplierCount,
            DateTimeOffset.UtcNow);

        if (msg.IsVanilla)
        {
            _log?.LogInfo("[LobbyVisibility] Host is using vanilla timing — no overrides active.");
        }
        else
        {
            _log?.LogInfo(
                $"[LobbyVisibility] Host timing profile '{msg.ProfileName}': " +
                $"dayDuration={msg.DayDurationSeconds}s, daysBeforeQuota={msg.DaysBeforeQuota}, " +
                $"startingQuota={msg.StartingQuota}, catchUpFactor={msg.CatchUpFactor}, " +
                $"quotaScalingMode={ResolveQuotaScalingMode(msg.QuotaScalingModeValue)}, " +
                $"quotaMultiplierCount={msg.QuotaMultiplierCount}");
        }
    }

    private static QuotaScalingMode ResolveQuotaScalingMode(int rawValue) =>
        Enum.IsDefined(typeof(QuotaScalingMode), rawValue)
            ? (QuotaScalingMode)rawValue
            : QuotaScalingMode.Vanilla;
}
