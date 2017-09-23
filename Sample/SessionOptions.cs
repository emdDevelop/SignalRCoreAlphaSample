using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sample
{
    public class SessionOptions
    {
        public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromSeconds(20);
        public int MaxBufferSize { get; set; }
    }
}
