using System;

namespace Network.NetworkHost
{
    public sealed class ServerAuthoritativeCombatConfiguration
    {
        public int DamagePerShot { get; set; } = 25;

        internal void Validate()
        {
            if (DamagePerShot <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(DamagePerShot), "Damage per shot must be positive.");
            }
        }
    }
}
