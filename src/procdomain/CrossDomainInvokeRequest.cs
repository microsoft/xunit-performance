using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace Microsoft.ProcessDomain
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
