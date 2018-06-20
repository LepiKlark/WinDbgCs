﻿using CsDebugScript.Engine.Utility;
using static CxxDemangler.CxxDemangler;
using System.Collections.Generic;

namespace CsDebugScript.DwarfSymbolProvider
{
    /// <summary>
    /// Public symbol defined in image container (<see cref="IDwarfImage"/>).
    /// </summary>
    public class PublicSymbol
    {
        /// <summary>
        /// The demangled name cache
        /// </summary>
        private SimpleCache<string> demangledName;

        /// <summary>
        /// Initializes a new instance of the <see cref="PublicSymbol"/> class.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <param name="address">The address.</param>
        public PublicSymbol(string name, ulong address)
        {
            Name = name;
            Address = address;
            demangledName = SimpleCache.Create(() => Demangle(name));
        }

        /// <summary>
        /// Gets the public symbol name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the public symbol address.
        /// </summary>
        public ulong Address { get; private set; }

        /// <summary>
        /// Gets the demangled name using <see cref="CxxDemangler.CxxDemangler.Demangle(string, bool)"/>.
        /// </summary>
        public string DemangledName
        {
            get
            {
                return demangledName.Value;
            }
        }
    }

    /// <summary>
    /// Interface that defines image format that contains DWARF data.
    /// </summary>
    internal interface IDwarfImage
    {
        /// <summary>
        /// Gets the debug data.
        /// </summary>
        byte[] DebugData { get; }

        /// <summary>
        /// Gets the debug data description.
        /// </summary>
        byte[] DebugDataDescription { get; }

        /// <summary>
        /// Gets the debug data strings.
        /// </summary>
        byte[] DebugDataStrings { get; }

        /// <summary>
        /// Gets the debug line.
        /// </summary>
        byte[] DebugLine { get; }

        /// <summary>
        /// Gets the debug frame.
        /// </summary>
        byte[] DebugFrame { get; }

        /// <summary>
        /// Gets the exception handling frames used for unwinding (generated by usually GCC compiler).
        /// </summary>
        byte[] EhFrame { get; }

        /// <summary>
        /// Gets the code segment offset.
        /// </summary>
        ulong CodeSegmentOffset { get; }

        /// <summary>
        /// Gets the address of exception handling frames stream after loading into memory.
        /// </summary>
        ulong EhFrameAddress { get; }

        /// <summary>
        /// Gets the address of text section after loading into memory.
        /// </summary>
        ulong TextSectionAddress { get; }

        /// <summary>
        /// Gets the address of data section after loading into memory.
        /// </summary>
        ulong DataSectionAddress { get; }

        /// <summary>
        /// Gets the public symbols.
        /// </summary>
        IReadOnlyList<PublicSymbol> PublicSymbols { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="IDwarfImage"/> is 64 bit image.
        /// </summary>
        /// <value>
        ///   <c>true</c> if is 64 bit image; otherwise, <c>false</c>.
        /// </value>
        bool Is64bit { get; }
    }
}