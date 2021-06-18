using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NinMods.Hooking.LowLevel.hde
{


    [StructLayout(LayoutKind.Explicit)]
    public struct imm64
    {
        [FieldOffset(0)]
        public byte imm8;
        [FieldOffset(0)]
        public ushort imm16;
        [FieldOffset(0)]
        public uint imm_32;
        [FieldOffset(0)]
        public ulong imm_64;
    };

    public struct hde64s
    {
        public byte len;
        public byte p_rep;
        public byte p_lock;
        public byte p_seg;
        public byte p_66;
        public byte p_67;
        public byte rex;
        public byte rex_w;
        public byte rex_r;
        public byte rex_x;
        public byte rex_b;
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
        public imm64 imm;
        public disp32 disp;
        public uint flags;
    }

    public class hde64
    {
        public const uint F_MODRM = 0x00000001;
        public const uint F_SIB = 0x00000002;
        public const uint F_IMM8 = 0x00000004;
        public const uint F_IMM16 = 0x00000008;
        public const uint F_IMM32 = 0x00000010;
        public const uint F_IMM64 = 0x00000020;
        public const uint F_DISP8 = 0x00000040;
        public const uint F_DISP16 = 0x00000080;
        public const uint F_DISP32 = 0x00000100;
        public const uint F_RELATIVE = 0x00000200;
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
        public const uint F_PREFIX_REX = 0x40000000;
        public const uint F_PREFIX_ANY = 0x7f000000;

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

        public unsafe static uint hde64_disasm(void* code, hde64s* hs)
        {
            byte x, c = 0, cflags, opcode, pref = 0;
            byte* p = (byte*)code;
            byte m_mod, m_reg, m_rm, disp_size = 0;
            byte op64 = 0;

            // TRANSLATION NOTE: 
            // hde64s struct is already zero'd

            table64 derpTable = new hde.table64();
            fixed (byte* htFixed = derpTable.hde64_table)
            {
                byte* ht = htFixed;

                for (x = 16; (x != 0); x--)
                {
                    switch (c = *p++)
                    {
                        case 0xf3:
                            hs->p_rep = c;
                            pref |= table64.PRE_F3;
                            break;
                        case 0xf2:
                            hs->p_rep = c;
                            pref |= table64.PRE_F2;
                            break;
                        case 0xf0:
                            hs->p_lock = c;
                            pref |= table64.PRE_LOCK;
                            break;
                        case 0x26:
                        case 0x2e:
                        case 0x36:
                        case 0x3e:
                        case 0x64:
                        case 0x65:
                            hs->p_seg = c;
                            pref |= table64.PRE_SEG;
                            break;
                        case 0x66:
                            hs->p_66 = c;
                            pref |= table64.PRE_66;
                            break;
                        case 0x67:
                            hs->p_67 = c;
                            pref |= table64.PRE_67;
                            break;
                        default:
                            goto pref_done;
                    }
                }
                pref_done:

                hs->flags = (uint)pref << 23;

                if ((pref) == 0)
                    pref |= table64.PRE_NONE;

                if ((c & 0xf0) == 0x40)
                {
                    hs->flags |= F_PREFIX_REX;
                    if (((hs->rex_w = (byte)((c & 0xf) >> 3)) != 0) && (*p & 0xf8) == 0xb8)
                        op64++;
                    hs->rex_r = (byte)((c & 7) >> 2);
                    hs->rex_x = (byte)((c & 3) >> 1);
                    hs->rex_b = (byte)(c & 1);
                    if (((c = *p++) & 0xf0) == 0x40)
                    {
                        opcode = c;
                        cflags = table64.C_ERROR;
                        goto error_opcode;
                    }
                }

                if ((hs->opcode = c) == 0x0f)
                {
                    hs->opcode2 = c = *p++;
                    ht += table64.DELTA_OPCODES;
                }
                else if (c >= 0xa0 && c <= 0xa3)
                {
                    op64++;
                    if ((pref & table64.PRE_67) != 0)
                        pref |= table64.PRE_66;
                    else
                        pref = (byte)((int)pref & (int)~table64.PRE_66);
                }

                opcode = c;
                cflags = ht[ht[opcode / 4] + (opcode % 4)];

                // TRANSLATION NOTE:
                // have to move this label outside so the goto above is in the same scope
                // and set cflags to C_ERROR so the if is followed
                error_opcode:
                if (cflags == table64.C_ERROR)
                {
                    //error_opcode:
                    hs->flags |= F_ERROR | F_ERROR_OPCODE;
                    cflags = 0;
                    if ((opcode & -3) == 0x24)
                        cflags++;
                }

                x = 0;
                if ((cflags & table64.C_GROUP) != 0)
                {
                    ushort t;
                    t = *(ushort*)(ht + (cflags & 0x7f));
                    cflags = (byte)t;
                    x = (byte)(t >> 8);
                }

                if ((hs->opcode2) != 0)
                {
                    ht = htFixed + table64.DELTA_PREFIXES;
                    if ((ht[ht[opcode / 4] + (opcode % 4)] & pref) != 0)
                        hs->flags |= F_ERROR | F_ERROR_OPCODE;
                }

                if ((cflags & table64.C_MODRM) != 0)
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
                            ht = htFixed + table64.DELTA_FPU_MODRM + t * 8;
                            t = (byte)(ht[m_reg] << m_rm);
                        }
                        else
                        {
                            ht = htFixed + table64.DELTA_FPU_REG;
                            t = (byte)(ht[t] << m_reg);
                        }
                        if ((t & 0x80) != 0)
                            hs->flags |= F_ERROR | F_ERROR_OPCODE;
                    }

                    if ((pref & table64.PRE_LOCK) != 0)
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
                                ht = htFixed + table64.DELTA_OP2_LOCK_OK;
                                table_end = ht + table64.DELTA_OP_ONLY_MEM - table64.DELTA_OP2_LOCK_OK;
                            }
                            else
                            {
                                ht = htFixed + table64.DELTA_OP_LOCK_OK;
                                table_end = ht + table64.DELTA_OP2_LOCK_OK - table64.DELTA_OP_LOCK_OK;
                                op = (byte)(op & -2);
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
                            ht = htFixed + table64.DELTA_OP2_ONLY_MEM;
                            table_end = ht + derpTable.hde64_table.Length - table64.DELTA_OP2_ONLY_MEM;
                        }
                        else
                        {
                            ht = htFixed + table64.DELTA_OP_ONLY_MEM;
                            table_end = ht + table64.DELTA_OP2_ONLY_MEM - table64.DELTA_OP_ONLY_MEM;
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
                                if ((pref & (table64.PRE_NONE | table64.PRE_66)) != 0)
                                    goto error_operand;
                                break;
                            case 0xd6:
                                if ((pref & (table64.PRE_F2 | table64.PRE_F3)) != 0)
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
                            cflags |= table64.C_IMM8;
                        else if (opcode == 0xf7)
                            cflags |= table64.C_IMM_P66;
                    }

                    switch (m_mod)
                    {
                        case 0:
                            if ((pref & table64.PRE_67) != 0)
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
                            if ((pref & table64.PRE_67) == 0)
                                disp_size <<= 1;
                            break;
                    }

                    if (m_mod != 3 && m_rm == 4)
                    {
                        hs->flags |= F_SIB;
                        p++;
                        hs->sib = c;
                        hs->sib_scale = (byte)(c >> 6);
                        hs->sib_index = (byte)((c & 0x3f) >> 3);
                        if ((hs->sib_base = (byte)(c & 7)) == 5 && ((m_mod & 1) == 0))
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
                else if ((pref & table64.PRE_LOCK) != 0)
                    hs->flags |= F_ERROR | F_ERROR_LOCK;

                if ((cflags & table64.C_IMM_P66) != 0)
                {
                    if ((cflags & table64.C_REL32) != 0)
                    {
                        if ((pref & table64.PRE_66) != 0)
                        {
                            hs->flags |= F_IMM16 | F_RELATIVE;
                            hs->imm.imm16 = *(ushort*)p;
                            p += 2;
                            goto disasm_done;
                        }
                        hs->flags |= F_IMM32 | F_RELATIVE;
                        hs->imm.imm_32 = *(uint*)p;
                        p += 4;
                        goto disasm_done;
                    }
                    if ((op64) != 0)
                    {
                        hs->flags |= F_IMM64;
                        hs->imm.imm_64 = *(ulong*)p;
                        p += 8;
                    }
                    else if ((pref & table64.PRE_66) == 0)
                    {
                        hs->flags |= F_IMM32;
                        hs->imm.imm_32 = *(uint*)p;
                        p += 4;
                    }
                    else
                    {
                        hs->flags |= F_IMM16;
                        hs->imm.imm16 = *(ushort*)p;
                        p += 2;
                        goto imm16_ok;
                    }
                }


                if ((cflags & table64.C_IMM16) != 0)
                {
                    //imm16_ok:
                    hs->flags |= F_IMM16;
                    hs->imm.imm16 = *(ushort*)p;
                    p += 2;
                }
                imm16_ok:
                if ((cflags & table64.C_IMM8) != 0)
                {
                    hs->flags |= F_IMM8;
                    hs->imm.imm8 = *p++;
                }

                if ((cflags & table64.C_REL32) != 0)
                {
                    //rel32_ok:
                    hs->flags |= F_IMM32 | F_RELATIVE;
                    hs->imm.imm_32 = *(uint*)p;
                    p += 4;
                }
                else if ((cflags & table64.C_REL8) != 0)
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
            }

            return (uint)hs->len;
        }
    }
}