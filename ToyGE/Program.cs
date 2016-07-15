using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ToyGE
{
    public class freeStruct
    {
        public IntPtr curAddr;
        public Int32 nextOffset;
        public Int32 freePre;

        public freeStruct(IntPtr _addr, Int32 _freeNext, Int32 _freePre)
        {
            this.addr = _addr;
            this.freeNext = _freeNext;
            this.freePre = _freePre;
        }
    }

    class Program
    {
        /* global variable */
        //hash index b-tree, contains cellID and logistic address
        static BTreeNode hashTree = Index.BTCreate();

        //memory parts begin address
        static List<IntPtr> blockAddrs = new List<IntPtr>();

        //memory parts node count
        static List<int> blockCounts = new List<int>();

        //free memory list for every block
        static List<IntPtr[]> freeAddrs = new List<IntPtr[]>();

        //free little memory list for every block, because some type cannot load freeNext(32) and freePre(32)
        static List<freeStruct[]> littleFreeAddrs = new List<freeStruct[]>();

        ////current last tail addr for every block
        //static List<IntPtr> tailAddrs = new List<IntPtr>();

        //1GB per memory block size
        static Int32 perBlockSize = 1 << 30;

        //gap for every string or list
        static Int16 gap = 0;

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
                        Int64 amount = Int64.Parse(inputs[2]);
                        int count = Foreach(Statistic.Amount_Statistic, amount, 0);
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
                    else if (inputs[2] == "hash")
                    {
                        Int64 key = Int64.Parse(inputs[1]);
                        string newHash = inputs[3];
                        UpdateHash(key, newHash);
                    }
                }
                Console.WriteLine();
            }
        }

        static unsafe void LoadTxs()
        {
            Console.WriteLine("LoadTxs begin..." + DateTime.Now);

            IntPtr memAddr = Marshal.AllocHGlobal(perBlockSize);
            blockAddrs.Add(memAddr);
            int cellCount = 0;  //cell count
            freeAddrs.Add(new IntPtr[1 << 16]);
            littleFreeAddrs.Add(new freeStruct[64]);
            IntPtr curAddr = memAddr;

            //load staitc floder
            //test: D:\\Bit\\TSLBit\\Generator\\bin\\x64\\Debug\\test
            //full: D:\\Bit\\TSLBit\\Generator\\bin\x64\\Debug\\fullBlocks
            //remote: D:\\v-hulou\\fullBlocks
            DirectoryInfo dirInfo = new DirectoryInfo(@"D:\\Bit\\TSLBit\\Generator\\bin\\x64\\Debug\\test");
            foreach (FileInfo file in dirInfo.GetFiles("block90000.txt"))
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

                        //if mem parts is full, create new memory
                        if (curAddr.ToInt64() - memAddr.ToInt64() > (perBlockSize - jsonBack.GetLength()))
                        {
                            preAddr = new IntPtr(0);
                            blockCounts.Add(cellCount);
                            memAddr = Marshal.AllocHGlobal(perBlockSize);
                            blockAddrs.Add(memAddr);
                            curAddr = memAddr;
                            cellCount = 0;
                            freeAddrs.Add(new IntPtr[1 << 16]);
                            littleFreeAddrs.Add(new freeStruct[64]);
                        }

                        //insert one node into memory
                        InsertNode(jsonBack, ref curAddr, ref preAddr, ref cellCount, gap);
                    }
                }
            }
            blockCounts.Add(cellCount);

            Console.WriteLine("LoadTxs end..." + DateTime.Now);
        }

        static unsafe IntPtr CreateMemoryBlock(ref IntPtr memAddr,ref IntPtr preAddr, int cellCount)
        {
            preAddr = new IntPtr(0);
            if (cellCount != 0)
                blockCounts.Add(cellCount);
            memAddr = Marshal.AllocHGlobal(perBlockSize);
            blockAddrs.Add(memAddr);
            cellCount = 0;
            freeAddrs.Add(new IntPtr[1 << 16]);
            littleFreeAddrs.Add(new freeStruct[64]);
            return memAddr;
        }

        //insert one node into memory and b-tree
        static void InsertNode(JSONBack jsonBack, ref IntPtr curAddr, ref IntPtr preAddr, ref int count, Int16 gap)
        {
            //isnert cellID in b-tree
            Int64 cellID = jsonBack.CellID;
            Index.BTInsert(ref hashTree, cellID, curAddr);

            //insert node
            TxHelper.InsertJsonBack(jsonBack, ref curAddr, ref preAddr, gap);
            count++;
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
                    JSONBack result = TxHelper.GetCell(node);
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
            for (int i = 0; i < blockAddrs.Count; i++)
            {
                IntPtr memAddr = blockAddrs[i];
                for (int j = 0; j < blockCounts[i]; j++)
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
                //get whichi block the nodeAddr in
                int blockIndex = GetBlockIndex(nodeAddr);
                //reduce count in block, for foreach
                blockCounts[blockIndex] -= 1;

                TxHelper.DeleteCell(nodeAddr, freeAddrs[blockIndex]);
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

        static void UpdateHash(Int64 key, string newHash)
        {
            Console.WriteLine("UpdateHash begin..." + DateTime.Now);

            IntPtr nodeAddr = new IntPtr();
            //search index
            if (Index.BTSearch(hashTree, key, ref nodeAddr))
            {
                int blockIndex = GetBlockIndex(nodeAddr);
                IntPtr[] freeAddr = freeAddrs[blockIndex];
                TxHelper.UpdateHash(nodeAddr, newHash, freeAddr);
                Console.WriteLine("UpdateHash end..." + DateTime.Now);
            }
        }

        //get the block index of cellAddr
        static int GetBlockIndex(IntPtr cellAddr)
        {
            for (int i = 0; i < blockAddrs.Count; i++)
            {
                if (cellAddr.ToInt64() >= blockAddrs[i].ToInt64())
                {
                    return i;
                }
            }
            return -1;
        }

        //get suit free space
        static IntPtr GetFreeSpace(int blockIndex, int size)
        {
            for (int i = size; i < (1 << 16); i++)
            {
                if (freeAddrs[blockIndex][i].ToInt64() != 0)
                {
                    IntPtr result = freeAddrs[blockIndex][i];
                    MemHelper.DeleteFree(result, freeAddrs[blockIndex]);
                    return result;
                }
            }
            return new IntPtr(0);
        }
    }
}
