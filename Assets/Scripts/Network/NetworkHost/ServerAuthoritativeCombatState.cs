using System;
using System.Net;

namespace Network.NetworkHost
{
    public sealed class ServerAuthoritativeCombatState
    {
        public ServerAuthoritativeCombatState(IPEndPoint remoteEndPoint, string playerId, int hp, bool isDead)
        {
            RemoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
            PlayerId = playerId ?? throw new ArgumentNullException(nameof(playerId));
            Hp = hp;
            IsDead = isDead;
        }

        public IPEndPoint RemoteEndPoint { get; }

        public string PlayerId { get; }

        public long LastAcceptedShootTick { get; internal set; }

        public long LastResolvedCombatTick { get; internal set; }

        public int Hp { get; internal set; }

        public bool IsDead { get; internal set; }
    }
}
