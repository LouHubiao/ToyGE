using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToyGE
{
    class Statistic
    {
        public static unsafe bool Amount_Statistic(IntPtr curAddr, Int64 stdAmount, Int64 noUse)
        {
            int* length = (int*)(curAddr.ToPointer());
            Int64* amount = (Int64*)(curAddr.ToInt64() + *length - sizeof(Int64));
            if (*amount >= stdAmount)
            {
                return true;
            }
            return false;
        }

        public static unsafe bool Count_Statistic(IntPtr curAddr, Int64 noUse1, Int64 noUse2)
        {

            return true;
        }
    }
}
