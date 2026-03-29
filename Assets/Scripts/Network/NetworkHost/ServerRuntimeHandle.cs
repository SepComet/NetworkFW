using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Network.NetworkApplication;

namespace Network.NetworkHost
{
    public sealed class ServerRuntimeHandle : IDisposable
    {
        private readonly ServerNetworkHost host;
        private bool isStopped;

        internal ServerRuntimeHandle(ServerNetworkHost host)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            IsRunning = true;
        }

        public ServerNetworkHost Host => host;

        public bool IsRunning { get; private set; }

        public IReadOnlyList<ManagedNetworkSession> ManagedSessions => host.ManagedSessions;

        public event Action<MultiSessionLifecycleEvent> LifecycleChanged
        {
            add => host.LifecycleChanged += value;
            remove => host.LifecycleChanged -= value;
        }

        public Task<int> DrainPendingMessagesAsync(int maxMessages = int.MaxValue)
        {
            return host.DrainPendingMessagesAsync(maxMessages);
        }

        public void UpdateLifecycle()
        {
            host.UpdateLifecycle();
        }

        public bool TryGetSession(IPEndPoint remoteEndPoint, out ManagedNetworkSession session)
        {
            return host.TryGetSession(remoteEndPoint, out session);
        }

        public void Stop()
        {
            if (isStopped)
            {
                return;
            }

            isStopped = true;
            IsRunning = false;
            host.Stop();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
