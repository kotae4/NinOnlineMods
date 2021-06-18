using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NinMods.Hooking.LowLevel.hde
{

    [StructLayout(LayoutKind.Explicit)]
    public struct imm32
    {
        [FieldOffset(0)]
        public byte imm8;
        [FieldOffset(0)]
        public ushort imm16;
        [FieldOffset(0)]
        public uint imm_32;
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct disp32
    {
        [FieldOffset(0)]
        public byte disp8;
        [FieldOffset(0)]
        public ushort disp16;
        [FieldOffset(0)]
        public uint disp_32;
    };

    public struct hde32s
    {
        public byte len;
        public byte p_rep;
        public byte p_lock;
        public byte p_seg;
        public byte p_66;
        public byte p_67;
        public byte opcode;
        public byte opcode2;
        public byte modrm;
        public byte modrm_mod;
        public byte modrm_reg;
        public byte modrm_rm;
        public byte sib;
        public byte sib_scale;
        public byte sib_index;
        public byte sib_base;
        public imm32 imm;
        public disp32 disp;
        public uint flags;
    };

    public class hde32
    {
        public const uint F_MODRM = 0x00000001;
        public const uint F_SIB = 0x00000002;
        public const uint F_IMM8 = 0x00000004;
        public const uint F_IMM16 = 0x00000008;
        public const uint F_IMM32 = 0x00000010;
        public const uint F_DISP8 = 0x00000020;
        public const uint F_DISP16 = 0x00000040;
        public const uint F_DISP32 = 0x00000080;
        public const uint F_RELATIVE = 0x00000100;
        public const uint F_2IMM16 = 0x00000800;
        public const uint F_ERROR = 0x00001000;
        public const uint F_ERROR_OPCODE = 0x00002000;
        public const uint F_ERROR_LENGTH = 0x00004000;
        public const uint F_ERROR_LOCK = 0x00008000;
        public const uint F_ERROR_OPERAND = 0x00010000;
        public const uint F_PREFIX_REPNZ = 0x01000000;
        public const uint F_PREFIX_REPX = 0x02000000;
        public const uint F_PREFIX_REP = 0x03000000;
        public const uint F_PREFIX_66 = 0x04000000;
        public const uint F_PREFIX_67 = 0x08000000;
        public const uint F_PREFIX_LOCK = 0x10000000;
        public const uint F_PREFIX_SEG = 0x20000000;
        public const uint F_PREFIX_ANY = 0x3f000000;

        public const byte PREFIX_SEGMENT_CS = 0x2e;
        public const byte PREFIX_SEGMENT_SS = 0x36;
        public const byte PREFIX_SEGMENT_DS = 0x3e;
        public const byte PREFIX_SEGMENT_ES = 0x26;
        public const byte PREFIX_SEGMENT_FS = 0x64;
        public const byte PREFIX_SEGMENT_GS = 0x65;
        public const byte PREFIX_LOCK = 0xf0;
        public const byte PREFIX_REPNZ = 0xf2;
        public const byte PREFIX_REPX = 0xf3;
        public const byte PREFIX_OPERAND_SIZE = 0x66;
        public const byte PREFIX_ADDRESS_SIZE = 0x67;


        public unsafe static uint hde32_disasm(void* code, hde32s* hs)
        {
            // TRANSLATION NOTE:
            // hde32s struct is already initialized to 0

            // TRANSLATION NOTE:
            // have to assign c outside of the for loop otherwise compiler complains about using an unassigned variable later
            // (even though the loop is guaranteed to execute at least once)
            byte x, c = 0, cflags, opcode, pref = 0;
            byte* p = (byte*)code;
            byte m_mod, m_reg, m_rm, disp_size = 0;
            table32 derpTable = new hde.table32();
            fixed (byte* htFixed = derpTable.hde32_table)
            {
                byte* ht = htFixed;
                for (x = 16; x != 0; x--)
                {
                    c = *p++;
                    switch (c)
                    {
                        case 0xf3:
                            hs->p_rep = c;
                            pref |= table32.PRE_F3;
                            break;
                        case 0xf2:
                            hs->p_rep = c;
                            pref |= table32.PRE_F2;
                            break;
                        case 0xf0:
                            hs->p_lock = c;
                            pref |= table32.PRE_LOCK;
                            break;
                        case 0x26:
                        case 0x2e:
                        case 0x36:
                        case 0x3e:
                        case 0x64:
                        case 0x65:
                            hs->p_seg = c;
                            pref |= table32.PRE_SEG;
                            break;
                        case 0x66:
                            hs->p_66 = c;
                            pref |= table32.PRE_66;
                            break;
                        case 0x67:
                            hs->p_67 = c;
                            pref |= table32.PRE_67;
                            break;
                        default:
                            goto pref_done;
                    }
                }
                pref_done:

                hs->flags = (uint)pref << 23;

                if (pref == 0)
                    pref |= table32.PRE_NONE;

                if ((hs->opcode = c) == 0x0f)
                {
                    hs->opcode2 = c = *p++;
                    ht += table32.DELTA_OPCODES;
                }
                else if (c >= 0xa0 && c <= 0xa3)
                {
                    if ((pref & table32.PRE_67) != 0)
                    {
                        pref |= table32.PRE_66;
                    }
                    else
                    {
                        // TRANSLATION NOTE:
                        // bitwise operations fucking suck in C#
                        // i assume -9 needs to be converted to int32 for the & operation (otherwise (int32)-9 is just (byte)247)
                        // but assigning the result of the operation to a byte leads to the same behaviour regardless
                        // so what's the best way of doing this? cast both sides to int then downcast to byte?
                        pref = (byte)((int)pref & (int)~table32.PRE_66);
                        // unchecked exists, too. probably not safe.
                        //pref &= unchecked((byte)~table32.PRE_66);
                    }
                }

                opcode = c;
                cflags = ht[ht[opcode / 4] + (opcode % 4)];

                if (cflags == table32.C_ERROR)
                {
                    hs->flags |= F_ERROR | F_ERROR_OPCODE;
                    cflags = 0;
                    if ((opcode & -3) == 0x24)
                        cflags++;
                }

                x = 0;
                if ((cflags & table32.C_GROUP) != 0)
                {
                    ushort t;
                    t = *(ushort*)(ht + (cflags & 0x7f));
                    cflags = (byte)t;
                    x = (byte)(t >> 8);
                }

                if ((hs->opcode2) != 0)
                {
                    ht = htFixed + table32.DELTA_PREFIXES;
                    if ((ht[ht[opcode / 4] + (opcode % 4)] & pref) != 0)
                        hs->flags |= F_ERROR | F_ERROR_OPCODE;
                }

                if ((cflags & table32.C_MODRM) != 0)
                {
                    hs->flags |= F_MODRM;
                    hs->modrm = c = *p++;
                    hs->modrm_mod = m_mod = (byte)(c >> 6);
                    hs->modrm_rm = m_rm = (byte)(c & 7);
                    hs->modrm_reg = m_reg = (byte)((c & 0x3f) >> 3);

                    if ((x != 0) && (((x << m_reg) & 0x80) != 0))
                        hs->flags |= F_ERROR | F_ERROR_OPCODE;

                    if ((hs->opcode2 == 0) && opcode >= 0xd9 && opcode <= 0xdf)
                    {
                        byte t = (byte)(opcode - 0xd9);
                        if (m_mod == 3)
                        {
                            ht = htFixed + table32.DELTA_FPU_MODRM + t * 8;
                            t = (byte)(ht[m_reg] << m_rm);
                        }
                        else
                        {
                            ht = htFixed + table32.DELTA_FPU_REG;
                            t = (byte)(ht[t] << m_reg);
                        }
                        if ((t & 0x80) != 0)
                            hs->flags |= F_ERROR | F_ERROR_OPCODE;
                    }

                    if ((pref & table32.PRE_LOCK) != 0)
                    {
                        if (m_mod == 3)
                        {
                            hs->flags |= F_ERROR | F_ERROR_LOCK;
                        }
                        else
                        {
                            byte* table_end;
                            byte op = opcode;
                            if ((hs->opcode2) != 0)
                            {
                                ht = htFixed + table32.DELTA_OP2_LOCK_OK;
                                table_end = ht + table32.DELTA_OP_ONLY_MEM - table32.DELTA_OP2_LOCK_OK;
                            }
                            else
                            {
                                ht = htFixed + table32.DELTA_OP_LOCK_OK;
                                table_end = ht + table32.DELTA_OP2_LOCK_OK - table32.DELTA_OP_LOCK_OK;
                                op = (byte)((int)op & (int)-2);
                            }
                            for (; ht != table_end; ht++)
                                if (*ht++ == op)
                                {
                                    if (((*ht << m_reg) & 0x80) == 0)
                                        goto no_lock_error;
                                    else
                                        break;
                                }
                            hs->flags |= F_ERROR | F_ERROR_LOCK;
                            no_lock_error:
                            ;
                        }
                    }

                    if ((hs->opcode2) != 0)
                    {
                        switch (opcode)
                        {
                            case 0x20:
                            case 0x22:
                                m_mod = 3;
                                if (m_reg > 4 || m_reg == 1)
                                    goto error_operand;
                                else
                                    goto no_error_operand;
                            case 0x21:
                            case 0x23:
                                m_mod = 3;
                                if (m_reg == 4 || m_reg == 5)
                                    goto error_operand;
                                else
                                    goto no_error_operand;
                        }
                    }
                    else
                    {
                        switch (opcode)
                        {
                            case 0x8c:
                                if (m_reg > 5)
                                    goto error_operand;
                                else
                                    goto no_error_operand;
                            case 0x8e:
                                if (m_reg == 1 || m_reg > 5)
                                    goto error_operand;
                                else
                                    goto no_error_operand;
                        }
                    }

                    if (m_mod == 3)
                    {
                        byte* table_end;
                        if ((hs->opcode2) != 0)
                        {
                            ht = htFixed + table32.DELTA_OP2_ONLY_MEM;
                            // TRANSLATION NOTE:
                            // .Length is used here instead of sizeof. since it's a byte array the result should be the same.
                            table_end = ht + derpTable.hde32_table.Length - table32.DELTA_OP2_ONLY_MEM;
                        }
                        else
                        {
                            ht = htFixed + table32.DELTA_OP_ONLY_MEM;
                            table_end = ht + table32.DELTA_OP2_ONLY_MEM - table32.DELTA_OP_ONLY_MEM;
                        }
                        for (; ht != table_end; ht += 2)
                            if (*ht++ == opcode)
                            {
                                if (((*ht++ & pref) != 0) && (((*ht << m_reg) & 0x80) == 0))
                                    goto error_operand;
                                else
                                    break;
                            }
                        goto no_error_operand;
                    }
                    else if ((hs->opcode2) != 0)
                    {
                        switch (opcode)
                        {
                            case 0x50:
                            case 0xd7:
                            case 0xf7:
                                if ((pref & (table32.PRE_NONE | table32.PRE_66)) != 0)
                                    goto error_operand;
                                break;
                            case 0xd6:
                                if ((pref & (table32.PRE_F2 | table32.PRE_F3)) != 0)
                                    goto error_operand;
                                break;
                            case 0xc5:
                                goto error_operand;
                        }
                        goto no_error_operand;
                    }
                    else
                        goto no_error_operand;

                    error_operand:
                    hs->flags |= F_ERROR | F_ERROR_OPERAND;
                    no_error_operand:

                    c = *p++;
                    if (m_reg <= 1)
                    {
                        if (opcode == 0xf6)
                            cflags |= table32.C_IMM8;
                        else if (opcode == 0xf7)
                            cflags |= table32.C_IMM_P66;
                    }

                    switch (m_mod)
                    {
                        case 0:
                            if ((pref & table32.PRE_67) != 0)
                            {
                                if (m_rm == 6)
                                    disp_size = 2;
                            }
                            else
                                if (m_rm == 5)
                                disp_size = 4;
                            break;
                        case 1:
                            disp_size = 1;
                            break;
                        case 2:
                            disp_size = 2;
                            if ((pref & table32.PRE_67) == 0)
                                disp_size <<= 1;
                            // TRANSLATION NOTE:
                            // C# requires a break statement here
                            break;
                    }

                    if (m_mod != 3 && m_rm == 4 && ((pref & table32.PRE_67) == 0))
                    {
                        hs->flags |= F_SIB;
                        p++;
                        hs->sib = c;
                        hs->sib_scale = (byte)(c >> 6);
                        hs->sib_index = (byte)((c & 0x3f) >> 3);
                        if (((hs->sib_base = (byte)(c & 7)) == 5) && ((byte)(m_mod & 1) == 0))
                            disp_size = 4;
                    }

                    p--;
                    switch (disp_size)
                    {
                        case 1:
                            hs->flags |= F_DISP8;
                            hs->disp.disp8 = *p;
                            break;
                        case 2:
                            hs->flags |= F_DISP16;
                            hs->disp.disp16 = *(ushort*)p;
                            break;
                        case 4:
                            hs->flags |= F_DISP32;
                            hs->disp.disp_32 = *(uint*)p;
                            break;
                    }
                    p += disp_size;
                }
                else if ((pref & table32.PRE_LOCK) != 0)
                    hs->flags |= F_ERROR | F_ERROR_LOCK;

                if ((cflags & table32.C_IMM_P66) != 0)
                {
                    if ((cflags & table32.C_REL32) != 0)
                    {
                        if ((pref & table32.PRE_66) != 0)
                        {
                            hs->flags |= F_IMM16 | F_RELATIVE;
                            hs->imm.imm16 = *(ushort*)p;
                            p += 2;
                            goto disasm_done;
                        }
                        // TRANSLATION NOTE:
                        // goto statements require the label to be in the same scope as the statement itself
                        // so i just copied the original code and then transfer to disasm_done
                        // since rel32_ok is out of scope
                        hs->flags |= F_IMM32 | F_RELATIVE;
                        hs->imm.imm_32 = *(uint*)p;
                        p += 4;
                        goto disasm_done;
                    }
                    if ((pref & table32.PRE_66) != 0)
                    {
                        hs->flags |= F_IMM16;
                        hs->imm.imm16 = *(ushort*)p;
                        p += 2;
                    }
                    else
                    {
                        hs->flags |= F_IMM32;
                        hs->imm.imm_32 = *(uint*)p;
                        p += 4;
                    }
                }

                if ((cflags & table32.C_IMM16) != 0)
                {
                    if ((hs->flags & F_IMM32) != 0)
                    {
                        hs->flags |= F_IMM16;
                        hs->disp.disp16 = *(ushort*)p;
                    }
                    else if ((hs->flags & F_IMM16) != 0)
                    {
                        hs->flags |= F_2IMM16;
                        hs->disp.disp16 = *(ushort*)p;
                    }
                    else
                    {
                        hs->flags |= F_IMM16;
                        hs->imm.imm16 = *(ushort*)p;
                    }
                    p += 2;
                }
                if ((cflags & table32.C_IMM8) != 0)
                {
                    hs->flags |= F_IMM8;
                    hs->imm.imm8 = *p++;
                }

                if ((cflags & table32.C_REL32) != 0)
                {
                    hs->flags |= F_IMM32 | F_RELATIVE;
                    hs->imm.imm_32 = *(uint*)p;
                    p += 4;
                }
                else if ((cflags & table32.C_REL8) != 0)
                {
                    hs->flags |= F_IMM8 | F_RELATIVE;
                    hs->imm.imm8 = *p++;
                }

                disasm_done:

                if ((hs->len = (byte)(p - (byte*)code)) > 15)
                {
                    hs->flags |= F_ERROR | F_ERROR_LENGTH;
                    hs->len = 15;
                }

                return (uint)hs->len;
            }
        }
    }
}