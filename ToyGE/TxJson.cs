using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;

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
    public class JSONBack
    {
        [JsonProperty("CellID")]
        public Int64 CellID;

        [JsonProperty("hash")]
        public String hash;

        [JsonProperty("time")]
        public Int64 time;

        [JsonProperty("ins")]
        public List<Input> ins;

        [JsonProperty("outs")]
        public List<string> outs;

        [JsonProperty("amount")]
        public Int64 amount;

        //convert to jsonback from a string
        public static JSONBack ConvertStringToJSONBack(string jsonStr)
        {
            JSONBack JSONBack = new JSONBack();
            try
            {
                JSONBack = JsonConvert.DeserializeObject<JSONBack>(jsonStr);
            }
            catch (Exception e)
            {
                return null;
            }
            return JSONBack;
        }

        //convert jsonback to byte[]
        public byte[] ConvertTobytes()
        {
            byte[] result = new byte[0];
            int sumCount = 0;
            result = Combine(result, BitConverter.GetBytes(this.CellID));
            sumCount += 8;

            result = Combine(result, BitConverter.GetBytes(this.hash.Length));
            sumCount += 4;

            result = Combine(result, System.Text.Encoding.ASCII.GetBytes(this.hash));
            sumCount += this.hash.Length;

            result = Combine(result, BitConverter.GetBytes(this.time));
            sumCount += 8;

            result = Combine(result, BitConverter.GetBytes(this.ins.Count));
            sumCount += 4;
            foreach (Input _in in this.ins)
            {
                result = Combine(result, BitConverter.GetBytes(_in.addr.Length));
                sumCount += 4;
                result = Combine(result, System.Text.Encoding.ASCII.GetBytes(_in.addr));
                sumCount += _in.addr.Length;
                result = Combine(result, BitConverter.GetBytes(_in.tx_index));
                sumCount += 8;
            }

            result = Combine(result, BitConverter.GetBytes(this.outs.Count));
            sumCount += 4;
            foreach (string _out in this.outs)
            {
                result = Combine(result, BitConverter.GetBytes(_out.Length));
                sumCount += 4;
                result = Combine(result, System.Text.Encoding.ASCII.GetBytes(_out));
                sumCount += _out.Length;
            }

            result = Combine(result, BitConverter.GetBytes(this.amount));
            sumCount += 8;

            sumCount += 4;
            result = Combine(BitConverter.GetBytes(sumCount), result);

            return result;
        }

        static byte[] Combine(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }

        public override string ToString()
        {
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.Append("{\"CellID\":");
            strBuilder.Append(this.CellID);
            strBuilder.Append(",\"hash\":");
            strBuilder.Append(this.hash);
            strBuilder.Append(",\"time\":");
            strBuilder.Append(this.time);
            strBuilder.Append(",\"ins\":[");
            foreach (Input _in in this.ins)
            {
                strBuilder.Append("{\"addr\":");
                strBuilder.Append(this.time);
                strBuilder.Append(",\"tx_index\":");
                strBuilder.Append(this.time);
                strBuilder.Append("},");
            }
            strBuilder.Append("],\"outs\":[");
            foreach(string _out in this.outs)
            {
                strBuilder.Append(_out);
                strBuilder.Append(",");
            }
            strBuilder.Append("],\"amount\":");
            strBuilder.Append(this.amount);
            strBuilder.Append("}");

            return strBuilder.ToString();
        }
    }

    public class Input
    {
        public Input(string _addr, Int64 _tx_index)
        {
            this.addr = _addr;
            this.tx_index = _tx_index;
        }

        [JsonProperty("addr")]
        public string addr;

        [JsonProperty("tx_index")]
        public Int64 tx_index;
    }
}
