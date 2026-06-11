using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace VidyoClient
{
    using _size_t = System.IntPtr;

    public class SizeT
    {
        private _size_t m_value;
        static private UInt64 _size_t_max_value = (_size_t.Size == 8) ? UInt64.MaxValue : UInt32.MaxValue;

        public SizeT(_size_t value)
        {
            m_value = value;
        }

        public _size_t Value
        {
            get { return m_value; }
        }

        public static implicit operator SizeT(byte value)
        {
            return (_size_t.Size == 8) ?
                new SizeT((_size_t)((UInt64)value)) :
                new SizeT((_size_t)((UInt32)value));
        }

        public static explicit operator SizeT(sbyte value)
        {
            if (value < 0) throw new System.OverflowException(String.Format("'{0}' is out of range of the SizeT data type.", value));
            return (_size_t.Size == 8) ?
                new SizeT((_size_t)((UInt64)value)) :
                new SizeT((_size_t)((UInt32)value));
        }

        public static implicit operator SizeT(ushort value)
        {
            return (_size_t.Size == 8) ?
                new SizeT((_size_t)((UInt64)value)) :
                new SizeT((_size_t)((UInt32)value));
        }

        public static explicit operator SizeT(short value)
        {
            if (value < 0) throw new System.OverflowException(String.Format("'{0}' is out of range of the SizeT data type.", value));
            return (_size_t.Size == 8) ?
                new SizeT((_size_t)((UInt64)value)) :
                new SizeT((_size_t)((UInt32)value));
        }

        public static implicit operator SizeT(uint value)
        {
            return (_size_t.Size == 8) ?
                new SizeT((_size_t)((UInt64)value)) :
                new SizeT((_size_t)((UInt32)value));
        }

        public static explicit operator SizeT(int value)
        {
            if (value < 0) throw new System.OverflowException(String.Format("'{0}' is out of range of the SizeT data type.", value));
            return (_size_t.Size == 8) ?
                new SizeT((_size_t)((UInt64)value)) :
                new SizeT((_size_t)((UInt32)value));
        }

        public static implicit operator SizeT(ulong value)
        {
            if (value > _size_t_max_value) throw new System.OverflowException(String.Format("'{0}' is out of range of the SizeT data type.", value));
            return (_size_t.Size == 8) ?
                new SizeT((_size_t)((UInt64)value)) :
                new SizeT((_size_t)((UInt32)value));
        }

        public static explicit operator SizeT(long value)
        {
            if (value < 0 || (ulong)value > _size_t_max_value) throw new System.OverflowException(String.Format("'{0}' is out of range of the SizeT data type.", value));
            return (_size_t.Size == 8) ?
                new SizeT((_size_t)((UInt64)value)) :
                new SizeT((_size_t)((UInt32)value));
        }


        public static explicit operator byte(SizeT value)
        {
            if (value.m_value.ToInt64() > byte.MaxValue) throw new System.OverflowException(String.Format("'{0}' is out of range of the Byte data type.", value));
            return (byte)value.m_value;
        }

        public static explicit operator sbyte(SizeT value)
        {
            if (value.m_value.ToInt64() > (byte)sbyte.MaxValue) throw new System.OverflowException(String.Format("'{0}' is out of range of the SByte data type.", value));
            return (sbyte)value.m_value;
        }

        public static explicit operator ushort(SizeT value)
        {
            if (value.m_value.ToInt64() > ushort.MaxValue) throw new System.OverflowException(String.Format("'{0}' is out of range of the UInt16 data type.", value));
            return (ushort)value.m_value;
        }

        public static explicit operator short(SizeT value)
        {
            if (value.m_value.ToInt64() > (ushort)short.MaxValue) throw new System.OverflowException(String.Format("'{0}' is out of range of the Int16 data type.", value));
            return (short)value.m_value;
        }

        public static explicit operator uint(SizeT value)
        {
            if (value.m_value.ToInt64() > uint.MaxValue) throw new System.OverflowException(String.Format("'{0}' is out of range of the UInt32 data type.", value));
            return (uint)value.m_value;
        }

        public static explicit operator int(SizeT value)
        {
            if (value.m_value.ToInt64() > int.MaxValue) throw new System.OverflowException(String.Format("'{0}' is out of range of the Int32 data type.", value));
            return (int)value.m_value;
        }

        public static implicit operator ulong(SizeT value)
        {
            return (ulong)value.m_value;
        }

        public static explicit operator long(SizeT value)
        {
            if (value.m_value.ToInt64() > long.MaxValue) throw new System.OverflowException(String.Format("'{0}' is out of range of the Int64 data type.", value));
            return (long)value.m_value;
        }

        public override string ToString()
        {
            return m_value.ToString();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SizeT)) return false;

            return Equals((SizeT)obj);
        }

        public bool Equals(SizeT other)
        {
            return m_value == other.m_value;
        }

        public override int GetHashCode()
        {
            return m_value.GetHashCode();
        }

        public static bool operator ==(SizeT first, SizeT second)
        {
            return first.Equals(second);
        }

        public static bool operator !=(SizeT first, SizeT second)
        {
            return !first.Equals(second);
        }

        public static bool operator <(SizeT first, SizeT second)
        {
            return first.m_value.ToInt64() < second.m_value.ToInt64();
        }

        public static bool operator >(SizeT first, SizeT second)
        {
            return first.m_value.ToInt64() > second.m_value.ToInt64();
        }
    }

    public sealed class SizeTMarshaler : ICustomMarshaler
    {
        public static ICustomMarshaler GetInstance(string data)
        {
            return new SizeTMarshaler();
        }

        #region ICustomMarshaler Members

        public void CleanUpManagedData(object ManagedObj)
        {
            // Nothing to do
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            // Nothing to do
        }

        public int GetNativeDataSize()
        {
            return IntPtr.Size;
        }

        public IntPtr MarshalManagedToNative(object ManagedObj)
        {
            if (ManagedObj == null)
                return IntPtr.Zero;
            if (!(ManagedObj is SizeT))
                throw new MarshalDirectiveException("SizeTMarshaler must be used on SizeT.");

            SizeT value = (SizeT)ManagedObj;

            IntPtr ptr = (IntPtr)value.Value;

            return ptr;
        }


        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            return new SizeT((IntPtr)pNativeData);
        }
        #endregion
    }

} // namespace VidyoClient