using AmigaA500.Core.Floppy;

namespace AmigaA500.Tests.Floppy;

public class TrackBuilderTests
{
    private static byte[] MakeTrackData(byte fill = 0xAA)
    {
        var data = new byte[AdfDisk.SectorsPerTrack * AdfDisk.BytesPerSector];
        Array.Fill(data, fill);
        return data;
    }

    [Fact]
    public void Build_ReturnsRawTrackBytes()
    {
        var data = MakeTrackData();
        var track = TrackBuilder.Build(data, 0, 0);
        Assert.Equal(TrackBuilder.RawTrackBytes, track.Length);
    }

    [Fact]
    public void Build_StartsWithGapWords()
    {
        var data = MakeTrackData(0x00);
        var track = TrackBuilder.Build(data, 0, 0);

        // First bytes should be the gap filler $AA
        Assert.Equal(0xAA, track[0]);
        Assert.Equal(0xAA, track[1]);
    }

    [Fact]
    public void Build_ContainsSyncWords()
    {
        var data = MakeTrackData(0x00);
        var track = TrackBuilder.Build(data, 0, 0);

        // Find 0x4489 sync word somewhere in the track
        bool found = false;
        for (int i = 0; i + 1 < track.Length; i++)
        {
            if (track[i] == 0x44 && track[i + 1] == 0x89)
            {
                found = true;
                break;
            }
        }
        Assert.True(found, "Track must contain at least one $4489 sync word.");
    }

    [Fact]
    public void Build_ThrowsOnWrongDataLength()
    {
        var badData = new byte[100]; // too short
        Assert.Throws<ArgumentException>(() => TrackBuilder.Build(badData, 0, 0));
    }

    [Fact]
    public void Build_TwelveSyncWordsForElevenSectors()
    {
        // Each sector has 2 sync words; with 11 sectors we expect 22 occurrences of $4489.
        var data = MakeTrackData(0x00);
        var track = TrackBuilder.Build(data, 0, 0);

        int count = 0;
        for (int i = 0; i + 1 < track.Length; i++)
        {
            if (track[i] == 0x44 && track[i + 1] == 0x89)
                count++;
        }

        // 11 sectors × 2 sync words each = 22
        Assert.Equal(22, count);
    }

    [Fact]
    public void Build_DifferentCylinderProducesDifferentOutput()
    {
        var data = MakeTrackData(0x55);
        var track0 = TrackBuilder.Build(data, 0, 0);
        var track1 = TrackBuilder.Build(data, 1, 0);

        // Track number is encoded in the sector header, so the tracks must differ
        Assert.NotEqual(track0, track1);
    }

    [Fact]
    public void Build_DifferentSideProducesDifferentOutput()
    {
        var data = MakeTrackData(0x55);
        var trackSide0 = TrackBuilder.Build(data, 5, 0);
        var trackSide1 = TrackBuilder.Build(data, 5, 1);

        Assert.NotEqual(trackSide0, trackSide1);
    }

    [Fact]
    public void BuildFromAdf_ProducesValidTrack()
    {
        var adfData = new byte[AdfDisk.TotalSize];
        // Fill sector 0 of track 0 with a recognisable pattern
        for (int i = 0; i < AdfDisk.BytesPerSector; i++)
            adfData[i] = (byte)(i & 0xFF);

        var disk = new AdfDisk(adfData);
        var track = TrackBuilder.BuildFromAdf(disk, 0, 0);

        Assert.Equal(TrackBuilder.RawTrackBytes, track.Length);
    }
}
