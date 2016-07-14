﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/*	
    In Memory:

    Tx {
        status      byte
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
        status      byte
        strLen      int32
        strMaxLen   int32
        nextPart    Int64
        context     char[]
    }

    ins{
        status      byte
        listLen     int32
        listMaxLen  int32
        nextPart    Int64
        in_1        int32   // =>in
        ...
        in_N        int32   // =>in
    }

    in{
        status      byte
        addr        int32   // => 
        tx_index    Int64
    }

    in_addr{
        status      byte
        strLen      int32
        strMaxLen   int32
        nextPart    Int64
        context     char[]
    }

    outs{
        status      byte
        listLen     int32
        listMaxLen  int32
        nextPart    Int64
        out_1       int32   // =>out
        ...
        out_N       int32   // =>out
    }

    out{
        status      byte
        strLen      int32
        strMaxLen   int32
        nextPart    Int64
        context     char[]
    }
*/

namespace ToyGE
{
    public class TxHelper
    {
        //convert memory Tx to jsonback object for random access
        public static JSONBack GetJsonBack(IntPtr jsonBackAddr)
        {
            JSONBack jsonBack = new JSONBack();
            try
            {
                // jump cellStatus/ cellNextNode/ cellPreNode
                MemHelper.addrJump(ref jsonBackAddr, 9);

                //read cellID
                jsonBack.CellID = MemHelper.GetInt64(ref jsonBackAddr);

                //read hash
                jsonBack.hash = MemHelper.GetString(ref jsonBackAddr);

                //read time
                jsonBack.time = MemHelper.GetInt64(ref jsonBackAddr);

                //read ins
                jsonBack.ins = MemHelper.GetList<Input>(ref jsonBackAddr, GetIn);

                //read outs
                jsonBack.outs = MemHelper.GetList<string>(ref jsonBackAddr, MemHelper.GetString);

                //time amount
                jsonBack.amount = MemHelper.GetInt64(ref jsonBackAddr);
            }
            catch (Exception e)
            {
                return null;
            }
            return jsonBack;
        }

        //get In struct
        public static Input GetIn(ref IntPtr inAddr)
        {
            IntPtr offsetMemAddr = MemHelper.GetOffsetAddr(ref inAddr);

            byte status = MemHelper.GetByte(ref offsetMemAddr);

            string addr = MemHelper.GetString(ref offsetMemAddr);

            Int64 tx_index = MemHelper.GetInt64(ref offsetMemAddr);

            return new Input(addr, tx_index);
        }

        //convert jsonback to byte[] in memory
        public static void InsertJsonBack(JSONBack jsonBack, ref IntPtr memAddr, ref IntPtr preAddr, Int16 gap)
        {
            //update nextNode and preNode
            if (preAddr.ToInt64() != 0)
            {
                MemHelper.UpdateNextNode(memAddr, preAddr);
                MemHelper.UpdatePreNode(memAddr, preAddr);
            }
            preAddr = memAddr;

            //pointer for insert unsure length type, 45 is the length of tx
            IntPtr nextPartAddr = memAddr + 45;

            //insert cellStatus
            MemHelper.InsertByte(ref memAddr, (byte)0);

            //jump nextNode and preNode, has updated
            MemHelper.addrJump(ref memAddr, 8);

            //insert CellID
            MemHelper.InsertInt64(ref memAddr, jsonBack.CellID);

            //insert hash(X)
            MemHelper.InsertEntireString(ref memAddr, jsonBack.hash, ref nextPartAddr, gap);

            //insert time
            MemHelper.InsertInt64(ref memAddr, jsonBack.time);

            //insert ins(X)
            MemHelper.InsertEntireList<Input>(ref memAddr, jsonBack.ins, ref nextPartAddr, sizeof(Int32), 4, null, InsertIn);

            //insert outs(X)
            MemHelper.InsertEntireList(ref memAddr, jsonBack.outs, ref nextPartAddr, sizeof(Int32), gap, null, MemHelper.InsertEntireString);

            //insert amount
            MemHelper.InsertInt64(ref memAddr, jsonBack.amount);

            memAddr = nextPartAddr;
        }

        //insert In struct
        static void InsertIn(ref IntPtr memAddr, Input input, ref IntPtr nextPartAddr, Int16 gap)
        {
            //insert pointer
            MemHelper.InsertInt32(ref memAddr, (Int32)(nextPartAddr.ToInt64() - memAddr.ToInt64()));

            //struct length
            IntPtr nextNextPartAddr = nextPartAddr + 13;

            //insert inStatus
            MemHelper.InsertByte(ref nextPartAddr, (byte)0);

            //insert in_addr
            MemHelper.InsertEntireString(ref nextPartAddr, input.addr, ref nextNextPartAddr, gap);

            //insert tx_index
            MemHelper.InsertInt64(ref nextPartAddr, input.tx_index);

            nextPartAddr = nextNextPartAddr;
        }

        //insert out string
        public static unsafe void InsertOut(IntPtr memAddr, string _out, ref IntPtr nextPartAddr, Int16 gap)
        {
            MemHelper.InsertEntireString(ref memAddr, _out, ref nextPartAddr, gap);
        }

        //delete noed
        public static unsafe void DeleteNode(IntPtr memAddr, IntPtr[] freeAddrs)
        {
            //update status
            byte* status = (byte*)(memAddr.ToPointer());
            byte mask = 0x80;
            *status = (byte)(*status | mask);

            //update cell link list
            IntPtr nextAddr = new IntPtr(memAddr.ToInt64() + *(Int32*)(memAddr + 1));
            IntPtr preAddr = new IntPtr(memAddr.ToInt64() - *(Int32*)(memAddr + 5));
            MemHelper.UpdateNextNode(nextAddr, preAddr);
            MemHelper.UpdatePreNode(nextAddr, preAddr);

            //delete hash
            IntPtr hashAddr = memAddr + 17;
            MemHelper.DeleteString(ref hashAddr, freeAddrs);

            //delete ins
            IntPtr insAddr = memAddr + 21;
            MemHelper.DeleteList<Input>(ref insAddr, freeAddrs, DeleteIn);

            //delete outs
            IntPtr outsAddr = memAddr + 25;
            MemHelper.DeleteList<Input>(ref outsAddr, freeAddrs, MemHelper.DeleteString);
        }

        public static void DeleteIn(ref IntPtr memAddr, IntPtr[] freeAddrs)
        {
            //delete in_addr
            IntPtr in_addr = memAddr + 1;
            MemHelper.DeleteString(ref in_addr, freeAddrs);
        }

        //update hash
        public static unsafe void UpdateHash(IntPtr memAddr, string newHash, ref IntPtr nextPartAddr, Int16 gap, IntPtr[] freeAdds)
        {
            //pointer for hash
            memAddr += 17;
            MemHelper.UpdateString(memAddr, newHash, ref nextPartAddr, gap, freeAdds);
        }

        //update amount
        public static unsafe void UpdateAmount(IntPtr memAddr, Int64 newAmount)
        {
            //pointer for amount
            memAddr += 37;
            MemHelper.InsertInt64(ref memAddr, newAmount);
        }
    }
}
