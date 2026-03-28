using System.Text.Json;

namespace AmigaA500.Core;

/// <summary>
/// Save/load emulator state for snapshots.
/// </summary>
public static class SaveState
{
    public record CpuState(uint[] D, uint[] A, uint PC, ushort SR, uint USP, uint SSP, bool Halted);

    public record AmigaState(
        CpuState Cpu,
        bool Overlay,
        int HPos, int VPos,
        ushort DMACON, ushort INTENA, ushort INTREQ);

    public static string Serialize(Amiga amiga)
    {
        var state = new AmigaState(
            new CpuState(
                (uint[])amiga.Cpu.D.Clone(),
                (uint[])amiga.Cpu.A.Clone(),
                amiga.Cpu.PC,
                amiga.Cpu.SR,
                0, 0, // USP/SSP not directly accessible
                amiga.Cpu.Halted),
            amiga.Bus.Overlay,
            0, 0, // Beam position
            amiga.Custom.DMACON,
            amiga.Custom.INTENA,
            amiga.Custom.INTREQ);

        return JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
    }

    public static void SaveToFile(Amiga amiga, string path)
    {
        File.WriteAllText(path, Serialize(amiga));
    }
}
