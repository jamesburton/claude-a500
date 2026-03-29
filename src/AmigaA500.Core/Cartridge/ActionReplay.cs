namespace AmigaA500.Core.Cartridge;

/// <summary>
/// Action Replay cartridge emulation.
/// Maps ROM at $F00000-$F7FFFF, provides freeze button (NMI), and menu system.
/// Supports Action Replay Mk I, II, III and compatible ROMs.
/// </summary>
public sealed class ActionReplay
{
    private byte[]? _romData;
    private bool _enabled;
    private bool _freezeRequested;
    private bool _menuActive;
    private ActionReplayVersion _version;

    // ROM mapping
    public const uint RomBase = 0xF00000;
    public const uint RomEnd = 0xF80000;
    public const int MaxRomSize = 512 * 1024; // 512KB max

    // Status register at $BFD100 area (directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly directly)
    private byte _statusRegister;

    public bool Enabled => _enabled;
    public bool MenuActive => _menuActive;
    public bool FreezeRequested => _freezeRequested;
    public ActionReplayVersion Version => _version;
    public byte[]? RomData => _romData;

    /// <summary>
    /// Load an Action Replay ROM from file.
    /// Supports 256KB (Mk I/II) and 512KB (Mk III) ROMs.
    /// </summary>
    public bool LoadRom(string path)
    {
        if (!File.Exists(path)) return false;

        var data = File.ReadAllBytes(path);
        return LoadRom(data);
    }

    /// <summary>
    /// Load an Action Replay ROM from byte array.
    /// </summary>
    public bool LoadRom(byte[] data)
    {
        if (data.Length != 262144 && data.Length != 524288 && data.Length != 65536)
            return false;

        _romData = data;
        _enabled = true;
        _version = DetectVersion(data);
        return true;
    }

    /// <summary>
    /// Read a byte from the Action Replay ROM space.
    /// </summary>
    public byte ReadByte(uint address)
    {
        if (_romData == null || !_enabled) return 0xFF;
        uint offset = (address - RomBase) % (uint)_romData.Length;
        return _romData[offset];
    }

    /// <summary>
    /// Read a word from the Action Replay ROM space.
    /// </summary>
    public ushort ReadWord(uint address)
    {
        return (ushort)(ReadByte(address) << 8 | ReadByte(address + 1));
    }

    /// <summary>
    /// Trigger the freeze button — sends NMI (level 7) to the 68000.
    /// The Action Replay ROM's NMI handler saves machine state and shows the menu.
    /// </summary>
    public void Freeze()
    {
        if (!_enabled || _romData == null) return;
        _freezeRequested = true;
        _menuActive = true;
    }

    /// <summary>
    /// Check and clear freeze request. Called by the emulator's interrupt handler.
    /// Returns true if an NMI should be raised.
    /// </summary>
    public bool CheckAndClearFreeze()
    {
        if (!_freezeRequested) return false;
        _freezeRequested = false;
        return true;
    }

    /// <summary>
    /// Resume execution from freeze menu.
    /// </summary>
    public void Resume()
    {
        _menuActive = false;
    }

    /// <summary>
    /// Write to Action Replay status/control register.
    /// </summary>
    public void WriteStatus(byte value)
    {
        _statusRegister = value;
        // Bit 0: enable/disable cartridge ROM mapping
        _enabled = (value & 0x01) != 0;
    }

    /// <summary>
    /// Check if an address is in the Action Replay ROM range.
    /// </summary>
    public bool HandlesAddress(uint address)
    {
        return _enabled && _romData != null && address >= RomBase && address < RomEnd;
    }

    /// <summary>
    /// Detect Action Replay version from ROM contents.
    /// </summary>
    private static ActionReplayVersion DetectVersion(byte[] rom)
    {
        if (rom.Length <= 0) return ActionReplayVersion.Unknown;

        // Check for version signatures in ROM
        string romStart = System.Text.Encoding.ASCII.GetString(
            rom, Math.Min(16, rom.Length - 1), Math.Min(64, rom.Length - 16));

        if (rom.Length == 65536) return ActionReplayVersion.MkI;
        if (rom.Length == 262144)
        {
            // Search for version strings
            for (int i = 0; i < rom.Length - 20; i++)
            {
                if (rom[i] == 'M' && rom[i + 1] == 'k' && i + 5 < rom.Length)
                {
                    if (rom[i + 3] == 'I' && rom[i + 4] == 'I' && rom[i + 5] == 'I')
                        return ActionReplayVersion.MkIII;
                    if (rom[i + 3] == 'I' && rom[i + 4] == 'I')
                        return ActionReplayVersion.MkII;
                    if (rom[i + 3] == 'I')
                        return ActionReplayVersion.MkI;
                }
                // Check for version numbers
                if (rom[i] == 'v' || rom[i] == 'V')
                {
                    if (i + 2 < rom.Length && rom[i + 1] == '3')
                        return ActionReplayVersion.MkIII;
                    if (i + 2 < rom.Length && rom[i + 1] == '2')
                        return ActionReplayVersion.MkII;
                }
            }
            return ActionReplayVersion.MkII; // Default for 256KB
        }
        if (rom.Length == 524288) return ActionReplayVersion.MkIII;

        return ActionReplayVersion.Unknown;
    }

    public void Reset()
    {
        _freezeRequested = false;
        _menuActive = false;
        _enabled = _romData != null;
        _statusRegister = 0;
    }
}

public enum ActionReplayVersion
{
    Unknown,
    MkI,
    MkII,
    MkIII,
    SuperIV
}
