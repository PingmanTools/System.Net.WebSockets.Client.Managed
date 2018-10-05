using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ManagedSSPI
{
    [Flags]
    public enum CredentialsUse
    {
        Inbound = 0x1,
        Outbound = 0x2,
        InboundAndOutbound = Inbound | Outbound
    }

    [Flags]
    public enum SecurityCapabilities
    {
        SupportsIntegrity = 0x1,
        SupportsPrivacy = 0x2,
        SupportsTokenOnly = 0x4,
        SupportsDatagram = 0x8,
        SupportsConnections = 0x10,
        MultipleLegsRequired = 0x20,
        ClientOnly = 0x40,
        ExtendedErrorSupport = 0x80,
        SupportsImpersonation = 0x100,
        AccepsWin32Names = 0x200,
        SupportsStreams = 0x400,
        Negotiable = 0x800,
        GSSAPICompatible = 0x1000,
        SupportsLogon = 0x2000,
        BuffersAreASCII = 0x4000,
        SupportsTokenFragmentation = 0x8000,
        SupportsMutualAuthentication = 0x10000,
        SupportsDelegation = 0x20000,
        SupportsChecksumOnly = 0x40000,
        SupportsRestrictedTokens = 0x80000,
        ExtendsNegotiate = 0x100000,
        NegotiableByExtendedNegotiate = 0x200000,
        AppContainerPassThrough = 0x400000,
        AppContainerChecks = 0x800000,
        CredentialIsolationEnabled = 0x1000000
    }

    [Flags]
    public enum ContextRequirements
    {
        Delegation =                    0x00000001,
        MutualAuthentication =          0x00000002,
        ReplayDetection =               0x00000004,
        SequenceDetection =             0x00000008,
        Confidentiality =               0x00000010,
        UseSessionKey =                 0x00000020,
        PromptForCredentials =          0x00000040,
        UseSuppliedCredentials =        0x00000080,
        AllocateMemory =                0x00000100,
        UseDceStyle =                   0x00000200,
        DatagramCommunications =        0x00000400,
        ConnectionCommunications =      0x00000800,
        CallLevel =                     0x00001000,
        FragmentSupplied =              0x00002000,
        ExtendedError =                 0x00004000,
        StreamCommunications =          0x00008000,
        Integrity =                     0x00010000,
        Identity =                      0x00020000,
        NullSession =                   0x00040000,
        ManualCredValidation =          0x00080000,
        Reserved =                      0x00100000,
        FragmentToFit =                 0x00200000,
        ForwardCredentials =            0x00400000,
        NoIntegrity =                   0x00800000,
        UseHttpStyle =                  0x01000000,
        UnverifiedTargetName =          0x20000000,
        ConfidentialityOnly =           0x40000000
    }

    public enum SecBufferType
    {
        Empty =             0,
        Data =              1,
        Token =             2,
        PackageParameters = 3,
        MissingBuffer =     4,
        ExtraData =         5,
        StreamTrailer =     6,
        StreamHeader =      7,
        NegotiationInfo =   8,
        Padding =           9,
        Stream =            10,
        ObjectIdList =      11,
        OidListSignature =  12,
        Target =            13,
        ChannelBindings =   14,
        ChangePassResp =    15,
        TargetHost =        16,
        Alert =             17,
        AppProtocolIds =    18,
        StrpProtProfiles =  19,
        StrpMasterKeyId =   20,
        TokenBinding =      21,
        PresharedKey =      22,
        PresharedKeyId =    23,
        DtlsMtu =           24
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SecurityHandle
    {
        public IntPtr LowPart;
        public IntPtr HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SecurityPackageInfo
    {
        [MarshalAs(UnmanagedType.U4)]
        public SecurityCapabilities Capabilities;
        public UInt16 Version;
        public UInt16 RpcId;
        public UInt32 MaxTokenSize;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Name;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Comment;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SecBufferDescription
    {
        public UInt32 version;
        public UInt32 numOfBuffers;
        public IntPtr buffersPtr;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SecBuffer
    {
        public UInt32 size;
        [MarshalAs(UnmanagedType.U4)]
        public SecBufferType type;
        public IntPtr bufferPtr;
    }

    public class SSPIClient : IDisposable
    {
        private const int NoError = 0;
        private const int ContinueNeeded = 0x90312;
        private const int NativeDataRepresentation = 0x10;

        private SecurityHandle _credHandle;
        private SecurityHandle _contextHandle;
        private DateTime _credExpiration;
        private DateTime _contextExpiration;

        public SSPIClient(string packageName)
        {
            _credHandle = new SecurityHandle()
            {
                HighPart = IntPtr.Zero,
                LowPart = IntPtr.Zero
            };
            _contextHandle = new SecurityHandle()
            {
                HighPart = IntPtr.Zero,
                LowPart = IntPtr.Zero
            };
            _contextExpiration = DateTime.MinValue;
            _credExpiration = DateTime.MinValue;

            UInt64 expiration = 0;
            var retCode = AcquireCredentialsHandle(
                null,
                packageName,
                CredentialsUse.Outbound,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                ref _credHandle,
                ref expiration
                );
            try
            {
                _credExpiration = DateTime.FromFileTime((Int64)expiration);
            }
            catch(ArgumentException)
            {
                // no expiration
                _credExpiration = DateTime.MaxValue;
            }
        }

        public byte[] GetClientToken(byte[] serverToken)
        {
            var pinnedServerToken = GCHandle.Alloc(serverToken, GCHandleType.Pinned);
            var serverBuffer = new SecBuffer()
            {
                type = SecBufferType.Token,
                size = null == serverToken ? 0 : (UInt32)serverToken.Length,
                bufferPtr = pinnedServerToken.AddrOfPinnedObject()
            };
            var pinnedServerBuffer = GCHandle.Alloc(serverBuffer, GCHandleType.Pinned);
            var clientBuffer = new SecBuffer()
            {
                type = SecBufferType.Token,
                size = 0,
                bufferPtr = IntPtr.Zero
            };
            var pinnedClientBuffer = GCHandle.Alloc(clientBuffer, GCHandleType.Pinned);
            byte[] clientToken = null;

            try
            {
                var inBuffDesc = new SecBufferDescription()
                {
                    version = 0,
                    numOfBuffers = 1,
                    buffersPtr = pinnedServerBuffer.AddrOfPinnedObject()
                };

                var outBuffDesc = new SecBufferDescription()
                {
                    version = 0,
                    numOfBuffers = 1,
                    buffersPtr = pinnedClientBuffer.AddrOfPinnedObject()
                };

                UInt64 expiration = 0;
                ContextRequirements availableCapabilities = default(ContextRequirements);
                int retCode = NoError;

                if (serverToken == null)
                {
                    // first leg - no server token
                    retCode = InitializeSecurityContext(
                    ref _credHandle,
                    IntPtr.Zero,
                    null,
                    ContextRequirements.AllocateMemory | ContextRequirements.ConnectionCommunications,
                    0,
                    NativeDataRepresentation,
                    IntPtr.Zero,
                    0,
                    ref _contextHandle,
                    ref outBuffDesc,
                    ref availableCapabilities,
                    ref expiration
                    );
                }
                else
                {
                    retCode = InitializeSecurityContext(
                    ref _credHandle,
                    ref _contextHandle,
                    null,
                    ContextRequirements.AllocateMemory | ContextRequirements.ConnectionCommunications,
                    0,
                    NativeDataRepresentation,
                    ref inBuffDesc,
                    0,
                    ref _contextHandle,
                    ref outBuffDesc,
                    ref availableCapabilities,
                    ref expiration
                    );
                }

                if (retCode != NoError && retCode != ContinueNeeded)
                    throw new Win32Exception(retCode);

                var newClientBuff = (SecBuffer)Marshal.PtrToStructure(outBuffDesc.buffersPtr, typeof(SecBuffer));
                clientToken = new byte[newClientBuff.size];
                Marshal.Copy(newClientBuff.bufferPtr, clientToken, 0, (int)newClientBuff.size);
                FreeContextBuffer(newClientBuff.bufferPtr);

                try
                {
                    _contextExpiration = DateTime.FromFileTimeUtc((Int64)expiration);
                }
                catch (ArgumentException)
                {
                    // no expiration
                    _contextExpiration = DateTime.MaxValue;
                }

            }
            finally
            {
                pinnedClientBuffer.Free();
                pinnedServerBuffer.Free();
                pinnedServerToken.Free();
            }
            return clientToken;
        }

        public static SecurityPackageInfo[] EnumerateSecurityPackages()
        {
            UInt32 numOfPackges = 0;
            IntPtr packgeInfosPtr = IntPtr.Zero;
            int retCode = EnumerateSecurityPackagesW(ref numOfPackges, ref packgeInfosPtr);
            if (retCode != NoError)
            {
                throw new Win32Exception(retCode);
            }
            try
            {
                var infos = new SecurityPackageInfo[numOfPackges];
                var infoSize = Marshal.SizeOf(typeof(SecurityPackageInfo));
                var currentPtr = packgeInfosPtr;
                for(int i = 0; i < numOfPackges; i++)
                {
                    infos[i] = (SecurityPackageInfo)Marshal.PtrToStructure(currentPtr, typeof(SecurityPackageInfo));
                    currentPtr = IntPtr.Add(currentPtr, infoSize);
                }
                return infos;
            }
            finally
            {
                FreeContextBuffer(packgeInfosPtr);
            }
        }

        public DateTime TokenExpiration => _contextExpiration;

        [DllImport("secur32", CharSet = CharSet.Unicode)]
        private static extern int AcquireCredentialsHandle(
            string principal,
            string package,
            [MarshalAs(UnmanagedType.U4)]
            CredentialsUse credentialUse,
            IntPtr authenticationID,
            IntPtr authData,
            IntPtr getKeyFn,
            IntPtr getKeyArgument,
            ref SecurityHandle credential,
            ref UInt64 expiration
        );

        [DllImport("secur32", CharSet = CharSet.Unicode)]
        private static extern int InitializeSecurityContext(
            ref SecurityHandle credential,
            ref SecurityHandle context,
            string pszTargetName,
            [MarshalAs(UnmanagedType.U4)]
            ContextRequirements requirements,
            int Reserved1,
            int TargetDataRep,
            ref SecBufferDescription inBuffDesc,
            int Reserved2,
            ref SecurityHandle newContext,
            ref SecBufferDescription outBuffDesc,
            ref ContextRequirements contextAttributes,
            ref UInt64 expiration
        );

        [DllImport("secur32", CharSet = CharSet.Unicode)]
        private static extern int InitializeSecurityContext(
            ref SecurityHandle credential,
            IntPtr context,
            string pszTargetName,
            [MarshalAs(UnmanagedType.U4)]
            ContextRequirements requirements,
            int Reserved1,
            int TargetDataRep,
            IntPtr inBuffDesc,
            int Reserved2,
            ref SecurityHandle newContext,
            ref SecBufferDescription outBuffDesc,
            ref ContextRequirements contextAttributes,
            ref UInt64 expiration
        );

        [DllImport("secur32", CharSet = CharSet.Unicode)]
        private static extern int FreeCredentialsHandle(ref SecurityHandle credential);

        [DllImport("secur32", CharSet = CharSet.Unicode)]
        private static extern int DeleteSecurityContext(ref SecurityHandle context);

        [DllImport("secur32", CharSet = CharSet.Unicode)]
        private static extern int FreeContextBuffer(IntPtr buffer);

        [DllImport("secur32", CharSet = CharSet.Unicode)]
        private static extern int EnumerateSecurityPackagesW(
            ref UInt32 numOfPackages,
            ref IntPtr packageInfosPtr
        );

        public void Dispose()
        {
            FreeCredentialsHandle(ref _credHandle);
            DeleteSecurityContext(ref _contextHandle);
        }
    }



}
