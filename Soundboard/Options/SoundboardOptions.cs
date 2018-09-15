using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soundboard.Options
{
    public class SoundboardOptions
    {
        public string CommandUri { get; set; }
        public IDictionary<string, string> Bindings { get; set; }
    }
}
