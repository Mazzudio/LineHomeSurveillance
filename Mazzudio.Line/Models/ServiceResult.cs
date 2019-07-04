using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mazzudio.Line.Models
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; }
    }
}
