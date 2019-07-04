using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;

using Mazzudio.Line.Models;
namespace Mazzudio.Line
{
    public class LineClient
    {
        private const string API_HOST = "https://api.line.me/v2";
        private string _accessToken = "";
        private List<SendMessageItem> _messages = new List<SendMessageItem>();

        public LineClient(string accessToken)
        {
            _accessToken = accessToken; 
        }

        public void AddMessageQueue(SendMessageItem message)
        {
            _messages.Add(message);
        }

        public ServiceResult ReplyToUser(string replyToken)
        {
            return OperateAction("reply", replyToken);
        }

        public ServiceResult PushToUser(string userId)
        {
            return OperateAction("push", userId);
        }

        public ServiceResult OperateAction(string action, string contactToken)
        {
            try
            {
                if (_messages.Count == 0) return new ServiceResult { Success = false, Message = "No message to send." };
                string serviceUrl = API_HOST;
                var serializer = new JavaScriptSerializer();
                string parameters = "";

                switch (action)
                {
                    case "push":
                        serviceUrl = API_HOST + "/bot/message/push";
                        parameters = serializer.Serialize(new
                        {
                            to = contactToken,
                            messages = _messages
                        });
                        break;
                    default: // default is replyback.
                        serviceUrl = API_HOST + "/bot/message/reply";
                        parameters = serializer.Serialize(new
                        {
                            replyToken = contactToken,
                            messages = _messages
                        });
                        break;
                }

                var res = DoSendDataToServer("POST", serviceUrl, parameters, _accessToken);
                if (res.Success)
                {
                    _messages.Clear();
                }

                return res;
            }
            catch (Exception ex)
            {
                return new ServiceResult { Success = false, Message = ex.Message };
            }
        }

        private ServiceResult DoSendDataToServer(string method, string serviceUrl, string parameters, string authen, string contentType = "application/json; charset=utf-8", string accept = "application/json; charset=utf-8")
        {
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(serviceUrl);
                httpWebRequest.ContentType = contentType;
                httpWebRequest.Method = method;
                httpWebRequest.Accept = accept;

                httpWebRequest.Proxy = null;
                if (!string.IsNullOrEmpty(authen))
                {
                    httpWebRequest.Headers["Authorization"] = authen;
                }

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(parameters);
                    streamWriter.Flush();
                    streamWriter.Close();

                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();
                        return new ServiceResult
                        {
                            Success = true,
                            StatusCode = 200,
                            Message = result
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Success = false,
                    Message = ex.Message + (ex.InnerException != null ? ">>" + ex.InnerException.Message : "") + ",Params:" + parameters
                };
            }
        }
    }
}
