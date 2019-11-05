﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PolyMessage.Server
{
    internal interface IAcceptor : IDisposable
    {
        Task Start(ITransport transport, IFormat format, ServerComponents serverComponents, CancellationToken cancelToken);

        void Stop();
    }

    internal sealed class DefaultAcceptor : IAcceptor
    {
        private IListener _listener;
        private readonly HashSet<IProcessor> _processors;
        // logging
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        // stop/dispose
        private readonly ManualResetEventSlim _stoppedEvent;
        private bool _isDisposed;
        private bool _isStopRequested;

        public DefaultAcceptor(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger(GetType());
            _processors = new HashSet<IProcessor>();
            _stoppedEvent = new ManualResetEventSlim(initialState: false);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            foreach (IProcessor processor in _processors)
            {
                processor.Stop();
            }
            _listener?.StopAccepting();
            _isStopRequested = true;
            _logger.LogTrace("Waiting worker thread...");
            _stoppedEvent.Wait();

            _listener?.Dispose();
            _stoppedEvent.Dispose();

            _isDisposed = true;
            _logger.LogTrace("Stopped.");
        }

        public async Task Start(ITransport transport, IFormat format, ServerComponents serverComponents, CancellationToken cancelToken)
        {
            if (_isDisposed)
                throw new InvalidOperationException("Acceptor is already stopped.");

            try
            {
                await DoStart(transport, format, serverComponents, cancelToken).ConfigureAwait(false);
            }
            catch (ObjectDisposedException objectDisposedException) when (objectDisposedException.ObjectName == typeof(Socket).FullName)
            {
                // TODO: catch this exception in the transport logic and throw a recognizable one
                _logger.LogTrace("Disposed the listener for {0} transport.", transport.DisplayName);
            }
            catch (Exception exception)
            {
                _logger.LogError("Unexpected: {0}", exception);
            }
            finally
            {
                _stoppedEvent.Set();
                _logger.LogTrace("Stopped worker thread.");
            }
        }

        private async Task DoStart(ITransport transport, IFormat format, ServerComponents serverComponents, CancellationToken cancelToken)
        {
            _listener = transport.CreateListener();
            await _listener.PrepareAccepting().ConfigureAwait(false);

            while (!cancelToken.IsCancellationRequested && !_isStopRequested)
            {
                IChannel channel = await _listener.AcceptClient().ConfigureAwait(false);
                _logger.LogTrace("Accepted client.");

                IProcessor processor = new DefaultProcessor(_loggerFactory, format, channel);
                // TODO: add stopped event so that we remove the processor when it has finished
                _processors.Add(processor);

                Task _ = Task.Run(async () => await processor.Start(serverComponents, cancelToken), cancelToken);
            }
        }

        public void Stop()
        {
            Dispose();
        }
    }
}
