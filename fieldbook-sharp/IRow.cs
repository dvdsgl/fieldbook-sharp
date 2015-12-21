using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Net.Http;
using Newtonsoft.Json.Serialization;

namespace FieldBook
{
    using Newtonsoft.Json;

    public interface IRow
    {
        int Id { get; set; }
    }
    
}
