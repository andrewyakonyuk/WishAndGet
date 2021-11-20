using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace WishAndGet.Infrastructure.JsonLd
{
    internal class Obj
    {
        public new static bool Equals(object v1, object v2)
        {
            return v1 == null ? v2 == null : v1.Equals(v2);
        }
    }
}
