using System;
using NinMods.Hooking.NativeImports;
using System.Runtime.InteropServices;

namespace NinMods.Hooking.LowLevel
{
    public class MemoryBuffer
    {
        // Size of each memory slot.
        public const int MEMORY_SLOT_SIZE = 256;

        // Size of each memory block. (= page size of VirtualAlloc)
        public const uint MEMORY_BLOCK_SIZE = 0x1000;

        // Max range for seeking a memory block. (= 1024MB)
        public const ulong MAX_MEMORY_RANGE = 0x40000000;

        [StructLayout(LayoutKind.Explicit)]
        unsafe struct MEMORY_SLOT_S
        {
            // POTENTIAL BUG:
            // C# unions are kinda weird. but it's necessary to have a union here, otherwise the slots will exceed their block.
            [FieldOffset(0)]
            public MEMORY_SLOT_S* pNext;
            [FieldOffset(0)]
            public fixed byte buffer[MEMORY_SLOT_SIZE];
        }

        unsafe struct MEMORY_BLOCK_S
        {
            public MEMORY_BLOCK_S* pNext;
            public MEMORY_SLOT_S* pFree;
            public uint usedCount;
        }

        unsafe static MEMORY_BLOCK_S* g_pMemoryBlocks;

        //-------------------------------------------------------------------------
        public unsafe static void UninitializeBuffer()
        {
            MEMORY_BLOCK_S* pBlock = g_pMemoryBlocks;
            g_pMemoryBlocks = null;

            while (pBlock != null)
            {
                MEMORY_BLOCK_S* pNext = pBlock->pNext;
                NativeImport.VirtualFree((IntPtr)pBlock, IntPtr.Zero, FreeType.Release);
                pBlock = pNext;
            }
        }

        public unsafe static void* AllocateBuffer(void* pOrigin)
        {
            MEMORY_SLOT_S* pSlot;
            MEMORY_BLOCK_S* pBlock;
            if (IntPtr.Size == 8)
                pBlock = GetMemoryBlock64(pOrigin);
            else
                pBlock = GetMemoryBlock32(pOrigin);
            if (pBlock == null)
                return null;
            // Remove an unused slot from the list.
            pSlot = pBlock->pFree;
            pBlock->pFree = pSlot->pNext;
            pBlock->usedCount++;
            // so there's a bug with minhook here edit: no there isn't i'm just dumb. keeping this here anyway.
            // i'm not sure exactly what's going on, but it's something to do with allocation granularity vs page size
            // i think you're supposed to reserve in multiples of allocation granularity
            // and commit in multiples of page size
            // each chunk of allocation granularity is subdivided into pages of page size?
            // anyway, because we reserve && commit in 4k chunks, we don't actually get the full 4k we ask for: we get 4096 - 24.
            // and because of that, the last slot (first returned) isn't actually sizeof(MEMORY_SLOT_S), it's sizeof(MEMORY_SLOT_S) - 24.
            // i wonder where the 24 is coming from though.
            // and shouldn't this bug only exist when page size isn't a factor of granularity? it is, though.
            // granularity is 65536 and page size is 4096
            // min address is 0x10000 and max address is 7ffeffff resulting in an address space of 7FFDFFFF bytes
            // 7FFDFFFF (2147352575d) / 65536 = 32,765.99999
            // ah i think that's the issue, then.
            // but still, this should only be an issue if you're at 7FFE0000, right?
            // everything before then should be aligned to multiples of 0x10000
            //
            // okay maybe it's not an issue. cheat engine and reclass just don't display the memory properly.
            // cheat engine's memory browser thing displays it properly though. the disassembler window does not.
            // weird...
            for (int index = 0; index < sizeof(MEMORY_SLOT_S); index++)
            {
                (*pSlot).buffer[index] = 0x90;
            }
            return pSlot;
        }

        unsafe static MEMORY_BLOCK_S* GetMemoryBlock64(void* pOrigin)
        {
            MEMORY_BLOCK_S* pBlock;
            ulong minAddr;
            ulong maxAddr;

            SYSTEM_INFO si;
            NativeImport.GetSystemInfo(out si);
            minAddr = (ulong)si.MinimumApplicationAddress;
            maxAddr = (ulong)si.MaximumApplicationAddress;

            // pOrigin ± 512MB
            if ((ulong)pOrigin > MAX_MEMORY_RANGE && minAddr < (ulong)pOrigin - MAX_MEMORY_RANGE)
                minAddr = (ulong)pOrigin - MAX_MEMORY_RANGE;

            if (maxAddr > (ulong)pOrigin + MAX_MEMORY_RANGE)
                maxAddr = (ulong)pOrigin + MAX_MEMORY_RANGE;

            // Make room for MEMORY_BLOCK_SIZE bytes.
            maxAddr -= MEMORY_BLOCK_SIZE - 1;
            // Look the registered blocks for a reachable one.
            for (pBlock = g_pMemoryBlocks; pBlock != null; pBlock = pBlock->pNext)
            {
                // Ignore the blocks too far.
                if ((ulong)pBlock < minAddr || (ulong)pBlock >= maxAddr)
                    continue;
                // The block has at least one unused slot.
                if (pBlock->pFree != null)
                    return pBlock;
            }

            // Alloc a new block above if not found.
            {
                void* pAlloc = pOrigin;
                while ((ulong)pAlloc >= minAddr)
                {
                    pAlloc = FindPrevFreeRegion(pAlloc, (void*)minAddr, si.AllocationGranularity);
                    if (pAlloc == null)
                        break;

                    pBlock = (MEMORY_BLOCK_S*)NativeImport.VirtualAlloc(
                        (IntPtr)pAlloc, (IntPtr)MEMORY_BLOCK_SIZE, (AllocationType.Commit | AllocationType.Reserve), AllocationProtect.PAGE_EXECUTE_READWRITE);
                    if (pBlock != null)
                        break;
                }
            }

            // Alloc a new block below if not found.
            if (pBlock == null)
            {
                void* pAlloc = pOrigin;
                while ((ulong)pAlloc <= maxAddr)
                {
                    pAlloc = FindNextFreeRegion(pAlloc, (void*)maxAddr, si.AllocationGranularity);
                    if (pAlloc == null)
                        break;

                    pBlock = (MEMORY_BLOCK_S*)NativeImport.VirtualAlloc(
                        (IntPtr)pAlloc, (IntPtr)MEMORY_BLOCK_SIZE, (AllocationType.Commit | AllocationType.Reserve), AllocationProtect.PAGE_EXECUTE_READWRITE);
                    if (pBlock != null)
                        break;
                }
            }

            if (pBlock != null)
            {
                // Build a linked list of all the slots.
                MEMORY_SLOT_S* pSlot = (MEMORY_SLOT_S*)pBlock + 1;
                pBlock->pFree = null;
                pBlock->usedCount = 0;
                do
                {
                    pSlot->pNext = pBlock->pFree;
                    pBlock->pFree = pSlot;
                    pSlot++;
                } while ((ulong)pSlot - (ulong)pBlock <= MEMORY_BLOCK_SIZE - MEMORY_SLOT_SIZE);

                pBlock->pNext = g_pMemoryBlocks;
                g_pMemoryBlocks = pBlock;
            }

            return pBlock;
        }

        unsafe static MEMORY_BLOCK_S* GetMemoryBlock32(void* pOrigin)
        {
            MEMORY_BLOCK_S* pBlock;
            // Look the registered blocks for a reachable one.
            for (pBlock = g_pMemoryBlocks; pBlock != null; pBlock = pBlock->pNext)
            {
                // The block has at least one unused slot.
                if (pBlock->pFree != null)
                {
                    Logger.Log.Write("MinHook", "GetMemoryBlock32", "Using free slot from existing buffer block");
                    return pBlock;
                }
            }

            // In x86 mode, a memory block can be placed anywhere.
            IntPtr bufAddr = NativeImport.VirtualAlloc(
                IntPtr.Zero, (IntPtr)MEMORY_BLOCK_SIZE, (AllocationType.Commit | AllocationType.Reserve), AllocationProtect.PAGE_EXECUTE_READWRITE);
            if (bufAddr == IntPtr.Zero)
            {
                int errCode = Marshal.GetLastWin32Error();
                Logger.Log.Alert("Minhook", "GetMemoryBlock32", "Could not allocate memory (errCode: " + errCode.ToString() + " [0x" + errCode.ToString("X") + "])", NinMods.Main.MAIN_CAPTION);
            }
            pBlock = (MEMORY_BLOCK_S*)bufAddr;
            if (pBlock != null)
            {
                // Build a linked list of all the slots.
                MEMORY_SLOT_S* pSlot = (MEMORY_SLOT_S*)pBlock + 1;
                pBlock->pFree = null;
                pBlock->usedCount = 0;
                do
                {
                    pSlot->pNext = pBlock->pFree;
                    pBlock->pFree = pSlot;
                    pSlot++;
                } while ((ulong)pSlot - (ulong)pBlock <= MEMORY_BLOCK_SIZE - MEMORY_SLOT_SIZE);

                pBlock->pNext = g_pMemoryBlocks;
                g_pMemoryBlocks = pBlock;
            }

            return pBlock;
        }

        //-------------------------------------------------------------------------
        public unsafe static void FreeBuffer(void* pBuffer)
        {
            MEMORY_BLOCK_S* pBlock = g_pMemoryBlocks;
            MEMORY_BLOCK_S* pPrev = null;
            ulong pTargetBlock = ((ulong)pBuffer / MEMORY_BLOCK_SIZE) * MEMORY_BLOCK_SIZE;

            while (pBlock != null)
            {
                if ((ulong)pBlock == pTargetBlock)
                {
                    MEMORY_SLOT_S* pSlot = (MEMORY_SLOT_S*)pBuffer;
                    // Restore the released slot to the list.
                    pSlot->pNext = pBlock->pFree;
                    pBlock->pFree = pSlot;
                    pBlock->usedCount--;

                    // Free if unused.
                    if (pBlock->usedCount == 0)
                    {
                        if (pPrev != null)
                            pPrev->pNext = pBlock->pNext;
                        else
                            g_pMemoryBlocks = pBlock->pNext;

                        NativeImport.VirtualFree((IntPtr)pBlock, IntPtr.Zero, FreeType.Release);
                    }

                    break;
                }

                pPrev = pBlock;
                pBlock = pBlock->pNext;
            }
        }

        //-------------------------------------------------------------------------
        static unsafe void* FindPrevFreeRegion(void* pAddress, void* pMinAddr, uint dwAllocationGranularity)
        {
            ulong tryAddr = (ulong)pAddress;

            // Round down to the allocation granularity.
            tryAddr -= tryAddr % dwAllocationGranularity;

            // Start from the previous allocation granularity multiply.
            tryAddr -= dwAllocationGranularity;

            while (tryAddr >= (ulong)pMinAddr)
            {
                MEMORY_BASIC_INFORMATION mbi;
                if (NativeImport.VirtualQuery((IntPtr)tryAddr, out mbi, (IntPtr)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) == 0)
                    break;

                if (mbi.State == PageState.MEM_FREE)
                    return (void*)tryAddr;

                if ((ulong)mbi.AllocationBase < dwAllocationGranularity)
                    break;

                tryAddr = (ulong)mbi.AllocationBase - dwAllocationGranularity;
            }

            return null;
        }

        //-------------------------------------------------------------------------
        static unsafe void* FindNextFreeRegion(void* pAddress, void* pMaxAddr, uint dwAllocationGranularity)
        {
            ulong tryAddr = (ulong)pAddress;

            // Round down to the allocation granularity.
            tryAddr -= tryAddr % dwAllocationGranularity;

            // Start from the next allocation granularity multiply.
            tryAddr += dwAllocationGranularity;

            while (tryAddr <= (ulong)pMaxAddr)
            {
                MEMORY_BASIC_INFORMATION mbi;
                if (NativeImport.VirtualQuery((IntPtr)tryAddr, out mbi, (IntPtr)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) == 0)
                    break;

                if (mbi.State == PageState.MEM_FREE)
                    return (void*)tryAddr;

                tryAddr = (ulong)mbi.BaseAddress + (ulong)mbi.RegionSize;

                // Round up to the next allocation granularity.
                tryAddr += dwAllocationGranularity - 1;
                tryAddr -= tryAddr % dwAllocationGranularity;
            }

            return null;
        }
    }
}
