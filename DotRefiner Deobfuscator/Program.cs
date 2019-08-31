using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace DotRefiner_Deobfuscator {
    class Program {
        public static ModuleDefMD module;
        public static storage storage;
        public static Dictionary<string, string> decshit = new Dictionary<string, string>();
        public static string ResourceName;

        static void Main(string[] args) {
            module = ModuleDefMD.Load(args[0]);
            var cctor = module.GlobalType.FindOrCreateStaticConstructor();
            cctor = (MethodDef)cctor.Body.Instructions[0].Operand; //This is the string initilize method!

            if (StringDecMethod(cctor)) {
                GetStorageNames();
                ResourceName = cctor.Body.Instructions[1].Operand.ToString();
                storage = new storage(decryptor.DecryptIt(((EmbeddedResource)module.Resources.Find(ResourceName)).CreateReader().AsStream())); //Resource name is set to '0' by default!
                for (var i = 4; i < cctor.Body.Instructions.Count; i++) {
                    if (cctor.Body.Instructions[i].OpCode == OpCodes.Dup &&
                       cctor.Body.Instructions[i + 1].OpCode == OpCodes.Call &&
                       cctor.Body.Instructions[i + 2].OpCode == OpCodes.Stsfld) {

                        string type = cctor.Body.Instructions[i + 1].Operand.ToString();
                        type = new string(type.Remove(type.Length - 2).Reverse().Take(1).ToArray());
                        decshit.Add(cctor.Body.Instructions[i + 2].Operand.ToString(), DecValue(type));
                    }
                }
            }
            else {
                Console.WriteLine("Could not find string dec method!");
                Thread.Sleep(3000);
                Environment.Exit(0);
            }

            foreach (var type in module.GetTypes()) {
                if (!type.HasMethods)
                    continue;

                foreach (var method in type.Methods) {
                    if (!method.HasBody)
                        continue;

                    for (var i = 0; i < method.Body.Instructions.Count; i++) {
                        if (method.Body.Instructions[i].OpCode == OpCodes.Ldsfld &&
                            decshit.TryGetValue(method.Body.Instructions[i].Operand.ToString(), out string value)) {

                            var opcode = GetOpCode(method.Body.Instructions[i].Operand.ToString());
                            if (opcode == OpCodes.Nop) {
                                Console.WriteLine("ERROR");
                                Console.WriteLine(method.Body.Instructions[i].Operand.ToString()); //If this says anything, then u need to update this tool
                                Console.ReadLine();
                            }

                            method.Body.Instructions[i].OpCode = opcode;
                            switch (opcode.ToString()) {
                                case "ldstr":    method.Body.Instructions[i].Operand = value; break;
                                case "ldc.i4":   method.Body.Instructions[i].Operand = int.Parse(value); break;
                                case "ldc.i4.s": method.Body.Instructions[i].Operand = sbyte.Parse(value); break;
                                case "ldc.i8":   method.Body.Instructions[i].Operand = long.Parse(value); break;
                                case "ldc.r4":   method.Body.Instructions[i].Operand = float.Parse(value); break;
                                case "ldc.r8":   method.Body.Instructions[i].Operand = double.Parse(value); break;
                            }
                        }
                    }
                }
            }

            CleanUp();
            Console.WriteLine("Saving methods...");
            Save(args[0], module);
            Console.WriteLine("Saved!");
        }

        static void CleanUp() {
            module.GlobalType.FindOrCreateStaticConstructor().Body.Instructions.RemoveAt(0);
            module.Resources.Remove(module.Resources.Where(x => x.Name == ResourceName).FirstOrDefault());
        }

        static void GetStorageNames() {
            foreach (var type in module.GetTypes()) {
                if (!type.HasMethods)
                    continue;

                foreach (var method in type.Methods) {
                    if (!method.HasBody)
                        continue;

                    for (var i = 0; i < method.Body.Instructions.Count; i++) {
                        if (method.Body.Instructions[i].OpCode == OpCodes.Ldarg_0 &&
                            method.Body.Instructions[i + 1].OpCode == OpCodes.Ldfld &&
                            method.Body.Instructions[i + 1].Operand.ToString().Contains("System.IO.BinaryReader") &&
                            method.Body.Instructions[i + 2].OpCode == OpCodes.Callvirt &&
                            method.Body.Instructions[i + 2].Operand.ToString().Contains("System.IO.BinaryReader::Read") &&
                            method.Body.Instructions[i + 3].OpCode == OpCodes.Ret &&
                            method.Body.Instructions.Count == 4) {

                            switch (method.Body.Instructions[i + 2].Operand.ToString()) {
                                case "System.String System.IO.BinaryReader::ReadString()": NameStorage.ReadString = method.Name; break;
                                case "System.SByte System.IO.BinaryReader::ReadSByte()":   NameStorage.ReadSByte  = method.Name; break;
                                case "System.Int32 System.IO.BinaryReader::ReadInt32()":   NameStorage.ReadInt    = method.Name; break;
                                case "System.Int64 System.IO.BinaryReader::ReadInt64()":   NameStorage.ReadLong   = method.Name; break;
                                case "System.Single System.IO.BinaryReader::ReadSingle()": NameStorage.ReadFloat  = method.Name; break;
                                case "System.Double System.IO.BinaryReader::ReadDouble()": NameStorage.ReadDouble = method.Name; break;
                            }
                        }
                    }
                }
            }
        }
        
        static OpCode GetOpCode(string type) {
            if (type.Contains("String")) return OpCodes.Ldstr;
            else if (type.Contains("Int32")) return OpCodes.Ldc_I4; //Int
            else if (type.Contains("SByte")) return OpCodes.Ldc_I4_S;
            else if (type.Contains("Int64")) return OpCodes.Ldc_I8; //Long
            else if (type.Contains("Single")) return OpCodes.Ldc_R4; //Float
            else if (type.Contains("Double")) return OpCodes.Ldc_R8;
            return OpCodes.Nop;
        }

        static string DecValue(string type) {
            if (type == NameStorage.ReadString)      return storage.GetString();
            else if (type == NameStorage.ReadSByte)  return storage.GetSByte().ToString();
            else if (type == NameStorage.ReadInt)    return storage.GetInt().ToString();
            else if (type == NameStorage.ReadLong)   return storage.GetLong().ToString();
            else if (type == NameStorage.ReadFloat)  return storage.GetFloat().ToString();
            else if (type == NameStorage.ReadDouble) return storage.GetDouble().ToString();
            return string.Empty;
        }
        
        static bool StringDecMethod(MethodDef method) => (
            method.Body.Instructions[0].OpCode == OpCodes.Call &&
            method.Body.Instructions[0].Operand.ToString().Contains("System.Reflection.Assembly::GetExecutingAssembly") &&
            method.Body.Instructions[1].OpCode == OpCodes.Ldstr &&
            method.Body.Instructions[2].OpCode == OpCodes.Callvirt &&
            method.Body.Instructions[2].Operand.ToString().Contains("System.Reflection.Assembly::GetManifestResourceStream") &&
            method.Body.Instructions[3].OpCode == OpCodes.Call &&
            method.Body.Instructions[4].OpCode == OpCodes.Newobj
            );
        
        static void Save(string location, ModuleDefMD module) {

            Console.WriteLine("saving module!");

            var Writer = new NativeModuleWriterOptions(module, true) {
                KeepExtraPEData = true,
                KeepWin32Resources = true,
                Logger = DummyLogger.NoThrowInstance //prevents errors from being thrown
            };

            Writer.MetadataOptions.Flags = MetadataFlags.PreserveAll | MetadataFlags.KeepOldMaxStack;
            module.NativeWrite($"{Path.GetFileNameWithoutExtension(location)}-Dec{Path.GetExtension(location)}", Writer);

        }
    }
}

/* Made by DarkObb */