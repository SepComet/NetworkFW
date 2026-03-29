using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Network.Defines;
using Network.NetworkApplication;
using Network.NetworkTransport;

namespace Network.NetworkHost
{
    public sealed class ServerNetworkHost
    {
        private readonly ITransport transport;
        private readonly ITransport syncTransport;
        private readonly MessageManager messageManager;
        private readonly ServerAuthoritativeMovementCoordinator authoritativeMovementCoordinator;
        private readonly ServerAuthoritativeCombatCoordinator authoritativeCombatCoordinator;
        private readonly object playerIdentityGate = new();
        private readonly Dictionary<string, string> playerIdsByPeer = new();

        public ServerNetworkHost(
            ITransport transport,
            INetworkMessageDispatcher dispatcher = null,
            SessionReconnectPolicy reconnectPolicy = null,
            Func<DateTimeOffset> utcNowProvider = null,
            ITransport syncTransport = null,
            IMessageDeliveryPolicyResolver deliveryPolicyResolver = null,
            SyncSequenceTracker syncSequenceTracker = null,
            ServerAuthoritativeMovementConfiguration authoritativeMovement = null,
            ServerAuthoritativeCombatConfiguration authoritativeCombat = null)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.syncTransport = syncTransport;
            SessionCoordinator = new MultiSessionManager(reconnectPolicy, utcNowProvider);
            this.transport.OnReceive += HandleTransportReceive;
            if (this.syncTransport != null && !ReferenceEquals(this.syncTransport, this.transport))
            {
                this.syncTransport.OnReceive += HandleTransportReceive;
            }

            messageManager = new MessageManager(
                this.transport,
                dispatcher ?? new ImmediateNetworkMessageDispatcher(),
                deliveryPolicyResolver ?? new DefaultMessageDeliveryPolicyResolver(),
                this.syncTransport,
                syncSequenceTracker ?? new SyncSequenceTracker());
            authoritativeMovementCoordinator = new ServerAuthoritativeMovementCoordinator(
                this,
                messageManager,
                authoritativeMovement ?? new ServerAuthoritativeMovementConfiguration());
            authoritativeCombatCoordinator = new ServerAuthoritativeCombatCoordinator(
                messageManager,
                authoritativeMovementCoordinator,
                authoritativeCombat ?? new ServerAuthoritativeCombatConfiguration());
            messageManager.RegisterHandler(MessageType.MoveInput, authoritativeMovementCoordinator.HandleMoveInputAsync);
            messageManager.RegisterHandler(MessageType.ShootInput, authoritativeCombatCoordinator.HandleShootInputAsync);
        }

        public MessageManager MessageManager => messageManager;

        public ITransport Transport => transport;

        public ITransport SyncTransport => syncTransport;

        public MultiSessionManager SessionCoordinator { get; }

        public IReadOnlyList<ManagedNetworkSession> ManagedSessions => SessionCoordinator.Sessions;

        public IReadOnlyList<ServerAuthoritativeMovementState> AuthoritativeMovementStates => authoritativeMovementCoordinator.States;

        public IReadOnlyList<ServerAuthoritativeCombatState> AuthoritativeCombatStates => authoritativeCombatCoordinator.States;

        public event Action<MultiSessionLifecycleEvent> LifecycleChanged
        {
            add => SessionCoordinator.LifecycleChanged += value;
            remove => SessionCoordinator.LifecycleChanged -= value;
        }

        public Task StartAsync()
        {
            var startTask = transport.StartAsync();
            if (syncTransport == null || ReferenceEquals(syncTransport, transport))
            {
                return startTask;
            }

            return StartWithSyncAsync(startTask);
        }

        public void Stop()
        {
            PublishMetricsSessionSnapshots();
            transport.Stop();
            if (syncTransport != null && !ReferenceEquals(syncTransport, transport))
            {
                syncTransport.Stop();
            }

            SessionCoordinator.RemoveAllSessions("Transport stopped");
            authoritativeMovementCoordinator.Clear();
            authoritativeCombatCoordinator.Clear();
            lock (playerIdentityGate)
            {
                playerIdsByPeer.Clear();
            }
            PublishMetricsSessionSnapshots();
        }

        public Task<int> DrainPendingMessagesAsync(int maxMessages = int.MaxValue)
        {
            return messageManager.DrainPendingMessagesAsync(maxMessages);
        }

        public void UpdateLifecycle()
        {
            SessionCoordinator.UpdateLifecycle();
            PublishMetricsSessionSnapshots();
        }

        public void UpdateAuthoritativeMovement(TimeSpan elapsed)
        {
            authoritativeMovementCoordinator.Update(elapsed);
        }

        public bool TryGetSession(IPEndPoint remoteEndPoint, out ManagedNetworkSession session)
        {
            return SessionCoordinator.TryGetSession(remoteEndPoint, out session);
        }

        public bool TryGetAuthoritativeMovementState(IPEndPoint remoteEndPoint, out ServerAuthoritativeMovementState state)
        {
            return authoritativeMovementCoordinator.TryGetState(remoteEndPoint, out state);
        }

        public bool TryGetAuthoritativeCombatState(IPEndPoint remoteEndPoint, out ServerAuthoritativeCombatState state)
        {
            return authoritativeCombatCoordinator.TryGetState(remoteEndPoint, out state);
        }

        public void NotifyLoginStarted(IPEndPoint remoteEndPoint)
        {
            SessionCoordinator.NotifyLoginStarted(remoteEndPoint);
            PublishMetricsSessionSnapshot(remoteEndPoint);
        }

        public void NotifyLoginSucceeded(IPEndPoint remoteEndPoint)
        {
            SessionCoordinator.NotifyLoginSucceeded(remoteEndPoint);
            BootstrapAuthoritativeMovementState(remoteEndPoint);
            PublishMetricsSessionSnapshot(remoteEndPoint);
        }

        public void NotifyLoginSucceeded(IPEndPoint remoteEndPoint, string playerId)
        {
            RememberPlayerId(remoteEndPoint, playerId);
            NotifyLoginSucceeded(remoteEndPoint);
        }

        public void NotifyLoginFailed(IPEndPoint remoteEndPoint, string reason = null)
        {
            SessionCoordinator.NotifyLoginFailed(remoteEndPoint, reason);
            ForgetPlayerId(remoteEndPoint);
            PublishMetricsSessionSnapshot(remoteEndPoint);
        }

        public void NotifyHeartbeatSent(IPEndPoint remoteEndPoint)
        {
            SessionCoordinator.NotifyHeartbeatSent(remoteEndPoint);
            PublishMetricsSessionSnapshot(remoteEndPoint);
        }

        public void NotifyHeartbeatReceived(IPEndPoint remoteEndPoint, long? serverTick = null)
        {
            SessionCoordinator.NotifyHeartbeatReceived(remoteEndPoint, serverTick);
            PublishMetricsSessionSnapshot(remoteEndPoint);
        }

        public void ObserveAuthoritativeState(IPEndPoint remoteEndPoint, long? serverTick)
        {
            SessionCoordinator.ObserveAuthoritativeState(remoteEndPoint, serverTick);
            PublishMetricsSessionSnapshot(remoteEndPoint);
        }

        public void NotifyInboundActivity(IPEndPoint remoteEndPoint)
        {
            SessionCoordinator.NotifyInboundActivity(remoteEndPoint);
            PublishMetricsSessionSnapshot(remoteEndPoint);
        }

        public bool RemoveSession(IPEndPoint remoteEndPoint, string reason = null)
        {
            if (!SessionCoordinator.TryGetSession(remoteEndPoint, out var session))
            {
                return false;
            }

            var removed = SessionCoordinator.RemoveSession(remoteEndPoint, reason);
            if (!removed)
            {
                return false;
            }

            authoritativeMovementCoordinator.RemoveState(remoteEndPoint);
            authoritativeCombatCoordinator.RemoveState(remoteEndPoint);
            ForgetPlayerId(remoteEndPoint);

            RecordMetricsSessionSnapshot(transport, "server-host", session, ConnectionState.Disconnected);
            if (syncTransport != null && !ReferenceEquals(syncTransport, transport))
            {
                RecordMetricsSessionSnapshot(syncTransport, "server-host-sync", session, ConnectionState.Disconnected);
            }

            return true;
        }

        private void HandleTransportReceive(byte[] data, IPEndPoint sender)
        {
            SessionCoordinator.ObserveTransportActivity(sender);
            ObservePlayerIdentity(data, sender);
            PublishMetricsSessionSnapshot(sender);
        }

        private void BootstrapAuthoritativeMovementState(IPEndPoint remoteEndPoint)
        {
            if (!TryGetKnownPlayerId(remoteEndPoint, out var playerId))
            {
                return;
            }

            authoritativeMovementCoordinator.EnsureState(remoteEndPoint, playerId, out _);
        }

        private void ObservePlayerIdentity(byte[] data, IPEndPoint sender)
        {
            if (data == null || sender == null)
            {
                return;
            }

            Envelope envelope;
            try
            {
                envelope = Envelope.Parser.ParseFrom(data);
            }
            catch
            {
                return;
            }

            if ((MessageType)envelope.Type != MessageType.LoginRequest)
            {
                return;
            }

            LoginRequest request;
            try
            {
                request = LoginRequest.Parser.ParseFrom(envelope.Payload);
            }
            catch
            {
                return;
            }

            RememberPlayerId(sender, request.PlayerId);
        }

        private void RememberPlayerId(IPEndPoint remoteEndPoint, string playerId)
        {
            if (remoteEndPoint == null || string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            var key = Normalize(remoteEndPoint).ToString();
            lock (playerIdentityGate)
            {
                playerIdsByPeer[key] = playerId;
            }
        }

        private bool TryGetKnownPlayerId(IPEndPoint remoteEndPoint, out string playerId)
        {
            playerId = null;
            if (remoteEndPoint == null)
            {
                return false;
            }

            var key = Normalize(remoteEndPoint).ToString();
            lock (playerIdentityGate)
            {
                return playerIdsByPeer.TryGetValue(key, out playerId);
            }
        }

        private void ForgetPlayerId(IPEndPoint remoteEndPoint)
        {
            if (remoteEndPoint == null)
            {
                return;
            }

            var key = Normalize(remoteEndPoint).ToString();
            lock (playerIdentityGate)
            {
                playerIdsByPeer.Remove(key);
            }
        }

        private static IPEndPoint Normalize(IPEndPoint remoteEndPoint)
        {
            if (remoteEndPoint == null)
            {
                throw new ArgumentNullException(nameof(remoteEndPoint));
            }

            return new IPEndPoint(remoteEndPoint.Address, remoteEndPoint.Port);
        }

        private void PublishMetricsSessionSnapshots()
        {
            foreach (var session in ManagedSessions)
            {
                RecordMetricsSessionSnapshot(transport, "server-host", session);
                if (syncTransport != null && !ReferenceEquals(syncTransport, transport))
                {
                    RecordMetricsSessionSnapshot(syncTransport, "server-host-sync", session);
                }
            }
        }

        private void PublishMetricsSessionSnapshot(IPEndPoint remoteEndPoint)
        {
            if (!TryGetSession(remoteEndPoint, out var session))
            {
                return;
            }

            RecordMetricsSessionSnapshot(transport, "server-host", session);
            if (syncTransport != null && !ReferenceEquals(syncTransport, transport))
            {
                RecordMetricsSessionSnapshot(syncTransport, "server-host-sync", session);
            }
        }

        private static void RecordMetricsSessionSnapshot(
            ITransport targetTransport,
            string scope,
            ManagedNetworkSession session,
            ConnectionState? overrideState = null)
        {
            if (targetTransport is not ITransportMetricsSink metricsSink || session == null)
            {
                return;
            }

            metricsSink.RecordApplicationSessionSnapshot(new TransportApplicationSessionSnapshot
            {
                Scope = scope,
                RemoteEndPoint = session.RemoteEndPoint.ToString(),
                ConnectionState = (overrideState ?? session.SessionManager.State).ToString(),
                CanSendHeartbeat = overrideState.HasValue ? overrideState.Value == ConnectionState.LoggedIn : session.SessionManager.CanSendHeartbeat,
                LastRoundTripTimeMs = session.SessionManager.LastRoundTripTime.HasValue
                    ? (long?)Math.Max(0d, session.SessionManager.LastRoundTripTime.Value.TotalMilliseconds)
                    : null,
                LastFailureReason = session.SessionManager.LastFailureReason,
                LastLivenessUtc = session.SessionManager.LastLivenessUtc,
                LastHeartbeatSentUtc = session.SessionManager.LastHeartbeatSentUtc,
                NextReconnectAtUtc = session.SessionManager.NextReconnectAtUtc,
                CurrentServerTick = session.ClockSync.CurrentServerTick,
                ObservedAtUtc = DateTimeOffset.UtcNow
            });
        }

        private async Task StartWithSyncAsync(Task transportStartTask)
        {
            await transportStartTask;
            await syncTransport.StartAsync();
        }
    }
}
