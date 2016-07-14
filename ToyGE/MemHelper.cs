using System;
using System.Collections.Generic;

/*
memory struct:
    string: status(8)| length(16)| Body| [cruLength(16)]| [nextPart(32)]|
    list:   status(8)| length(16)| Body| [cruLength(16)]| [nextPart(32)]|
    struct: status(8)| Body|
    cell:   status(8)| nextNode(32)| preNode(32)| Body|
    deleted:status(8)| length(16)|

status:
    string: isDeleted| hasNext| isFull| ...
    list:   isDeleted| hasNext| isFull| ...
    struct: isDeleted| ...
    cell:   isDeleted| hasLocked| ...
*/

namespace ToyGE
{
    class MemHelper
    {
        //// TOOLS contians: GET info from memory, INSERT infor to memory, node operation

        #region GET info from memory

        //pay attention: get cell and get struct is depend on different object

        //get next byte
        public static unsafe byte GetByte(ref IntPtr memAddr)
        {
            byte* result = (byte*)(memAddr.ToPointer());
            memAddr += sizeof(byte);
            return *result;
        }

        //get next int16
        public static unsafe Int16 GetInt16(ref IntPtr memAddr)
        {
            Int16* result = (Int16*)(memAddr.ToPointer());
            memAddr += sizeof(Int16);
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

            byte status = GetByte(ref offsetMemAddr);

            Int16 length = GetInt16(ref offsetMemAddr);

            byte isFullMask = 0x20;
            byte isFull = (byte)(status & isFullMask);
            if (isFull > 0)
            {
                return GetChars(ref offsetMemAddr, length);
            }
            else
            {
                byte hasNextMask = 0x40;
                byte hasNext = (byte)(status & hasNextMask);
                if (hasNext > 0)
                {
                    string result = GetChars(ref offsetMemAddr, length - sizeof(Int32));
                    Int32 strNextPartOffset = GetInt32(ref offsetMemAddr);

                    IntPtr nextPartAddr = offsetMemAddr + strNextPartOffset;

                    return result + GetString(ref nextPartAddr);
                }
                else
                {
                    IntPtr curLengthAddr = offsetMemAddr + length - sizeof(Int16);
                    Int16 curLength = GetInt16(ref curLengthAddr);

                    string result = GetChars(ref offsetMemAddr, curLength);
                    offsetMemAddr = curLengthAddr;

                    return result;
                }
            }
        }

        public delegate T GetListPart<T>(ref IntPtr memAddr);
        public static List<T> GetList<T>(ref IntPtr memAddr, GetListPart<T> getListPart)
        {
            List<T> result = new List<T>();

            IntPtr offsetMemAddr = GetOffsetAddr(ref memAddr);

            byte status = GetByte(ref offsetMemAddr);

            byte isFullMask = 0x20;
            byte isFull = (byte)(status & isFullMask);

            Int16 length = GetInt16(ref offsetMemAddr);

            if (isFull > 0)
            {
                IntPtr lastAddr = offsetMemAddr + length;
                while (offsetMemAddr.ToInt64() < lastAddr.ToInt64())
                {
                    result.Add(getListPart(ref offsetMemAddr));
                }
            }
            else
            {
                byte hasNextMask = 0x40;
                byte hasNext = (byte)(status & hasNextMask);
                if (hasNext > 0)
                {
                    IntPtr lastAddr = offsetMemAddr + length - sizeof(Int32);
                    while (offsetMemAddr.ToInt64() < lastAddr.ToInt64())
                    {
                        result.Add(getListPart(ref offsetMemAddr));
                    }

                    Int32 strNextPartOffset = GetInt32(ref offsetMemAddr);

                    IntPtr nextPartAddr = offsetMemAddr + strNextPartOffset;

                    result.AddRange(GetList<T>(ref nextPartAddr, getListPart));
                }
                else
                {
                    IntPtr curLengthAddr = offsetMemAddr + length - sizeof(Int16);
                    Int16 curLength = GetInt16(ref curLengthAddr);

                    IntPtr lastAddr = offsetMemAddr + curLength;
                    while (offsetMemAddr.ToInt64() < lastAddr.ToInt64())
                    {
                        result.Add(getListPart(ref offsetMemAddr));
                    }
                    offsetMemAddr = curLengthAddr;
                }
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

        //insert Int16
        public static unsafe void InsertInt16(ref IntPtr memAddr, Int16 input)
        {
            *(Int16*)(memAddr.ToPointer()) = input;
            memAddr += sizeof(Int16);
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

        //insert Entire string, must has enough space
        public static unsafe void InsertEntireString(ref IntPtr memAddr, string input, ref IntPtr nextPartAddr, Int16 gap)
        {
            //insert pointer
            MemHelper.InsertInt32(ref memAddr, (Int32)(nextPartAddr.ToInt64() - memAddr.ToInt64()));

            if (gap != 0)
            {
                //insert status, isFull=0
                InsertByte(ref nextPartAddr, (byte)0);

                //insert length
                InsertInt16(ref nextPartAddr, (Int16)(input.Length + gap));
                IntPtr lastAddr = nextPartAddr + (Int16)(input.Length + gap);

                //insert curLength
                nextPartAddr = lastAddr - sizeof(Int16);
                InsertInt16(ref nextPartAddr, (Int16)input.Length);

                //inser content
                InsertChars(ref nextPartAddr, input);
            }
            else
            {
                //insert status, isFull=1
                InsertByte(ref nextPartAddr, (byte)0x20);

                //insert length
                InsertInt16(ref nextPartAddr, (Int16)input.Length);

                //inser content
                InsertChars(ref nextPartAddr, input);
            }
        }


        //insert list, must has enough space
        public delegate void InsertListPartPointer<T>(ref IntPtr memAddr, T input, ref IntPtr nextPartAddr, Int16 gap);
        public delegate void InsertListPart<T>(ref IntPtr memAddr, T input, Int16 gap);
        public static void InsertEntireList<T>(ref IntPtr memAddr, List<T> inputs, ref IntPtr nextPartAddr, Int16 tLength, Int16 gap, InsertListPart<T> insertListPart, InsertListPartPointer<T> insertListPartPointer)
        {
            //insert pointer
            MemHelper.InsertInt32(ref memAddr, (Int32)(nextPartAddr.ToInt64() - memAddr.ToInt64()));

            IntPtr lastAddr = new IntPtr(0);
            if (gap != 0)
            {
                //insert insStatus, isFull=0
                MemHelper.InsertByte(ref nextPartAddr, (byte)0);

                //insert length
                MemHelper.InsertInt32(ref nextPartAddr, (Int16)((inputs.Count + gap) * tLength));
                lastAddr = nextPartAddr + (Int16)((inputs.Count + gap) * tLength);

                //insert curLength
                nextPartAddr = lastAddr - sizeof(Int16);
                InsertInt16(ref nextPartAddr, (Int16)inputs.Count);

                //insert context
                if (typeof(T) != typeof(Int32) && typeof(T) != typeof(Int64))
                {
                    //insert list content
                    for (int i = 0; i < inputs.Count; i++)
                    {
                        insertListPartPointer(ref nextPartAddr, inputs[i], ref lastAddr, gap);
                    }
                }
                else
                {
                    //insert list content
                    for (int i = 0; i < inputs.Count; i++)
                    {
                        insertListPart(ref nextPartAddr, inputs[i], gap);
                    }
                }
            }
            else
            {
                //insert status, isFull=1
                InsertByte(ref nextPartAddr, (byte)0x20);

                //insert length
                InsertInt16(ref nextPartAddr, (Int16)(inputs.Count * tLength));
                lastAddr = nextPartAddr + (Int16)(inputs.Count * tLength);

                //insert context
                if (typeof(T) != typeof(Int32) && typeof(T) != typeof(Int64))
                {
                    //insert list content
                    for (int i = 0; i < inputs.Count; i++)
                    {
                        insertListPartPointer(ref nextPartAddr, inputs[i], ref lastAddr, gap);
                    }
                }
                else
                {
                    //insert list content
                    for (int i = 0; i < inputs.Count; i++)
                    {
                        insertListPart(ref nextPartAddr, inputs[i], gap);
                    }
                }
            }

            //update nextPartAddr
            nextPartAddr = lastAddr;
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

        //update cell nextNode
        public static unsafe void UpdateNextNode(IntPtr curAddr, IntPtr preAddr)
        {
            Int32* nextNode = (Int32*)(preAddr + 1);
            *nextNode = (Int32)(curAddr.ToInt64() - preAddr.ToInt64());
        }

        //update cell preNode
        public static unsafe void UpdatePreNode(IntPtr curAddr, IntPtr preAddr)
        {
            Int32* preNode = (Int32*)(curAddr + 5);
            *preNode = (Int32)(curAddr.ToInt64() - preAddr.ToInt64());
        }

        #endregion INSERT infor to memory

        #region UPDATE info
        //update string, have found space
        public static unsafe void UpdateString(IntPtr memAddr, string newString, ref IntPtr tailAddr, Int16 gap, IntPtr[] freeAdds)
        {
            IntPtr offsetMemAddr = GetOffsetAddr(ref memAddr);

            byte* status = (byte*)(offsetMemAddr.ToPointer());
            offsetMemAddr += 1;

            Int16 length = GetInt16(ref offsetMemAddr);

            byte hasNextMask = 0x40;
            byte hasNext = (byte)(*status & hasNextMask);
            if (hasNext > 0)
            {
                if (length > newString.Length)
                {
                    //delete nextPart
                    IntPtr nextPartAddr = offsetMemAddr + length - sizeof(Int32);
                    DeleteString(ref nextPartAddr, freeAdds);

                    //insert status, hasNext=0
                    *status = (byte)0x00;

                    //insert curLength
                    IntPtr curLengthAddr = offsetMemAddr + length - sizeof(Int16);
                    InsertInt16(ref curLengthAddr, (Int16)newString.Length);

                    //insert content
                    InsertChars(ref offsetMemAddr, newString);
                }
                else if (length == newString.Length)
                {
                    //delete nextPart
                    IntPtr nextPartAddr = offsetMemAddr + length - sizeof(Int32);
                    DeleteString(ref nextPartAddr, freeAdds);

                    //insert status, hasNext=0, isFull=1
                    *status = (byte)0x20;

                    //insert content
                    InsertChars(ref offsetMemAddr, newString);
                }
                else
                {
                    //insert status, hasNext=1
                    *status = (byte)0x40;

                    //nextPart
                    IntPtr nextPartOffset = offsetMemAddr + length - sizeof(Int16);
                    IntPtr nextPartAddr = nextPartOffset;
                    nextPartAddr = GetOffsetAddr(ref nextPartAddr);

                    //insert content
                    string leftString = newString.Substring(0, length - sizeof(Int32));
                    InsertChars(ref offsetMemAddr, leftString);

                    string rigthtString = newString.Substring(length - sizeof(Int32), newString.Length - length + sizeof(Int32));
                    UpdateString(nextPartOffset, rigthtString, ref nextPartAddr, gap, freeAdds);
                }
            }
            else
            {
                if (length > newString.Length)
                {
                    //insert status, isFull=0
                    *status = (byte)0x00;

                    //insert curLength
                    IntPtr curLengthAddr = offsetMemAddr + length - sizeof(Int16);
                    InsertInt16(ref curLengthAddr, (Int16)newString.Length);

                    //insert content
                    InsertChars(ref offsetMemAddr, newString);
                }
                else if (length == newString.Length)
                {
                    //insert status, isFull=1
                    *status = (byte)0x20;

                    //insert content
                    InsertChars(ref offsetMemAddr, newString);
                }
                else
                {
                    //insert status, hasNext=1
                    *status = (byte)0x40;

                    //nextPart
                    IntPtr nextPartAddr = offsetMemAddr + length - sizeof(Int16);

                    //insert content
                    string leftString = newString.Substring(0, length - sizeof(Int32));
                    InsertChars(ref offsetMemAddr, leftString);

                    string rigthtString = newString.Substring(length - sizeof(Int32), newString.Length - length + sizeof(Int32));
                    InsertEntireString(ref nextPartAddr, rigthtString, ref tailAddr, gap);
                }
            }
        }
        #endregion UPDATE info

        #region DELETE info

        //delete string
        public static unsafe void DeleteString(ref IntPtr memAddr, IntPtr[] freeAdds)
        {
            Int32* memAddrContext = (Int32*)memAddr.ToPointer();
            IntPtr offsetMemAddr = GetOffsetAddr(ref memAddr);
            *memAddrContext = 0;    //no use
            IntPtr deleteAddr = offsetMemAddr;

            byte* status = (byte*)(offsetMemAddr.ToPointer());
            offsetMemAddr += 1;

            Int16 length = GetInt16(ref offsetMemAddr);

            byte hasNextMask = 0x40;
            byte hasNext = (byte)(*status & hasNextMask);
            if (hasNext > 0)
            {
                //delete next part
                IntPtr nextPartOffset = offsetMemAddr + length - sizeof(Int16);
                DeleteString(ref nextPartOffset, freeAdds);
            }

            //change isDelete
            byte mask = 0x80;
            *status = (byte)(*status | mask);

            //get full length
            Int16 fullLength = (Int16)(length + sizeof(byte) + sizeof(Int32));

            //update length=fullLength
            InsertFreeLength(deleteAddr, fullLength);

            //add to freeAdds
            InsertFree(deleteAddr, freeAdds, fullLength);

            //merge with after
            IntPtr nextAddr = offsetMemAddr + length;
            MergeWithNext(deleteAddr, nextAddr, freeAdds);
        }

        //delete list
        public delegate void DeleteListPart<T>(ref IntPtr memAddr, IntPtr[] freeAdds);
        public static unsafe void DeleteList<T>(ref IntPtr memAddr, IntPtr[] freeAdds, DeleteListPart<T> deleteListPart)
        {
            Int32* memAddrContext = (Int32*)memAddr.ToPointer();
            IntPtr offsetMemAddr = GetOffsetAddr(ref memAddr);
            *memAddrContext = 0;    //no use
            IntPtr deleteAddr = offsetMemAddr;

            byte* status = (byte*)(offsetMemAddr.ToPointer());
            offsetMemAddr += 1;

            Int16 length = GetInt16(ref offsetMemAddr);

            byte isFullMask = 0x20;
            byte isFull = (byte)(*status & isFullMask);
            if (typeof(T) != typeof(Int32) && typeof(T) != typeof(Int64))
            {
                if (isFull > 0)
                {
                    IntPtr lastAddr = offsetMemAddr + length;
                    while (offsetMemAddr.ToInt64() < lastAddr.ToInt64())
                    {
                        deleteListPart(ref offsetMemAddr, freeAdds);
                    }
                }
                else
                {
                    byte hasNextMask = 0x40;
                    byte hasNext = (byte)(*status & hasNextMask);
                    if (hasNext > 0)
                    {
                        IntPtr lastAddr = offsetMemAddr + length - sizeof(Int32);
                        while (offsetMemAddr.ToInt64() < lastAddr.ToInt64())
                        {
                            deleteListPart(ref offsetMemAddr, freeAdds);
                        }

                        //delete next part
                        IntPtr nextPartOffset = lastAddr - sizeof(Int16);
                        DeleteList<T>(ref nextPartOffset, freeAdds, deleteListPart);
                    }
                    else
                    {
                        IntPtr curLengthAddr = offsetMemAddr + length - sizeof(Int16);
                        Int16 curLength = GetInt16(ref curLengthAddr);

                        IntPtr lastAddr = offsetMemAddr + curLength;
                        while (offsetMemAddr.ToInt64() < lastAddr.ToInt64())
                        {
                            deleteListPart(ref offsetMemAddr, freeAdds);
                        }
                    }
                }
            }

            //change isDelete
            byte mask = 0x80;
            *status = (byte)(*status | mask);

            //get full length
            Int16 fullLength = (Int16)(length + sizeof(byte) + sizeof(Int32));

            //update length=fullLength
            InsertFreeLength(deleteAddr, fullLength);

            //add to freeAdds
            InsertFree(deleteAddr, freeAdds, fullLength);

            //merge with after
            IntPtr nextAddr = offsetMemAddr + length;
            MergeWithNext(deleteAddr, nextAddr, freeAdds);
        }

        #endregion DELETE info

        #region freeAddr operation

        //get free space from free memory parts
        public static unsafe IntPtr GetFreeSpace(Int16 byteLength, IntPtr[] freeAdds)
        {
            if (freeAdds[byteLength].ToInt64() != 0)
            {
                IntPtr result = freeAdds[byteLength];
                freeAdds[byteLength] = new IntPtr(GetFreeNext(result));
                return result;
            }
            else
            {
                return new IntPtr(0);
            }
        }

        static unsafe void InsertFree(IntPtr curAddr, IntPtr[] freeAdds, Int16 length)
        {
            UpdateFreeNext(curAddr, freeAdds[length].ToInt64());
            IntPtr nextFree = freeAdds[length];
            UpdateFreePre(nextFree, curAddr.ToInt64());
            freeAdds[length] = curAddr;
        }

        static unsafe void DeleteFree(IntPtr curAddr)
        {
            Int64 freeNext = GetFreeNext(curAddr);
            Int64 freePre = GetFreePre(curAddr);
            UpdateFreeNext(new IntPtr(freePre), freeNext);
            if (freeNext != 0)
                UpdateFreePre(new IntPtr(freeNext), freePre);
        }

        //get next free part
        static unsafe Int64 GetFreeNext(IntPtr freeAddr)
        {
            IntPtr freeNextAddr = freeAddr + sizeof(byte) + sizeof(Int32);
            return GetInt64(ref freeNextAddr);
        }

        //get pre free part
        static unsafe Int64 GetFreePre(IntPtr freeAddr)
        {
            IntPtr freeNextAddr = freeAddr + sizeof(byte) + sizeof(Int32) + sizeof(Int64);
            return GetInt64(ref freeNextAddr);
        }

        //update free part's freeNext
        static unsafe void UpdateFreeNext(IntPtr freeAddr, Int64 addr)
        {
            IntPtr freeNextAddr = freeAddr + sizeof(byte) + sizeof(Int32);
            InsertInt64(ref freeNextAddr, addr);
        }

        //update free part's freePre
        static unsafe void UpdateFreePre(IntPtr freeAddr, Int64 addr)
        {
            IntPtr freePreAddr = freeAddr + sizeof(byte) + sizeof(Int32) + sizeof(Int64);
            InsertInt64(ref freePreAddr, addr);
        }

        //get free part's length
        static unsafe Int16 GetLength(IntPtr freeAddr)
        {
            IntPtr freeLenAddr = freeAddr + sizeof(byte);
            return GetInt16(ref freeAddr);
        }

        //update free part's length
        static unsafe void InsertFreeLength(IntPtr freeAddr, Int16 newLength)
        {
            Int16* freeLenAddr = (Int16*)(freeAddr + sizeof(byte));
            *freeLenAddr = newLength;
        }

        //merge with next free part
        static unsafe void MergeWithNext(IntPtr curAddr, IntPtr nextAddr, IntPtr[] freeAdds)
        {
            if (IsDeleted(nextAddr))
            {
                //delete cur free
                DeleteFree(curAddr);

                //delete next free
                DeleteFree(nextAddr);

                //merge
                Int64 nextNext = GetFreeNext(nextAddr);
                Int64 nextPre = GetFreePre(nextAddr);
                UpdateFreeNext(curAddr, nextNext);
                UpdateFreePre(curAddr, nextPre);

                //get new length
                Int16 curLen = GetLength(curAddr);
                Int16 nextLen = GetLength(nextAddr);
                Int16 fullLength = (Int16)(curLen + nextLen);

                //update length
                InsertFreeLength(curAddr, fullLength);

                //insert new one
                InsertFree(curAddr, freeAdds, fullLength);

                //use next
                MergeWithNext(curAddr, curAddr + fullLength, freeAdds);
            }
        }

        #endregion freeAddr operation

        #region judgement
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

        #endregion judgement
    }
}
