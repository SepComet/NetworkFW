using System;
using System.Collections.Generic;
using System.Linq;
using Network.Defines;

namespace Network.NetworkApplication
{
    public sealed class ClientPredictionBuffer
    {
        private readonly List<MoveInput> pendingInputs = new();

        public long? LastAuthoritativeTick { get; private set; }

        public IReadOnlyList<MoveInput> PendingInputs => pendingInputs;

        public void Record(MoveInput input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (pendingInputs.Count > 0 && pendingInputs[^1].Tick >= input.Tick)
            {
                return;
            }

            pendingInputs.Add(input);
        }

        public bool TryApplyAuthoritativeState(PlayerState state, out IReadOnlyList<MoveInput> replayInputs)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (LastAuthoritativeTick.HasValue && state.Tick <= LastAuthoritativeTick.Value)
            {
                replayInputs = Array.Empty<MoveInput>();
                return false;
            }

            LastAuthoritativeTick = state.Tick;
            pendingInputs.RemoveAll(input => input.Tick <= state.Tick);
            replayInputs = pendingInputs.ToArray();
            return true;
        }
    }
}