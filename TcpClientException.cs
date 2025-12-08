using System;
using System.Runtime.Serialization;

namespace передача_файлов
{
    [Serializable]
    internal class TcpClientException : Exception
    {
        public TcpClientException()
        {
        }

        public TcpClientException(string message) : base(message)
        {
        }

        public TcpClientException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TcpClientException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}