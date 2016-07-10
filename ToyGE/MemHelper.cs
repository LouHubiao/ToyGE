using System;
using System.Collections.Generic;

namespace ToyGE
{
    class MemHelper
    {
        //// TOOLS contians: GET info from memory, INSERT infor to memory, node operation

        #region GET info from memory

        //get next byte
        public static unsafe byte GetByte(ref IntPtr memAddr)
        {
            byte* result = (byte*)(memAddr.ToPointer());
            memAddr += sizeof(byte);
            return *result;
        }

        //get next int32
        public static unsafe Int32 GetInt32(ref IntPtr memAddr)
        {
            Int32* result = (Int32*)(memAddr.ToPointer());
            memAddr += sizeof(Int32);
            return *result;
        }

        //get next int64
        public static unsafe Int64 GetInt64(ref IntPtr memAddr)
        {
            Int64* result = (Int64*)(memAddr.ToPointer());
            memAddr += sizeof(Int64);
            return *result;
        }

        //get offset string
        public static string GetString(ref IntPtr memAddr)
        {
            IntPtr offsetMemAddr = GetOffsetAddr(ref memAddr);

            Int32 strLen = GetInt32(ref offsetMemAddr);

            byte strStatus = GetByte(ref offsetMemAddr);

            Int64 strNextPart = GetInt64(ref offsetMemAddr);

            return GetChars(ref offsetMemAddr, strLen);
        }

        public delegate T GetListPart<T>(ref IntPtr memAddr);
        public static List<T> GetList<T>(ref IntPtr memAddr, GetListPart<T> getListPart)
        {
            IntPtr offsetMemAddr = GetOffsetAddr(ref memAddr);

            List<T> result = new List<T>();

            Int32 listLen = GetInt32(ref offsetMemAddr);

            byte listStatus = GetByte(ref offsetMemAddr);

            Int64 listNextPart = GetInt64(ref offsetMemAddr);

            for (int i = 0; i < listLen; i++)
            {
                result.Add(getListPart(ref offsetMemAddr));
            }

            return result;
        }

        //Get next addr by offset
        public static unsafe IntPtr GetOffsetAddr(ref IntPtr memAddr)
        {
            Int32* offset = (Int32*)(memAddr.ToPointer());
            IntPtr result = memAddr + *offset;
            memAddr += sizeof(Int32);
            return result;
        }

        //get next string
        static unsafe string GetChars(ref IntPtr memAddr, int len)
        {
            byte[] resultBytes = new byte[len];
            CopyBytesFromMem((byte*)(memAddr.ToPointer()), 0, resultBytes, 0, len);
            string result = System.Text.Encoding.ASCII.GetString(resultBytes);
            memAddr += len;
            return result;
        }

        //copy memory from source byte* to target byte[]
        static unsafe void CopyBytesFromMem(byte* source, int sourceOffset, byte[] target, int targetOffset, int count)
        {
            fixed (byte* pTarget = &target[0])
            {
                byte* ps = source + sourceOffset;
                byte* pt = pTarget + targetOffset;

                for (int i = 0; i < count; i++)
                {
                    *pt = *ps;
                    pt++;
                    ps++;
                }
            }
        }

        //jump nouse part
        public static unsafe void addrJump(ref IntPtr memAddr, int interval)
        {
            memAddr += interval;
        }

        #endregion GET info from memory

        #region INSERT infor to memory

        //insert byte
        public static unsafe void InsertByte(ref IntPtr memAddr, byte input)
        {
            *(byte*)(memAddr.ToPointer()) = input;
            memAddr += sizeof(byte);
        }

        //insert Int32
        public static unsafe void InsertInt32(ref IntPtr memAddr, Int32 input)
        {
            *(Int32*)(memAddr.ToPointer()) = input;
            memAddr += sizeof(Int32);
        }

        //insert Int64
        public static unsafe void InsertInt64(ref IntPtr memAddr, Int64 input)
        {
            *(Int64*)(memAddr.ToPointer()) = input;
            memAddr += sizeof(Int64);
        }

        public static unsafe void InsertString(ref IntPtr memAddr, string input, ref IntPtr nextPartAddr)
        {
            //insert pointer
            MemHelper.InsertInt32(ref memAddr, (Int32)(nextPartAddr.ToInt64() - memAddr.ToInt64()));

            //insert strLen
            InsertInt32(ref nextPartAddr, input.Length);

            //insert hashStatus
            InsertByte(ref nextPartAddr, (byte)0);

            //insert nextPart
            InsertInt64(ref nextPartAddr, 0);

            //inser content
            InsertChars(ref nextPartAddr, input);
        }


        //insert list
        public delegate void InsertListPart<T>(ref IntPtr memAddr, T input, ref IntPtr nextPartAddr);
        public static void InsertList<T>(ref IntPtr memAddr, List<T> inputs, ref IntPtr nextPartAddr, InsertListPart<T> insertListPart)
        {
            //insert pointer
            MemHelper.InsertInt32(ref memAddr, (Int32)(nextPartAddr.ToInt64() - memAddr.ToInt64()));

            //insert listLen
            MemHelper.InsertInt32(ref nextPartAddr, inputs.Count);

            //insert insStatus
            MemHelper.InsertByte(ref nextPartAddr, (byte)0);

            //insert nextPart
            MemHelper.InsertInt64(ref nextPartAddr, 0);

            IntPtr nextNextPartAddr = new IntPtr(0);
            if (typeof(T) != typeof(Int32) && typeof(T) != typeof(Int64))
                nextNextPartAddr = nextPartAddr + inputs.Count * sizeof(Int32) ;

            //insert list content
            for (int i = 0; i < inputs.Count; i++)
            {
                insertListPart(ref nextPartAddr, inputs[i], ref nextNextPartAddr);
            }

            nextPartAddr = nextNextPartAddr;
        }

        //isnert string content
        static unsafe IntPtr InsertChars(ref IntPtr memAddr, string input)
        {
            IntPtr result = memAddr;
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(input);
            CopyToMem(bytes, 0, (byte*)memAddr.ToPointer(), 0, bytes.Length);
            memAddr += bytes.Length;
            return result;
        }

        //copy memory from source byte[] to target byte*
        static unsafe void CopyToMem(byte[] source, int sourceOffset, byte* target, int targetOffset, int count)
        {
            if (source.Length > 0)
            {
                fixed (byte* pSource = &source[0])
                {
                    byte* ps = pSource + sourceOffset;
                    byte* pt = target + targetOffset;

                    for (int i = 0; i < count; i++)
                    {
                        *pt = *ps;
                        pt++;
                        ps++;
                    }
                }
            }
        }

        //update nextNode
        public static unsafe void UpdateNextNode(IntPtr curAddr, IntPtr preAddr)
        {
            Int32* nextNode = (Int32*)(preAddr + 1);
            *nextNode = (Int32)(curAddr.ToInt64() - preAddr.ToInt64());
        }

        //update preNode
        public static unsafe void UpdatePreNode(IntPtr curAddr, IntPtr preAddr)
        {
            Int32* preNode = (Int32*)(curAddr + 5);
            *preNode = (Int32)(curAddr.ToInt64() - preAddr.ToInt64());
        }

        #endregion INSERT infor to memory

        #region node operation
        //delete noed
        public static unsafe void DeleteNode(IntPtr memAddr)
        {
            byte* status = (byte*)(memAddr.ToPointer());
            byte mask = 0x80;
            *status = (byte)(*status | mask);

            Int32* nextOffset = (Int32*)(memAddr + 1);
            IntPtr nextAddr = new IntPtr(memAddr.ToInt64() + *nextOffset);

            Int32* preOffset = (Int32*)(memAddr + 9);
            IntPtr preAddr = new IntPtr(memAddr.ToInt64() - *nextOffset);

            MemHelper.UpdateNextNode(nextAddr, preAddr);
            MemHelper.UpdatePreNode(nextAddr, preAddr);
        }

        //is node deleted
        public static unsafe bool IsDeleted(IntPtr memAddr)
        {
            byte* status = (byte*)(memAddr.ToPointer());
            byte mask = 0x80;
            byte isDeleted = (byte)(*status & mask);

            if (isDeleted > 0)
                return true;
            return false;
        }

        //is node has next
        public static unsafe bool HasNextNode(IntPtr memAddr)
        {
            Int32* nextNode = (Int32*)(memAddr + 1);
            if (*nextNode == 0)
                return true;
            return false;
        }

        #endregion
    }
}
