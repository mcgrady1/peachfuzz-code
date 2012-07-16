﻿
//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Peach.Core.Agent.Monitors
{
	[Monitor("PageHeap")]
	[Parameter("Executable", typeof(string), "Name of executable to enable (NO PATH)", true)]
	[Parameter("WinDbgPath", typeof(string), "Path to WinDbg install.  If not provided we will try and locate it.", false)]
	public class PageHeap : Monitor
	{
		string _executable = null;
		string _winDbgPath = null;
		string _gflags = "gflags.exe";
		string _gflagsArgsEnable = "/p /enable \"{0}\" /full";
		string _gflagsArgsDisable = "/p /disable \"{0}\"";

		public PageHeap(string name, Dictionary<string, Variant> args)
			: base(name, args)
		{
			_executable = (string)args["Executable"];
			
			if(args.ContainsKey("WinDbgPath"))
				_winDbgPath = (string)args["WinDbgPath"];
			else
			{
				_winDbgPath = FindWinDbg();
				if (_winDbgPath == null)
					throw new PeachException("Error, unable to locate WinDbg, please specify using 'WinDbgPath' parameter.");
			}
		}

		protected string FindWinDbg()
		{
			// Lets try a few common places before failing.
			List<string> pgPaths = new List<string>();
			pgPaths.Add(@"c:\");
			pgPaths.Add(Environment.GetEnvironmentVariable("SystemDrive"));
			pgPaths.Add(Environment.GetEnvironmentVariable("ProgramFiles"));

			if (Environment.GetEnvironmentVariable("ProgramW6432") != null)
				pgPaths.Add(Environment.GetEnvironmentVariable("ProgramW6432"));
			if (Environment.GetEnvironmentVariable("ProgramFiles") != null)
				pgPaths.Add(Environment.GetEnvironmentVariable("ProgramFiles"));
			if (Environment.GetEnvironmentVariable("ProgramFiles(x86)") != null)
				pgPaths.Add(Environment.GetEnvironmentVariable("ProgramFiles(x86)"));

			List<string> dbgPaths = new List<string>();
			dbgPaths.Add("Debuggers");
			dbgPaths.Add("Debugger");
			dbgPaths.Add("Debugging Tools for Windows");
			dbgPaths.Add("Debugging Tools for Windows (x64)");
			dbgPaths.Add("Debugging Tools for Windows (x86)");

			foreach (string path in pgPaths)
			{
				foreach (string dpath in dbgPaths)
				{
					string pathCheck = Path.Combine(path, dpath);
					if (Directory.Exists(pathCheck) && File.Exists(Path.Combine(pathCheck, "dbgeng.dll")))
					{
						//verify x64 vs x86

						var type = GetDllMachineType(Path.Combine(pathCheck, "dbgeng.dll"));
						if (Environment.Is64BitProcess && type != MachineType.IMAGE_FILE_MACHINE_AMD64)
							continue;
						else if (!Environment.Is64BitProcess && type != MachineType.IMAGE_FILE_MACHINE_I386)
							continue;

						return pathCheck;
					}
				}
			}

			return null;
		}

		protected void Enable()
		{
			var startInfo = new ProcessStartInfo();
			startInfo.FileName = Path.Combine(_winDbgPath, _gflags);
			startInfo.Arguments = string.Format(_gflagsArgsEnable, _executable);
			startInfo.CreateNoWindow = true;
			startInfo.UseShellExecute = false;
			System.Diagnostics.Process.Start(startInfo).WaitForExit();
		}

		protected void Disable()
		{
			var startInfo = new ProcessStartInfo();
			startInfo.FileName = Path.Combine(_winDbgPath, _gflags);
			startInfo.Arguments = string.Format(_gflagsArgsDisable, _executable);
			startInfo.CreateNoWindow = true;
			startInfo.UseShellExecute = false;
			System.Diagnostics.Process.Start(startInfo).WaitForExit();
		}

		public override void StopMonitor()
		{
			Disable();
		}

		public override void SessionStarting()
		{
			Enable();
		}

		public override void SessionFinished()
		{
			Disable();
		}

		public override bool DetectedFault()
		{
			return false;
		}


		public override void IterationStarting(int iterationCount, bool isReproduction)
		{
		}

		public override bool IterationFinished()
		{
			return false;
		}

		public override void GetMonitorData(System.Collections.Hashtable data)
		{
		}

		public override bool MustStop()
		{
			return false;
		}

		public override Variant Message(string name, Variant data)
		{
			return null;
		}
		public static MachineType GetDllMachineType(string dllPath)
		{
			//see http://www.microsoft.com/whdc/system/platform/firmware/PECOFF.mspx
			//offset to PE header is always at 0x3C
			//PE header starts with "PE\0\0" =  0x50 0x45 0x00 0x00
			//followed by 2-byte machine type field (see document above for enum)
			FileStream fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read);
			BinaryReader br = new BinaryReader(fs);
			fs.Seek(0x3c, SeekOrigin.Begin);
			Int32 peOffset = br.ReadInt32();
			fs.Seek(peOffset, SeekOrigin.Begin);
			UInt32 peHead = br.ReadUInt32();
			if (peHead != 0x00004550) // "PE\0\0", little-endian
				throw new Exception("Can't find PE header");
			MachineType machineType = (MachineType)br.ReadUInt16();
			br.Close();
			fs.Close();
			return machineType;
		}

		public enum MachineType : ushort
		{
			IMAGE_FILE_MACHINE_UNKNOWN = 0x0,
			IMAGE_FILE_MACHINE_AM33 = 0x1d3,
			IMAGE_FILE_MACHINE_AMD64 = 0x8664,
			IMAGE_FILE_MACHINE_ARM = 0x1c0,
			IMAGE_FILE_MACHINE_EBC = 0xebc,
			IMAGE_FILE_MACHINE_I386 = 0x14c,
			IMAGE_FILE_MACHINE_IA64 = 0x200,
			IMAGE_FILE_MACHINE_M32R = 0x9041,
			IMAGE_FILE_MACHINE_MIPS16 = 0x266,
			IMAGE_FILE_MACHINE_MIPSFPU = 0x366,
			IMAGE_FILE_MACHINE_MIPSFPU16 = 0x466,
			IMAGE_FILE_MACHINE_POWERPC = 0x1f0,
			IMAGE_FILE_MACHINE_POWERPCFP = 0x1f1,
			IMAGE_FILE_MACHINE_R4000 = 0x166,
			IMAGE_FILE_MACHINE_SH3 = 0x1a2,
			IMAGE_FILE_MACHINE_SH3DSP = 0x1a3,
			IMAGE_FILE_MACHINE_SH4 = 0x1a6,
			IMAGE_FILE_MACHINE_SH5 = 0x1a8,
			IMAGE_FILE_MACHINE_THUMB = 0x1c2,
			IMAGE_FILE_MACHINE_WCEMIPSV2 = 0x169,
		}
		// returns true if the dll is 64-bit, false if 32-bit, and null if unknown
		public static bool? UnmanagedDllIs64Bit(string dllPath)
		{
			switch (GetDllMachineType(dllPath))
			{
				case MachineType.IMAGE_FILE_MACHINE_AMD64:
				case MachineType.IMAGE_FILE_MACHINE_IA64:
					return true;
				case MachineType.IMAGE_FILE_MACHINE_I386:
					return false;
				default:
					return null;
			}
		}

	}
}
