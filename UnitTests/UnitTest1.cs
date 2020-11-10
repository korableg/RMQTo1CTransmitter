using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Channels;
using System.Threading;
using System.Threading.Tasks;

namespace OneCExchanger
{
    [TestClass]
    public class OneCExchangerTest
    {

        private string rmq_host = "192.168.1.31";
        private int rmq_port = 5672;
        private string rmq_vhost = "test";
        private string rmq_login = "might_erp_mirror";
        private string rmq_password = "m2k1JVpJdZ";
        private string rmq_queue = "might_erp_mirror";

        private string oneC_server = "ts-dev";
        private string oneC_baseName = "might_erp_titov_2";
        private string oneC_login = "СистемныйП";
        private string oneC_password = "sXlUC#Qfigf3A9JH2gK#S0elJT8XCR";


        [TestMethod]
        public void RMQ_Connect()
        {
            RMQConnector instance = new RMQConnector(rmq_host, rmq_port, rmq_vhost, rmq_login, rmq_password, rmq_queue);
            bool isOpen = instance.Connect();
            Assert.IsTrue(isOpen);
            instance.Close();

            instance = new RMQConnector(rmq_host, 1111, rmq_vhost, rmq_login, rmq_password, rmq_queue);
            isOpen = instance.Connect();
            Assert.IsFalse(isOpen);
        }

        [TestMethod]
        public void OneC_Connect()
        {
            OneCConnector oneCConnector = new OneCConnector(oneC_server, oneC_baseName, oneC_login, oneC_password);
            bool isConnected = oneCConnector.Connect();

            Assert.IsTrue(isConnected);

            bool isClosed = oneCConnector.Close();

            Assert.IsTrue(isClosed);

        }

        [TestMethod]
        public async Task StartConsumeAsync()
        {
            RMQConnector rmq = new RMQConnector(rmq_host, rmq_port, rmq_vhost, rmq_login, rmq_password, rmq_queue);
            
            Assert.IsTrue(rmq.Connect());

            OneCConnector oneC = new OneCConnector(oneC_server, oneC_baseName, oneC_login, oneC_password);

            Assert.IsTrue(oneC.Connect());

            Channel<Message> messages = Channel.CreateUnbounded<Message>();
            Channel<Message> acks = Channel.CreateUnbounded<Message>();

            try
            {
                rmq.StartConsume(messages, acks);
                await oneC.StartConsumeAsync(messages, acks);
            }
            catch { }
            finally
            {
                rmq.Close();
                oneC.Close();
                messages.Writer.Complete();
                acks.Writer.Complete();

            }


        }

    }

}
