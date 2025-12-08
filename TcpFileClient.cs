using System;
using System.Threading.Tasks;

namespace передача_файлов
{
    internal class TcpFileClient
    {
        private string serverIp;
        private int serverPort;

        public TcpFileClient(string serverIp, int serverPort)
        {
            this.serverIp = serverIp;
            this.serverPort = serverPort;
        }

        internal async Task SendFileAsync(string file)
        {
            throw new NotImplementedException();
        }
    }
}