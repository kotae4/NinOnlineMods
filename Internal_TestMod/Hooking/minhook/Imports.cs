﻿using System;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace NinMods.Hooking.NativeImports
{
    // credit: http://www.dotnetframework.org/default.aspx/Net/Net/3@5@50727@3053/DEVDIV/depot/DevDiv/releases/whidbey/netfxsp/ndp/fx/src/Services/Monitoring/system/Diagnosticts/ProcessManager@cs/2/ProcessManager@cs
    [StructLayout(LayoutKind.Sequential)]
    internal class SystemProcessInformation
    {
        internal int NextEntryOffset;
        internal uint NumberOfThreads;
        long SpareLi1;
        long SpareLi2;
        long SpareLi3;
        long CreateTime;
        long UserTime;
        long KernelTime;

        internal ushort NameLength;   // UNICODE_STRING 
        internal ushort MaximumNameLength;
        internal IntPtr NamePtr;     // This will point into the data block returned by NtQuerySystemInformation 

        internal int BasePriority;
        internal IntPtr UniqueProcessId;
        internal IntPtr InheritedFromUniqueProcessId;
        internal uint HandleCount;
        internal uint SessionId;
        internal IntPtr PageDirectoryBase;
        internal IntPtr PeakVirtualSize;
        internal IntPtr VirtualSize;
        internal uint PageFaultCount;

        internal IntPtr PeakWorkingSetSize;
        internal IntPtr WorkingSetSize;
        internal IntPtr QuotaPeakPagedPoolUsage;
        internal IntPtr QuotaPagedPoolUsage;
        internal IntPtr QuotaPeakNonPagedPoolUsage;
        internal IntPtr QuotaNonPagedPoolUsage;
        internal IntPtr PagefileUsage;
        internal IntPtr PeakPagefileUsage;
        internal IntPtr PrivatePageCount;

        long ReadOperationCount;
        long WriteOperationCount;
        long OtherOperationCount;
        long ReadTransferCount;
        long WriteTransferCount;
        long OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class SystemThreadInformation
    {
        long KernelTime;
        long UserTime;
        long CreateTime;

        uint WaitTime;
        internal IntPtr StartAddress;
        internal IntPtr UniqueProcess;
        internal IntPtr UniqueThread;
        internal int Priority;
        internal int BasePriority;
        internal uint ContextSwitches;
        internal uint ThreadState;
        internal uint WaitReason;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct s_SystemProcessInformation
    {
        internal int NextEntryOffset;
        internal uint NumberOfThreads;
        long SpareLi1;
        long SpareLi2;
        long SpareLi3;
        long CreateTime;
        long UserTime;
        long KernelTime;

        internal ushort NameLength;   // UNICODE_STRING 
        internal ushort MaximumNameLength;
        internal IntPtr NamePtr;     // This will point into the data block returned by NtQuerySystemInformation 

        internal int BasePriority;
        internal IntPtr UniqueProcessId;
        internal IntPtr InheritedFromUniqueProcessId;
        internal uint HandleCount;
        internal uint SessionId;
        internal IntPtr PageDirectoryBase;
        internal IntPtr PeakVirtualSize;
        internal IntPtr VirtualSize;
        internal uint PageFaultCount;

        internal IntPtr PeakWorkingSetSize;
        internal IntPtr WorkingSetSize;
        internal IntPtr QuotaPeakPagedPoolUsage;
        internal IntPtr QuotaPagedPoolUsage;
        internal IntPtr QuotaPeakNonPagedPoolUsage;
        internal IntPtr QuotaNonPagedPoolUsage;
        internal IntPtr PagefileUsage;
        internal IntPtr PeakPagefileUsage;
        internal IntPtr PrivatePageCount;

        long ReadOperationCount;
        long WriteOperationCount;
        long OtherOperationCount;
        long ReadTransferCount;
        long WriteTransferCount;
        long OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct s_SystemThreadInformation
    {
        long KernelTime;
        long UserTime;
        long CreateTime;

        uint WaitTime;
        internal IntPtr StartAddress;
        internal IntPtr UniqueProcess;
        internal IntPtr UniqueThread;
        internal int Priority;
        internal int BasePriority;
        internal uint ContextSwitches;
        internal uint ThreadState;
        internal uint WaitReason;
    }

    // NTStatus, used by NtQuerySystemInformation
    public enum NtStatus : uint
    {
        // Success
        Success = 0x00000000,
        Wait0 = 0x00000000,
        Wait1 = 0x00000001,
        Wait2 = 0x00000002,
        Wait3 = 0x00000003,
        Wait63 = 0x0000003f,
        Abandoned = 0x00000080,
        AbandonedWait0 = 0x00000080,
        AbandonedWait1 = 0x00000081,
        AbandonedWait2 = 0x00000082,
        AbandonedWait3 = 0x00000083,
        AbandonedWait63 = 0x000000bf,
        UserApc = 0x000000c0,
        KernelApc = 0x00000100,
        Alerted = 0x00000101,
        Timeout = 0x00000102,
        Pending = 0x00000103,
        Reparse = 0x00000104,
        MoreEntries = 0x00000105,
        NotAllAssigned = 0x00000106,
        SomeNotMapped = 0x00000107,
        OpLockBreakInProgress = 0x00000108,
        VolumeMounted = 0x00000109,
        RxActCommitted = 0x0000010a,
        NotifyCleanup = 0x0000010b,
        NotifyEnumDir = 0x0000010c,
        NoQuotasForAccount = 0x0000010d,
        PrimaryTransportConnectFailed = 0x0000010e,
        PageFaultTransition = 0x00000110,
        PageFaultDemandZero = 0x00000111,
        PageFaultCopyOnWrite = 0x00000112,
        PageFaultGuardPage = 0x00000113,
        PageFaultPagingFile = 0x00000114,
        CrashDump = 0x00000116,
        ReparseObject = 0x00000118,
        NothingToTerminate = 0x00000122,
        ProcessNotInJob = 0x00000123,
        ProcessInJob = 0x00000124,
        ProcessCloned = 0x00000129,
        FileLockedWithOnlyReaders = 0x0000012a,
        FileLockedWithWriters = 0x0000012b,

        // Informational
        Informational = 0x40000000,
        ObjectNameExists = 0x40000000,
        ThreadWasSuspended = 0x40000001,
        WorkingSetLimitRange = 0x40000002,
        ImageNotAtBase = 0x40000003,
        RegistryRecovered = 0x40000009,

        // Warning
        Warning = 0x80000000,
        GuardPageViolation = 0x80000001,
        DatatypeMisalignment = 0x80000002,
        Breakpoint = 0x80000003,
        SingleStep = 0x80000004,
        BufferOverflow = 0x80000005,
        NoMoreFiles = 0x80000006,
        HandlesClosed = 0x8000000a,
        PartialCopy = 0x8000000d,
        DeviceBusy = 0x80000011,
        InvalidEaName = 0x80000013,
        EaListInconsistent = 0x80000014,
        NoMoreEntries = 0x8000001a,
        LongJump = 0x80000026,
        DllMightBeInsecure = 0x8000002b,

        // Error
        Error = 0xc0000000,
        Unsuccessful = 0xc0000001,
        NotImplemented = 0xc0000002,
        InvalidInfoClass = 0xc0000003,
        InfoLengthMismatch = 0xc0000004,
        AccessViolation = 0xc0000005,
        InPageError = 0xc0000006,
        PagefileQuota = 0xc0000007,
        InvalidHandle = 0xc0000008,
        BadInitialStack = 0xc0000009,
        BadInitialPc = 0xc000000a,
        InvalidCid = 0xc000000b,
        TimerNotCanceled = 0xc000000c,
        InvalidParameter = 0xc000000d,
        NoSuchDevice = 0xc000000e,
        NoSuchFile = 0xc000000f,
        InvalidDeviceRequest = 0xc0000010,
        EndOfFile = 0xc0000011,
        WrongVolume = 0xc0000012,
        NoMediaInDevice = 0xc0000013,
        NoMemory = 0xc0000017,
        NotMappedView = 0xc0000019,
        UnableToFreeVm = 0xc000001a,
        UnableToDeleteSection = 0xc000001b,
        IllegalInstruction = 0xc000001d,
        AlreadyCommitted = 0xc0000021,
        AccessDenied = 0xc0000022,
        BufferTooSmall = 0xc0000023,
        ObjectTypeMismatch = 0xc0000024,
        NonContinuableException = 0xc0000025,
        BadStack = 0xc0000028,
        NotLocked = 0xc000002a,
        NotCommitted = 0xc000002d,
        InvalidParameterMix = 0xc0000030,
        ObjectNameInvalid = 0xc0000033,
        ObjectNameNotFound = 0xc0000034,
        ObjectNameCollision = 0xc0000035,
        ObjectPathInvalid = 0xc0000039,
        ObjectPathNotFound = 0xc000003a,
        ObjectPathSyntaxBad = 0xc000003b,
        DataOverrun = 0xc000003c,
        DataLate = 0xc000003d,
        DataError = 0xc000003e,
        CrcError = 0xc000003f,
        SectionTooBig = 0xc0000040,
        PortConnectionRefused = 0xc0000041,
        InvalidPortHandle = 0xc0000042,
        SharingViolation = 0xc0000043,
        QuotaExceeded = 0xc0000044,
        InvalidPageProtection = 0xc0000045,
        MutantNotOwned = 0xc0000046,
        SemaphoreLimitExceeded = 0xc0000047,
        PortAlreadySet = 0xc0000048,
        SectionNotImage = 0xc0000049,
        SuspendCountExceeded = 0xc000004a,
        ThreadIsTerminating = 0xc000004b,
        BadWorkingSetLimit = 0xc000004c,
        IncompatibleFileMap = 0xc000004d,
        SectionProtection = 0xc000004e,
        EasNotSupported = 0xc000004f,
        EaTooLarge = 0xc0000050,
        NonExistentEaEntry = 0xc0000051,
        NoEasOnFile = 0xc0000052,
        EaCorruptError = 0xc0000053,
        FileLockConflict = 0xc0000054,
        LockNotGranted = 0xc0000055,
        DeletePending = 0xc0000056,
        CtlFileNotSupported = 0xc0000057,
        UnknownRevision = 0xc0000058,
        RevisionMismatch = 0xc0000059,
        InvalidOwner = 0xc000005a,
        InvalidPrimaryGroup = 0xc000005b,
        NoImpersonationToken = 0xc000005c,
        CantDisableMandatory = 0xc000005d,
        NoLogonServers = 0xc000005e,
        NoSuchLogonSession = 0xc000005f,
        NoSuchPrivilege = 0xc0000060,
        PrivilegeNotHeld = 0xc0000061,
        InvalidAccountName = 0xc0000062,
        UserExists = 0xc0000063,
        NoSuchUser = 0xc0000064,
        GroupExists = 0xc0000065,
        NoSuchGroup = 0xc0000066,
        MemberInGroup = 0xc0000067,
        MemberNotInGroup = 0xc0000068,
        LastAdmin = 0xc0000069,
        WrongPassword = 0xc000006a,
        IllFormedPassword = 0xc000006b,
        PasswordRestriction = 0xc000006c,
        LogonFailure = 0xc000006d,
        AccountRestriction = 0xc000006e,
        InvalidLogonHours = 0xc000006f,
        InvalidWorkstation = 0xc0000070,
        PasswordExpired = 0xc0000071,
        AccountDisabled = 0xc0000072,
        NoneMapped = 0xc0000073,
        TooManyLuidsRequested = 0xc0000074,
        LuidsExhausted = 0xc0000075,
        InvalidSubAuthority = 0xc0000076,
        InvalidAcl = 0xc0000077,
        InvalidSid = 0xc0000078,
        InvalidSecurityDescr = 0xc0000079,
        ProcedureNotFound = 0xc000007a,
        InvalidImageFormat = 0xc000007b,
        NoToken = 0xc000007c,
        BadInheritanceAcl = 0xc000007d,
        RangeNotLocked = 0xc000007e,
        DiskFull = 0xc000007f,
        ServerDisabled = 0xc0000080,
        ServerNotDisabled = 0xc0000081,
        TooManyGuidsRequested = 0xc0000082,
        GuidsExhausted = 0xc0000083,
        InvalidIdAuthority = 0xc0000084,
        AgentsExhausted = 0xc0000085,
        InvalidVolumeLabel = 0xc0000086,
        SectionNotExtended = 0xc0000087,
        NotMappedData = 0xc0000088,
        ResourceDataNotFound = 0xc0000089,
        ResourceTypeNotFound = 0xc000008a,
        ResourceNameNotFound = 0xc000008b,
        ArrayBoundsExceeded = 0xc000008c,
        FloatDenormalOperand = 0xc000008d,
        FloatDivideByZero = 0xc000008e,
        FloatInexactResult = 0xc000008f,
        FloatInvalidOperation = 0xc0000090,
        FloatOverflow = 0xc0000091,
        FloatStackCheck = 0xc0000092,
        FloatUnderflow = 0xc0000093,
        IntegerDivideByZero = 0xc0000094,
        IntegerOverflow = 0xc0000095,
        PrivilegedInstruction = 0xc0000096,
        TooManyPagingFiles = 0xc0000097,
        FileInvalid = 0xc0000098,
        InstanceNotAvailable = 0xc00000ab,
        PipeNotAvailable = 0xc00000ac,
        InvalidPipeState = 0xc00000ad,
        PipeBusy = 0xc00000ae,
        IllegalFunction = 0xc00000af,
        PipeDisconnected = 0xc00000b0,
        PipeClosing = 0xc00000b1,
        PipeConnected = 0xc00000b2,
        PipeListening = 0xc00000b3,
        InvalidReadMode = 0xc00000b4,
        IoTimeout = 0xc00000b5,
        FileForcedClosed = 0xc00000b6,
        ProfilingNotStarted = 0xc00000b7,
        ProfilingNotStopped = 0xc00000b8,
        NotSameDevice = 0xc00000d4,
        FileRenamed = 0xc00000d5,
        CantWait = 0xc00000d8,
        PipeEmpty = 0xc00000d9,
        CantTerminateSelf = 0xc00000db,
        InternalError = 0xc00000e5,
        InvalidParameter1 = 0xc00000ef,
        InvalidParameter2 = 0xc00000f0,
        InvalidParameter3 = 0xc00000f1,
        InvalidParameter4 = 0xc00000f2,
        InvalidParameter5 = 0xc00000f3,
        InvalidParameter6 = 0xc00000f4,
        InvalidParameter7 = 0xc00000f5,
        InvalidParameter8 = 0xc00000f6,
        InvalidParameter9 = 0xc00000f7,
        InvalidParameter10 = 0xc00000f8,
        InvalidParameter11 = 0xc00000f9,
        InvalidParameter12 = 0xc00000fa,
        MappedFileSizeZero = 0xc000011e,
        TooManyOpenedFiles = 0xc000011f,
        Cancelled = 0xc0000120,
        CannotDelete = 0xc0000121,
        InvalidComputerName = 0xc0000122,
        FileDeleted = 0xc0000123,
        SpecialAccount = 0xc0000124,
        SpecialGroup = 0xc0000125,
        SpecialUser = 0xc0000126,
        MembersPrimaryGroup = 0xc0000127,
        FileClosed = 0xc0000128,
        TooManyThreads = 0xc0000129,
        ThreadNotInProcess = 0xc000012a,
        TokenAlreadyInUse = 0xc000012b,
        PagefileQuotaExceeded = 0xc000012c,
        CommitmentLimit = 0xc000012d,
        InvalidImageLeFormat = 0xc000012e,
        InvalidImageNotMz = 0xc000012f,
        InvalidImageProtect = 0xc0000130,
        InvalidImageWin16 = 0xc0000131,
        LogonServer = 0xc0000132,
        DifferenceAtDc = 0xc0000133,
        SynchronizationRequired = 0xc0000134,
        DllNotFound = 0xc0000135,
        IoPrivilegeFailed = 0xc0000137,
        OrdinalNotFound = 0xc0000138,
        EntryPointNotFound = 0xc0000139,
        ControlCExit = 0xc000013a,
        PortNotSet = 0xc0000353,
        DebuggerInactive = 0xc0000354,
        CallbackBypass = 0xc0000503,
        PortClosed = 0xc0000700,
        MessageLost = 0xc0000701,
        InvalidMessage = 0xc0000702,
        RequestCanceled = 0xc0000703,
        RecursiveDispatch = 0xc0000704,
        LpcReceiveBufferExpected = 0xc0000705,
        LpcInvalidConnectionUsage = 0xc0000706,
        LpcRequestsNotAllowed = 0xc0000707,
        ResourceInUse = 0xc0000708,
        ProcessIsProtected = 0xc0000712,
        VolumeDirty = 0xc0000806,
        FileCheckedOut = 0xc0000901,
        CheckOutRequired = 0xc0000902,
        BadFileType = 0xc0000903,
        FileTooLarge = 0xc0000904,
        FormsAuthRequired = 0xc0000905,
        VirusInfected = 0xc0000906,
        VirusDeleted = 0xc0000907,
        TransactionalConflict = 0xc0190001,
        InvalidTransaction = 0xc0190002,
        TransactionNotActive = 0xc0190003,
        TmInitializationFailed = 0xc0190004,
        RmNotActive = 0xc0190005,
        RmMetadataCorrupt = 0xc0190006,
        TransactionNotJoined = 0xc0190007,
        DirectoryNotRm = 0xc0190008,
        CouldNotResizeLog = 0xc0190009,
        TransactionsUnsupportedRemote = 0xc019000a,
        LogResizeInvalidSize = 0xc019000b,
        RemoteFileVersionMismatch = 0xc019000c,
        CrmProtocolAlreadyExists = 0xc019000f,
        TransactionPropagationFailed = 0xc0190010,
        CrmProtocolNotFound = 0xc0190011,
        TransactionSuperiorExists = 0xc0190012,
        TransactionRequestNotValid = 0xc0190013,
        TransactionNotRequested = 0xc0190014,
        TransactionAlreadyAborted = 0xc0190015,
        TransactionAlreadyCommitted = 0xc0190016,
        TransactionInvalidMarshallBuffer = 0xc0190017,
        CurrentTransactionNotValid = 0xc0190018,
        LogGrowthFailed = 0xc0190019,
        ObjectNoLongerExists = 0xc0190021,
        StreamMiniversionNotFound = 0xc0190022,
        StreamMiniversionNotValid = 0xc0190023,
        MiniversionInaccessibleFromSpecifiedTransaction = 0xc0190024,
        CantOpenMiniversionWithModifyIntent = 0xc0190025,
        CantCreateMoreStreamMiniversions = 0xc0190026,
        HandleNoLongerValid = 0xc0190028,
        NoTxfMetadata = 0xc0190029,
        LogCorruptionDetected = 0xc0190030,
        CantRecoverWithHandleOpen = 0xc0190031,
        RmDisconnected = 0xc0190032,
        EnlistmentNotSuperior = 0xc0190033,
        RecoveryNotNeeded = 0xc0190034,
        RmAlreadyStarted = 0xc0190035,
        FileIdentityNotPersistent = 0xc0190036,
        CantBreakTransactionalDependency = 0xc0190037,
        CantCrossRmBoundary = 0xc0190038,
        TxfDirNotEmpty = 0xc0190039,
        IndoubtTransactionsExist = 0xc019003a,
        TmVolatile = 0xc019003b,
        RollbackTimerExpired = 0xc019003c,
        TxfAttributeCorrupt = 0xc019003d,
        EfsNotAllowedInTransaction = 0xc019003e,
        TransactionalOpenNotAllowed = 0xc019003f,
        TransactedMappingUnsupportedRemote = 0xc0190040,
        TxfMetadataAlreadyPresent = 0xc0190041,
        TransactionScopeCallbacksNotSet = 0xc0190042,
        TransactionRequiredPromotion = 0xc0190043,
        CannotExecuteFileInTransaction = 0xc0190044,
        TransactionsNotFrozen = 0xc0190045,

        MaximumNtStatus = 0xffffffff
    }

    // also used by NtQuerySystemInformation
    public enum SYSTEM_INFORMATION_CLASS
    {
        SystemBasicInformation = 0x0000,
        SystemProcessorInformation = 0x0001,
        SystemPerformanceInformation = 0x0002,
        SystemTimeOfDayInformation = 0x0003,
        SystemPathInformation = 0x0004,
        SystemProcessInformation = 0x0005,
        SystemCallCountInformation = 0x0006,
        SystemDeviceInformation = 0x0007,
        SystemProcessorPerformanceInformation = 0x0008,
        SystemFlagsInformation = 0x0009,
        SystemCallTimeInformation = 0x000A,
        SystemModuleInformation = 0x000B,
        SystemLocksInformation = 0x000C,
        SystemStackTraceInformation = 0x000D,
        SystemPagedPoolInformation = 0x000E,
        SystemNonPagedPoolInformation = 0x000F,
        SystemHandleInformation = 0x0010,
        SystemObjectInformation = 0x0011,
        SystemPageFileInformation = 0x0012,
        SystemVdmInstemulInformation = 0x0013,
        SystemVdmBopInformation = 0x0014,
        SystemFileCacheInformation = 0x0015,
        SystemPoolTagInformation = 0x0016,
        SystemInterruptInformation = 0x0017,
        SystemDpcBehaviorInformation = 0x0018,
        SystemFullMemoryInformation = 0x0019,
        SystemLoadGdiDriverInformation = 0x001A,
        SystemUnloadGdiDriverInformation = 0x001B,
        SystemTimeAdjustmentInformation = 0x001C,
        SystemSummaryMemoryInformation = 0x001D,
        SystemMirrorMemoryInformation = 0x001E,
        SystemPerformanceTraceInformation = 0x001F,
        SystemCrashDumpInformation = 0x0020,
        SystemExceptionInformation = 0x0021,
        SystemCrashDumpStateInformation = 0x0022,
        SystemKernelDebuggerInformation = 0x0023,
        SystemContextSwitchInformation = 0x0024,
        SystemRegistryQuotaInformation = 0x0025,
        SystemExtendServiceTableInformation = 0x0026,
        SystemPrioritySeperation = 0x0027,
        SystemVerifierAddDriverInformation = 0x0028,
        SystemVerifierRemoveDriverInformation = 0x0029,
        SystemProcessorIdleInformation = 0x002A,
        SystemLegacyDriverInformation = 0x002B,
        SystemCurrentTimeZoneInformation = 0x002C,
        SystemLookasideInformation = 0x002D,
        SystemTimeSlipNotification = 0x002E,
        SystemSessionCreate = 0x002F,
        SystemSessionDetach = 0x0030,
        SystemSessionInformation = 0x0031,
        SystemRangeStartInformation = 0x0032,
        SystemVerifierInformation = 0x0033,
        SystemVerifierThunkExtend = 0x0034,
        SystemSessionProcessInformation = 0x0035,
        SystemLoadGdiDriverInSystemSpace = 0x0036,
        SystemNumaProcessorMap = 0x0037,
        SystemPrefetcherInformation = 0x0038,
        SystemExtendedProcessInformation = 0x0039,
        SystemRecommendedSharedDataAlignment = 0x003A,
        SystemComPlusPackage = 0x003B,
        SystemNumaAvailableMemory = 0x003C,
        SystemProcessorPowerInformation = 0x003D,
        SystemEmulationBasicInformation = 0x003E,
        SystemEmulationProcessorInformation = 0x003F,
        SystemExtendedHandleInformation = 0x0040,
        SystemLostDelayedWriteInformation = 0x0041,
        SystemBigPoolInformation = 0x0042,
        SystemSessionPoolTagInformation = 0x0043,
        SystemSessionMappedViewInformation = 0x0044,
        SystemHotpatchInformation = 0x0045,
        SystemObjectSecurityMode = 0x0046,
        SystemWatchdogTimerHandler = 0x0047,
        SystemWatchdogTimerInformation = 0x0048,
        SystemLogicalProcessorInformation = 0x0049,
        SystemWow64SharedInformationObsolete = 0x004A,
        SystemRegisterFirmwareTableInformationHandler = 0x004B,
        SystemFirmwareTableInformation = 0x004C,
        SystemModuleInformationEx = 0x004D,
        SystemVerifierTriageInformation = 0x004E,
        SystemSuperfetchInformation = 0x004F,
        SystemMemoryListInformation = 0x0050, // SYSTEM_MEMORY_LIST_INFORMATION
        SystemFileCacheInformationEx = 0x0051,
        SystemThreadPriorityClientIdInformation = 0x0052,
        SystemProcessorIdleCycleTimeInformation = 0x0053,
        SystemVerifierCancellationInformation = 0x0054,
        SystemProcessorPowerInformationEx = 0x0055,
        SystemRefTraceInformation = 0x0056,
        SystemSpecialPoolInformation = 0x0057,
        SystemProcessIdInformation = 0x0058,
        SystemErrorPortInformation = 0x0059,
        SystemBootEnvironmentInformation = 0x005A,
        SystemHypervisorInformation = 0x005B,
        SystemVerifierInformationEx = 0x005C,
        SystemTimeZoneInformation = 0x005D,
        SystemImageFileExecutionOptionsInformation = 0x005E,
        SystemCoverageInformation = 0x005F,
        SystemPrefetchPatchInformation = 0x0060,
        SystemVerifierFaultsInformation = 0x0061,
        SystemSystemPartitionInformation = 0x0062,
        SystemSystemDiskInformation = 0x0063,
        SystemProcessorPerformanceDistribution = 0x0064,
        SystemNumaProximityNodeInformation = 0x0065,
        SystemDynamicTimeZoneInformation = 0x0066,
        SystemCodeIntegrityInformation = 0x0067,
        SystemProcessorMicrocodeUpdateInformation = 0x0068,
        SystemProcessorBrandString = 0x0069,
        SystemVirtualAddressInformation = 0x006A,
        SystemLogicalProcessorAndGroupInformation = 0x006B,
        SystemProcessorCycleTimeInformation = 0x006C,
        SystemStoreInformation = 0x006D,
        SystemRegistryAppendString = 0x006E,
        SystemAitSamplingValue = 0x006F,
        SystemVhdBootInformation = 0x0070,
        SystemCpuQuotaInformation = 0x0071,
        SystemNativeBasicInformation = 0x0072,
        SystemErrorPortTimeouts = 0x0073,
        SystemLowPriorityIoInformation = 0x0074,
        SystemBootEntropyInformation = 0x0075,
        SystemVerifierCountersInformation = 0x0076,
        SystemPagedPoolInformationEx = 0x0077,
        SystemSystemPtesInformationEx = 0x0078,
        SystemNodeDistanceInformation = 0x0079,
        SystemAcpiAuditInformation = 0x007A,
        SystemBasicPerformanceInformation = 0x007B,
        SystemQueryPerformanceCounterInformation = 0x007C,
        SystemSessionBigPoolInformation = 0x007D,
        SystemBootGraphicsInformation = 0x007E,
        SystemScrubPhysicalMemoryInformation = 0x007F,
        SystemBadPageInformation = 0x0080,
        SystemProcessorProfileControlArea = 0x0081,
        SystemCombinePhysicalMemoryInformation = 0x0082,
        SystemEntropyInterruptTimingInformation = 0x0083,
        SystemConsoleInformation = 0x0084,
        SystemPlatformBinaryInformation = 0x0085,
        SystemThrottleNotificationInformation = 0x0086,
        SystemHypervisorProcessorCountInformation = 0x0087,
        SystemDeviceDataInformation = 0x0088,
        SystemDeviceDataEnumerationInformation = 0x0089,
        SystemMemoryTopologyInformation = 0x008A,
        SystemMemoryChannelInformation = 0x008B,
        SystemBootLogoInformation = 0x008C,
        SystemProcessorPerformanceInformationEx = 0x008D,
        SystemSpare0 = 0x008E,
        SystemSecureBootPolicyInformation = 0x008F,
        SystemPageFileInformationEx = 0x0090,
        SystemSecureBootInformation = 0x0091,
        SystemEntropyInterruptTimingRawInformation = 0x0092,
        SystemPortableWorkspaceEfiLauncherInformation = 0x0093,
        SystemFullProcessInformation = 0x0094,
        MaxSystemInfoClass = 0x0095
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct _UNICODE_STRING
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct _SYSTEM_PROCESS_INFORMATION
    {
        public uint NextEntryOffset;
        public uint NumberOfThreads;
        public fixed byte Reserved1[48];
        public _UNICODE_STRING ImageName;
        public int BasePriority;
        public IntPtr UniqueProcessId;
        public IntPtr Reserved2;
        public uint HandleCount;
        public uint SessionId;
        public IntPtr Reserved3;
        public ulong PeakVirtualSize;
        public ulong VirtualSize;
        public uint Reserved4;
        public ulong PeakWorkingSetSize;
        public ulong WorkingSetSize;
        public IntPtr Reserved5;
        public ulong QuotaPagedPoolUsage;
        public IntPtr Reserved6;
        public ulong QuotaNonPagedPoolUsage;
        public ulong PagefileUsage;
        public ulong PeakPagefileUsage;
        public ulong PrivatePageCount;
        // NOTE:
        // no idea on proper size for LARGE_INTEGER
        // apparently it's 8
        public fixed byte Reserved7[48];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct _CLIENT_ID
    {
        public IntPtr UniqueProcess;
        public IntPtr UniqueThread;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct _SYSTEM_THREAD_INFORMATION
    {
        // NOTE: originally LARGE_INTEGER[3]
        public fixed byte Reserved1[24];
        public uint Reserved2;
        public IntPtr StartAddress;
        public _CLIENT_ID ClientId;
        public int Priority;
        public int BasePriority;
        public uint Reserved3;
        public uint ThreadState;
        public uint WaitReason;
    }

    // protection flags
    [Flags]
    public enum AllocationProtect : uint
    {
        PAGE_EXECUTE = 0x00000010,
        PAGE_EXECUTE_READ = 0x00000020,
        PAGE_EXECUTE_READWRITE = 0x00000040,
        PAGE_EXECUTE_WRITECOPY = 0x00000080,
        PAGE_NOACCESS = 0x00000001,
        PAGE_READONLY = 0x00000002,
        PAGE_READWRITE = 0x00000004,
        PAGE_WRITECOPY = 0x00000008,
        PAGE_GUARD = 0x00000100,
        PAGE_NOCACHE = 0x00000200,
        PAGE_WRITECOMBINE = 0x00000400
    }
    // allocation
    [Flags]
    public enum AllocationType : uint
    {
        Commit = 0x1000,
        Reserve = 0x2000,
        Decommit = 0x4000,
        Release = 0x8000,
        Reset = 0x80000,
        Physical = 0x400000,
        TopDown = 0x100000,
        WriteWatch = 0x200000,
        LargePages = 0x20000000
    }

    [Flags]
    public enum FreeType
    {
        Decommit = 0x4000,
        Release = 0x8000,
    }

    // page state (from VirtualQuery)
    [Flags]
    public enum PageState : uint
    {
        MEM_COMMIT = 0x1000,
        MEM_FREE = 0x10000,
        MEM_RESERVE = 0x2000
    }

    [Flags]
    public enum ThreadAccess : int
    {
        TERMINATE = (0x0001),
        SUSPEND_RESUME = (0x0002),
        GET_CONTEXT = (0x0008),
        SET_CONTEXT = (0x0010),
        SET_INFORMATION = (0x0020),
        QUERY_INFORMATION = (0x0040),
        SET_THREAD_TOKEN = (0x0080),
        IMPERSONATE = (0x0100),
        DIRECT_IMPERSONATION = (0x0200)
    }

    #region Get/SetThreadContext
    public enum CONTEXT_FLAGS : uint
    {
        CONTEXT_i386 = 0x10000,
        CONTEXT_i486 = 0x10000,   //  same as i386
        CONTEXT_CONTROL = CONTEXT_i386 | 0x01, // SS:SP, CS:IP, FLAGS, BP
        CONTEXT_INTEGER = CONTEXT_i386 | 0x02, // AX, BX, CX, DX, SI, DI
        CONTEXT_SEGMENTS = CONTEXT_i386 | 0x04, // DS, ES, FS, GS
        CONTEXT_FLOATING_POINT = CONTEXT_i386 | 0x08, // 387 state
        CONTEXT_DEBUG_REGISTERS = CONTEXT_i386 | 0x10, // DB 0-3,6,7
        CONTEXT_EXTENDED_REGISTERS = CONTEXT_i386 | 0x20, // cpu specific extensions
        CONTEXT_FULL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS,
        CONTEXT_ALL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS | CONTEXT_EXTENDED_REGISTERS
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct FLOATING_SAVE_AREA
    {
        public uint ControlWord;
        public uint StatusWord;
        public uint TagWord;
        public uint ErrorOffset;
        public uint ErrorSelector;
        public uint DataOffset;
        public uint DataSelector;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        public byte[] RegisterArea;
        public uint Cr0NpxState;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CONTEXT
    {
        public uint ContextFlags; //set this to an appropriate value
                                  // Retrieved by CONTEXT_DEBUG_REGISTERS
        public uint Dr0;
        public uint Dr1;
        public uint Dr2;
        public uint Dr3;
        public uint Dr6;
        public uint Dr7;
        // Retrieved by CONTEXT_FLOATING_POINT
        public FLOATING_SAVE_AREA FloatSave;
        // Retrieved by CONTEXT_SEGMENTS
        public uint SegGs;
        public uint SegFs;
        public uint SegEs;
        public uint SegDs;
        // Retrieved by CONTEXT_INTEGER
        public uint Edi;
        public uint Esi;
        public uint Ebx;
        public uint Edx;
        public uint Ecx;
        public uint Eax;
        // Retrieved by CONTEXT_CONTROL
        public uint Ebp;
        public uint Eip;
        public uint SegCs;
        public uint EFlags;
        public uint Esp;
        public uint SegSs;
        // Retrieved by CONTEXT_EXTENDED_REGISTERS
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
        public byte[] ExtendedRegisters;
    }

    // Next x64

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct M128A
    {
        public ulong High;
        public long Low;

        public override string ToString()
        {
            return string.Format("High:{0}, Low:{1}", this.High, this.Low);
        }
    }

    /// <summary>
    /// x64
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct XSAVE_FORMAT64
    {
        public ushort ControlWord;
        public ushort StatusWord;
        public byte TagWord;
        public byte Reserved1;
        public ushort ErrorOpcode;
        public uint ErrorOffset;
        public ushort ErrorSelector;
        public ushort Reserved2;
        public uint DataOffset;
        public ushort DataSelector;
        public ushort Reserved3;
        public uint MxCsr;
        public uint MxCsr_Mask;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public M128A[] FloatRegisters;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public M128A[] XmmRegisters;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)]
        public byte[] Reserved4;
    }

    /// <summary>
    /// x64
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CONTEXT64
    {
        public ulong P1Home;
        public ulong P2Home;
        public ulong P3Home;
        public ulong P4Home;
        public ulong P5Home;
        public ulong P6Home;

        public uint ContextFlags;
        public uint MxCsr;

        public ushort SegCs;
        public ushort SegDs;
        public ushort SegEs;
        public ushort SegFs;
        public ushort SegGs;
        public ushort SegSs;
        public uint EFlags;

        public ulong Dr0;
        public ulong Dr1;
        public ulong Dr2;
        public ulong Dr3;
        public ulong Dr6;
        public ulong Dr7;

        public ulong Rax;
        public ulong Rcx;
        public ulong Rdx;
        public ulong Rbx;
        public ulong Rsp;
        public ulong Rbp;
        public ulong Rsi;
        public ulong Rdi;
        public ulong R8;
        public ulong R9;
        public ulong R10;
        public ulong R11;
        public ulong R12;
        public ulong R13;
        public ulong R14;
        public ulong R15;
        public ulong Rip;
        // works
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 122)]
        public byte[] _unnecessary;
        /*
        // doesn't work. fuck marshaling; we don't even need this stuff
        /*
        public XSAVE_FORMAT64 DUMMYUNIONNAME;
        [MarshalAs(UnmanagedType.LPArray, SizeConst = 26)]
        public M128A[] VectorRegister;
        public ulong VectorControl;

        public ulong DebugControl;
        public ulong LastBranchToRip;
        public ulong LastBranchFromRip;
        public ulong LastExceptionToRip;
        public ulong LastExceptionFromRip;
        */
    }
    #endregion


    // VirtualQuery
    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORY_BASIC_INFORMATION
    {
        public IntPtr BaseAddress;
        public IntPtr AllocationBase;
        public AllocationProtect AllocationProtect;
        public IntPtr RegionSize;
        public PageState State;
        public uint Protect;
        public uint Type;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct SYSTEM_INFO_UNION
    {
        [FieldOffset(0)]
        public UInt32 OemId;
        [FieldOffset(0)]
        public UInt16 ProcessorArchitecture;
        [FieldOffset(2)]
        public UInt16 Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SYSTEM_INFO
    {
        public SYSTEM_INFO_UNION CpuInfo;
        public UInt32 PageSize;
        public IntPtr MinimumApplicationAddress;
        public IntPtr MaximumApplicationAddress;
        public IntPtr ActiveProcessorMask;
        public UInt32 NumberOfProcessors;
        public UInt32 ProcessorType;
        public UInt32 AllocationGranularity;
        public UInt16 ProcessorLevel;
        public UInt16 ProcessorRevision;
    }

    [Flags]
    public enum SnapshotFlags : uint
    {
        HeapList = 0x00000001,
        Process = 0x00000002,
        Thread = 0x00000004,
        Module = 0x00000008,
        Module32 = 0x00000010,
        All = (HeapList | Process | Thread | Module),
        Inherit = 0x80000000,
        NoHeaps = 0x40000000

    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
    public struct THREADENTRY32

    {

        internal UInt32 dwSize;
        internal UInt32 cntUsage;
        internal UInt32 th32ThreadID;
        internal UInt32 th32OwnerProcessID;
        // TIL: long in C is 32bit
        // int in C is also 32bit.
        // i feel like i need to go back and review every bit of C/C++ code i've written
        internal UInt32 tpBasePri;
        internal UInt32 tpDeltaPri;
        internal UInt32 dwFlags;
    }

    internal class NativeImport
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool VirtualProtect(IntPtr address, IntPtr size, AllocationProtect newProtect, out AllocationProtect oldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr VirtualAlloc(IntPtr address, IntPtr size, AllocationType allocationType, AllocationProtect protectType);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool VirtualFree(IntPtr address, IntPtr size, FreeType freeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int VirtualQuery(IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, IntPtr dwLength);

        // very unfortunately, this is necessary for unmanaged->unmanaged copying.
        // Marshal.Copy only handles managed<->unmanaged
        // Array.Copy and Buffer.BlockCopy only handle managed<->managed
        // for our usage, copying the bytes over in a for loop is probably optimal, but fuck that clutter.
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static unsafe extern void* memcpy(void* dest, void* src, IntPtr count);

        // necessary for minhook to allocate a buffer within range of trampoline
        [DllImport("kernel32.dll", SetLastError = false)]
        public static extern void GetSystemInfo(out SYSTEM_INFO Info);

        [DllImport("kernel32.dll")]
        public static extern bool FlushInstructionCache(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr dwSize);

        // next 3 needed for freezing & setting (E|R)IP of threads during (un)hooking
        // Unity's version of mono doesn't implement Process.Threads
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateToolhelp32Snapshot(SnapshotFlags dwFlags, uint th32ProcessID);

        [DllImport("kernel32.dll")]
        public static extern bool Thread32First(IntPtr hSnapshot, ref THREADENTRY32 lpte);

        [DllImport("kernel32.dll")]
        public static extern bool Thread32Next(IntPtr hSnapshot, out THREADENTRY32 lpte);

        /// <summary>Retrieves the specified system information.</summary>
        /// <param name="InfoClass">indicate the kind of system information to be retrieved</param>
        /// <param name="Info">a buffer that receives the requested information</param>
        /// <param name="Size">The allocation size of the buffer pointed to by Info</param>
        /// <param name="Length">If null, ignored.  Otherwise tells you the size of the information returned by the kernel.</param>
        /// <returns>Status Information</returns>
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/ms724509%28v=vs.85%29.aspx
        [DllImport("ntdll.dll")]
        public static extern NtStatus NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS InfoClass, IntPtr Info, uint Size, out uint Length);

        #region Thread-related imports
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, int dwThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int ResumeThread(IntPtr hThread);

        // jeez this is meaty, i wonder if it's all necessary
        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);


        // POTENTIAL BUG:
        // this might not work as expected, or at all.
        // yeah, even x64 native apps call the same function, and it ends up unconditionally jmp'ing to NtGetContextThread which leads to a syscall
        // so i guess if this doesn't work i'm fucked
        // or i can try changing the ref type to object and pray casting works
        // or maybe Wow64GetThreadContext is just a wrapper around GetThreadContext and i should use that. who knows.
        // WoW64GetThreadContext leads to NtQueryInformationThread so it's not exactly 1:1.
        // dunno enough about kernel to know if they are functionally equivalent
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "GetThreadContext")]
        public static extern bool GetThreadContext64(IntPtr hThread, ref CONTEXT64 lpContext);
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "SetThreadContext")]
        public static extern bool SetThreadContext64(IntPtr hThread, ref CONTEXT64 lpContext);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "GetThreadContext")]
        public static extern bool GetThreadContext32(IntPtr hThread, ref CONTEXT lpContext);
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "SetThreadContext")]
        public static extern bool SetThreadContext32(IntPtr hThread, ref CONTEXT lpContext);
        #endregion

    }
}
