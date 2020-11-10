using System;
using System.Collections.Generic;
using System.Text;

namespace OneCExchanger
{
    public class Message
    {
        public string Body { get; }
        public ulong Tag { get; }
        private Message() { }
        public Message(string body, ulong tag)
        {
            Body = body;
            Tag = tag;
        }
    }
}
