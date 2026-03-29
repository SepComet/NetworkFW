using System;
using Network.Defines;

public static class ClientCombatEventRouting
{
    public static bool TryGetAffectedPlayerId(CombatEvent combatEvent, out string playerId)
    {
        if (combatEvent == null)
        {
            throw new ArgumentNullException(nameof(combatEvent));
        }

        switch (combatEvent.EventType)
        {
            case CombatEventType.ShootRejected:
                playerId = combatEvent.AttackerId ?? string.Empty;
                return !string.IsNullOrEmpty(playerId);
            case CombatEventType.Hit:
            case CombatEventType.DamageApplied:
            case CombatEventType.Death:
                playerId = combatEvent.TargetId ?? string.Empty;
                return !string.IsNullOrEmpty(playerId);
            default:
                playerId = string.Empty;
                return false;
        }
    }
}
