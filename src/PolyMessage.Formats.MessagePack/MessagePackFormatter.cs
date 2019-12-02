﻿using System;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;

namespace PolyMessage.Formats.MessagePack
{
    public class MessagePackFormatter : PolyFormatter
    {
        private readonly MessagePackFormat _format;
        private readonly PolyStream _channelStream;
        private bool _isDisposed;
        private const string KnownErrorConnectionClosed = "Invalid MessagePack code was detected, code:-1";

        public MessagePackFormatter(MessagePackFormat format, PolyChannel channel)
        {
            _format = format;
            _channelStream = new PolyStream(channel);
        }

        protected override void DoDispose(bool isDisposing)
        {
            if (_isDisposed)
                return;

            if (isDisposing)
            {
                _channelStream.Dispose();
                _isDisposed = true;
            }
        }

        public override PolyFormat Format => _format;

        public override Task Write(object obj, CancellationToken cancelToken)
        {
            MessagePackSerializer.NonGeneric.Serialize(obj.GetType(), _channelStream, obj, MessagePackSerializer.DefaultResolver);
            return _channelStream.FlushAsync(cancelToken);
        }

        public override Task<object> Read(Type objType, CancellationToken cancelToken)
        {
            try
            {
                object obj = MessagePackSerializer.NonGeneric.Deserialize(objType, _channelStream, MessagePackSerializer.DefaultResolver,
                    readStrict: true);
                return Task.FromResult(obj);
            }
            catch (InvalidOperationException exception) when (exception.Message.StartsWith(KnownErrorConnectionClosed))
            {
                throw new PolyFormatException(PolyFormatError.EndOfDataStream, "Deserialization encountered end of stream.", _format);
            }
        }
    }
}
