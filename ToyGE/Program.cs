using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

/*	Tx {
*		CellID	int64_t
*		hash	char[64]
*		time	int64_t
*		ins		calculated
*		outs	calculated
*		amount	int64_t
*	}
*/

namespace ToyGE
{
    class Program
    {
        /* global variable */
        //hash index b-tree, contains cellID and logistic address
        static BTreeNode hashTree = BTree.BTCreate();

        //memory parts begin address and node count
        static List<IntPtr> memAddrs = new List<IntPtr>();
        static List<int> memCounts = new List<int>();

        static void Main(string[] args)
        {
            LoadTxs();
            Console.WriteLine();

            int amount_count = Foreach(Statistic.Amount_Statistic, 5000000000, 0);
            Console.WriteLine("Amount_Statistic:" + amount_count);
            Console.WriteLine();

            int count = Foreach(Statistic.Count_Statistic, 0, 0);
            Console.WriteLine("Count_Statistic:" + count);
            Console.WriteLine();

            while (true)
            {
                Console.WriteLine("Please input cellID");
                Int64 input = int.Parse(Console.ReadLine());
                JSONBack back = SearchNode(input);
                if(back!=null)
                    Console.WriteLine(back.ToString());
                else
                    Console.WriteLine("null!");
                Console.WriteLine();
            }
        }

        static unsafe void LoadTxs()
        {
            Console.WriteLine("LoadTxs begin..." + DateTime.Now);

            // #define max line (1 << 20)
            IntPtr memAddr = Marshal.AllocHGlobal(1 << 29); //512MB per memory part
            memAddrs.Add(memAddr);
            int offset = 0; //cur offset in mem part
            int count = 0;  //tx count

            //load staitc floder
            //test: D:\\Bit\\TSLBit\\Generator\\bin\\x64\\Debug\\test
            //full: D:\\Bit\\TSLBit\\Generator\\bin\x64\\Debug\\fullBlocks
            //remote: D:\\v-hulou\\fullBlocks
            DirectoryInfo dirInfo = new DirectoryInfo(@"D:\\v-hulou\\fullBlocks");
            foreach (FileInfo file in dirInfo.GetFiles("*"))
            {
                //read json line by line
                using (StreamReader reader = new StreamReader(file.FullName))
                {
                    string line;
                    while (null != (line = reader.ReadLine()))
                    {
                        //insert one node into memory
                        InsertNode(line, memAddr, ref offset);
                        count++;
                        //if mem parts is full, create new memory
                        if (offset > ((1 << 29) - (1 << 20)))
                        {
                            memCounts.Add(count);
                            memAddr = Marshal.AllocHGlobal(1 << 29);    //next memory part
                            memAddrs.Add(memAddr);
                            count = 0;
                            offset = 0;
                        }
                    }
                }
            }
            memCounts.Add(count);

            Console.WriteLine("LoadTxs end..." + DateTime.Now);
        }

        //insert one node into memory and b-tree
        static unsafe void InsertNode(string readLine, IntPtr txAddr, ref int offset)
        {
            //string to object
            JSONBack jsonBack = JSONBack.ConvertStringToJSONBack(readLine);

            //isnert cellID in b-tree
            Int64 cellID = jsonBack.CellID;
            BTree.BTInsert(ref hashTree, cellID, txAddr + offset);

            //insert node
            if (jsonBack.amount > 0)
            {
                byte[] memTx = jsonBack.ConvertTobytes();
                MemoryHelper.CopyToMem(memTx, 0, (byte*)txAddr.ToPointer(), offset, memTx.Length);
                offset += memTx.Length;
            }
        }

        // search node by key
        static JSONBack SearchNode(Int64 key)
        {
            Console.WriteLine("SearchNode begin..." + DateTime.Now);

            IntPtr node = new IntPtr();
            if (BTree.BTSearch(hashTree, key, ref node))
            {
                JSONBack result = MemoryHelper.ConvertMemToJSONBack(node);
                Console.WriteLine("SearchNode end..." + DateTime.Now);
                return result;
            }
            return null;
        }

        public delegate bool StatisticFun(IntPtr memAddr, Int64 a, Int64 b);
        static unsafe int Foreach(StatisticFun fun, Int64 amount, Int64 other)
        {
            Console.WriteLine("Foreach begin..." + DateTime.Now);

            int result = 0;
            for(int i = 0; i < memAddrs.Count; i++)
            {
                IntPtr memAddr = memAddrs[i];
                for(int j = 0; j < memCounts[i]; j++)
                {
                    int* len = (int*)(memAddr.ToPointer());
                    if(fun(memAddr, amount, other))
                    {
                        result++;
                    }
                    memAddr += *len;
                }
            }

            Console.WriteLine("Foreach end..." + DateTime.Now);
            return result;
        }
    }
}
