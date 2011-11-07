﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Peach.Core
{
	public class Random
	{
		System.Random _random;
		protected int _seed;

		public Random()
		{
			_seed = (int)DateTime.Now.Ticks;
			_random = new System.Random(_seed);
		}

		public Random(int seed)
		{
			_seed = seed;
			_random = new System.Random(seed);
		}

		public int Seed
		{
			get { return _seed; }
			set { _seed = value; }
		}

		public int Next()
		{
			return _random.Next();
		}

		public int Next(int max)
		{
			return _random.Next(max);
		}

		public int Next(int min, int max)
		{
			return _random.Next(min, max);
		}

        public uint NextUInt32()
        {
            byte[] b = new byte[sizeof(UInt32)];
            _random.NextBytes(b);
            UInt32 num = BitConverter.ToUInt32(b, 0);
            return num;
        }

        public Int64 NextInt64()
        {
            byte[] b = new byte[sizeof(Int64)];
            _random.NextBytes(b);
            Int64 num = BitConverter.ToInt64(b, 0);
            return num;
        }

        public Int64 NextInt64(Int64 min, Int64 max)
        {
            Int64 range = Math.Abs(max - min);

            byte[] b;
            if (range < 0x10000)
            {
                if (range < 0x100)
                    b = new byte[1];
                else
                    b = new byte[sizeof(Int16)];
            }
            else
            {
                if (range < 0x100000000L)
                    b = new byte[sizeof(Int32)];
                else
                    b = new byte[sizeof(Int64)];
            }

            _random.NextBytes(b);
            Int64 num = BitConverter.ToInt64(b, 0);
            return num;
        }

        public UInt64 NextUInt64()
        {
            byte[] b = new byte[sizeof(UInt64)];
            _random.NextBytes(b);
            UInt64 num = BitConverter.ToUInt64(b, 0);
            return num;
        }

		public T Choice<T>(IEnumerable<T> list)
		{
			return ElementAt<T>(list, _random.Next(0, list.Count()));
		}

        public T[] Sample<T>(IEnumerable<T> items, int k)
        {
            List<T> ret = new List<T>();

            for (int i = 0; i < k; ++i)
                ret.Add(items.ElementAt(_random.Next(0, items.Count())));

            return ret.ToArray();
        }

        public void Shuffle<T>(IEnumerable<T> items)
        {
            if (items == null)
                return;

            List<T> ret = new List<T>();

            for (int i = 0; i < items.Count(); ++i)
                ret.Add(Choice(items));

            items = ret;
        }

		/// <summary>
		/// Work around for missing method in Mono
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		protected T ElementAt<T>(IEnumerable<T> list, int index)
		{
			var enumerator = list.GetEnumerator();

                        // <= because Current is set before the first element and must be called once to get first element.
			for (int cnt = 0; cnt <= index; cnt++) 
				enumerator.MoveNext();

			return enumerator.Current;
		}
	}
}

// end
