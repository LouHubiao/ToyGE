using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ToyGE
{
    class Program
    {
        static void Main(string[] args)
        {
            LoadTxs();
            Console.WriteLine();

            while (true)
            {
                Console.WriteLine("test: Please input type and value");
                string[] inputs = Console.ReadLine().Split(' ');
                if (inputs[0] == "search")
                {
                    Console.WriteLine("SearchNode begin..." + DateTime.Now);
                    JSONBack back = TxMain.SearchNode(Int64.Parse(inputs[1]));
                    if (back != null)
                        Console.WriteLine(back.ToString());
                    else
                        Console.WriteLine("null!");
                    Console.WriteLine("SearchNode end..." + DateTime.Now);
                }
                else if (inputs[0] == "delete")
                {
                    Console.WriteLine("DeleteNode begin..." + DateTime.Now);
                    TxMain.DeleteNode(Int64.Parse(inputs[1]));
                    Console.WriteLine("DeleteNode end..." + DateTime.Now);
                }
                else if (inputs[0] == "statistic")
                {
                    Console.WriteLine("statistic begin..." + DateTime.Now);
                    if (inputs[1] == "count")
                    {
                        int count = TxMain.Foreach(Statistic.Count_Statistic, 0, 0);
                        Console.WriteLine("Count_Statistic:" + count);
                    }
                    if (inputs[1] == "amount")
                    {
                        Int64 amount = Int64.Parse(inputs[2]);
                        int count = TxMain.Foreach(Statistic.Amount_Statistic, amount, 0);
                        Console.WriteLine("Amount_Statistic:" + count);
                    }
                    Console.WriteLine("statistic end..." + DateTime.Now);
                }
                else if (inputs[0] == "update")
                {
                    Console.WriteLine("Update begin..." + DateTime.Now);
                    if (inputs[2] == "amount")
                    {
                        Int64 key = Int64.Parse(inputs[1]);
                        Int64 newAmount = Int64.Parse(inputs[3]);
                        TxMain.UpdateAmount(key, newAmount);
                    }
                    else if (inputs[2] == "hash")
                    {
                        Int64 key = Int64.Parse(inputs[1]);
                        string newHash = inputs[3];
                        TxMain.UpdateHash(key, newHash);
                    }
                    Console.WriteLine("Update end..." + DateTime.Now);
                }
                Console.WriteLine();
            }
        }

        //load tx files
        static unsafe void LoadTxs()
        {
            Console.WriteLine("LoadTxs begin..." + DateTime.Now);

            //load staitc floder
            //test: D:\\Bit\\TSLBit\\Generator\\bin\\x64\\Debug\\test
            //full: D:\\Bit\\TSLBit\\Generator\\bin\x64\\Debug\\fullBlocks
            //remote: D:\\v-hulou\\fullBlocks
            DirectoryInfo dirInfo = new DirectoryInfo(@"D:\\Bit\\TSLBit\\Generator\\bin\x64\\Debug\\fullBlocks");
            foreach (FileInfo file in dirInfo.GetFiles("block1.txt"))
            {
                //read json line by line
                using (StreamReader reader = new StreamReader(file.FullName))
                {
                    string line;
                    IntPtr preAddr = new IntPtr(0);
                    while (null != (line = reader.ReadLine()))
                    {
                        //string to object
                        JSONBack jsonBack = JSONBack.ConvertStringToJSONBack(line);

                        //insert one node into memory
                        TxMain.InsertTx_Cell_Index(jsonBack.CellID, jsonBack);

                        foreach (string _out in jsonBack.outs)
                        {
                            IntPtr nodeAddr = new IntPtr();
                            if (UserMain.hashTree.BTSearch(UserMain.hashTree.root, _out, ref nodeAddr))
                            {
                                //insert list part
                                
                            }
                            else
                            {
                                //insert new list
                                List<Int64> txs = new List<Int64>();
                                txs.Add(jsonBack.CellID);
                                UserMain.InsertUser_Cell_Index(_out, txs);
                            }
                        }

                    }
                }
            }
            Console.WriteLine("LoadTxs end..." + DateTime.Now);
        }

        public static void InsertListPart(IntPtr cellAddr, Int64 tx)
        {
            IntPtr offsetMemAddr = MemHelper.GetOffsetAddr(ref cellAddr);

            byte status = MemHelper.GetByte(ref offsetMemAddr);

            byte isFullMask = 0x20;
            byte isFull = (byte)(status & isFullMask);

            Int16 length = MemHelper.GetInt16(ref offsetMemAddr);

            if (isFull > 0)
            {
                //change from isFull to hasNext

            }
            else
            {
                byte hasNextMask = 0x40;
                byte hasNext = (byte)(status & hasNextMask);
                if (hasNext > 0)
                {

                }
                else
                {

                }
            }
        }
    }
}
