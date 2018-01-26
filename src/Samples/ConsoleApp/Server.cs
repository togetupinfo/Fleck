using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web.Script.Serialization;
using System.IO;

namespace Fleck.Samples.ConsoleApp
{
    class Server
    {
        static void Main()
        {
            FleckLog.Level = LogLevel.Debug;
            var allSockets = new List<IWebSocketConnection>();
            var server = new WebSocketServer("ws://0.0.0.0:8181");
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
                            ImgObj imgobj = js.Deserialize<ImgObj>(message);
                            if (imgobj != null)
                            {
                                string Pic_Path = "R:\\a.png";

                                using (FileStream fs = new FileStream(Pic_Path, FileMode.Create))
                                {
                                    using (BinaryWriter bw = new BinaryWriter(fs))
                                    {
                                        byte[] data = Convert.FromBase64String(imgobj.ImageData);
                                        bw.Write(data);
                                        bw.Close();
                                    }
                                }

                                DicomUtility.Bmp2Dcm(Pic_Path, Pic_Path.Replace("png", "dcm"), imgobj.Name, "F", imgobj.Age.ToString(), imgobj.ProcessNum, imgobj.Modality, imgobj.HospitalName,"US",DateTime.Now.ToString("yyyyMMdd"));

                                string sopinstanceuid = DicomUtility.GetValueByTag(Pic_Path.Replace("png", "dcm"), Dicom.DicomTag.SOPInstanceUID);
                                string patientid = DicomUtility.GetValueByTag(Pic_Path.Replace("png", "dcm"), Dicom.DicomTag.PatientID);
                                try
                                {
                                    DicomUtility.SendDcm(Pic_Path.Replace("png", "dcm"), "10.68.2.17", 104);
                                    //storescp.exe 104 -od "E:/dcms/" -fe ".dcm" -d
                                    
                                  
                                    string clientIp = socket.ConnectionInfo.ClientIpAddress;
                                    foreach (WebSocketConnection sock in allSockets.ToList())
                                    {
                                        if (sock.ConnectionInfo.ClientIpAddress== clientIp)
                                        {
                                            sock.Send("E:/dicom_files/US/" + DateTime.Now.ToString("yyyyMMdd") + "/" + patientid + "/" + sopinstanceuid);
                                        }
                                    }
                                    
                                }
                                catch (Exception e)
                                {
                                    socket.Send("上传图像失败，请检查网络设置"+e.Message);
                                }
                                
                              
                            }

                            //Console.WriteLine("suc");
                            //allSockets.ToList().ForEach(s => s.Send("Echo: " + "suc"));
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
