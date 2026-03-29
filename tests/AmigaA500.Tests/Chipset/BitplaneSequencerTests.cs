using AmigaA500.Core.Chipset;

namespace AmigaA500.Tests.Chipset;

public class BitplaneSequencerTests
{
    [Fact]
    public void SingleBitplane_ShiftsCorrectly()
    {
        var seq = new BitplaneSequencer { NumBitplanes = 1 };
        seq.LoadData(0, 0xAAAA); // 1010101010101010
        seq.TransferToShift();

        Assert.Equal(1, seq.ShiftPixel()); // bit 15 = 1
        Assert.Equal(0, seq.ShiftPixel()); // bit 14 = 0
        Assert.Equal(1, seq.ShiftPixel()); // bit 13 = 1
        Assert.Equal(0, seq.ShiftPixel()); // bit 12 = 0
    }

    [Fact]
    public void TwoBitplanes_CombinesIndices()
    {
        var seq = new BitplaneSequencer { NumBitplanes = 2 };
        seq.LoadData(0, 0xFFFF); // All 1s
        seq.LoadData(1, 0x0000); // All 0s
        seq.TransferToShift();

        Assert.Equal(1, seq.ShiftPixel()); // bpl0=1, bpl1=0 → index 1
    }

    [Fact]
    public void TwoBitplanes_Index3()
    {
        var seq = new BitplaneSequencer { NumBitplanes = 2 };
        seq.LoadData(0, 0xFFFF);
        seq.LoadData(1, 0xFFFF);
        seq.TransferToShift();

        Assert.Equal(3, seq.ShiftPixel()); // bpl0=1, bpl1=1 → index 3
    }

    [Fact]
    public void FiveBitplanes_Index31()
    {
        var seq = new BitplaneSequencer { NumBitplanes = 5 };
        for (int i = 0; i < 5; i++)
            seq.LoadData(i, 0xFFFF);
        seq.TransferToShift();

        Assert.Equal(31, seq.ShiftPixel()); // All bits set → 11111 = 31
    }

    [Fact]
    public void Reset_ClearsState()
    {
        var seq = new BitplaneSequencer { NumBitplanes = 1 };
        seq.LoadData(0, 0xFFFF);
        seq.TransferToShift();
        seq.Reset();

        Assert.Equal(0, seq.ShiftPixel()); // All cleared
    }

    [Fact]
    public void PendingData_NotVisibleUntilTransfer()
    {
        var seq = new BitplaneSequencer { NumBitplanes = 1 };
        seq.LoadData(0, 0xFFFF);
        // Don't call TransferToShift

        Assert.Equal(0, seq.ShiftPixel()); // Shift reg still 0
    }
}
