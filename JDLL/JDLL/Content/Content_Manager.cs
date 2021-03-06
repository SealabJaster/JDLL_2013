﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace JDLL.Content
{

    /// <summary>
    /// Class for handling binary files that contain multitudes of customly processed data
    /// </summary>
    public class Content_Manager
    {
        Dictionary<String, IContentProcessor> Processors = new Dictionary<String, IContentProcessor>();
        List<String> Names = new List<String>();

        /// <summary>
        /// Path to the file used
        /// </summary>
        public String Filename { get; private set; }


        /// <summary>
        /// Opcode written at the start of an entry
        /// </summary>
        public static ushort op_Start = 20;

        /// <summary>
        /// Opcode written ad the end of an entry
        /// </summary>
        [Obsolete("Causes CharBuffer crashes and is currently not used, pointless right now", true)]
        public static ushort op_End = 21;

        /// <summary>
        /// Class for handling binary files that contain multitudes of customly processed data
        /// </summary>
        /// <param name="filename">Path to the file to use</param>
        public Content_Manager(String filename, bool nonContentFile = false)
        {
            this.Filename = filename;

            if (!nonContentFile)
            {
                this.FillNames();
            }

            this.RegisterProcessor(new StringProcessor());          // string
            this.RegisterProcessor(new StringArrayProcessor());     // stringArray
            this.RegisterProcessor(new Int32Processor());           // int32
            this.RegisterProcessor(new Int32ArrayProcessor());      // int32Array
            this.RegisterProcessor(new BoolProcessor());            // bool
            this.RegisterProcessor(new FileProcessor());            // file
            this.RegisterProcessor(new ByteArrayProcessor());       // byteArray
        }

        /// <summary>
        /// Registers an IContentProcessor to use, is accessed in the "Write" class by it's "TypeName"
        /// </summary>
        /// <param name="processor">Processor to register</param>
        public void RegisterProcessor(IContentProcessor processor)
        {
            this.Processors.Add(processor.TypeName(), processor);
        }

        /// <summary>
        /// Checks for "entryName" and returns whether it exists or not
        /// </summary>
        /// <param name="entryName">The entry to check</param>
        /// <returns>Whether entryName exists or not</returns>
        public bool DoesEntryExist(String entryName)
        {
            return this.Names.Contains(entryName);
        }

        /// <summary>
        /// Deletes the file
        /// </summary>
        public void DeleteFile()
        {
            File.Delete(this.Filename);
        }

        /// <summary>
        /// Get's all of the TypeNames of the registered processors
        /// </summary>
        /// <returns>All of the TypeNames of the registered processors</returns>
        public String[] GetProcessorTypeNames()
        {
            return this.Processors.Keys.ToArray();
        }

        /// <summary>
        /// Gets all of the Processors currently registered
        /// </summary>
        /// <returns>An array of all the registered processors</returns>
        public IContentProcessor[] GetAllProcessors()
        {
            return this.Processors.Values.ToArray();
        }

        /// <summary>
        /// Gets the processor with the TypeName of "typeName"
        /// </summary>
        /// <param name="typeName">The processor with the TypeName of "typeName"</param>
        /// <returns></returns>
        public IContentProcessor GetProcessorByTypeName(String typeName)
        {
            return this.Processors[typeName];
        }

        /// <summary>
        /// Checks to see if the processor with the TypeName "processorTypeName" exists. True if it does. False if it doesn't
        /// </summary>
        /// <param name="processorTypeName">TypeName of the processor</param>
        /// <returns>True if it does exist. False if it doesn't</returns>
        public bool DoesProcessorExist(String processorTypeName)
        {
            return this.Processors.ContainsKey(processorTypeName);
        }

        /// <summary>
        /// Unregisters the processor with the TypeName "processorTypeName"
        /// </summary>
        /// <param name="processorTypeName">TypeName of the processor to remove</param>
        public void UnregisterProcessor(String processorTypeName)
        {
            this.Processors.Remove(processorTypeName);
        }

        /// <summary>
        /// Reads the file for the entry with the name "name", calls the processor it was processed with to parse the data and then returns that data. Throws EntryNotFoundException if an entry with "name" doesn't exist
        /// </summary>
        /// <param name="name">Name of the entry to read</param>
        /// <returns>Parsed data from the processor the entry is associated to</returns>
        public object Read(String name)
        {
            using (FileStream fs = new FileStream(this.Filename, FileMode.OpenOrCreate))
            {
                using (BinaryReader br = new BinaryReader(fs, Encoding.UTF8))
                {
                    try
                    {
                        while (br.ReadByte() != -20)
                        {
                            br.BaseStream.Position -= 1;

                            if (br.ReadUInt16() == Content_Manager.op_Start)
                            {
                                if (Helper.ReadString(br).Equals(name))
                                {
                                    String Process = Helper.ReadString(br);

                                    return this.Processors[Process].Import(br);
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }

            throw new EntryNotFoundException(name);
        }

        /// <summary>
        /// Calls the Read method but casts it to T
        /// </summary>
        /// <typeparam name="T">Type to cast it to</typeparam>
        /// <param name="name">Name of the entry to read</param>
        /// <returns>Casted data from Read()</returns>
        /// <seealso cref="Read(String name)"/>
        public T Read<T>(String name)
        {
            return (T)this.Read(name);
        }

        /// <summary>
        /// Writes an entry into the file, "data" get's passed into the processor of "processorTypeName" to be written. Throws ProcessorNotRegisteredException if a processor that's TypeName is "processorTypeName" hasn't been registered. Will return prematurly if the entry with "name" already exists
        /// </summary>
        /// <param name="data">Data to pass into the processor</param>
        /// <param name="name">Name of the entry</param>
        /// <param name="processorTypeName">TypeName of the processor to use</param>
        public void Write(object data, String name, String processorTypeName)
        {
            if (this.Names.Contains(name))
            {
                // TODO: Add exception to throw
                return;
            }

            if (!this.Processors.ContainsKey(processorTypeName))
            {
                throw new ProcessorNotRegisteredException("Processor '" + processorTypeName + "' hasn't been registered!");
            }

            this.Names.Add(name);

            using (FileStream fs = new FileStream(this.Filename, FileMode.Append))
            {
                using (BinaryWriter bw = new BinaryWriter(fs, Encoding.UTF8))
                {
                    bw.Write(Content_Manager.op_Start);
                    Helper.WriteString(name, bw);
                    Helper.WriteString((this.Processors[processorTypeName].TypeName()), bw);

                    this.Processors[processorTypeName].Export(bw, data);

                    // TODO: Fix Char Buffer error related to this opcode writing out null values
                    // bw.Write(Content_Manager.op_End);
                }
            }
        }

        private void FillNames()
        {
            using (FileStream fs = new FileStream(this.Filename, FileMode.OpenOrCreate))
            {
                using (BinaryReader br = new BinaryReader(fs, Encoding.UTF8))
                {
                    ushort Num = 0;

                    try
                    {
                        while (br.ReadByte() != -20)
                        {
                            br.BaseStream.Position -= 1;

                            try
                            {
                                Num = br.ReadUInt16();
                            }
                            catch
                            {
                                return;
                            }

                            if (Num == Content_Manager.op_Start)
                            {
                                this.Names.Add(Helper.ReadString(br));
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        #region Jaster Program Customs

        #region JIAsm

        public void JIAsmWrite(object data, String name, String processorTypeName, BinaryWriter bw)
        {
            if (!this.Processors.ContainsKey(processorTypeName))
            {
                throw new ProcessorNotRegisteredException("Processor '" + processorTypeName + "' hasn't been registered!");
            }

            bw.Write(Content_Manager.op_Start);
            Helper.WriteString(name, bw);
            Helper.WriteString((this.Processors[processorTypeName].TypeName()), bw);

            this.Processors[processorTypeName].Export(bw, data);
        }

        public T JIAsmRead<T>(String name, BinaryReader br)
        {
            if (br.ReadUInt16() == Content_Manager.op_Start)
            {
                if (Helper.ReadString(br).Equals(name))
                {
                    String Process = Helper.ReadString(br);

                    return (T)this.Processors[Process].Import(br);
                }
            }

            throw new EntryNotFoundException(name);
        }

        #endregion

        #endregion

        ~Content_Manager()
        {
            //File.WriteAllLines("Debug.txt", Names.ToArray());

            this.Filename = "";
            this.Names = null;
            this.Processors = null;
        }
    }
}
