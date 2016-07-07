using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/*	
    In Memory:

    Tx {
        sumLen      int
        CellID	    Int64
        hashLen     int
        hash	    string
        time	    Int64
        insLen      int
        ins		    calculated
        outsLen     int
        outs	    calculated
        amount	    Int64
    }

    In{
        addrLen     int
        addr        Int64
        tx_index    Int64
    }

    Out{
        addrLen     int
        addr        Int64
    }
*/

namespace ToyGE
{
    public class MemoryHelper
    {
        //convert jsonback in memory to jsonback object
        public unsafe static JSONBack ConvertMemToJSONBack(IntPtr memAddr)
        {
            JSONBack JSONBack = new JSONBack();
            try
            {
                int sumLen = GetInt(ref memAddr);

                JSONBack.CellID = GetInt64(ref memAddr);

                int hashLen = GetInt(ref memAddr);

                JSONBack.hash = GetString(ref memAddr, hashLen);

                JSONBack.time = GetInt64(ref memAddr);

                int insLen = GetInt(ref memAddr);

                JSONBack.ins = new List<Input>();
                for (int i = 0; i < insLen; i++)
                {
                    int addrLen = GetInt(ref memAddr);

                    string addr = GetString(ref memAddr, addrLen);

                    Int64 tx_index = GetInt64(ref memAddr);

                    JSONBack.ins.Add(new Input(addr, tx_index));
                }

                int outsLen = GetInt(ref memAddr);

                JSONBack.outs = new List<string>();
                for (int i = 0; i < outsLen; i++)
                {
                    int addrLen = GetInt(ref memAddr);

                    string addr = GetString(ref memAddr, addrLen);

                    JSONBack.outs.Add(addr);
                }

                JSONBack.amount = GetInt64(ref memAddr);
            }
            catch (Exception e)
            {
                return null;
            }
            return JSONBack;
        }

        static unsafe int GetInt(ref IntPtr memAddr)
        {
            int* result = (int*)(memAddr.ToPointer());
            memAddr += sizeof(int);
            return *result;
        }

        static unsafe Int64 GetInt64(ref IntPtr memAddr)
        {
            Int64* result = (Int64*)(memAddr.ToPointer());
            memAddr += sizeof(Int64);
            return *result;
        }

        static unsafe string GetString(ref IntPtr memAddr, int len)
        {
            byte[] resultBytes = new byte[len];
            CopyFromMem((byte*)(memAddr.ToPointer()), 0, resultBytes, 0, len);
            string result = System.Text.Encoding.ASCII.GetString(resultBytes);
            memAddr += len;
            return result;
        }
        
        //copy from memory char* to char[]
        public static unsafe void CopyFromMem(byte* source, int sourceOffset, byte[] target, int targetOffset, int count)
        {
            fixed (byte* pTarget = &target[0])
            {
                byte* ps = source + sourceOffset;
                byte* pt = pTarget + targetOffset;

                // Copy the specified number of bytes from source to target.
                for (int i = 0; i < count; i++)
                {
                    *pt = *ps;
                    pt++;
                    ps++;
                }
            }
        }

        //copy from char[] to memory char*
        public static unsafe void CopyToMem(byte[] source, int sourceOffset, byte* target, int targetOffset, int count)
        {
            fixed (byte* pSource = &source[0])
            {
                byte* ps = pSource + sourceOffset;
                byte* pt = target + targetOffset;

                // Copy the specified number of bytes from source to target.
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
