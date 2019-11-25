﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DotNetTcpListener = System.Net.Sockets.TcpListener;

namespace PolyMessage.Transports.Tcp
{
    internal sealed class TcpListener : PolyListener
    {
        // TCP
        private readonly TcpTransport _tcpTransport;
        private DotNetTcpListener _tcpListener;
        // stop/dispose
        private bool _isDisposed;

        public TcpListener(TcpTransport tcpTransport)
        {
            _tcpTransport = tcpTransport;
        }

        protected override void DoDispose(bool isDisposing)
        {
            if (_isDisposed)
                return;

            if (isDisposing)
            {
                _tcpListener?.Stop();
                _isDisposed = true;
            }

            base.DoDispose(isDisposing);
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
                throw new InvalidOperationException($"{_tcpTransport.DisplayName} listener is already disposed.");
        }

        public override void PrepareAccepting()
        {
            EnsureNotDisposed();

            IPAddress hostname = IPAddress.Parse(_tcpTransport.Address.Host);
            _tcpListener = new DotNetTcpListener(hostname, _tcpTransport.Address.Port);
            _tcpListener.Start();
        }

        public override async Task<Func<PolyChannel>> AcceptClient()
        {
            EnsureNotDisposed();

            try
            {
                TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
                return () => CreateClientChannel(tcpClient);
            }
            catch (ObjectDisposedException objectDisposedException) when (objectDisposedException.ObjectName == typeof(Socket).FullName)
            {
                throw new PolyListenerStoppedException(_tcpTransport, objectDisposedException);
            }
        }

        private PolyChannel CreateClientChannel(TcpClient tcpClient)
        {
            tcpClient.ReceiveTimeout = (int)_tcpTransport.Settings.ServerSideClientIdleTimeout.TotalMilliseconds;
            return new TcpChannel(tcpClient, _tcpTransport);
        }

        public override void StopAccepting()
        {
            Dispose();
        }
    }
}