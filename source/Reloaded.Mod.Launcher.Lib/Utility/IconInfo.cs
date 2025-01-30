using System.Drawing;
using System.Globalization;

namespace Reloaded.Mod.Launcher.Lib.Utility;

/// <summary>
/// Utility class for extracting all icons from a file.
/// Author: Simon Mourier
/// Source: https://stackoverflow.com/a/77812338
/// CC BY-SA 4.0: https://creativecommons.org/licenses/by-sa/4.0/
/// </summary>
public sealed class IconInfo : IDisposable
{
    private IconInfo(int groupIndex, string groupId, int index, string id, IntPtr handle)
    {
        GroupIndex = groupIndex;
        GroupId = groupId;
        Index = index;
        Id = id;
        Handle = handle;
    }

    public IntPtr Handle { get; }
    public int Index { get; }
    public int GroupIndex { get; }
    public string Id { get; }
    public string GroupId { get; }

    public void WithIcon(Action<Icon> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        using var icon = Icon.FromHandle(Handle);
        action(icon);
    }

    public T WithIcon<T>(Func<Icon, T> func)
    {
        if (func == null)
            throw new ArgumentNullException(nameof(func));

        using var icon = Icon.FromHandle(Handle);
        return func(icon);
    }

    public override string ToString() => Index.ToString();
    public void Dispose() => DestroyIcon(Handle);

    public static List<IconInfo> LoadIconsFromBinary(string iconFilePath, int? byIndexOrResourceId = null)
    {
        if (iconFilePath == null)
            throw new ArgumentNullException(nameof(iconFilePath));

        var list = new List<IconInfo>();
        var handle = LoadLibraryEx(iconFilePath, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE | LOAD_LIBRARY_AS_IMAGE_RESOURCE);
        if (handle != IntPtr.Zero)
        {
            try
            {
                list = LoadIconsFromBinary(handle, byIndexOrResourceId);
            }
            finally
            {
                FreeLibrary(handle);
            }
        }
        return list;
    }

    private static List<IconInfo> LoadIconsFromBinary(IntPtr handle, int? byIndexOrResourceId)
    {
        var list = new List<IconInfo>();
        var entries = new Dictionary<ushort, GRPICONDIRENTRY>();
        var groupIndices = new Dictionary<ushort, int>();
        var groupIds = new Dictionary<ushort, string>();
        var groupIndex = 0;
        if (EnumResourceNames(handle, new IntPtr(RT_GROUP_ICON), (m, t, n, lp) =>
        {
            if (byIndexOrResourceId.HasValue && byIndexOrResourceId.Value >= 0 && byIndexOrResourceId.Value != groupIndex)
            {
                groupIndex++;
                return true;
            }

            string name;
            if (n.ToInt64() > ushort.MaxValue)
            {
                name = Marshal.PtrToStringAuto(n);
            }
            else
            {
                name = n.ToInt32().ToString(CultureInfo.InvariantCulture);
            }

            if (byIndexOrResourceId.HasValue && byIndexOrResourceId.Value < 0 && !string.Equals((-byIndexOrResourceId.Value).ToString(CultureInfo.InvariantCulture), name, StringComparison.Ordinal))
            {
                groupIndex++;
                return true;
            }

            try
            {
                ExtractIconGroupEntries(handle, n, t, groupIndex, entries, groupIndices, groupIds);
                groupIndex++;
            }
            catch
            {
                // do nothing
            }
            return true;
        }, IntPtr.Zero))
        {
            EnumResourceNames(handle, new IntPtr(RT_ICON), (m, t, n, lp) =>
            {
                var iconHandle = ExtractIcon(handle, n, t, entries);
                if (iconHandle != IntPtr.Zero)
                {
                    var info = new IconInfo(groupIndices[(ushort)n.ToInt32()], groupIds[(ushort)n.ToInt32()], n.ToInt32() - 1, n.ToString(), iconHandle);
                    list.Add(info);
                }
                return true;
            }, IntPtr.Zero);
        }
        return list;
    }

    private static void ExtractIconGroupEntries(IntPtr module, IntPtr name, IntPtr type, int index, Dictionary<ushort, GRPICONDIRENTRY> entries, Dictionary<ushort, int> groupIndices, Dictionary<ushort, string> groupIds)
    {
        var handle = FindResource(module, name, type);
        if (handle == IntPtr.Zero)
            return;

        var size = SizeofResource(module, handle);
        if (size == 0)
            return;

        var resource = LoadResource(module, handle);
        if (resource == IntPtr.Zero)
            return;

        var ptr = LockResource(resource);
        if (ptr == IntPtr.Zero)
            return;

        // GRPICONDIR
        ptr += 2; // idReserved;
        var idtype = Marshal.ReadInt16(ptr);
        if (idtype != 1)  // idType, 1 for ICO
            return;

        var elementSize = Marshal.SizeOf<GRPICONDIRENTRY>();

        ptr += 2;
        var count = Marshal.ReadInt16(ptr);
        ptr += 2;
        for (var i = 0; i < count; i++)
        {
            var entry = Marshal.PtrToStructure<GRPICONDIRENTRY>(ptr);
            ptr += elementSize;
            entries[entry.nId] = entry;

            // is it a string or an id?
            groupIndices[entry.nId] = index;
            if (name.ToInt64() > ushort.MaxValue)
            {
                var id = Marshal.PtrToStringAuto(name);
                groupIds[entry.nId] = id;
            }
            else
            {
                groupIds[entry.nId] = "#" + name.ToInt32();
            }
        }
    }

    private static IntPtr ExtractIcon(IntPtr module, IntPtr name, IntPtr type, Dictionary<ushort, GRPICONDIRENTRY> entries)
    {
        if (!entries.TryGetValue((ushort)name.ToInt32(), out _))
            return IntPtr.Zero;

        var hres = FindResource(module, name, type);
        if (hres == IntPtr.Zero)
            return IntPtr.Zero;

        var size = SizeofResource(module, hres);
        if (size == 0)
            return IntPtr.Zero;

        var res = LoadResource(module, hres);
        if (res == IntPtr.Zero)
            return IntPtr.Zero;

        var ptr = LockResource(res);
        if (ptr == IntPtr.Zero)
            return IntPtr.Zero;

        return CreateIconFromResourceEx(ptr, size, true, 0x30000, 0, 0, 0);
    }

    private delegate bool EnumResNameProc(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam);

    [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool EnumResourceNames(IntPtr hModule, IntPtr lpszType, EnumResNameProc lpEnumFunc, IntPtr lParam);

    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindResource(IntPtr hModule, IntPtr lpName, IntPtr lpType);

    [DllImport("kernel32")]
    private static extern int SizeofResource(IntPtr hModule, IntPtr hResInfo);

    [DllImport("kernel32")]
    private static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

    [DllImport("user32")]
    private static extern IntPtr CreateIconFromResourceEx(IntPtr presbits, int dwResSize, bool fIcon, int dwVer, int cxDesired, int cyDesired, int flags);

    [DllImport("user32")]
    private static extern bool DestroyIcon(IntPtr handle);

    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    private static extern IntPtr LockResource(IntPtr hResData);

    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, int dwFlags);

    [DllImport("kernel32")]
    private static extern bool FreeLibrary(IntPtr hModule);

    private const int LOAD_LIBRARY_AS_DATAFILE = 0x2;
    private const int LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x20;
    private const int RT_ICON = 3;
    private const int RT_GROUP_ICON = RT_ICON + 11;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct GRPICONDIRENTRY
    {
        public byte bWidth;
        public byte bHeight;
        public byte bColorCount;
        public byte bReserved;
        public short wPlanes;
        public short wBitCount;
        public int dwBytesInRes;
        public ushort nId;
    }
}