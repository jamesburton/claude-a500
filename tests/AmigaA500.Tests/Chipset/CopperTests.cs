using AmigaA500.Core.Chipset;

namespace AmigaA500.Tests.Chipset;

public class CopperTests
{
    private readonly byte[] _memory = new byte[65536];
    private readonly CustomRegisters _regs = new();

    private ushort DmaRead(uint addr) => (ushort)(_memory[addr] << 8 | _memory[addr + 1]);
    private void DmaWrite(uint addr, ushort val) { _memory[addr] = (byte)(val >> 8); _memory[addr + 1] = (byte)val; }

    [Fact]
    public void Move_WritesToRegister()
    {
        var copper = new Copper(_regs, DmaRead);
        copper.Enabled = true;

        // Copper list: MOVE $180, $0F00 (set COLOR00 to red)
        DmaWrite(0x1000, 0x0180); // Register address
        DmaWrite(0x1002, 0x0F00); // Value (red)
        // End: WAIT $FFFF,$FFFE (wait forever)
        DmaWrite(0x1004, 0xFFFF);
        DmaWrite(0x1006, 0xFFFE);

        copper.COP1LC = 0x1000;
        copper.RestartFromList1();

        // Execute: fetch IR1, fetch IR2, execute MOVE
        copper.ExecuteCycle(0, 0);  // Fetch first word
        copper.ExecuteCycle(0, 0);  // Fetch second word + execute

        Assert.Equal(0x0F00, _regs.Color[0]);
    }

    [Fact]
    public void Wait_BlocksUntilBeamPosition()
    {
        var copper = new Copper(_regs, DmaRead);
        copper.Enabled = true;

        // WAIT for line 100, position 0
        DmaWrite(0x1000, 0x6401); // VP=$64 (100), HP=$00, bit0=1 (WAIT)
        DmaWrite(0x1002, 0xFFFE); // Full VP/HP mask, BFD=0

        copper.COP1LC = 0x1000;
        copper.RestartFromList1();

        copper.ExecuteCycle(0, 0);   // Fetch IR1
        copper.ExecuteCycle(0, 0);   // Fetch IR2 + evaluate WAIT

        Assert.True(copper.IsWaiting); // Beam at line 0 — should wait

        copper.ExecuteCycle(0, 100);  // Now at line 100
        Assert.False(copper.IsWaiting); // Should proceed
    }

    [Fact]
    public void DangerBit_ProtectsLowRegisters()
    {
        var copper = new Copper(_regs, DmaRead);
        copper.Enabled = true;
        copper.CopperDanger = false;

        // Try to write to register $020 (DSKPTH) — below $040
        DmaWrite(0x1000, 0x0020);
        DmaWrite(0x1002, 0x0007);

        copper.COP1LC = 0x1000;
        copper.RestartFromList1();

        copper.ExecuteCycle(0, 0);
        copper.ExecuteCycle(0, 0);

        // Should NOT have written (danger bit off, register < $040)
        Assert.Equal(0u, _regs.DSKPT);
    }

    [Fact]
    public void RestartFromList2()
    {
        var copper = new Copper(_regs, DmaRead);
        copper.Enabled = true;

        // List 2: MOVE $182, $00F0 (COLOR01 = green)
        DmaWrite(0x2000, 0x0182);
        DmaWrite(0x2002, 0x00F0);

        copper.COP2LC = 0x2000;
        copper.RestartFromList2();

        copper.ExecuteCycle(0, 0);
        copper.ExecuteCycle(0, 0);

        Assert.Equal(0x00F0, _regs.Color[1]);
    }
}
