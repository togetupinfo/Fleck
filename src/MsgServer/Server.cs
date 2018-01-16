using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web.Script.Serialization;
using System.IO;

namespace Fleck.MsgServer
{
    class Server
    {
        static void Main()
        {
            FleckLog.Level = LogLevel.Debug;
            var allSockets = new List<IWebSocketConnection>();
            var server = new WebSocketServer("ws://0.0.0.0:8888");
            server.Start(socket =>
                {
                    socket.OnOpen = () =>
                        {
                            Console.WriteLine("Open!");
                            allSockets.Add(socket);
                        };
                    socket.OnClose = () =>
                        {
                            Console.WriteLine("Close!");
                            allSockets.Remove(socket);
                        };
                    socket.OnMessage = message =>
                        {
                            JavaScriptSerializer js = new JavaScriptSerializer();
                            Client clientobj = js.Deserialize<Client>(message);
                            if (clientobj != null)
                            {
                             
                                try
                                {
                                    string clientIp = socket.ConnectionInfo.ClientIpAddress;
                                    socket.ConnectionInfo.Consumer = clientobj.Consumer;
                                    socket.ConnectionInfo.Producer = clientobj.Producer;
                                    socket.ConnectionInfo.Page = clientobj.Page;
                                    socket.ConnectionInfo.Level = clientobj.Level;
                                    foreach (WebSocketConnection sock in allSockets.ToList())
                                    {
                                        if (sock.ConnectionInfo.ClientIpAddress== clientIp)
                                        {
                                            sock.Send("");
                                        }
                                    }
                                    
                                }
                                catch (Exception e)
                                {
                                    socket.Send("，请检查网络设置"+e.Message);
                                }
                                
                              
                            }
                        };
                });


            var input = Console.ReadLine();
            while (input != "exit")
            {
                foreach (var socket in allSockets.ToList())
                {
                    socket.Send(input);
                }
                input = Console.ReadLine();
            }

        }
    }
 
}
