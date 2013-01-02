using System;
using System.Net.Sockets;

namespace dAmnProxy
{
    public class Client
    {
        private TcpClient _local_client = null;
        private TcpClient _remote_client = null;
        private byte[] _local_buffer = new byte[8192];
        private byte[] _remote_buffer = new byte[8192];

        public Client(TcpClient local, TcpClient remote, String host, int port)
        {
            _local_client   = local;
            _remote_client  = remote;

            _remote_client.BeginConnect(host, port, OnConnect, null);
        }

        private void OnConnect(IAsyncResult result)
        {
            ConIO.Write("Connected to remote host: " + EndPoint(false), EndPoint());

            _remote_client.Client.BeginReceive(_remote_buffer, 0, 8192, SocketFlags.None, OnRemoteReceive, result);
            _local_client.Client.BeginReceive(_local_buffer, 0, 8192, SocketFlags.None, OnLocalReceive, result);
        }

        private void OnLocalReceive(IAsyncResult result)
        {
            try
            {
                int recv_len = _local_client.Client.EndReceive(result);

                if (recv_len > 0)
                {
                    byte[] data = new byte[recv_len];
                    Buffer.BlockCopy(_local_buffer, 0, data, 0, recv_len);

                    ConIO.Write("Got data from client to server.", EndPoint());

                    // If you want to see the packets, uncomment this line.
                    //ConIO.Write(Encoding.ASCII.GetString(data));

                    SendRemote(data);

                    _local_buffer = new byte[8192];
                    _local_client.Client.BeginReceive(_local_buffer, 0, 8192, SocketFlags.None, OnLocalReceive, result);
                }
            }
            catch
            {
                ConIO.Warning("Client.OnLocalReceive", "Got exception. Dead client?");
                _remote_client.Close();
                _local_client.Close();
            }
        }

        private void OnRemoteReceive(IAsyncResult result)
        {
            try
            {
                int recv_len = _remote_client.Client.EndReceive(result);

                if (recv_len > 0)
                {
                    byte[] data = new byte[recv_len];
                    Buffer.BlockCopy(_remote_buffer, 0, data, 0, recv_len);

                    ConIO.Write("Got data from server to client.", EndPoint());

                    // If you want to see the packets, uncomment this line.
                    //ConIO.Write(Encoding.ASCII.GetString(data));

                    SendLocal(data);

                    _remote_buffer = new byte[8192];
                    _remote_client.Client.BeginReceive(_remote_buffer, 0, 8192, SocketFlags.None, OnRemoteReceive, result);
                }
            }
            catch
            {
                ConIO.Warning("Client.OnRemoteReceive", "Got exception. Dead client?");
                _remote_client.Close();
                _local_client.Close();
            }
        }

        public void SendRemote(byte[] payload)
        {
            try
            {
                _remote_client.Client.Send(payload);
            }
            catch (Exception)
            {
                ConIO.Warning("Client.SendRemote", "Got exception. Dead client?");
                _remote_client.Close();
                _local_client.Close();
            }
        }

        public void SendLocal(byte[] payload)
        {
            try
            {
                _local_client.Client.Send(payload);
            }
            catch (Exception)
            {
                ConIO.Warning("Client.SendLocal", "Got exception. Dead client?");
                _remote_client.Close();
                _local_client.Close();
            }
        }

        public String EndPoint(bool local = true)
        {
            if (local)
                return _local_client.Client.RemoteEndPoint.ToString();
            else
                return _remote_client.Client.RemoteEndPoint.ToString();
        }
    }
}
