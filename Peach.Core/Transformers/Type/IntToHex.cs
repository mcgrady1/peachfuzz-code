﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Compression;
using System.IO;
using Peach.Core.Dom;
using Peach.Core.IO;

namespace Peach.Core.Transformers.Type
{
    [TransformerAttribute("IntToHex", "Transforms an integer into hex.")]
    [TransformerAttribute("type.IntToHex", "Transforms an integer into hex.")]
    public class IntToHex : Transformer
    {
        public IntToHex(Dictionary<string,Variant> args) : base(args)
		{
		}

		protected override BitStream internalEncode(BitStream data)
		{
            int dataAsInt = BitConverter.ToInt32(data.Value, 0);
            string dataAsHex = dataAsInt.ToString("X");
            return new BitStream(ASCIIEncoding.ASCII.GetBytes(dataAsHex));
		}

		protected override BitStream internalDecode(BitStream data)
		{
            throw new NotImplementedException();
		}
    }
}

// end