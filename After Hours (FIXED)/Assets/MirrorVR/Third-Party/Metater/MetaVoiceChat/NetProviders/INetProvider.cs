using System;

namespace MetaVoiceChat.NetProviders
{
    public interface INetProvider
    {
        bool IsLocalPlayerDeafened { get; }
        bool HasRemotePlayers { get; }

        void RelayFrame(int index, double timestamp, ReadOnlySpan<byte> data);
    }
}
