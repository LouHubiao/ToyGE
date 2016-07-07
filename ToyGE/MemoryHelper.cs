using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/*	
    In Memory:

    Tx {
        status      char
        nextNode    int32   // next node
        preNode     int32   // pre node
        CellID	    Int64
        hash        int32   // =>hash
        time	    Int64
        ins		    int32   // =>ins
        outs	    int32   // =>outs
        amount	    Int64
    }

    hash{
        strLen      int32
        status      char
        nextPart    int32
        context     char[]
    }

    ins{
        listLen     int32
        status      char
        nextPart    int32
        in_1        int32   // =>in
        ...
        in_N        int32   // =>in
    }

    in{
        status      char
        addr        int32   // =>in_addr
        tx_index    Int64
    }

    in_addr{
        strLen      int32
        status      char
        nextPart    int32
        context     char[]
    }

    outs{
        listLen     int32
        status      char
        nextPart    int32
        out_1       int32   // =>out_addr
        ...
        out_N       int32   // =>out_addr
    }

    out{
        strLen      int32
        status      char
        nextPart    int32
        context     char[]
    }
*/

namespace ToyGE
{
    public class MemoryHelper
    {
        //convert memory Tx to jsonback object for random access
        public static JSONBack ConvertMemToJSONBack(IntPtr memAddr)
        {
            JSONBack jsonBack = new JSONBack();
            try
            {
                byte cellStatus = GetByte(ref memAddr);

                addrJump(ref memAddr, 8);   // jump cellNextNode and cellPreNode

                //read cellID
                jsonBack.CellID = GetInt64(ref memAddr);

                //read hash
                IntPtr hashAddr = GetOffsetAddr(ref memAddr);
                ReadHash(hashAddr, jsonBack);

                //read time
                jsonBack.time = GetInt64(ref memAddr);

                //read ins
                IntPtr insAddr = GetOffsetAddr(ref memAddr);
                ReadIns(insAddr, jsonBack);

                //read outs
                jsonBack.outs = new List<string>();
                IntPtr outsAddr = GetOffsetAddr(ref memAddr);
                ReadOuts(outsAddr, jsonBack.outs);

                //time amount
                jsonBack.amount = GetInt64(ref memAddr);
            }
            catch (Exception e)
            {
                return null;
            }
            return jsonBack;
        }

        //convert jsonback to byte[]
        public static void ConvertJsonBackToMem(JSONBack jsonBack, ref IntPtr memAddr)
        {
            //insert cellStatus
            InsertByte(ref memAddr, (byte)0);

            //jump nextNode and preNode
            addrJump(ref memAddr, 8);

            //insert CellID
            InsertInt64(ref memAddr, jsonBack.CellID);

            //insert hash(X)
            IntPtr hashOffset = InsertInt32(ref memAddr, 0);

            //insert time
            InsertInt64(ref memAddr, jsonBack.time);

            //insert ins(X)
            IntPtr insOffsetAddr = InsertInt32(ref memAddr, 0);

            //insert outs(X)
            IntPtr outsOffsetAddr = InsertInt32(ref memAddr, 0);

            //insert CellID
            InsertInt64(ref memAddr, jsonBack.amount);
            
            //update hash(X)
            InsertInt32(ref hashOffset, (Int32)(memAddr.ToInt64() - hashOffset.ToInt64()));
            //insert hash
            InsertHash(ref memAddr, jsonBack.hash);

            //update ins(X)
            InsertInt32(ref insOffsetAddr, (Int32)(memAddr.ToInt64() - insOffsetAddr.ToInt64()));
            //insert ins
            List<IntPtr> insOffset = new List<IntPtr>();
            InsertIns(ref memAddr, jsonBack.ins, insOffset);

            List<IntPtr> in_addrOffset = new List<IntPtr>();
            for (int i = 0; i < insOffset.Count; i++)
            {
                //update in
                IntPtr offset = insOffset[i];
                InsertInt32(ref offset, (Int32)(memAddr.ToInt64() - offset.ToInt64()));

                //insert in
                InsertIn(ref memAddr, jsonBack.ins[i], in_addrOffset);
            }

            for(int i = 0; i < in_addrOffset.Count; i++)
            {
                //update in_addr
                IntPtr offset = in_addrOffset[i];
                InsertInt32(ref offset, (Int32)(memAddr.ToInt64() - offset.ToInt64()));

                //insert in_addr
                InsertIn_addr(ref memAddr, jsonBack.ins[i].addr);
            }

            //update outs(X)
            InsertInt32(ref outsOffsetAddr, (Int32)(memAddr.ToInt64() - outsOffsetAddr.ToInt64()));
            //insert outs
            List<IntPtr> outsOffset = new List<IntPtr>();
            InsertOuts(ref memAddr, jsonBack.outs, outsOffset);

            for (int i = 0; i < outsOffset.Count; i++)
            {
                //update out
                IntPtr offset = outsOffset[i];
                InsertInt32(ref offset, (Int32)(memAddr.ToInt64() - offset.ToInt64()));

                //insert out
                InsertOut(ref memAddr, jsonBack.outs[i]);
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

        //delete noede
        public static unsafe void DeleteNode(IntPtr memAddr)
        {
            byte* status = (byte*)(memAddr.ToPointer());
            byte mask = 0x80;
            *status = (byte)(*status | mask);

            Int32* nextOffset = (Int32*)(memAddr + 1);
            IntPtr nextAddr = new IntPtr(memAddr.ToInt64() + *nextOffset);

            Int32* preOffset = (Int32*)(memAddr + 9);
            IntPtr preAddr = new IntPtr(memAddr.ToInt64() - *nextOffset);

            UpdateNextNode(nextAddr, preAddr);
            UpdatePreNode(nextAddr, preAddr);
        }

        public static unsafe bool IsDeleted(IntPtr memAddr)
        {
            byte* status = (byte*)(memAddr.ToPointer());
            byte mask = 0x80;
            byte isDeleted = (byte)(*status & mask);

            if (isDeleted > 0)
                return true;
            return false;
        }

        public static unsafe bool HasNextNode(IntPtr memAddr)
        {
            Int32* nextNode = (Int32*)(memAddr + 1);
            if (*nextNode == 0)
                return true;
            return false;
        }

        #region read helper

        static void ReadHash(IntPtr hashAddr, JSONBack jsonBack)
        {
            Int32 hashLen = GetInt32(ref hashAddr);

            byte hashStatus = GetByte(ref hashAddr);

            Int32 hashNextPart = GetInt32(ref hashAddr);

            jsonBack.hash = GetString(ref hashAddr, hashLen);
        }

        static void ReadIns(IntPtr insAddr, JSONBack jsonBack)
        {
            jsonBack.ins = new List<Input>();
            {
                Int32 insLen = GetInt32(ref insAddr);

                byte insStatus = GetByte(ref insAddr);

                Int32 insNextPart = GetInt32(ref insAddr);

                for (int i = 0; i < insLen; i++)
                {
                    //read in
                    IntPtr inAddr = GetOffsetAddr(ref insAddr);
                    ReadIn(inAddr, jsonBack.ins);
                }
            }
        }

        static void ReadIn(IntPtr inAddr, List<Input> ins)
        {
            byte inStatus = GetByte(ref inAddr);

            //read addr
            string addr;
            IntPtr addrAddr = GetOffsetAddr(ref inAddr);
            {
                Int32 addrLen = GetInt32(ref addrAddr);

                byte addrStatus = GetByte(ref addrAddr);

                Int32 addrNextPart = GetInt32(ref addrAddr);

                addr = GetString(ref addrAddr, addrLen);
            }

            Int64 tx_index = GetInt64(ref inAddr);

            ins.Add(new Input(addr, tx_index));
        }

        static void ReadOuts(IntPtr outsAddr, List<string> outs)
        {
            Int32 outsLen = GetInt32(ref outsAddr);

            byte outsStatus = GetByte(ref outsAddr);

            Int32 outsNextPart = GetInt32(ref outsAddr);

            //read out
            IntPtr addrAddr = GetOffsetAddr(ref outsAddr);
            for (int i = 0; i < outsLen; i++)
            {
                Int32 addrLen = GetInt32(ref addrAddr);

                byte addrStatus = GetByte(ref addrAddr);

                Int32 addrNextPart = GetInt32(ref addrAddr);

                string addr = GetString(ref addrAddr, addrLen);

                outs.Add(addr);
            }
        }

        #endregion read helper

        #region insert helper

        static void InsertHash(ref IntPtr memAddr, string hash)
        {
            //insert strLen
            InsertInt32(ref memAddr, hash.Length);

            //insert hashStatus
            InsertByte(ref memAddr, (byte)0);

            //insert nextPart
            InsertInt32(ref memAddr, 0);

            //inser context
            InsertString(ref memAddr, hash);
        }

        static void InsertIns(ref IntPtr memAddr, List<Input> ins, List<IntPtr> insOffset)
        {
            //insert listLen
            InsertInt32(ref memAddr, ins.Count);

            //insert insStatus
            InsertByte(ref memAddr, (byte)0);

            //insert nextPart
            InsertInt32(ref memAddr, 0);

            //insert in(X)
            for (int i = 0; i < ins.Count; i++)
            {
                //insert nextPart
                insOffset.Add(InsertInt32(ref memAddr, 0));
            }
        }

        static void InsertIn(ref IntPtr memAddr, Input input, List<IntPtr> in_addrOffset)
        {
            //insert inStatus
            InsertByte(ref memAddr, (byte)0);

            //insert in_addr
            in_addrOffset.Add(InsertInt32(ref memAddr, 0));

            //insert tx_index
            InsertInt64(ref memAddr, input.tx_index);
        }

        static void InsertIn_addr(ref IntPtr memAddr, string in_addr)
        {
            //insert strLen
            InsertInt32(ref memAddr, in_addr.Length);

            //insert hashStatus
            InsertByte(ref memAddr, (byte)0);

            //insert nextPart
            InsertInt32(ref memAddr, 0);

            //inser context
            InsertString(ref memAddr, in_addr);
        }

        static void InsertOuts(ref IntPtr memAddr, List<string> outs, List<IntPtr> outsOffset)
        {
            //insert listLen
            InsertInt32(ref memAddr, outs.Count);

            //insert insStatus
            InsertByte(ref memAddr, (byte)0);

            //insert nextPart
            InsertInt32(ref memAddr, 0);

            //insert in(X)
            for (int i = 0; i < outs.Count; i++)
            {
                //insert nextPart
                outsOffset.Add(InsertInt32(ref memAddr, 0));
            }
        }

        static void InsertOut(ref IntPtr memAddr, string out_addr)
        {
            //insert strLen
            InsertInt32(ref memAddr, out_addr.Length);

            //insert hashStatus
            InsertByte(ref memAddr, (byte)0);

            //insert nextPart
            InsertInt32(ref memAddr, 0);

            //inser context
            InsertString(ref memAddr, out_addr);
        }

        #endregion insert helper

        #region tools contians: GET info from memory, INSERT infor to memory

        #region GET info from memory

        //get next char
        static unsafe byte GetByte(ref IntPtr memAddr)
        {
            byte* result = (byte*)(memAddr.ToPointer());
            memAddr += sizeof(byte);
            return *result;
        }

        //get next int32
        static unsafe Int32 GetInt32(ref IntPtr memAddr)
        {
            Int32* result = (Int32*)(memAddr.ToPointer());
            memAddr += sizeof(Int32);
            return *result;
        }

        //get next int64
        static unsafe Int64 GetInt64(ref IntPtr memAddr)
        {
            Int64* result = (Int64*)(memAddr.ToPointer());
            memAddr += sizeof(Int64);
            return *result;
        }

        //Get next Int32 by offset
        static unsafe IntPtr GetOffsetAddr(ref IntPtr memAddr)
        {
            Int32* offset = (Int32*)(memAddr.ToPointer());
            IntPtr result = memAddr + *offset;
            memAddr += sizeof(Int32);
            return result;
        }

        //get next string
        static unsafe string GetString(ref IntPtr memAddr, int len)
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

        static unsafe void addrJump(ref IntPtr memAddr, int interval)
        {
            memAddr += interval;
        }

        #endregion GET info from memory

        #region INSERT infor to memory

        //insert char
        static unsafe IntPtr InsertByte(ref IntPtr memAddr, byte input)
        {
            IntPtr result = memAddr;
            *(byte*)(memAddr.ToPointer()) = input;
            memAddr += sizeof(byte);
            return result;
        }

        static unsafe IntPtr InsertInt32(ref IntPtr memAddr, Int32 input)
        {
            IntPtr result = memAddr;
            *(Int32*)(memAddr.ToPointer()) = input;
            memAddr += sizeof(Int32);
            return result;
        }

        static unsafe IntPtr InsertInt64(ref IntPtr memAddr, Int64 input)
        {
            IntPtr result = memAddr;
            *(Int64*)(memAddr.ToPointer()) = input;
            memAddr += sizeof(Int64);
            return result;
        }

        static unsafe IntPtr InsertString(ref IntPtr memAddr, string input)
        {
            IntPtr result = memAddr;
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(input);
            MemoryHelper.CopyToMem(bytes, 0, (byte*)memAddr.ToPointer(), 0, bytes.Length);
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

        #endregion INSERT infor to memory

        #endregion
    }
}
