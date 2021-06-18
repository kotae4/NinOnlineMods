using System;
using NinMods.Hooking.NativeImports;
using System.Runtime.InteropServices;

namespace NinMods.Hooking.LowLevel
{
    // 8-bit relative jump.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct JMP_REL_SHORT
    {
        public byte opcode;      // EB xx: JMP +2+xx
        public byte operand;
    };

    // 32-bit direct relative jump/call.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct JMP_CALL_REL // size: 5 bytes
    {
        public byte opcode;      // E9/E8 xxxxxxxx: JMP/CALL +5+xxxxxxxx
        public uint operand;     // Relative destination address
    };

    // 64-bit indirect absolute jump.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct JMP_ABS // size: 14 bytes
    {
        public byte opcode0;     // FF25 00000000: JMP [+6]
        public byte opcode1;
        public uint dummy;
        public ulong address;     // Absolute destination address
    };

    // 64-bit indirect absolute call.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct CALL_ABS
    {
        public byte opcode0;     // FF15 00000002: CALL [+6]
        public byte opcode1;
        public uint dummy0;
        public byte dummy1;      // EB 08:         JMP +10
        public byte dummy2;
        public ulong address;     // Absolute destination address
    };

    // 32-bit direct relative conditional jumps.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct JCC_REL
    {
        public byte opcode0;     // 0F8* xxxxxxxx: J** +6+xxxxxxxx
        public byte opcode1;
        public uint operand;     // Relative destination address
    };

    // 64bit indirect absolute conditional jumps that x64 lacks.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct JCC_ABS
    {
        public byte opcode;      // 7* 0E:         J** +16
        public byte dummy0;
        public byte dummy1;      // FF25 00000000: JMP [+6]
        public byte dummy2;
        public uint dummy3;
        public ulong address;     // Absolute destination address
    };

#pragma pack(pop)

    public unsafe struct TRAMPOLINE_S
    {
        public IntPtr pTarget;         // [In] Address of the target function.
        public IntPtr pDetour;         // [In] Address of the detour function.
        public IntPtr pTrampoline;     // [In] Buffer address for the trampoline and relay function.

        // the trampoline is guaranteed(?) to be allocated within 512MB(?) of the target function on x64
        // this is within range of an e9 jmp
        // so we patch the target function to e9 jmp to the relay
        // then the relay ff 25 jmp's to the detour function
        // the relay must exist at the bottom of the trampoline
        public IntPtr pRelay;          // [Out] Address of the relay function.

        public bool patchAbove;      // [Out] Should use the hot patch area?
        public uint nIP;             // [Out] Number of the instruction boundaries.
        public fixed byte oldIPs[8];       // [Out] Instruction boundaries of the target function.
        public fixed byte newIPs[8];       // [Out] Instruction boundaries of the trampoline function.
    };

    public class Trampoline
    {
        const int JMP_REL_SIZE = 5;

        //-------------------------------------------------------------------------
        unsafe static bool IsCodePadding(byte* pInst, uint size)
        {
            uint i;
            unsafe
            {
                if (pInst[0] != 0x00 && pInst[0] != 0x90 && pInst[0] != 0xCC)
                    return false;

                for (i = 1; i < size; ++i)
                {
                    if (pInst[i] != pInst[0])
                        return false;
                }
            }
            return true;
        }

        //-------------------------------------------------------------------------
        public unsafe static bool IsExecutableAddress(IntPtr pAddress)
        {
            MEMORY_BASIC_INFORMATION mi;
            NativeImport.VirtualQuery(pAddress, out mi, (IntPtr)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)));

            return ((mi.State == PageState.MEM_COMMIT)
                && ((mi.Protect & ((uint)(AllocationProtect.PAGE_EXECUTE | AllocationProtect.PAGE_EXECUTE_READ | AllocationProtect.PAGE_EXECUTE_READWRITE | AllocationProtect.PAGE_EXECUTE_WRITECOPY))) == 1));
        }

        public static unsafe bool CreateTrampolineFunction64(TRAMPOLINE_S* ct)
        {
            // Maximum size of a trampoline function.
            const uint TRAMPOLINE_MAX_SIZE = (uint)(MemoryBuffer.MEMORY_SLOT_SIZE - 14);

            CALL_ABS call;
            call.opcode0 = 0xFF; // FF15 00000002: CALL [RIP+8]
            call.opcode1 = 0x15;
            call.dummy0 = 0x00000002;
            call.dummy1 = 0xEB; // EB 08:         JMP +10
            call.dummy2 = 0x08;
            call.address = 0x0; // Absolute destination address


            JMP_ABS jmp;
            jmp.opcode0 = 0xFF; // FF25 00000000: JMP [RIP+6]
            jmp.opcode1 = 0x25;
            jmp.dummy = 0x00000000;
            jmp.address = 0x0; // Absolute destination address


            JCC_ABS jcc;
            jcc.opcode = 0x70; // 7* 0E:         J** +16
            jcc.dummy0 = 0x0E;
            jcc.dummy1 = 0xFF; // FF25 00000000: JMP [RIP+6]
            jcc.dummy2 = 0x25;
            jcc.dummy3 = 0x00000000;
            jcc.address = 0x0; // Absolute destination address

            byte oldPos = 0;
            byte newPos = 0;
            ulong jmpDest = 0;     // Destination address of an internal jump.
            bool finished = false; // Is the function completed?

            byte[] instBuff = new byte[16];
            fixed (byte* instBuf = instBuff)
            {

                ct->patchAbove = false;
                ct->nIP = 0;

                do
                {
                    hde.hde64s hs;
                    uint copySize;
                    void* pCopySrc;
                    ulong pOldInst = (ulong)ct->pTarget + oldPos;
                    ulong pNewInst = (ulong)ct->pTrampoline + newPos;

                    copySize = hde.hde64.hde64_disasm((void*)pOldInst, &hs);
                    if ((hs.flags & hde.hde32.F_ERROR) != 0)
                        return false;

                    pCopySrc = (void*)pOldInst;
                    if (oldPos >= sizeof(JMP_CALL_REL))
                    {
                        // The trampoline function is long enough.
                        // Complete the function with the jump to the target function.
                        jmp.address = pOldInst;
                        pCopySrc = &jmp;
                        copySize = (uint)sizeof(JMP_ABS);

                        finished = true;
                    }
                    else if ((hs.modrm & 0xC7) == 0x05)
                    {
                        // Instructions using RIP relative addressing. (ModR/M = 00???101B)

                        // Modify the RIP relative address.
                        uint* pRelAddr;

                        // Avoid using memcpy to reduce the footprint.
                        // TRANSLATION NOTE:
                        // copies to managed memory from unmanaged memory
                        // unmanaged -> byte[]
                        // Marshal.Copy(pOldInst, instBuf, 0, copySize);
                        NativeImport.memcpy(instBuf, (byte*)pOldInst, (IntPtr)copySize);

                        pCopySrc = instBuf;

                        // Relative address is stored at (instruction length - immediate value length - 4).
                        pRelAddr = (uint*)(instBuf + hs.len - ((hs.flags & 0x3C) >> 2) - 4);
                        *pRelAddr
                            = (uint)((pOldInst + (ulong)hs.len + (ulong)(int)hs.disp.disp_32) - (pNewInst + hs.len));

                        // Complete the function if JMP (FF /4).
                        if (hs.opcode == 0xFF && hs.modrm_reg == 4)
                            finished = true;
                    }
                    else if (hs.opcode == 0xE8)
                    {
                        // Direct relative CALL
                        ulong dest = pOldInst + hs.len + (ulong)(int)hs.imm.imm_32;
                        call.address = dest;
                        pCopySrc = &call;
                        copySize = (uint)sizeof(CALL_ABS);
                    }
                    else if ((hs.opcode & 0xFD) == 0xE9)
                    {
                        // Direct relative JMP (EB or E9)
                        ulong dest = (ulong)pOldInst + hs.len;

                        if (hs.opcode == 0xEB) // isShort jmp
                            dest += (byte)hs.imm.imm8;
                        else
                            dest = dest + (ulong)(int)hs.imm.imm_32;

                        // Simply copy an internal jump.
                        if ((ulong)ct->pTarget <= dest
                            && dest < ((ulong)ct->pTarget + (ulong)sizeof(JMP_CALL_REL)))
                        {
                            if (jmpDest < dest)
                                jmpDest = dest;
                        }
                        else
                        {
                            jmp.address = dest;
                            pCopySrc = &jmp;
                            copySize = (uint)sizeof(JMP_ABS);

                            // Exit the function If it is not in the branch
                            finished = (pOldInst >= jmpDest);
                        }
                    }
                    else if ((hs.opcode & 0xF0) == 0x70
                        || (hs.opcode & 0xFC) == 0xE0
                        || (hs.opcode2 & 0xF0) == 0x80)
                    {
                        // Direct relative Jcc
                        ulong dest = pOldInst + hs.len;

                        if ((hs.opcode & 0xF0) == 0x70      // Jcc
                            || (hs.opcode & 0xFC) == 0xE0)  // LOOPNZ/LOOPZ/LOOP/JECXZ
                            dest += (byte)hs.imm.imm8;
                        else
                            dest += (ulong)(int)hs.imm.imm_32;

                        // Simply copy an internal jump.
                        if ((ulong)ct->pTarget <= dest
                            && dest < ((ulong)ct->pTarget + (ulong)sizeof(JMP_CALL_REL)))
                        {
                            if (jmpDest < dest)
                                jmpDest = dest;
                        }
                        else if ((hs.opcode & 0xFC) == 0xE0)
                        {
                            // LOOPNZ/LOOPZ/LOOP/JCXZ/JECXZ to the outside are not supported.
                            return false;
                        }
                        else
                        {
                            byte cond = (byte)(((hs.opcode != 0x0F ? hs.opcode : hs.opcode2) & 0x0F));
                            // Invert the condition in x64 mode to simplify the conditional jump logic.
                            jcc.opcode = (byte)(0x71 ^ cond);
                            jcc.address = dest;
                            pCopySrc = &jcc;
                            copySize = (uint)sizeof(JCC_ABS);
                        }
                    }
                    else if ((hs.opcode & 0xFE) == 0xC2)
                    {
                        // RET (C2 or C3)

                        // Complete the function if not in a branch.
                        finished = (pOldInst >= jmpDest);
                    }

                    // Can't alter the instruction length in a branch.
                    if (pOldInst < jmpDest && copySize != hs.len)
                        return false;

                    // Trampoline function is too large.
                    if ((newPos + copySize) > TRAMPOLINE_MAX_SIZE)
                        return false;

                    // Trampoline function has too many instructions.
                    // TRANSLATION NOTE:
                    // hardcoded 8 here
                    if (ct->nIP >= 8)
                        return false;

                    ct->oldIPs[ct->nIP] = oldPos;
                    ct->newIPs[ct->nIP] = newPos;
                    ct->nIP++;

                    // Avoid using memcpy to reduce the footprint.
                    // TRANSLATION NOTE:
                    // copies to unmanaged memory from unmanaged memory
                    // unmanaged -> unmanaged
                    // 
                    NativeImport.memcpy((byte*)ct->pTrampoline + newPos, pCopySrc, (IntPtr)copySize);

                    newPos += (byte)copySize;
                    oldPos += hs.len;
                }
                while (!finished);

                // Is there enough place for a long jump?
                if (oldPos < sizeof(JMP_CALL_REL)
                    && !IsCodePadding((byte*)ct->pTarget + oldPos, (uint)sizeof(JMP_CALL_REL) - oldPos))
                {
                    // Is there enough place for a short jump?
                    if (oldPos < sizeof(JMP_REL_SHORT)
                        && !IsCodePadding((byte*)ct->pTarget + oldPos, (uint)sizeof(JMP_REL_SHORT) - oldPos))
                    {
                        return false;
                    }

                    // Can we place the long jump above the function?
                    if (!IsExecutableAddress((IntPtr)((byte*)ct->pTarget - sizeof(JMP_CALL_REL))))
                        return false;

                    if (!IsCodePadding((byte*)ct->pTarget - sizeof(JMP_CALL_REL), (uint)sizeof(JMP_CALL_REL)))
                        return false;

                    ct->patchAbove = true;
                }
                // Create a relay function.
                jmp.address = (ulong)ct->pDetour;

                ct->pRelay = (IntPtr)((byte*)ct->pTrampoline + newPos);
                // TRANSLATION NOTE:
                // copies to unmanaged memory FROM managed struct
                // struct -> unmanaged
                NativeImport.memcpy((void*)ct->pRelay, &jmp, (IntPtr)sizeof(JMP_ABS));
            }

            return true;
        }

        //-------------------------------------------------------------------------
        public static unsafe bool CreateTrampolineFunction32(TRAMPOLINE_S* ct)
        {
            // Maximum size of a trampoline function.
            const uint TRAMPOLINE_MAX_SIZE = MemoryBuffer.MEMORY_SLOT_SIZE;

            JMP_CALL_REL call = new JMP_CALL_REL();
            call.opcode = 0xE8; // E8 xxxxxxxx: CALL +5+xxxxxxxx
            call.operand = 0x0; // Relative destination address


            JMP_CALL_REL jmp = new JMP_CALL_REL();
            jmp.opcode = (byte)0xE9; // E9 xxxxxxxx: JMP +5+xxxxxxxx
            jmp.operand = (uint)0x0; // Relative destination address


            JCC_REL jcc = new JCC_REL();
            jcc.opcode0 = 0x0F; // 0F8* xxxxxxxx: J** +6+xxxxxxxx
            jcc.opcode1 = 0x80;
            jcc.operand = 0x0; // Relative destination address

            byte oldPos = 0;
            byte newPos = 0;
            ulong jmpDest = 0;     // Destination address of an internal jump.
            bool finished = false; // Is the function completed?

            ct->patchAbove = false;
            ct->nIP = 0;

            do
            {
                hde.hde32s hs;
                uint copySize;
                void* pCopySrc;
                ulong pOldInst = (ulong)ct->pTarget + oldPos;
                ulong pNewInst = (ulong)ct->pTrampoline + newPos;

                copySize = hde.hde32.hde32_disasm((void*)pOldInst, &hs);
                if ((hs.flags & hde.hde32.F_ERROR) != 0)
                    return false;

                pCopySrc = (void*)pOldInst;
                if (oldPos >= sizeof(JMP_CALL_REL))
                {
                    // The trampoline function is long enough.
                    // Complete the function with the jump to the target function.
                    // TRANSLATION NOTE:
                    // sizeof only accepts Type as parameter, but since it's x86 (preprocessor conditional) the variable jmp will always be type JMP_CALL_REL
                    jmp.operand = (uint)(pOldInst - (pNewInst + (ulong)sizeof(JMP_CALL_REL)));
                    pCopySrc = &jmp;
                    copySize = (uint)sizeof(JMP_CALL_REL);

                    finished = true;
                }
                else if (hs.opcode == 0xE8)
                {
                    // Direct relative CALL
                    ulong dest = pOldInst + hs.len + (ulong)(int)hs.imm.imm_32;
                    call.operand = (uint)(dest - (pNewInst + (ulong)sizeof(JMP_CALL_REL)));
                    pCopySrc = &call;
                    copySize = (uint)sizeof(JMP_CALL_REL);
                }
                else if ((hs.opcode & 0xFD) == 0xE9)
                {
                    // Direct relative JMP (EB or E9)
                    ulong dest = (ulong)pOldInst + hs.len;

                    if (hs.opcode == 0xEB) // isShort jmp
                        dest += (byte)hs.imm.imm8;
                    else
                        dest = dest + (ulong)(int)hs.imm.imm_32;

                    // Simply copy an internal jump.
                    if ((ulong)ct->pTarget <= dest
                        && dest < ((ulong)ct->pTarget + (ulong)sizeof(JMP_CALL_REL)))
                    {
                        if (jmpDest < dest)
                            jmpDest = dest;
                    }
                    else
                    {
                        jmp.operand = (uint)(dest - (pNewInst + (ulong)sizeof(JMP_CALL_REL)));
                        pCopySrc = &jmp;
                        copySize = (uint)sizeof(JMP_CALL_REL);

                        // Exit the function If it is not in the branch
                        finished = (pOldInst >= jmpDest);
                    }
                }
                else if ((hs.opcode & 0xF0) == 0x70
                    || (hs.opcode & 0xFC) == 0xE0
                    || (hs.opcode2 & 0xF0) == 0x80)
                {
                    // Direct relative Jcc
                    ulong dest = pOldInst + hs.len;

                    if ((hs.opcode & 0xF0) == 0x70      // Jcc
                        || (hs.opcode & 0xFC) == 0xE0)  // LOOPNZ/LOOPZ/LOOP/JECXZ
                        dest += (byte)hs.imm.imm8;
                    else
                        dest += (ulong)(int)hs.imm.imm_32;

                    // Simply copy an internal jump.
                    if ((ulong)ct->pTarget <= dest
                        && dest < ((ulong)ct->pTarget + (ulong)sizeof(JMP_CALL_REL)))
                    {
                        if (jmpDest < dest)
                            jmpDest = dest;
                    }
                    else if ((hs.opcode & 0xFC) == 0xE0)
                    {
                        // LOOPNZ/LOOPZ/LOOP/JCXZ/JECXZ to the outside are not supported.
                        return false;
                    }
                    else
                    {
                        byte cond = (byte)(((hs.opcode != 0x0F ? hs.opcode : hs.opcode2) & 0x0F));
                        jcc.opcode1 = (byte)(0x80 | cond);
                        jcc.operand = (uint)(dest - (pNewInst + (ulong)sizeof(JCC_REL)));
                        pCopySrc = &jcc;
                        copySize = (uint)sizeof(JCC_REL);
                    }
                }
                else if ((hs.opcode & 0xFE) == 0xC2)
                {
                    // RET (C2 or C3)

                    // Complete the function if not in a branch.
                    finished = (pOldInst >= jmpDest);
                }

                // Can't alter the instruction length in a branch.
                if (pOldInst < jmpDest && copySize != hs.len)
                    return false;

                // Trampoline function is too large.
                if ((newPos + copySize) > TRAMPOLINE_MAX_SIZE)
                    return false;

                // Trampoline function has too many instructions.
                // TRANSLATION NOTE:
                // hardcoded 8 here
                if (ct->nIP >= 8)
                    return false;

                ct->oldIPs[ct->nIP] = oldPos;
                ct->newIPs[ct->nIP] = newPos;
                ct->nIP++;

                // Avoid using memcpy to reduce the footprint.
                // TRANSLATION NOTE:
                // copies to unmanaged memory from unmanaged memory
                // unmanaged -> unmanaged
                // 
                NativeImport.memcpy((byte*)ct->pTrampoline + newPos, pCopySrc, (IntPtr)copySize);

                newPos += (byte)copySize;
                oldPos += hs.len;
            }
            while (!finished);

            // Is there enough place for a long jump?
            if (oldPos < sizeof(JMP_CALL_REL)
                && !IsCodePadding((byte*)ct->pTarget + oldPos, (uint)sizeof(JMP_CALL_REL) - oldPos))
            {
                // Is there enough place for a short jump?
                if (oldPos < sizeof(JMP_REL_SHORT)
                    && !IsCodePadding((byte*)ct->pTarget + oldPos, (uint)sizeof(JMP_REL_SHORT) - oldPos))
                {
                    return false;
                }

                // Can we place the long jump above the function?
                if (!IsExecutableAddress((IntPtr)((byte*)ct->pTarget - sizeof(JMP_CALL_REL))))
                    return false;

                if (!IsCodePadding((byte*)ct->pTarget - sizeof(JMP_CALL_REL), (uint)sizeof(JMP_CALL_REL)))
                    return false;

                ct->patchAbove = true;
            }

            return true;
        }
    }
}
