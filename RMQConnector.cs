using System;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OneCExchanger
{
    public class RMQConnector: IDisposable
    {

        private string Host { get; }
        private int Port { get; }
        private string VHost { get; }
        private string Login { get; }
        private string Password { get; }
        private string Queue { get; }

        private IConnection connection;
        private IModel channel;
        public bool IsOpen { get => connection != null && connection.IsOpen; }
        public bool ChannelIsOpen { get => channel != null && channel.IsOpen; }
        private RMQConnector() { }
        public RMQConnector(string host, int port, string vhost, string login, string password, string queue)
        {
            Host = host;
            Port = port;
            VHost = vhost;
            Login = login;
            Password = password;
            Queue = queue;
        }
        public bool Connect()
        {

            ConnectionFactory factory = new ConnectionFactory
            {
                UserName = Login,
                Password = Password,
                VirtualHost = VHost,
                HostName = Host,
                Port = Port
            };
            
            try {
                connection = factory.CreateConnection();
            }
            catch { }

            return IsOpen;

        }
        public void Close()
        {
            if (ChannelIsOpen) { channel.Close(); };
            if (IsOpen) { connection.Close(); };
        }
        public void StartConsume(ChannelWriter<Message> messages, ChannelReader<Message> acks)
        {
            if (!IsOpen)
            {
                throw new Exception("Connection isn't open");
            }
           
            channel = connection.CreateModel();
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                Message message = new Message(Encoding.UTF8.GetString(ea.Body.ToArray()), ea.DeliveryTag);
                await messages.WriteAsync(message);
            };
            channel.BasicConsume(Queue, false, consumer);
            startAcks(acks);
        }

        private void startAcks(ChannelReader<Message> acks)
        {
            if (!ChannelIsOpen)
            {
                throw new Exception("Channel isn't open");
            }
            new Thread(async () =>
            {
                while (await acks.WaitToReadAsync())
                {
                    Message message = await acks.ReadAsync();
                    channel.BasicAck(message.Tag, false);
                }
            }).Start();
        }

        public void Dispose()
        {
            Close();
        }
    }
}
