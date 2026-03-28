namespace AmigaA500.Core.Chipset;

/// <summary>
/// Amiga Copper — display-synchronized coprocessor.
/// </summary>
public sealed class Copper
{
    private readonly IChipRegisters _registers;
    private readonly Func<uint, ushort> _dmaRead;

    public uint COP1LC, COP2LC;
    public uint PC;
    public bool Enabled;
    public bool CopperDanger; // COPCON: allow writes to lower registers

    private CopperState _state = CopperState.FetchFirst;
    private ushort _ir1, _ir2;
    private bool _waiting;

    private enum CopperState { FetchFirst, FetchSecond, Execute, Waiting }

    public Copper(IChipRegisters registers, Func<uint, ushort> dmaRead)
    {
        _registers = registers;
        _dmaRead = dmaRead;
    }

    public void RestartFromList1()
    {
        PC = COP1LC;
        _state = CopperState.FetchFirst;
        _waiting = false;
    }

    public void RestartFromList2()
    {
        PC = COP2LC;
        _state = CopperState.FetchFirst;
        _waiting = false;
    }

    /// <summary>
    /// Execute one Copper cycle. Called each DMA slot allocated to Copper.
    /// </summary>
    public void ExecuteCycle(int hpos, int vpos)
    {
        if (!Enabled) return;

        switch (_state)
        {
            case CopperState.FetchFirst:
                _ir1 = _dmaRead(PC);
                PC += 2;
                _state = CopperState.FetchSecond;
                break;

            case CopperState.FetchSecond:
                _ir2 = _dmaRead(PC);
                PC += 2;
                _state = CopperState.Execute;
                goto case CopperState.Execute; // Process immediately

            case CopperState.Execute:
                if ((_ir1 & 1) == 0)
                {
                    // MOVE: write value to register
                    uint reg = (uint)(_ir1 & 0x1FE);
                    if (reg >= 0x040 || CopperDanger)
                        _registers.WriteRegister(reg, _ir2);
                    _state = CopperState.FetchFirst;
                }
                else
                {
                    // WAIT or SKIP
                    bool isSkip = (_ir2 & 1) != 0;
                    int waitVP = (_ir1 >> 8) & 0xFF;
                    int waitHP = _ir1 & 0xFE;
                    int maskVP = ((_ir2 >> 8) & 0x7F) | 0x80; // Bit 15 of word 2 = BFD
                    int maskHP = _ir2 & 0xFE;

                    if (isSkip)
                    {
                        if (BeamMatch(hpos, vpos, waitHP, waitVP, maskHP, maskVP))
                            PC += 4; // Skip next instruction
                        _state = CopperState.FetchFirst;
                    }
                    else
                    {
                        // WAIT
                        if (BeamMatch(hpos, vpos, waitHP, waitVP, maskHP, maskVP))
                        {
                            _state = CopperState.FetchFirst;
                        }
                        else
                        {
                            _state = CopperState.Waiting;
                            _waiting = true;
                        }
                    }
                }
                break;

            case CopperState.Waiting:
            {
                int vp = (_ir1 >> 8) & 0xFF;
                int hp = _ir1 & 0xFE;
                int vm = ((_ir2 >> 8) & 0x7F) | 0x80;
                int hm = _ir2 & 0xFE;

                if (BeamMatch(hpos, vpos, hp, vp, hm, vm))
                {
                    _state = CopperState.FetchFirst;
                    _waiting = false;
                }
                break;
            }
        }
    }

    private static bool BeamMatch(int hpos, int vpos, int hp, int vp, int hm, int vm)
    {
        int beamV = vpos & vm;
        int waitV = vp & vm;
        int beamH = hpos & hm;
        int waitH = hp & hm;

        return beamV > waitV || (beamV == waitV && beamH >= waitH);
    }

    public bool IsWaiting => _waiting;
}
