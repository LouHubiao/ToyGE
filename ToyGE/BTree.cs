using System;
using System.Collections.Generic;

/*
* test 10000000 node insert, 13s:
    Console.WriteLine(DateTime.Now.ToString());
    BTreeNode b = BTree.BTCreate();
    for (int i = 0; i < 10000000; i += 2)
    {
        BTree.BTInsert(b, i, i);
    }
    Console.WriteLine(DateTime.Now.ToString());
*/

namespace ToyGE
{
    class BTreeNode
    {
        //MAX_KEYS = 1024;
        public int isLeaf;  //is this a leaf node?
        public List<Int64> keys = new List<Int64>();
        public List<IntPtr> values = new List<IntPtr>();
        public List<BTreeNode> kids = new List<BTreeNode>();
    }

    class BTree
    {
        const int MAX_KEYS = 1024;

        /// <summary>
        /// init BTree
        /// </summary>
        /// <returns>BTree root</returns>
        public static BTreeNode BTCreate()
        {
            BTreeNode b = new BTreeNode();
            b.isLeaf = 1;
            return b;
        }

        /// <summary>
        /// search in a node
        /// </summary>
        /// <param name="keys">node's keys</param>
        /// <param name="key">target key</param>
        /// <returns>pos in this node</returns>
        static int SearchKey(List<Int64> keys, Int64 key)
        {
            int lo = -1;
            int hi = keys.Count;
            int mid = -1;
            while (lo + 1 < hi)
            {
                mid = (lo + hi) / 2;
                if (keys[mid] == key)
                {
                    return mid;
                }
                else if (keys[mid] < key)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }
            return hi;
        }

        /// <summary>
        /// search key in full BTree's subtree
        /// </summary>
        /// <param name="b">root node of subtree</param>
        /// <param name="key">target key</param>
        /// <returns>the value of key</returns>
        public static bool BTSearch(BTreeNode b, Int64 key, ref IntPtr result)
        {
            int pos;

            // have to check for empty tree
            if (b.keys.Count == 0)
            {
                return false;
            }

            // look for smallest position that key fits below
            pos = SearchKey(b.keys, key);

            //return the value of key
            if (pos < b.keys.Count && b.keys[pos] == key)
            {
                result = b.values[pos];
                return true;
            }
            else
            {
                if (b.isLeaf == 0)
                {
                    //not found and not leaf, find kid
                    return BTSearch(b.kids[pos], key, ref result);
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// insert core function
        /// </summary>
        /// <param name="b">root node of subtree</param>
        /// <param name="key">insert key</param>
        /// <param name="value">insert value</param>
        /// <param name="medianKey">splie out the mid key</param>
        /// <param name="medianValue">splie out the mid value</param>
        /// <returns>if inserted return null, if splited return right child</returns>
        static BTreeNode BTInsertInternal(BTreeNode b, Int64 key, IntPtr value, ref Int64 medianKey, ref IntPtr medianValue)
        {
            int pos;    //insert pos
            int mid;    //splite pos
            Int64 midKey = -1;      //splited mid key
            IntPtr midValue = new IntPtr();    //splited mid value
            BTreeNode b2;

            pos = SearchKey(b.keys, key);

            if (pos < b.keys.Count && b.keys[pos] == key)
            {
                //find nothing to do
                return null;
            }

            if (b.isLeaf == 1)
            {
                /* everybody above pos moves up one space */
                b.keys.Insert(pos, key);
                b.values.Insert(pos, value);
            }
            else
            {
                /* insert in child */
                b2 = BTInsertInternal(b.kids[pos], key, value, ref midKey, ref midValue);

                /* maybe insert a new key in b */
                if (b2 != null)
                {
                    b.keys.Insert(pos, midKey);
                    b.values.Insert(pos, midValue);
                    b.kids.Insert(pos + 1, b2);
                }
            }

            if (b.keys.Count > MAX_KEYS)
            {
                mid = b.keys.Count / 2;

                medianKey = b.keys[mid];
                medianValue = b.values[mid];

                b2 = new BTreeNode();

                b2.isLeaf = b.isLeaf;

                //shallow copy but safe
                int movLen = b.keys.Count - mid - 1;
                b2.keys = b.keys.GetRange(mid + 1, movLen);
                b2.values = b.values.GetRange(mid + 1, movLen);
                b.keys.RemoveRange(mid, movLen+1);
                b.values.RemoveRange(mid, movLen+1);
                if (b.isLeaf == 0)
                {
                    //Console.WriteLine(b.kids.Count);
                    b2.kids = b.kids.GetRange(mid + 1, movLen + 1);
                    b.kids.RemoveRange(mid + 1, movLen + 1);
                }

                return b2;
            }

            return null;
        }

        //insert one node into BTree
        public static void BTInsert(ref BTreeNode b, Int64 key, IntPtr value)
        {
            BTreeNode b1;   //new left child
            BTreeNode b2;   //new right child
            Int64 medianKey = -1;
            IntPtr medianValue = new IntPtr();

            b2 = BTInsertInternal(b, key, value, ref medianKey, ref medianValue);

            // split
            if (b2 != null)
            {
                // root to be child
                b1 = b;

                // make root point to b1 and b2
                b = new BTreeNode();
                b.isLeaf = 0;
                b.keys.Add(medianKey);
                b.values.Add(medianValue);
                b.kids.Add(b1);
                b.kids.Add(b2);
            }
        }
    }
}
