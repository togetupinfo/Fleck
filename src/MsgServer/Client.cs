using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fleck.MsgServer
{
    public class Client
    {
        string page;
        string level;
        string producer;//消息生产者
        string consumer;//消息消费者

        public string Page { get => page; set => page = value; }
        public string Level { get => level; set => level = value; }
        public string Producer { get => producer; set => producer = value; }
        public string Consumer { get => consumer; set => consumer = value; }
    }
}
