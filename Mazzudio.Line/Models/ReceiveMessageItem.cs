
namespace Mazzudio.Line.Models
{ 
    public class ReceiveMessageItem : Typable
    { 
        public string id { get; set; }

        /* For use with text. */ 
        public string text { get; set; }

        /* For use with file. */ 
        public string fileName { get; set; } 
        public long fileSize { get; set; }

        /* For use with media (image, video, audio). */ 
        public ContentProvider contentProvider { get; set; }
    }
}
