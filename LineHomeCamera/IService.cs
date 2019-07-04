using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

using Mazzudio.Line.Models;
namespace LineHomeCamera
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IService
    {
        [OperationContract]
        [WebInvoke(BodyStyle = WebMessageBodyStyle.WrappedRequest,
                   ResponseFormat = WebMessageFormat.Json,
                   UriTemplate = "notify")]
        void Notify(string destination, List<EventItem> events);
    }
}
