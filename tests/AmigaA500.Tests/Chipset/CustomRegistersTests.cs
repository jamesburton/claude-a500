using AmigaA500.Core.Chipset;

namespace AmigaA500.Tests.Chipset;

public class CustomRegistersTests
{
    [Fact]
    public void DMACON_SetClr_SetBits()
    {
        var regs = new CustomRegisters();
        // Set bits: bit 15 (SET) + bit 9 (DMAEN) + bit 0 (AUD0EN)
        regs.WriteRegister(0x096, 0x8201);
        Assert.Equal(0x0201, regs.DMACON);
    }

    [Fact]
    public void DMACON_SetClr_ClearBits()
    {
        var regs = new CustomRegisters();
        regs.WriteRegister(0x096, 0x83FF); // Set all
        regs.WriteRegister(0x096, 0x0001); // Clear bit 0
        Assert.Equal(0x03FE, regs.DMACON);
    }

    [Fact]
    public void INTENA_SetClr()
    {
        var regs = new CustomRegisters();
        regs.WriteRegister(0x09A, 0xC020); // SET + INTEN + VERTB
        Assert.Equal(0x4020, regs.INTENA);
    }

    [Fact]
    public void INTREQ_TriggerInterrupt()
    {
        var regs = new CustomRegisters();
        int level = 0;
        regs.OnInterruptRequest = l => level = l;

        // Enable master + VERTB
        regs.WriteRegister(0x09A, 0xC020); // INTENA: SET + INTEN + VERTB
        regs.WriteRegister(0x09C, 0x8020); // INTREQ: SET + VERTB

        Assert.Equal(3, level); // VERTB = level 3
    }

    [Fact]
    public void InterruptLevel_Priority()
    {
        var regs = new CustomRegisters();
        int level = 0;
        regs.OnInterruptRequest = l => level = l;

        regs.WriteRegister(0x09A, 0xFFFF); // Enable all
        regs.WriteRegister(0x09C, 0xA000); // EXTER (level 6)
        Assert.Equal(6, level);
    }

    [Fact]
    public void ColorRegister_ReadWrite()
    {
        var regs = new CustomRegisters();
        regs.WriteRegister(0x180, 0x0F00); // COLOR00 = red
        Assert.Equal(0x0F00, regs.Color[0]);

        regs.WriteRegister(0x182, 0x00F0); // COLOR01 = green
        Assert.Equal(0x00F0, regs.Color[1]);
    }

    [Fact]
    public void BitplanePointer_HighLow()
    {
        var regs = new CustomRegisters();
        regs.WriteRegister(0x0E0, 0x0007); // BPL1PTH
        regs.WriteRegister(0x0E2, 0xFFF0); // BPL1PTL
        Assert.Equal(0x0007FFF0u, regs.BplPt[0]);
    }

    [Fact]
    public void CLXDAT_ReadAndClear()
    {
        var regs = new CustomRegisters();
        regs.CLXDAT = 0x1234;
        ushort val = regs.ReadRegister(0x00E);
        Assert.Equal(0x1234, val);
        Assert.Equal(0, regs.CLXDAT); // Auto-cleared
    }
}
