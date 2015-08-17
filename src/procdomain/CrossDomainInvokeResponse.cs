using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Microsoft.ProcessDomain
{
    [Serializable]
    internal class CrossDomainInvokeResponse
    {
        public Guid MessageId { get; set; }

        public object Result { get; set; }

        public Exception Exception { get; set; }

        public byte[] ToByteArray()
        {
            MemoryStream memStream = new MemoryStream();

            BinaryFormatter formatter = new BinaryFormatter();

            formatter.Serialize(memStream, this);

            return memStream.ToArray();

        }

        public static CrossDomainInvokeResponse FromByteArray(byte[] bytes)
        {
            MemoryStream memStream = new MemoryStream(bytes);


            BinaryFormatter formatter = new BinaryFormatter();

            return (CrossDomainInvokeResponse)formatter.Deserialize(memStream);
        }

    }
}
