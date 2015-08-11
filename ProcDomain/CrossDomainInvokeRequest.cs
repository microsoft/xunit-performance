using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ProcDomain
{
    [Serializable]
    internal class CrossDomainInvokeRequest
    {
        public Guid MessageId { get; set; }

        public MethodInfo Method { get; set; }

        public object[] Arguments { get; set; }

        public byte[] ToByteArray()
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(memStream, this);
            return memStream.ToArray();
        }

        public static CrossDomainInvokeRequest FromByteArray(byte[] bytes)
        {
            MemoryStream memStream = new MemoryStream(bytes);
            BinaryFormatter formatter = new BinaryFormatter();
            return (CrossDomainInvokeRequest)formatter.Deserialize(memStream);
        }

    }
}
