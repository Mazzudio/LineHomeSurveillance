using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Web.Script.Serialization;

using Mazzudio.Line;
using Mazzudio.Line.Models;
namespace LineHomeCamera
{
    public class Service : IService
    {
        private string _domainName = "";
        private string _accessToken = "";
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger(); 
        public Service()
        {
            try
            {
                _logger.Info("Service call");

                _domainName = ConfigurationManager.AppSettings["domainName"].ToString(); 
                _accessToken = ConfigurationManager.AppSettings["lineServiceToken"].ToString();
            }
            catch(Exception ex)
            {
                _logger.Error(ex.Message);
            }
        }

        public List<string> GetAvailableCamera()
        {
            try
            {
                var lines = File.ReadAllLines(System.Web.Hosting.HostingEnvironment.MapPath("~/camera.info"));

                return lines.ToList();
            }
            catch(Exception ex)
            {
                _logger.Error("get-camera-list" + ",ERROR>" + ex.Message);
                return null;
            } 
        }

        public bool UpdateCameraInfo(int id, string ip, string user, string password, string brand)
        {
            try
            {
                var lines = File.ReadAllLines(System.Web.Hosting.HostingEnvironment.MapPath("~/camera.info"));

                Dictionary<int, CameraInfo> cameras = new Dictionary<int, CameraInfo>();
                foreach(var line in lines)
                {
                    var columns = line.Split(new char[] { '|' });
                    int cameraId = 0;
                    if(int.TryParse(columns[0], out cameraId))
                    {
                        if (!cameras.ContainsKey(cameraId))
                        {
                            cameras.Add(cameraId, new CameraInfo
                            {
                                Id = cameraId,
                                Api = columns[1],
                                StillImageUrl = columns[2],
                                UserName = columns[3],
                                Password = columns[4]
                            });
                        } 
                    } 
                }

                if (cameras.ContainsKey(id))
                {
                    cameras[id].Api = brand;
                    cameras[id].StillImageUrl = ip;
                    cameras[id].UserName = user;
                    cameras[id].Password = password;

                    // Store back to file.
                    var writeLines = new List<string>();
                    foreach(var content in cameras)
                    {
                        writeLines.Add(string.Format("{0}|{1}|{2}|{3}|{4}", content.Value.Id, content.Value.Api, content.Value.StillImageUrl, content.Value.UserName, content.Value.Password));
                    }
                    File.WriteAllLines(System.Web.Hosting.HostingEnvironment.MapPath("~/camera.info"), writeLines);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error("update-camera >> id:" + id + ",ERROR>" + ex.Message);
                return false;
            }
        }

        public void Notify(string destination, List<EventItem> events)
        {
            try
            {
                LogNotify(destination, events);
                string[] specificChannelKeywords = new[] { "camera", "cam", "c", "picture", "pic", "p", "channel", "ch", "กล้อง" };
                //string[] allChannelKeywords = new[] { "cameras", "pics", "channels" };
                string[] interestEventTypes = new string[] { "follow", "message" };
                LineClient client = new LineClient(_accessToken);
                foreach (var ev in events)
                {
                    if (!interestEventTypes.Contains(ev.type)) continue;
                    if (ev.source != null && ev.source.type == "user")
                    {
                        if (ev.message != null)
                        {
                            if (ev.message.type == "text")
                            {
                                var validCommand = false;
                                var comandBlocks = ev.message.text.Split(new char[] { ' ' });
                                if(comandBlocks.Length == 2)
                                {
                                    var key = comandBlocks[0].Trim().ToLower();
                                    var channel = comandBlocks[1].Trim();
                                    int channelId = 0;
                                    if (specificChannelKeywords.Contains(key) && int.TryParse(channel, out channelId))
                                    {
                                        _logger.Debug(string.Format("process-capture-command>> channel: {0}, mId:{1}", channelId, ev.message.id));
                                        var camera = GetCameraInfo(channelId);
                                        if (camera != null)
                                        {
                                            var imageUrl = GetCameraImage(camera.StillImageUrl, camera.UserName, camera.Password);
                                            if (!string.IsNullOrEmpty(imageUrl))
                                            {
                                                client.AddMessageQueue(new SendMessageItem
                                                {
                                                    type = "image",
                                                    originalContentUrl = imageUrl,
                                                    previewImageUrl = imageUrl,
                                                });
                                                var replyRes = client.ReplyToUser(ev.replyToken);
                                                validCommand = true;
                                                _logger.Debug("reply-image>>" + imageUrl + ";res:" + replyRes.Success.ToString() + ">" + replyRes.Message);
                                            }
                                        }
                                    }
                                }

                                if (!validCommand)
                                {
                                    client.AddMessageQueue(new SendMessageItem
                                    {
                                        type = "text",
                                        text = "คำสังไม่ถูกต้อง หรือไม่พบช่องที่ระบุ กรุณาลองใหม่อีกครั้ง"
                                    });
                                    _logger.Debug("reply-invalid message>> " + ev.message.text);
                                    var replyRes = client.ReplyToUser(ev.replyToken);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Notify-incoming:" + "des:" + destination + ", error:" + ex.Message);
            }
        }

        private CameraInfo GetCameraInfo(int id)
        {
            try
            {
                // Load config to dictionary.
                var lines = File.ReadAllLines(System.Web.Hosting.HostingEnvironment.MapPath("~/camera.info"));

                Dictionary<int, CameraInfo> cameras = new Dictionary<int, CameraInfo>();
                foreach (var line in lines)
                {
                    var columns = line.Split(new char[] { '|' });
                    int cameraId = 0;
                    if (int.TryParse(columns[0], out cameraId))
                    {
                        if (!cameras.ContainsKey(cameraId))
                        {
                            cameras.Add(cameraId, new CameraInfo
                            {
                                Id = cameraId,
                                Api = columns[1],
                                StillImageUrl = columns[2],
                                UserName = columns[3],
                                Password = columns[4]
                            });
                        }
                    }
                }
                if (!cameras.ContainsKey(id)) return null;

                return cameras[id];
            }
            catch(Exception ex)
            {
                return null;
            }
            //return new CameraInfo
            //{
            //    StillImageUrl = "http://192.168.1.203/ISAPI/Streaming/channels/101/picture",
            //    UserName = "admin",
            //    Password = "admin123"
            //};
        }

        private string GetCameraImage(string url, string user, string password)
        {
            try
            { 
                var client = new WebClient();
                client.UseDefaultCredentials = true;
                client.Credentials = new NetworkCredential(user, password);
                var bytes = client.DownloadData(url);

                if (bytes.Length > 0)
                {
                    string fullpath = System.Web.Hosting.HostingEnvironment.MapPath("~/imgs/") + string.Format("{0}.jpg", DateTime.Now.Ticks);
                    System.IO.MemoryStream ms = new System.IO.MemoryStream(bytes);
                    System.IO.FileStream fs = new System.IO.FileStream(fullpath, System.IO.FileMode.Create);
                    ms.WriteTo(fs);
                    ms.Close();
                    fs.Close();
                    fs.Dispose();
                    IncomingWebRequestContext request = WebOperationContext.Current.IncomingRequest; 
                    return _domainName + fullpath.Replace(System.Web.Hosting.HostingEnvironment.MapPath("~/imgs/"), "/imgs/").Replace(@"\", "/");
                }

                return "";
            }
            catch (Exception ex)
            {
                _logger.Error("view-camera >> ERROR>" + ex.Message);
                return "";
            }
        } 

        private void LogNotify(string destination, List<EventItem> events)
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                string param = serializer.Serialize(events);
                 
                _logger.Info("Notify-incoming:" + "des:" + destination, "", "events:" + param);
            }
            catch (Exception ex)
            {
                _logger.Error("Notify-incoming:" + "des:" + destination + ", error:" + ex.Message);
            }
        }
    }

    public class CameraInfo
    {
        public int Id { get; set; }

        public string StillImageUrl { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string Api { get; set; } 
    }
}
