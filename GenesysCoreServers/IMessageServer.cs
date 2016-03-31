using System;
using Genesyslab.Platform.Management.Protocols.MessageServer;

namespace GenesysCoreServers
{
    public interface IMessageServer
    {
        void SetStatus(int mode);
        void SendMessageToMessageServer(int entryId, LogCategory logCategory, LogLevel logLevel, String message);
        Boolean ConnectToMessageServer();
        void DisconnectMessageServer();
        void ShutDown();
        Boolean SendAlarm(int code, String message);
        Boolean ConnectToLca();
        void DisconnectLca();
        void FinalizePSDKApplicationBlocks();
        Boolean IsConnectedToMessageServer();
    }
}