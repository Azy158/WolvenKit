using System;
using System.ComponentModel;
using System.Linq;

namespace WolvenKit.RED4.Types
{
    public class SharedDataBuffer : IRedBufferWrapper, IRedPrimitive, IEquatable<SharedDataBuffer>
    {
        [Browsable(false)]
        public RedBuffer Buffer { get; set; }

        [Browsable(false)]
        public IParseableBuffer Data
        {
            get => Buffer.Data;
            set => Buffer.Data = value;
        }

        public Red4File File { get; set; }

        public bool Equals(SharedDataBuffer other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (!Equals(Buffer, other.Buffer))
            {
                return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj is SharedDataBuffer cObj)
            {
                return Equals(cObj);
            }

            return false;
        }

        public override int GetHashCode() => (Buffer != null ? Buffer.GetHashCode() : 0);
    }
}
