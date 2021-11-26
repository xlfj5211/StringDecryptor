﻿using System;
using System.Collections.Generic;
using Echo.Core.Code;
using Iced.Intel;

namespace Echo.Platforms.Iced
{
    /// <summary>
    /// Provides a description of the x86 instruction set architecture (ISA) that is modelled by Iced.   
    /// </summary>
    public class X86Architecture : IInstructionSetArchitecture<Instruction>
    {
        private readonly Formatter _formatter = new NasmFormatter();
        private readonly InstructionInfoFactory _infoFactory = new InstructionInfoFactory();
        private readonly IDictionary<Register, X86GeneralRegister> _gpr = new Dictionary<Register, X86GeneralRegister>();
        private readonly IDictionary<RflagsBits, X86FlagsRegister> _flags = new Dictionary<RflagsBits, X86FlagsRegister>();

        /// <summary>
        /// Creates a new instance of the <see cref="X86Architecture"/> class.
        /// </summary>
        public X86Architecture()
        {
            foreach (Register register in Enum.GetValues(typeof(Register)))
                _gpr[register] = new X86GeneralRegister(register);
            foreach (RflagsBits flag in Enum.GetValues(typeof(RflagsBits)))
                _flags[flag] = new X86FlagsRegister(flag);
        }

        /// <summary>
        /// Gets a register variable by its identifier.
        /// </summary>
        /// <param name="register">The register identifier.</param>
        /// <returns>The register variable.</returns>
        public X86GeneralRegister GetRegister(Register register) => _gpr[register];

        /// <summary>
        /// Gets a flag variable by its identifier.
        /// </summary>
        /// <param name="flag">The flag identifier.</param>
        /// <returns>The flag variable.</returns>
        public X86FlagsRegister GetFlag(RflagsBits flag) => _flags[flag];
        
        /// <inheritdoc />
        public long GetOffset(in Instruction instruction) => (long) instruction.IP;

        /// <inheritdoc />
        public int GetSize(in Instruction instruction) => instruction.Length;

        /// <inheritdoc />
        public InstructionFlowControl GetFlowControl(in Instruction instruction)
        {
            switch (instruction.FlowControl)
            {
                case FlowControl.UnconditionalBranch:
                case FlowControl.IndirectBranch:
                    return InstructionFlowControl.CanBranch;
                
                case FlowControl.ConditionalBranch:
                    return InstructionFlowControl.CanBranch | InstructionFlowControl.Fallthrough;
                
                case FlowControl.Return:
                    return InstructionFlowControl.IsTerminator;
                
                default:
                    return InstructionFlowControl.Fallthrough;
            }
        }

        /// <inheritdoc />
        public int GetStackPushCount(in Instruction instruction)
        {
            // TODO:
            return 0;
        }

        /// <inheritdoc />
        public int GetStackPopCount(in Instruction instruction)
        {
            // TODO:
            return 0;
        }

        /// <inheritdoc />
        public int GetReadVariablesCount(in Instruction instruction)
        {
            int count = 0;
            
            ref readonly var info = ref _infoFactory.GetInfo(instruction);
            
            // Check for any general purpose register reads.
            foreach (var use in info.GetUsedRegisters())
            {
                switch (use.Access)
                {
                    case OpAccess.Read:
                    case OpAccess.CondRead:
                    case OpAccess.ReadWrite:
                    case OpAccess.ReadCondWrite:
                        count++;
                        break;
                }
            }

            // Check for any flag register reads.
            var readFlags = instruction.RflagsRead;
            if (readFlags != RflagsBits.None)
            {
                for (int i = 1; i <= (int) RflagsBits.AC; i <<= 1)
                {
                    var flag = (RflagsBits) i;
                    if ((readFlags & flag) != 0)
                        count++;
                }
            }

            return count;
        }

        /// <inheritdoc />
        public int GetReadVariables(in Instruction instruction, Span<IVariable> variablesBuffer)
        {
            int count = 0;
            
            ref readonly var info = ref _infoFactory.GetInfo(instruction);
            
            // Check for any general purpose register reads.
            foreach (var use in info.GetUsedRegisters())
            {
                switch (use.Access)
                {
                    case OpAccess.Read:
                    case OpAccess.CondRead:
                    case OpAccess.ReadWrite:
                    case OpAccess.ReadCondWrite:
                        variablesBuffer[count] = _gpr[use.Register];
                        count++;
                        break;
                }
            }

            // Check for any flag register reads.
            var readFlags = instruction.RflagsRead;
            if (readFlags != RflagsBits.None)
            {
                for (int i = 1; i <= (int) RflagsBits.AC; i <<= 1)
                {
                    var flag = (RflagsBits) i;
                    if ((readFlags & flag) != 0)
                    {
                        variablesBuffer[count] = _flags[flag];
                        count++;
                    }
                }
            }

            return count;
        }

        /// <inheritdoc />
        public int GetWrittenVariablesCount(in Instruction instruction)
        {
            int count = 0;
            
            ref readonly var info = ref _infoFactory.GetInfo(instruction);
            
            // Check for any general purpose register writes.
            foreach (var use in info.GetUsedRegisters())
            {
                switch (use.Access)
                {
                    case OpAccess.Write:
                    case OpAccess.CondWrite:
                    case OpAccess.ReadWrite:
                    case OpAccess.ReadCondWrite:
                        count++;
                        break;
                }
            }

            // Check for any flag register writes.
            var modifiedFlags = instruction.RflagsModified;
            if (modifiedFlags != RflagsBits.None)
            {
                for (int i = 1; i <= (int) RflagsBits.AC; i <<= 1)
                {
                    var flag = (RflagsBits) i;
                    if ((modifiedFlags & flag) != 0)
                        count++;
                }
            }

            return count;
        }

        /// <inheritdoc />
        public int GetWrittenVariables(in Instruction instruction, Span<IVariable> variablesBuffer)
        {
            int count = 0;
            
            ref readonly var info = ref _infoFactory.GetInfo(instruction);
            
            // Check for any general purpose register writes.
            foreach (var use in info.GetUsedRegisters())
            {
                switch (use.Access)
                {
                    case OpAccess.Write:
                    case OpAccess.CondWrite:
                    case OpAccess.ReadWrite:
                    case OpAccess.ReadCondWrite:
                        variablesBuffer[count] = _gpr[use.Register];
                        count++;
                        break;
                }
            }

            // Check for any flag register writes.
            var modifiedFlags = instruction.RflagsModified;
            if (modifiedFlags != RflagsBits.None)
            {
                for (int i = 1; i <= (int) RflagsBits.AC; i <<= 1)
                {
                    var flag = (RflagsBits) i;
                    if ((modifiedFlags & flag) != 0)
                    {
                        variablesBuffer[count] = _flags[flag];
                        count++;
                    }
                }
            }

            return count;
        }
        
    }
}