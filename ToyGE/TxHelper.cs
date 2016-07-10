using System;
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
        strLen      int32
        status      byte
        nextPart    Int64
        context     char[]
    }

    ins{
        listLen     int32
        status      byte
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
        strLen      int32
        status      byte
        nextPart    Int64
        context     char[]
    }

    outs{
        listLen     int32
        status      byte
        nextPart    Int64
        out_1       int32   // =>out
        ...
        out_N       int32   // =>out
    }

    out{
        strLen      int32
        status      byte
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
        public static void InsertJsonBack(JSONBack jsonBack, ref IntPtr memAddr, ref IntPtr preAddr)
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
            MemHelper.InsertString(ref memAddr, jsonBack.hash, ref nextPartAddr);

            //insert time
            MemHelper.InsertInt64(ref memAddr, jsonBack.time);

            //insert ins(X)
            MemHelper.InsertList<Input>(ref memAddr, jsonBack.ins, ref nextPartAddr, InsertIn);

            //insert outs(X)
            MemHelper.InsertList(ref memAddr, jsonBack.outs, ref nextPartAddr, MemHelper.InsertString);

            //insert amount
            MemHelper.InsertInt64(ref memAddr, jsonBack.amount);

            memAddr = nextPartAddr;
        }

        //insert In struct
        static void InsertIn(ref IntPtr memAddr, Input input, ref IntPtr nextPartAddr)
        {
            //insert pointer
            MemHelper.InsertInt32(ref memAddr, (Int32)(nextPartAddr.ToInt64() - memAddr.ToInt64()));

            //struct length
            IntPtr nextNextPartAddr = nextPartAddr + 13;

            //insert inStatus
            MemHelper.InsertByte(ref nextPartAddr, (byte)0);

            //insert in_addr
            MemHelper.InsertString(ref nextPartAddr, input.addr, ref nextNextPartAddr);

            //insert tx_index
            MemHelper.InsertInt64(ref nextPartAddr, input.tx_index);

            nextPartAddr = nextNextPartAddr;
        }

        //update amount
        public static unsafe void UpdateAmount(IntPtr memAddr, Int64 newAmount)
        {
            //pointer for amount
            Int64* amount = (Int64*)((memAddr + 37).ToPointer());
            *amount = newAmount;
        }

        public static unsafe void InsertOut(IntPtr memAddr, string _out, ref IntPtr nextPartAddr)
        {

        }
    }
}
