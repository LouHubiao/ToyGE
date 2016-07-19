using System;
using System.Collections.Generic;
using System.Text;

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
    class MemTool
    {
        //Get next addr by offset, offset is from memAddr + sizeof(Int32) !
        public static unsafe IntPtr GetOffsetedAddr(ref IntPtr memAddrBeforeOffset)
        {
            Int32 offset = MemInt32.GetValue(ref memAddrBeforeOffset);
            //memAddrBeforeOffset has changed to memAddrAfterOffset
            return memAddrBeforeOffset + offset;
        }

        //get nextOffset(Int32) addr at tail
        public static unsafe IntPtr GetNextOffsetAddr(IntPtr memAddrAfterLength, Int16 length)
        {
            return memAddrAfterLength + length - sizeof(Int32);
        }

        //set nextOffset(Int32) at tail
        public static unsafe void SetNextOffsetAddr(IntPtr memAddrAfterLength, Int16 length, Int32 nextOffset)
        {
            IntPtr curLengthAddr = memAddrAfterLength + length - sizeof(Int32);
            MemInt32.SetValue(ref curLengthAddr, nextOffset);
        }

        //get curlen(Int16) at tail
        public static unsafe Int16 GetCurCount(IntPtr memAddrAfterLength, Int16 length)
        {
            IntPtr curLengthAddr = memAddrAfterLength + length - sizeof(Int16);
            return MemInt16.GetValue(ref curLengthAddr);
        }

        //set curlen(Int16) at tail
        public static unsafe void SetCurCount(IntPtr memAddrAfterLength, Int16 length, Int16 count)
        {
            IntPtr curLengthAddr = memAddrAfterLength + length - sizeof(Int16);
            MemInt16.SetValue(ref curLengthAddr, count);
        }

        //jump nouse part
        public static unsafe void addrJump(ref IntPtr memAddr, int interval)
        {
            memAddr += interval;
        }
    }

    //byte operation
    class MemByte
    {
        public static unsafe byte GetValue(ref IntPtr memAddr)
        {
            byte* result = (byte*)(memAddr.ToPointer());
            memAddr += sizeof(byte);
            return *result;
        }

        public static unsafe void SetValue(ref IntPtr memAddr, byte value)
        {
            *(byte*)(memAddr.ToPointer()) = value;
            memAddr += sizeof(byte);
        }
    }

    //int16 operation
    class MemInt16
    {
        public static unsafe Int16 GetValue(ref IntPtr memAddr)
        {
            Int16* result = (Int16*)(memAddr.ToPointer());
            memAddr += sizeof(Int16);
            return *result;
        }

        public static unsafe void SetValue(ref IntPtr memAddr, Int16 value)
        {
            *(Int16*)(memAddr.ToPointer()) = value;
            memAddr += sizeof(Int16);
        }
    }

    //int32 operation
    class MemInt32
    {
        public static unsafe Int32 GetValue(ref IntPtr memAddr)
        {
            Int32* result = (Int32*)(memAddr.ToPointer());
            memAddr += sizeof(Int32);
            return *result;
        }

        public static unsafe void SetValue(ref IntPtr memAddr, Int32 value)
        {
            *(Int32*)(memAddr.ToPointer()) = value;
            memAddr += sizeof(Int32);
        }
    }

    //int64 operation
    class MemInt64
    {
        public static unsafe Int64 GetValue(ref IntPtr memAddr)
        {
            Int64* result = (Int64*)(memAddr.ToPointer());
            memAddr += sizeof(Int64);
            return *result;
        }

        public static unsafe void SetValue(ref IntPtr memAddr, Int64 value)
        {
            *(Int64*)(memAddr.ToPointer()) = value;
            memAddr += sizeof(Int64);
        }
    }

    //status operation
    class MemStatus
    {
        public static bool GetIsDeleted(byte status)
        {
            byte mask = 0x80;
            byte isDeleted = (byte)(status & mask);
            return isDeleted > 0;
        }

        public static bool GetHasNext(byte status)
        {
            byte mask = 0x40;
            byte hasNext = (byte)(status & mask);
            return hasNext > 0;
        }

        public static bool GetIsFull(byte status)
        {
            byte mask = 0x20;
            byte isFull = (byte)(status & mask);
            return isFull > 0;
        }

        public unsafe static void SetIsDeleted(byte* statusPtr, bool isDeleted)
        {
            if (isDeleted == true)
            {
                byte mask = 0x80;
                *statusPtr = (byte)(*statusPtr | mask);
            }
            else
            {
                byte mask = 0x7F;
                *statusPtr = (byte)(*statusPtr & mask);
            }
        }

        public unsafe static void SetHasNext(byte* statusPtr, bool HasNext)
        {
            if (HasNext == true)
            {
                byte mask = 0x40;
                *statusPtr = (byte)(*statusPtr | mask);
            }
            else
            {
                byte mask = 0xCF;
                *statusPtr = (byte)(*statusPtr & mask);
            }
        }

        public unsafe static void SetIsFull(byte* statusPtr, bool isDeleted)
        {
            if (isDeleted == true)
            {
                byte mask = 0x20;
                *statusPtr = (byte)(*statusPtr | mask);
            }
            else
            {
                byte mask = 0xEF;
                *statusPtr = (byte)(*statusPtr & mask);
            }
        }
    }

    //string operation
    class MemString
    {
        //get entire string
        public static string GetValue(ref IntPtr memAddr)
        {
            //get offseted addr
            IntPtr offsetMemAddr = MemTool.GetOffsetedAddr(ref memAddr);

            //get status
            byte status = MemByte.GetValue(ref offsetMemAddr);

            //get length
            Int16 length = MemInt16.GetValue(ref offsetMemAddr);

            //get context
            if (MemStatus.GetIsFull(status) == true)
            {
                //full
                return GetChars(ref offsetMemAddr, length);
            }
            else if (MemStatus.GetHasNext(status) == true)
            {
                //has next
                StringBuilder strBuilder = new StringBuilder();
                strBuilder.Append(GetChars(ref offsetMemAddr, length - sizeof(Int32)));

                IntPtr nextPartAddr = MemTool.GetOffsetedAddr(ref offsetMemAddr);
                strBuilder.Append(GetValue(ref nextPartAddr));

                return strBuilder.ToString();
            }
            else
            {
                //not full
                Int16 curCount = MemTool.GetCurCount(offsetMemAddr, length);
                return GetChars(ref offsetMemAddr, curCount);
            }
        }

        //insert Entire string, must has enough space
        public static unsafe bool SetString(ref IntPtr memAddr, IntPtr[] freeList, IntPtr headAddr, IntPtr tailAddr, Int64 blockLength, string content, Int16 gap)
        {

            //insert pointer
            MemInt32.SetValue(ref memAddr, (Int32)(nextFreeAddr.ToInt64() - memAddr.ToInt64()));

            if (gap != 0)
            {
                //insert status
                MemByte.SetValue(ref nextFreeAddr, (byte)0x0);

                //insert length
                Int16 length = (Int16)(content.Length + sizeof(Int16) + gap);
                MemInt16.SetValue(ref nextFreeAddr, length);

                //lastAddr buffer
                IntPtr lastAddr = nextFreeAddr + length;

                //insert curLength
                MemTool.SetCurCount(nextFreeAddr, length, (Int16)(content.Length));

                //inser content
                InsertChars(ref nextFreeAddr, content);

                //nextPartAddr jump to last
                nextFreeAddr = lastAddr;
            }
            else
            {
                //insert status, isFull=1
                MemByte.SetValue(ref nextFreeAddr, (byte)0x20);

                //insert length
                MemInt16.SetValue(ref nextFreeAddr, (Int16)content.Length);

                //inser content
                InsertChars(ref nextFreeAddr, content);
            }
        }

        //delete Entire string
        public static unsafe void DeleteString(ref IntPtr memAddr, IntPtr[] freeList)
        {
            //get offseted addr
            IntPtr offsetMemAddr = MemTool.GetOffsetedAddr(ref memAddr);

            //backup offsetMemAddr as free begin addr 
            IntPtr contentBeginAddr = offsetMemAddr;

            //delete pointer
            MemInt32.SetValue(ref memAddr, 0);

            //change status isDelete=1
            byte* status = (byte*)(offsetMemAddr.ToPointer());
            MemStatus.SetIsDeleted(status, true);

            //go to next part
            offsetMemAddr += 1;

            //get string length: byte count
            Int16 length = MemInt16.GetValue(ref offsetMemAddr);

            //if has next part, recursion
            if (MemStatus.GetHasNext(*status) == true)
            {
                IntPtr nextPartOffset = MemTool.GetNextOffsetAddr(offsetMemAddr, length);
                DeleteString(ref nextPartOffset, freeList);
            }

            //add to freeAddr[length]
            if (length >= 64)
            {
                //insert content into freeAddr
                MemFreeList.InsertFreeList(contentBeginAddr, length, freeList);

                //merge with after
                IntPtr nextAddr = offsetMemAddr + length;
                MemFreeList.MergeWithNext(contentBeginAddr, nextAddr, freeList);
            }
            //else drop and wait to auto GC
        }

        //update string, return false is no free space, return true is update success
        public static unsafe bool UpdateString(ref IntPtr memAddr, string newValue, IntPtr[] freeList, IntPtr headAddr, IntPtr tailAddr, Int64 blockLength, Int16 gap)
        {
            //get offseted addr
            IntPtr offsetMemAddr = MemTool.GetOffsetedAddr(ref memAddr);

            //get status
            byte* status = (byte*)(offsetMemAddr.ToPointer());

            //go to next pointer
            offsetMemAddr += 1;

            //get length
            Int16 length = MemInt16.GetValue(ref offsetMemAddr);

            //update content
            if (MemStatus.GetHasNext(*status) == true)
            {
                if (length > newValue.Length)
                {
                    //delete nextPart
                    IntPtr nextPartAddr = MemTool.GetNextOffsetAddr(offsetMemAddr, length);
                    DeleteString(ref nextPartAddr, freeList);

                    //update status, hasNext=0, isFull=0
                    MemStatus.SetHasNext(status, false);
                    MemStatus.SetIsFull(status, false);

                    //insert curLength
                    MemTool.SetCurCount(offsetMemAddr, length, (Int16)newValue.Length);

                    //insert content
                    InsertChars(ref offsetMemAddr, newValue);
                }
                else if (length == newValue.Length)
                {
                    //delete nextPart
                    IntPtr nextPartAddr = MemTool.GetNextOffsetAddr(offsetMemAddr, length);
                    DeleteString(ref nextPartAddr, freeList);

                    //insert status, hasNext=0, isFull=1
                    MemStatus.SetHasNext(status, false);
                    MemStatus.SetIsFull(status, true);

                    //insert content
                    InsertChars(ref offsetMemAddr, newValue);
                }
                else
                {
                    //get nextPart offset addr
                    IntPtr nextPartAddr = MemTool.GetNextOffsetAddr(offsetMemAddr, length);

                    //insert content, left into orig, right into next
                    string leftString = newValue.Substring(0, length - sizeof(Int32));
                    InsertChars(ref offsetMemAddr, leftString);

                    string rigthtString = newValue.Substring(length - sizeof(Int32));
                    UpdateString(ref nextPartAddr, rigthtString, freeList, headAddr, tailAddr, blockLength, gap);
                }
            }
            else
            {
                if (length > newValue.Length)
                {
                    //update status, isFull=0
                    MemStatus.SetIsFull(status, false);

                    //insert curCount
                    MemTool.SetCurCount(offsetMemAddr, length, (Int16)newValue.Length);

                    //insert content
                    InsertChars(ref offsetMemAddr, newValue);
                }
                else if (length == newValue.Length)
                {
                    //insert status, isFull=1
                    MemStatus.SetIsFull(status, true);

                    //insert content
                    InsertChars(ref offsetMemAddr, newValue);
                }
                else
                {
                    //get nextPart offset addr
                    IntPtr nextPartOffset = MemTool.GetNextOffsetAddr(offsetMemAddr, length);

                    //update status, hasNext=1, isFull=0
                    MemStatus.SetHasNext(status, true);
                    MemStatus.SetIsFull(status, false);

                    //insert content, left into orig
                    string leftString = newValue.Substring(0, length - sizeof(Int32));
                    InsertChars(ref offsetMemAddr, leftString);

                    //insert content, right into new next free
                    string rigthtString = newValue.Substring(length - sizeof(Int32));

                    //get nextFreeAddr in this block
                    IntPtr nextFreeInBlock = MemFreeList.GetFreeInBlock(freeList, headAddr, tailAddr, blockLength, (Int16)rigthtString.Length);
                    if (nextFreeInBlock.ToInt64() == 0)
                        return false;   //update false

                    SetString(ref nextPartOffset, ref nextFreeInBlock, rigthtString, gap);
                }
            }
            return true;
        }

        //get string content
        static unsafe string GetChars(ref IntPtr memAddr, int length)
        {
            byte[] resultBytes = new byte[length];
            CopyBytesFromMem((byte*)(memAddr.ToPointer()), 0, resultBytes, 0, length);
            string result = System.Text.Encoding.ASCII.GetString(resultBytes);
            memAddr += length;
            return result;
        }

        //copy from source byte* in heap to byte[] in stack
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

        //isnert string content
        static unsafe IntPtr InsertChars(ref IntPtr memAddr, string input)
        {
            IntPtr result = memAddr;
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(input);
            CopyToMem(bytes, 0, (byte*)memAddr.ToPointer(), 0, bytes.Length);
            memAddr += bytes.Length;
            return result;
        }

        //copy from source byte[] in stack to target byte* in heap
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
    }

    class MemList
    {
        //get entire list, getListPart like GetString()
        public delegate T GetListItem<T>(ref IntPtr memAddr);
        public static List<T> GetList<T>(ref IntPtr memAddr, GetListItem<T> getListPart)
        {
            //last result
            List<T> result = new List<T>();

            //get offseted addr
            IntPtr offsetMemAddr = MemTool.GetOffsetedAddr(ref memAddr);

            //get status
            byte status = MemByte.GetValue(ref offsetMemAddr);

            //get length
            Int16 length = MemInt16.GetValue(ref offsetMemAddr);

            //get context
            if (MemStatus.GetIsFull(status) == true)
            {
                IntPtr lastAddr = offsetMemAddr + length;
                while (offsetMemAddr.ToInt64() < lastAddr.ToInt64())
                {
                    result.Add(getListPart(ref offsetMemAddr));
                }
            }
            else
            {
                if (MemStatus.GetHasNext(status) == true)
                {
                    IntPtr lastAddr = MemTool.GetNextOffsetAddr(offsetMemAddr, length);
                    while (offsetMemAddr.ToInt64() < lastAddr.ToInt64())
                    {
                        result.Add(getListPart(ref offsetMemAddr));
                    }

                    //next part
                    result.AddRange(GetList<T>(ref offsetMemAddr, getListPart));
                }
                else
                {
                    Int16 curCount = MemTool.GetCurCount(offsetMemAddr, length);
                    for (int i = 0; i < curCount; i++)
                    {
                        result.Add(getListPart(ref offsetMemAddr));
                    }
                }
            }
            return result;
        }

        //insert list, must has enough space
        public delegate void InsertListPartPointer<T>(ref IntPtr memAddr, T input, ref IntPtr nextPartAddr, Int16 gap);
        public delegate void InsertListPart<T>(ref IntPtr memAddr, T input);
        public static void InsertEntireList<T>(ref IntPtr memAddr, List<T> inputs, ref IntPtr nextPartAddr, Int16 tLength, Int16 gap, InsertListPart<T> insertListPart, InsertListPartPointer<T> insertListPartPointer)
        {
            //insert pointer
            MemInt32.SetValue(ref memAddr, (Int32)(nextPartAddr.ToInt64() - memAddr.ToInt64()));

            IntPtr lastAddr = new IntPtr(0);
            if (gap != 0)
            {
                //insert insStatus, isFull=0
                MemHelper.InsertValue(ref nextPartAddr, (byte)0);

                //insert length
                MemHelper.InsertValue(ref nextPartAddr, (Int16)(inputs.Count * tLength + gap + sizeof(Int16)));
                lastAddr = nextPartAddr + (Int16)(inputs.Count * tLength + gap + sizeof(Int16));

                //insert curLength
                IntPtr curLengthAddr = lastAddr - sizeof(Int16);
                InsertValue(ref curLengthAddr, (Int16)inputs.Count);

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
                        insertListPart(ref nextPartAddr, inputs[i]);
                    }
                }
            }
            else
            {
                //insert status, isFull=1
                InsertValue(ref nextPartAddr, (byte)0x20);

                //insert length
                InsertValue(ref nextPartAddr, (Int16)(inputs.Count * tLength));
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
                        insertListPart(ref nextPartAddr, inputs[i]);
                    }
                }
            }

            //update nextPartAddr
            nextPartAddr = lastAddr;
        }
    }

    class MemFreeList
    {
        //temp, log free list
        public static void ConsoleFree(IntPtr[] freeAdds)
        {
            for (int i = 0; i < freeAdds.Length; i++)
            {
                IntPtr first = freeAdds[i];
                if (first.ToInt64() != 0)
                {
                    IntPtr curAddr = first;
                    while (curAddr.ToInt64() != 0)
                    {
                        Console.WriteLine("Line:" + i + ",addr:" + curAddr);
                        IntPtr nextAddr = GetFreeNext(curAddr);
                        curAddr = nextAddr;
                    }
                }
            }
        }

        //insert curAddr part into freeList linedlist with length
        public static unsafe void InsertFreeList(IntPtr curAddr, Int16 length, IntPtr[] freeList)
        {
            //update curAddr's next and pre free part
            UpdateFreeNext(curAddr, freeList[length]);
            UpdateFreePre(curAddr, new IntPtr(0)); //insert 0 because curAddr is head

            //update cur freeList head's pre
            IntPtr nextFree = freeList[length];
            if (nextFree.ToInt64() != 0)
                UpdateFreePre(nextFree, curAddr);

            //update freeList head
            freeList[length] = curAddr;
        }

        //merge with next free part, next free part is close to cur free part
        public static unsafe void MergeWithNext(IntPtr curAddr, IntPtr nextAddr, IntPtr[] freeAdds)
        {
            //get status
            byte* status = (byte*)nextAddr.ToPointer();

            if (MemStatus.GetIsDeleted(*status) == true)
            {
                //delete cur free part
                DeleteFromFreelist(curAddr, freeAdds);

                //delete next free part
                DeleteFromFreelist(nextAddr, freeAdds);

                //get new length
                Int16 curLen = *(Int16*)(curAddr + sizeof(byte)).ToPointer();
                Int16 nextLen = *(Int16*)(nextAddr + sizeof(byte)).ToPointer();
                Int16 mergeLength = (Int16)(curLen + nextLen + sizeof(byte) + sizeof(Int16));

                //update length
                Int16* freeLenAddr = (Int16*)(curAddr + sizeof(byte));
                *freeLenAddr = mergeLength;

                //insert new one
                InsertFreeList(curAddr, mergeLength, freeAdds);

                //merge again
                IntPtr nextNextAddr = GetFreeNext(curAddr);
                if (nextNextAddr.ToInt64() != 0)
                    MergeWithNext(curAddr, nextNextAddr, freeAdds);
            }
        }

        //delete the part(curAddr) from the freeAddr
        public static unsafe void DeleteFromFreelist(IntPtr curAddr, IntPtr[] freeAddr)
        {
            IntPtr freeNext = GetFreeNext(curAddr);
            IntPtr freePre = GetFreePre(curAddr);
            if (freePre.ToInt64() == 0)
            {
                Int16 length = *(Int16*)(curAddr + sizeof(byte)).ToPointer();
                freeAddr[length] = curAddr;
            }
            else
            {
                UpdateFreeNext(freePre, freeNext);
                if (freeNext.ToInt64() != 0)
                    UpdateFreePre(freeNext, freePre);
            }
        }

        //get free space from current memory block, return 0 if has not enough space
        public static unsafe IntPtr GetFreeInBlock(IntPtr[] freeList, IntPtr headAddr, IntPtr tailAddr, Int64 blockLength, Int16 byteLength)
        {
            if (freeList[byteLength].ToInt64() != 0)
            {
                //get free addr
                IntPtr result = freeList[byteLength];

                //update freeList
                freeList[byteLength] = GetFreeNext(result);

                return result;
            }
            else if (headAddr.ToInt64() + blockLength - tailAddr.ToInt64() > byteLength)
            {
                return tailAddr;
            }
            else
            {
                return new IntPtr(0);
            }
        }

        //update free part's freeNext, curAddr to nextAddr
        static unsafe void UpdateFreeNext(IntPtr curAddr, IntPtr nextAddr)
        {
            IntPtr nextOffsetAddr = curAddr + sizeof(byte) + sizeof(Int16);
            if (nextAddr.ToInt64() != 0)
                MemInt32.SetValue(ref nextOffsetAddr, (Int32)(nextAddr.ToInt32() - nextOffsetAddr.ToInt32() - sizeof(Int32)));
            else
                MemInt32.SetValue(ref nextOffsetAddr, 0);
        }

        //update free part's freePre, curAddr to preAddr
        static unsafe void UpdateFreePre(IntPtr curAddr, IntPtr preAddr)
        {
            IntPtr preOffsetAddr = curAddr + sizeof(byte) + sizeof(Int16) + sizeof(Int32);
            if (preAddr.ToInt64() != 0)
                MemInt32.SetValue(ref preOffsetAddr, (Int32)(preOffsetAddr.ToInt32() + sizeof(Int32) - preAddr.ToInt32()));
            else
                MemInt32.SetValue(ref preOffsetAddr, 0);
        }

        //get next free part
        static unsafe IntPtr GetFreeNext(IntPtr freeAddr)
        {
            IntPtr nextOffsetAddr = freeAddr + sizeof(byte) + sizeof(Int16);
            Int32 nextOffset = MemInt32.GetValue(ref nextOffsetAddr);
            if (nextOffset == 0)
                return new IntPtr(0);
            return nextOffsetAddr + nextOffset;
        }

        //get pre free part
        static unsafe IntPtr GetFreePre(IntPtr freeAddr)
        {
            IntPtr preOffsetAddr = freeAddr + sizeof(byte) + sizeof(Int16) + sizeof(Int32);
            Int32 preOffset = MemInt32.GetValue(ref preOffsetAddr);
            if (preOffset == 0)
                return new IntPtr(0);
            return preOffsetAddr - preOffset;
        }
    }

    class MemCell
    {
        //update cell(updatingAddr) nextNode and preNode
        public static unsafe void UpdateNextNode_PreNode(IntPtr updatingAddr, IntPtr preAddr)
        {
            if (preAddr.ToInt64() != 0)
            {
                Int32* pre_nextNode = (Int32*)(preAddr + 1);
                *pre_nextNode = (Int32)(updatingAddr.ToInt64() - preAddr.ToInt64());

                Int32* cur_preNode = (Int32*)(updatingAddr + 5);
                *cur_preNode = (Int32)(updatingAddr.ToInt64() - preAddr.ToInt64());
            }
            else
            {
                Int32* cur_preNode = (Int32*)(updatingAddr + 5);
                *cur_preNode = (Int32)0;
            }
        }
    }

    class MemHelper
    {
        //// TOOLS contians: GET info from memory, INSERT infor to memory, node operation

        //pay attention: get cell and get struct is depend on different object

        #region INSERT infor to memory






        //get the intptr of list, if isFull==1&&hasNext==0, return 0;
        static IntPtr locateListTail(IntPtr cellAddr)
        {
            IntPtr offsetMemAddr = MemHelper.GetOffsetAddr(ref cellAddr);

            byte status = MemHelper.GetByte(ref offsetMemAddr);

            Int16 length = MemHelper.GetInt16(ref offsetMemAddr);

            byte isFullMask = 0x20;
            byte isFull = (byte)(status & isFullMask);

            if (isFull > 0)
            {
                byte hasNextMask = 0x40;
                byte hasNext = (byte)(status & hasNextMask);
                if (hasNext > 0)
                {
                    IntPtr nextOffsetAddr = offsetMemAddr + length - sizeof(Int32);
                    return locateListTail(nextOffsetAddr);
                }
                else
                {
                    //if can not inset direct, return 0
                    return new IntPtr(0);
                }
            }
            else
            {
                return new IntPtr(0);
            }
        }

        public static void InsertListTail<T>(IntPtr cellAddr, T value, IntPtr[] freeAddrs, IntPtr tailAddr, int gap)
        {
            if (typeof(T) != typeof(Int32) && typeof(T) != typeof(Int64))
            {

            }
            else
            {
                IntPtr listTailAddr = locateListTail(cellAddr);
                if (listTailAddr.ToInt64() != 0)
                {
                    if (typeof(T) == typeof(Int32))
                        InsertValue(ref listTailAddr, Int32.Parse(value.ToString()));
                    else if (typeof(T) == typeof(Int64))
                        InsertValue(ref listTailAddr, Int64.Parse(value.ToString()));
                }
                else
                {
                    //isFull==1, hasNext==0

                }
            }
        }

        #endregion INSERT infor to memory

        #region DELETE info



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
            IntPtr offsetMemAddrCopy = offsetMemAddr;

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

            //add to freeAdds
            if (length >= 64)
            {
                InsertFreeList(deleteAddr, freeAdds, length);
                //merge with after
                IntPtr nextAddr = offsetMemAddrCopy + length;
                MergeWithNext(deleteAddr, nextAddr, freeAdds);
            }
        }

        #endregion DELETE info

    }
}
