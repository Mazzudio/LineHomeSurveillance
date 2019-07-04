
namespace Mazzudio.Line.Models
{ 
    public class EventItem : Typable
    { 
        public string replyToken { get; set; } 
        public long timestamp { get; set; } 
        public Sender source { get; set; } 
        public ReceiveMessageItem message { get; set; }
    }
}
