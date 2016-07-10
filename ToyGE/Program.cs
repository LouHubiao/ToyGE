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
        /* global variable */
        //hash index b-tree, contains cellID and logistic address
        static BTreeNode hashTree = Index.BTCreate();

        //memory parts begin address and node count
        static List<IntPtr> memAddrs = new List<IntPtr>();
        static List<int> memCounts = new List<int>();

        //current frontest addr
        static IntPtr frontAddr = new IntPtr(0);

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
                    JSONBack back = SearchNode(Int64.Parse(inputs[1]));
                    if (back != null)
                        Console.WriteLine(back.ToString());
                    else
                        Console.WriteLine("null!");
                }
                else if (inputs[0] == "delete")
                {
                    DeleteNode(Int64.Parse(inputs[1]));
                }
                else if (inputs[0] == "statistic")
                {
                    if (inputs[1] == "count")
                    {
                        int count = Foreach(Statistic.Count_Statistic, 0, 0);
                        Console.WriteLine("Count_Statistic:" + count);
                    }
                    if (inputs[1] == "amount")
                    {
                        int count = Foreach(Statistic.Amount_Statistic, 5000000000, 0);
                        Console.WriteLine("Amount_Statistic:" + count);
                    }
                }
                else if (inputs[0] == "update")
                {
                    if (inputs[2] == "amount")
                    {
                        Int64 key = Int64.Parse(inputs[1]);
                        Int64 newAmount = Int64.Parse(inputs[3]);
                        UpdateAmount(key, newAmount);
                    }
                }
                Console.WriteLine();
            }
        }

        static unsafe void LoadTxs()
        {
            Console.WriteLine("LoadTxs begin..." + DateTime.Now);

            // #define max line (1 << 20)
            IntPtr memAddr = Marshal.AllocHGlobal(1 << 30); //512MB per memory part
            memAddrs.Add(memAddr);
            IntPtr curAddr = memAddr;
            int count = 0;  //tx count

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
                    IntPtr preAddr = new IntPtr();
                    while (null != (line = reader.ReadLine()))
                    {
                        //insert one node into memory
                        InsertNode(line, ref curAddr, ref preAddr, ref count);
                        //if mem parts is full, create new memory
                        if (curAddr.ToInt64() - memAddr.ToInt64() > ((1 << 30) - (1 << 20)))
                        {
                            preAddr = new IntPtr();
                            memCounts.Add(count);
                            memAddr = Marshal.AllocHGlobal(1 << 30);    //next memory part
                            memAddrs.Add(memAddr);
                            curAddr = memAddr;
                            count = 0;
                        }
                    }
                }
            }
            memCounts.Add(count);

            frontAddr = memAddr;

            Console.WriteLine("LoadTxs end..." + DateTime.Now);
        }

        //insert one node into memory and b-tree
        static void InsertNode(string readLine, ref IntPtr curAddr, ref IntPtr preAddr, ref int count)
        {
            //string to object
            JSONBack jsonBack = JSONBack.ConvertStringToJSONBack(readLine);

            //isnert cellID in b-tree
            Int64 cellID = jsonBack.CellID;
            Index.BTInsert(ref hashTree, cellID, curAddr);

            //insert node
            if (jsonBack.amount > 0)
            {
                TxHelper.InsertJsonBack(jsonBack, ref curAddr, ref preAddr);
                count++;
            }
        }

        //search node by key
        static JSONBack SearchNode(Int64 key)
        {
            Console.WriteLine("SearchNode begin..." + DateTime.Now);

            IntPtr node = new IntPtr();
            if (Index.BTSearch(hashTree, key, ref node))
            {
                if (!MemHelper.IsDeleted(node))
                {
                    JSONBack result = TxHelper.GetJsonBack(node);
                    Console.WriteLine("SearchNode end..." + DateTime.Now);
                    return result;
                }
            }
            return null;
        }

        // Statistic some property
        public delegate bool StatisticFun(IntPtr memAddr, Int64 a, Int64 b);
        static unsafe int Foreach(StatisticFun fun, Int64 amount, Int64 other)
        {
            Console.WriteLine("Foreach begin..." + DateTime.Now);

            int result = 0;
            for (int i = 0; i < memAddrs.Count; i++)
            {
                IntPtr memAddr = memAddrs[i];
                for (int j = 0; j < memCounts[i]; j++)
                {
                    Int32* nextOffset = (Int32*)(memAddr + 1);
                    if (fun(memAddr, amount, other))
                    {
                        if (!MemHelper.IsDeleted(memAddr))
                            result++;
                    }
                    if (*nextOffset == 0)
                        break;
                    memAddr += *nextOffset;
                }
            }

            Console.WriteLine("Foreach end..." + DateTime.Now);
            return result;
        }

        //delete node
        static void DeleteNode(Int64 key)
        {
            Console.WriteLine("DeleteNode begin..." + DateTime.Now);

            IntPtr nodeAddr = new IntPtr();
            if (Index.BTSearch(hashTree, key, ref nodeAddr))
            {
                MemHelper.DeleteNode(nodeAddr);
                Console.WriteLine("DeleteNode end..." + DateTime.Now);
            }
        }

        //update amount in tx
        static void UpdateAmount(Int64 key, Int64 newAmount)
        {
            Console.WriteLine("UpdateAmount begin..." + DateTime.Now);

            IntPtr nodeAddr = new IntPtr();
            if (Index.BTSearch(hashTree, key, ref nodeAddr))
            {
                TxHelper.UpdateAmount(nodeAddr, newAmount);
                Console.WriteLine("UpdateAmount end..." + DateTime.Now);
            }
        }
    }
}
