using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

internal static class UniversalAchievementWorker
{
    private static bool English;

    private static string Msg(string vietnamese, string english)
    {
        return English ? english : vietnamese;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string fileName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr module, string procedureName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr module);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool SteamApiInit();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SteamApiInitFlat(IntPtr errorMessage);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SteamApiShutdown();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SteamApiRunCallbacks();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr SteamClient();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SteamGetHandle();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private delegate IntPtr GetUserStatsInterface(
        IntPtr client,
        int steamUser,
        int steamPipe,
        [MarshalAs(UnmanagedType.LPStr)] string version);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
    private delegate IntPtr GetUserStatsInterfaceMethod(
        IntPtr client,
        int steamUser,
        int steamPipe,
        [MarshalAs(UnmanagedType.LPStr)] string version);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool RequestCurrentStats(IntPtr userStats);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool RequestCurrentStatsMethod(IntPtr userStats);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate uint GetNumAchievements(IntPtr userStats);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate uint GetNumAchievementsMethod(IntPtr userStats);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GetAchievementName(IntPtr userStats, uint index);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr GetAchievementNameMethod(IntPtr userStats, uint index);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool GetAchievement(
        IntPtr userStats,
        [MarshalAs(UnmanagedType.LPStr)] string achievementId,
        [MarshalAs(UnmanagedType.I1)] out bool achieved);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool GetAchievementAndUnlockTime(IntPtr userStats,
        [MarshalAs(UnmanagedType.LPStr)] string achievementId,
        [MarshalAs(UnmanagedType.I1)] out bool achieved, out uint unlockTime);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool GetAchievementMethod(
        IntPtr userStats,
        [MarshalAs(UnmanagedType.LPStr)] string achievementId,
        [MarshalAs(UnmanagedType.I1)] out bool achieved);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private delegate IntPtr GetAchievementDisplayAttribute(
        IntPtr userStats,
        [MarshalAs(UnmanagedType.LPStr)] string achievementId,
        [MarshalAs(UnmanagedType.LPStr)] string key);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
    private delegate IntPtr GetAchievementDisplayAttributeMethod(
        IntPtr userStats,
        [MarshalAs(UnmanagedType.LPStr)] string achievementId,
        [MarshalAs(UnmanagedType.LPStr)] string key);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool SetAchievement(IntPtr userStats, [MarshalAs(UnmanagedType.LPStr)] string achievementId);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool SetAchievementMethod(IntPtr userStats, [MarshalAs(UnmanagedType.LPStr)] string achievementId);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool StoreStats(IntPtr userStats);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool StoreStatsMethod(IntPtr userStats);

    private static T LoadFunction<T>(IntPtr module, string name) where T : class
    {
        T function = TryLoadFunction<T>(module, name);
        if (function == null)
            throw new InvalidOperationException(Msg("Thiếu Steam API export: ", "Missing Steam API export: ") + name);
        return function;
    }

    private static T TryLoadFunction<T>(IntPtr module, string name) where T : class
    {
        IntPtr address = GetProcAddress(module, name);
        if (address == IntPtr.Zero)
            return null;
        return Marshal.GetDelegateForFunctionPointer(address, typeof(T)) as T;
    }

    private static T LoadVTableFunction<T>(IntPtr instance, int index) where T : class
    {
        if (instance == IntPtr.Zero)
            throw new InvalidOperationException(Msg("Steam interface rỗng khi đọc vtable.", "Steam returned a null interface while reading the vtable."));
        IntPtr vtable = Marshal.ReadIntPtr(instance);
        IntPtr address = Marshal.ReadIntPtr(vtable, index * IntPtr.Size);
        if (address == IntPtr.Zero)
            throw new InvalidOperationException(Msg("Thiếu vtable method index ", "Missing vtable method index ") + index + ".");
        return Marshal.GetDelegateForFunctionPointer(address, typeof(T)) as T;
    }

    private static void PumpCallbacks(SteamApiRunCallbacks runCallbacks, int milliseconds)
    {
        int elapsed = 0;
        while (elapsed < milliseconds)
        {
            runCallbacks();
            Thread.Sleep(100);
            elapsed += 100;
        }
    }

    private static IEnumerable<string> InterfaceCandidates(string requested)
    {
        HashSet<string> emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(requested)
            && !string.Equals(requested, "AUTO", StringComparison.OrdinalIgnoreCase)
            && emitted.Add(requested))
            yield return requested;

        // v012 is the safest common flat-API ABI. Some third-party overlay DLLs
        // return a non-null but invalid pointer for newer version strings, so AUTO
        // intentionally probes the stable backwards-compatible range first.
        for (int version = 12; version >= 1; version--)
        {
            string candidate = "STEAMUSERSTATS_INTERFACE_VERSION" + version.ToString("000");
            if (emitted.Add(candidate))
                yield return candidate;
        }
    }

    private static bool InitializeSteam(
        SteamApiInit init,
        SteamApiInit initSafe,
        SteamApiInitFlat initFlat,
        out string errorMessage)
    {
        errorMessage = string.Empty;
        if (init != null)
            return init();
        if (initSafe != null)
            return initSafe();

        IntPtr errorBuffer = Marshal.AllocHGlobal(1024);
        try
        {
            for (int index = 0; index < 1024; index++)
                Marshal.WriteByte(errorBuffer, index, 0);
            int result = initFlat(errorBuffer);
            errorMessage = Marshal.PtrToStringAnsi(errorBuffer) ?? string.Empty;
            return result == 0;
        }
        finally
        {
            Marshal.FreeHGlobal(errorBuffer);
        }
    }

    private static string PtrToUtf8(IntPtr pointer)
    {
        if (pointer == IntPtr.Zero)
            return string.Empty;

        int length = 0;
        while (length < 65536 && Marshal.ReadByte(pointer, length) != 0)
            length++;
        if (length == 0)
            return string.Empty;

        byte[] bytes = new byte[length];
        Marshal.Copy(pointer, bytes, 0, length);
        return Encoding.UTF8.GetString(bytes);
    }

    private static string EncodeField(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
    }

    private static HashSet<string> LoadSelectedAchievements(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            throw new InvalidOperationException(Msg("Thiếu file danh sách achievement đã chọn.", "The selected-achievement file is missing."));

        HashSet<string> selected = new HashSet<string>(StringComparer.Ordinal);
        foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            selected.Add(Encoding.UTF8.GetString(Convert.FromBase64String(line.Trim())));
        }
        if (selected.Count == 0)
            throw new InvalidOperationException(Msg("Chưa có achievement nào được chọn.", "No achievements were selected."));
        return selected;
    }

    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (args.Length < 6 || args.Length > 8)
        {
            Console.Error.WriteLine("Worker arguments không hợp lệ.");
            return 10;
        }

        string appId = args[0];
        string gameName = args[1];
        string dllPath = args[2];
        string interfaceVersion = args[3];
        string gameRoot = args[4];
        English = string.Equals(args[5], "en", StringComparison.OrdinalIgnoreCase);
        string mode = args.Length >= 7 ? args[6] : "all";
        bool probeOnly = string.Equals(mode, "probe", StringComparison.OrdinalIgnoreCase);
        bool listOnly = string.Equals(mode, "list", StringComparison.OrdinalIgnoreCase);
        bool selectedOnly = string.Equals(mode, "selected", StringComparison.OrdinalIgnoreCase);
        if (!probeOnly && !listOnly && !selectedOnly && !string.Equals(mode, "all", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine(Msg("Chế độ worker không hợp lệ: ", "Invalid worker mode: ") + mode);
            return 10;
        }
        HashSet<string> selectedAchievements = selectedOnly
            ? LoadSelectedAchievements(args.Length >= 8 ? args[7] : string.Empty)
            : null;

        if (!File.Exists(dllPath))
        {
            Console.Error.WriteLine(Msg("Không tìm thấy Steam API DLL: ", "Steam API DLL not found: ") + dllPath);
            return 11;
        }

        Environment.SetEnvironmentVariable("SteamAppId", appId);
        Environment.SetEnvironmentVariable("SteamGameId", appId);
        Directory.SetCurrentDirectory(gameRoot);

        Console.WriteLine("Game: " + gameName);
        Console.WriteLine("AppID: " + appId);
        Console.WriteLine("DLL: " + dllPath);
        Console.WriteLine("Interface: " + interfaceVersion);

        IntPtr module = IntPtr.Zero;
        SteamApiShutdown shutdown = null;
        bool initialized = false;
        try
        {
            module = LoadLibrary(dllPath);
            if (module == IntPtr.Zero)
                throw new InvalidOperationException(Msg("Không thể nạp DLL. Win32 error: ", "Could not load the DLL. Win32 error: ") + Marshal.GetLastWin32Error());

            SteamApiInit init = TryLoadFunction<SteamApiInit>(module, "SteamAPI_Init");
            SteamApiInit initSafe = TryLoadFunction<SteamApiInit>(module, "SteamAPI_InitSafe");
            SteamApiInitFlat initFlat = TryLoadFunction<SteamApiInitFlat>(module, "SteamAPI_InitFlat");
            if (init == null && initSafe == null && initFlat == null)
                throw new InvalidOperationException(Msg("DLL không có hàm khởi tạo Steam API được hỗ trợ.", "The DLL has no supported Steam API initialization function."));

            shutdown = LoadFunction<SteamApiShutdown>(module, "SteamAPI_Shutdown");
            SteamApiRunCallbacks runCallbacks = LoadFunction<SteamApiRunCallbacks>(module, "SteamAPI_RunCallbacks");
            SteamClient getClient = LoadFunction<SteamClient>(module, "SteamClient");
            SteamGetHandle getUser = LoadFunction<SteamGetHandle>(module, "SteamAPI_GetHSteamUser");
            SteamGetHandle getPipe = LoadFunction<SteamGetHandle>(module, "SteamAPI_GetHSteamPipe");

            GetUserStatsInterface getUserStats = TryLoadFunction<GetUserStatsInterface>(module, "SteamAPI_ISteamClient_GetISteamUserStats");
            RequestCurrentStats requestStats = TryLoadFunction<RequestCurrentStats>(module, "SteamAPI_ISteamUserStats_RequestCurrentStats");
            GetNumAchievements getNumAchievements = TryLoadFunction<GetNumAchievements>(module, "SteamAPI_ISteamUserStats_GetNumAchievements");
            GetAchievementName getAchievementName = TryLoadFunction<GetAchievementName>(module, "SteamAPI_ISteamUserStats_GetAchievementName");
            GetAchievement getAchievement = TryLoadFunction<GetAchievement>(module, "SteamAPI_ISteamUserStats_GetAchievement");
            GetAchievementAndUnlockTime getUnlockTime = TryLoadFunction<GetAchievementAndUnlockTime>(module, "SteamAPI_ISteamUserStats_GetAchievementAndUnlockTime");
            GetAchievementDisplayAttribute getDisplayAttribute = TryLoadFunction<GetAchievementDisplayAttribute>(module, "SteamAPI_ISteamUserStats_GetAchievementDisplayAttribute");
            SetAchievement setAchievement = TryLoadFunction<SetAchievement>(module, "SteamAPI_ISteamUserStats_SetAchievement");
            StoreStats storeStats = TryLoadFunction<StoreStats>(module, "SteamAPI_ISteamUserStats_StoreStats");

            string initError;
            initialized = InitializeSteam(init, initSafe, initFlat, out initError);
            if (!initialized)
                throw new InvalidOperationException(Msg("Steam API init thất bại. ", "Steam API initialization failed. ") + initError);

            IntPtr client = getClient();
            int user = getUser();
            int pipe = getPipe();
            if (client == IntPtr.Zero || user == 0 || pipe == 0)
                throw new InvalidOperationException(Msg("Steam không trả về phiên đăng nhập hợp lệ.", "Steam did not return a valid signed-in session."));

            bool legacyVTable = getUserStats == null;
            GetUserStatsInterfaceMethod legacyGetUserStats = legacyVTable
                ? LoadVTableFunction<GetUserStatsInterfaceMethod>(client, 13)
                : null;
            IntPtr userStats = IntPtr.Zero;
            string resolvedInterface = string.Empty;
            foreach (string candidate in InterfaceCandidates(interfaceVersion))
            {
                userStats = !legacyVTable
                    ? getUserStats(client, user, pipe, candidate)
                    : legacyGetUserStats(client, user, pipe, candidate);
                if (userStats != IntPtr.Zero)
                {
                    resolvedInterface = candidate;
                    break;
                }
            }

            if (userStats == IntPtr.Zero)
                throw new InvalidOperationException(Msg("Không lấy được Steam User Stats interface (yêu cầu: ", "Could not acquire a Steam User Stats interface (requested: ") + interfaceVersion + ").");
            Console.WriteLine("RESOLVED_INTERFACE=" + resolvedInterface);

            RequestCurrentStatsMethod requestMethod = legacyVTable
                ? LoadVTableFunction<RequestCurrentStatsMethod>(userStats, 0)
                : null;
            GetNumAchievementsMethod getCountMethod = getNumAchievements == null
                ? LoadVTableFunction<GetNumAchievementsMethod>(userStats, 14)
                : null;
            GetAchievementNameMethod getNameMethod = getAchievementName == null
                ? LoadVTableFunction<GetAchievementNameMethod>(userStats, 15)
                : null;
            GetAchievementMethod getAchievementMethod = getAchievement == null
                ? LoadVTableFunction<GetAchievementMethod>(userStats, 6)
                : null;
            GetAchievementDisplayAttributeMethod getDisplayAttributeMethod = getDisplayAttribute == null
                ? LoadVTableFunction<GetAchievementDisplayAttributeMethod>(userStats, 12)
                : null;
            SetAchievementMethod setMethod = setAchievement == null
                ? LoadVTableFunction<SetAchievementMethod>(userStats, 7)
                : null;
            StoreStatsMethod storeMethod = storeStats == null
                ? LoadVTableFunction<StoreStatsMethod>(userStats, 10)
                : null;

            bool requested = requestStats != null
                ? requestStats(userStats)
                : (legacyVTable ? requestMethod(userStats) : true);
            if (!requested)
                throw new InvalidOperationException(Msg("Steam từ chối tải User Stats.", "Steam rejected the User Stats request."));

            Console.WriteLine(Msg("Đang tải schema achievement từ Steam...", "Loading the achievement schema from Steam..."));
            uint count = 0;
            for (int attempt = 0; attempt < 15 && count == 0; attempt++)
            {
                PumpCallbacks(runCallbacks, 1000);
                count = getNumAchievements != null
                    ? getNumAchievements(userStats)
                    : getCountMethod(userStats);
            }
            if (count == 0 || count > 10000)
                throw new InvalidOperationException(Msg("Số achievement không hợp lệ: ", "Invalid achievement count: ") + count);

            Console.WriteLine("DISCOVERED_COUNT=" + count);
            if (probeOnly)
            {
                Console.WriteLine("RESULT=PROBE_SUCCESS");
                return 0;
            }

            if (listOnly)
            {
                for (uint index = 0; index < count; index++)
                {
                    IntPtr pointer = getAchievementName != null
                        ? getAchievementName(userStats, index)
                        : getNameMethod(userStats, index);
                    string achievementId = PtrToUtf8(pointer);
                    if (string.IsNullOrEmpty(achievementId))
                        continue;

                    bool achieved;
                    bool stateOk = getAchievement != null
                        ? getAchievement(userStats, achievementId, out achieved)
                        : getAchievementMethod(userStats, achievementId, out achieved);
                    if (!stateOk)
                        achieved = false;

                    IntPtr displayNamePointer = getDisplayAttribute != null
                        ? getDisplayAttribute(userStats, achievementId, "name")
                        : getDisplayAttributeMethod(userStats, achievementId, "name");
                    IntPtr descriptionPointer = getDisplayAttribute != null
                        ? getDisplayAttribute(userStats, achievementId, "desc")
                        : getDisplayAttributeMethod(userStats, achievementId, "desc");
                    string displayName = PtrToUtf8(displayNamePointer);
                    string description = PtrToUtf8(descriptionPointer);
                    if (string.IsNullOrEmpty(displayName))
                        displayName = achievementId;

                    uint unlockSeconds = 0;
                    if (achieved && getUnlockTime != null)
                    {
                        bool timeState;
                        uint stamp;
                        if (getUnlockTime(userStats, achievementId, out timeState, out stamp) && timeState)
                            unlockSeconds = stamp;
                    }
                    Console.WriteLine(
                        "ACHIEVEMENT=" + index + "|" + (achieved ? "1" : "0") + "|"
                        + EncodeField(achievementId) + "|" + EncodeField(displayName) + "|"
                        + EncodeField(description) + "|" + unlockSeconds);
                }
                Console.WriteLine("RESULT=LIST_SUCCESS");
                return 0;
            }

            int accepted = 0;
            List<string> targetAchievementIds = new List<string>();
            int targetCount = selectedOnly ? selectedAchievements.Count : (int)count;
            for (uint index = 0; index < count; index++)
            {
                IntPtr pointer = getAchievementName != null
                    ? getAchievementName(userStats, index)
                    : getNameMethod(userStats, index);
                string achievementId = PtrToUtf8(pointer);
                if (string.IsNullOrEmpty(achievementId))
                {
                    Console.WriteLine("[FAIL] index " + index + Msg(" không có tên", " has no API name"));
                    continue;
                }

                if (selectedOnly && !selectedAchievements.Contains(achievementId))
                    continue;

                targetAchievementIds.Add(achievementId);

                bool ok = setAchievement != null
                    ? setAchievement(userStats, achievementId)
                    : setMethod(userStats, achievementId);
                Console.WriteLine((ok ? "[OK]   " : "[FAIL] ") + achievementId);
                if (ok)
                    accepted++;
            }

            bool stored = storeStats != null
                ? storeStats(userStats)
                : storeMethod(userStats);
            if (!stored)
                throw new InvalidOperationException(Msg("Steam từ chối StoreStats.", "Steam rejected StoreStats."));

            Console.WriteLine("SELECTED_COUNT=" + targetCount);
            Console.WriteLine(Msg("Đã gửi ", "Submitted ") + accepted + "/" + targetCount + Msg(". Đang chờ Steam lưu...", ". Waiting for Steam to persist the changes..."));
            PumpCallbacks(runCallbacks, 7000);

            bool refreshed = requestStats != null
                ? requestStats(userStats)
                : (legacyVTable ? requestMethod(userStats) : true);
            if (refreshed)
                PumpCallbacks(runCallbacks, 3000);

            int verified = 0;
            foreach (string achievementId in targetAchievementIds)
            {
                bool achieved;
                bool stateOk = getAchievement != null
                    ? getAchievement(userStats, achievementId, out achieved)
                    : getAchievementMethod(userStats, achievementId, out achieved);
                if (stateOk && achieved)
                    verified++;
                else
                    Console.WriteLine("[UNVERIFIED] " + achievementId);
            }
            Console.WriteLine("VERIFIED_COUNT=" + verified);
            bool success = accepted == targetCount && verified == targetCount;
            Console.WriteLine("RESULT=" + (success ? "SUCCESS" : "PARTIAL"));
            return success ? 0 : 2;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(Msg("LỖI: ", "ERROR: ") + error.Message);
            Console.WriteLine("RESULT=ERROR");
            return 1;
        }
        finally
        {
            if (initialized && shutdown != null)
            {
                try { shutdown(); }
                catch { }
            }
            if (module != IntPtr.Zero)
                FreeLibrary(module);
        }
    }
}
