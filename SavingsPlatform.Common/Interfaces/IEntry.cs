using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Common.Interfaces
{
    public interface IEntry
    {
        public string Key { get; init; }
        public string? Etag { get; set; }
    }
}
