using System;

/*	
    In Memory:

    Tx {
        status      byte
        nextNode    int32   // next node
        preNode     int32   // pre node
        CellID      Int64
        hash        int32   // =>hash
        time        Int64
        ins         int32   // =>ins
        outs        int32   // =>outs
        amount      Int64
    }

    hash{
        status      byte
        length      int16
        context     byte[]
        [curLnegth] int32
        [nextPart]  int32
    }

    ins{
        status      byte
        length      int16
        context     int32[] //=>in
        [curLnegth] int32
        [nextPart]  int32
    }

    in{
        status      byte
        addr        int32   // =>in_addr
        tx_index    Int64
    }

    in_addr{
        status      byte
        length      int16
        context     byte[]
        [curLnegth] int32
        [nextPart]  int32
    }

    outs{
        status      byte
        length      int16
        context     int32[] //=>out
        [curLnegth] int32
        [nextPart]  int32

    }

    out{
        status      byte
        length      int16
        context     byte[]
        [curLnegth] int32
        [nextPart]  int32
    }
*/

namespace ToyGE
{
    public class TxHelper
    {
        //convert memory Tx to jsonback object for random access
        public static JSONBack GetCell(IntPtr jsonBackAddr)
        {
            JSONBack jsonBack = new JSONBack();

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
        public static void InsertCell(JSONBack jsonBack, ref IntPtr memAddr, ref IntPtr preAddr, Int16 gap)
        {
            //update nextNode and preNode
            MemHelper.UpdateNextNode_PreNode(memAddr, preAddr);
            preAddr = memAddr;

            //pointer for insert unsure length type, 45 is the length of tx
            IntPtr nextPartAddr = memAddr + 45;

            //insert cellStatus
            MemHelper.InsertValue(ref memAddr, (byte)0);

            //jump nextNode and preNode, has updated
            MemHelper.addrJump(ref memAddr, 8);

            //insert CellID
            MemHelper.InsertValue(ref memAddr, jsonBack.CellID);

            //insert hash(X)
            MemHelper.InsertEntireString(ref memAddr, jsonBack.hash, ref nextPartAddr, gap);

            //insert time
            MemHelper.InsertValue(ref memAddr, jsonBack.time);

            //insert ins(X)
            MemHelper.InsertEntireList<Input>(ref memAddr, jsonBack.ins, ref nextPartAddr, sizeof(Int32), gap, null, InsertIn);

            //insert outs(X)
            MemHelper.InsertEntireList(ref memAddr, jsonBack.outs, ref nextPartAddr, sizeof(Int32), gap, null, MemHelper.InsertEntireString);

            //insert amount
            MemHelper.InsertValue(ref memAddr, jsonBack.amount);

            memAddr = nextPartAddr;
        }

        //insert In struct
        static void InsertIn(ref IntPtr memAddr, Input input, ref IntPtr nextPartAddr, Int16 gap)
        {
            //insert pointer
            MemHelper.InsertValue(ref memAddr, (Int32)(nextPartAddr.ToInt64() - memAddr.ToInt64()));

            //struct length
            IntPtr nextNextPartAddr = nextPartAddr + 13;

            //insert inStatus
            MemHelper.InsertValue(ref nextPartAddr, (byte)0);

            //insert in_addr
            MemHelper.InsertEntireString(ref nextPartAddr, input.addr, ref nextNextPartAddr, gap);

            //insert tx_index
            MemHelper.InsertValue(ref nextPartAddr, input.tx_index);

            nextPartAddr = nextNextPartAddr;
        }

        //insert out string
        public static unsafe void InsertOut(IntPtr memAddr, string _out, ref IntPtr nextPartAddr, Int16 gap)
        {
            MemHelper.InsertEntireString(ref memAddr, _out, ref nextPartAddr, gap);
        }

        //delete tx cell
        public static unsafe void DeleteCell(IntPtr memAddr, IntPtr[] freeAddrs)
        {
            //update status IsDelete=1
            IntPtr statusAddr = memAddr;
            byte* status = (byte*)(statusAddr.ToPointer());
            byte mask = 0x80;
            *status = (byte)(*status | mask);

            //delete hash
            IntPtr hashAddr = memAddr + 17;
            MemHelper.DeleteString(ref hashAddr, freeAddrs);

            //delete ins
            IntPtr insAddr = memAddr + 29;
            MemHelper.DeleteList<Input>(ref insAddr, freeAddrs, DeleteIn);

            //delete outs
            IntPtr outsAddr = memAddr + 33;
            MemHelper.DeleteList<Input>(ref outsAddr, freeAddrs, MemHelper.DeleteString);


            //update cell link list
            int length = 44;
            if (length >= 64)
            {
                IntPtr nextAddr = new IntPtr(memAddr.ToInt64() + *(Int32*)(memAddr + 1));
                Int32 preOffset = *(Int32*)(memAddr + 5);
                IntPtr preAddr = preOffset == 0 ? new IntPtr(0) : new IntPtr(memAddr.ToInt64() - preOffset);
                MemHelper.UpdateNextNode_PreNode(nextAddr, preAddr);
            }
        }

        public static void DeleteIn(ref IntPtr memAddr, IntPtr[] freeAddrs)
        {
            IntPtr offsetMemAddr = MemHelper.GetOffsetAddr(ref memAddr);

            //delete in_addr
            IntPtr in_addr = offsetMemAddr + 1;
            MemHelper.DeleteString(ref in_addr, freeAddrs);
        }

        //update hash
        public static unsafe void UpdateHash(IntPtr memAddr, string newHash, IntPtr[] freeAdds)
        {
            //pointer for hash
            memAddr += 17;
            MemHelper.UpdateString(memAddr, newHash, freeAdds);
        }

        //update amount
        public static unsafe void UpdateAmount(IntPtr memAddr, Int64 newAmount)
        {
            //pointer for amount
            memAddr += 37;
            MemHelper.InsertValue(ref memAddr, newAmount);
        }
    }
}
