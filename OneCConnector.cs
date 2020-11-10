using System;
using System.Data.Common;
using System.Dynamic;
using System.Net.Http.Headers;
using V83;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Runtime;
using System.IO;

namespace OneCExchanger
{

    public class OneCMessageProcessException : IOException
    {
        public OneCMessageProcessException(string message) : base(message) { }
    }

    public class OneCConnector
    {
        private string Server { get; }
        private string BaseName { get; }
        private string Login { get; }
        private string Password { get; }
        private string ConnectString { get => string.Format("Srvr=\"{0}\";Ref=\"{1}\";Usr=\"{2}\";Pwd=\"{3}\"", Server, BaseName, Login, Password); }
        public bool IsOpen { get => connection != null && connectionString().Length > 0; }

        private COMConnector comConnector;
        private dynamic connection;
        private dynamic xdtoSerializer;

        private OneCConnector()
        {
            comConnector = new COMConnector();
        }
        public OneCConnector(string server, string baseName, string login, string password) : this()
        {
            Server = server;
            BaseName = baseName;
            Login = login;
            Password = password;
        }
        public bool Connect()
        {
            connection = comConnector.Connect(ConnectString);
            bool isOpen = IsOpen;
            if (isOpen) { initXDTOSerializer(); }
            return isOpen;
        }
        public bool Close()
        {
            if (IsOpen) { xdtoSerializer = null; connection = null; }
            return !IsOpen;
        }

        public async Task StartConsumeAsync(ChannelReader<Message> messages, ChannelWriter<Message> acks)
        {
            if (!IsOpen)
            {
                throw new Exception("Connection isn't open");
            }

            while (await messages.WaitToReadAsync())
            {
                Message message = await messages.ReadAsync();
                processMessage(message);
                acks.WriteAsync(message);
            }
        }
        private string connectionString()
        {
            string connStr = "";
            try { connStr = connection.СтрокаСоединенияИнформационнойБазы(); } catch { }
            return connStr;
        }
        private void initXDTOSerializer()
        {
            if (xdtoSerializer == null) {
                xdtoSerializer = connection.NewObject("СериализаторXDTO", connection.ФабрикаXDTO);
            }
        }
        private void processMessage(Message message)
        {
            try
            {
                string fileName = messageToFile(message);
                dynamic xmlReader = connection.NewObject("ЧтениеXML");
                xmlReader.ОткрытьФайл(fileName);

                dynamic dataArray = xdtoSerializer.ПрочитатьXML(xmlReader);

                xmlReader.Закрыть();
                File.Delete(fileName);

                for (int i = 0; i < dataArray.Количество(); i++)
                {
                    dynamic item = dataArray.Получить(i);
                    item.ОбменДанными.Загрузка = true;
                    item.Записать();
                }

            }
            catch
            {
                throw new OneCMessageProcessException(message.Body);
            }
        }

        private string messageToFile(Message message)
        {
            string filename = Path.GetTempFileName();
            File.WriteAllText(filename, message.Body);
            return filename;
        }


    }
}
