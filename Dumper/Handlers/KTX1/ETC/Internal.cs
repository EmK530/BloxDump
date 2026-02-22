using System.Runtime.InteropServices;

namespace KTX1.ETC;

public static partial class ETCDecompress
{
    private static int ExtendSign(int val, int bits)
    {
        return (val << (32 - bits)) >> (32 - bits);
    }

    private static ulong BSwap(ulong x)
    {
        return ((x & 0xFF00000000000000) >> 56) | ((x & 0x00FF000000000000) >> 40) | ((x & 0x0000FF0000000000) >> 24) | ((x & 0x000000FF00000000) >> 8) | ((x & 0x00000000FF000000) << 8) | ((x & 0x0000000000FF0000) << 24) | ((x & 0x000000000000FF00) << 40) | ((x & 0x00000000000000FF) << 56);
    }
    
    private static byte Clamp255(int x) => x < 0 ? (byte)0x0 : x > 255 ? (byte)0xFF : (byte)x;
    private static byte Clamp255(ulong x) => Clamp255(x);
    
    private static void LegacyETC(ulong block, int r0, int g0, int b0, int r1, int g1, int b1, Span<byte> decompressed, bool opaque, int pitch)
    {
        int[][] remapTableOpaque = [
            [  2,   8,  -2,   -8 ],
            [  5,  17,  -5,  -17 ],
            [  9,  29,  -9,  -29 ],
            [ 13,  42, -13,  -42 ],
            [ 18,  60, -18,  -60 ],
            [ 24,  80, -24,  -80 ],
            [ 33, 106, -33, -106 ],
            [ 47, 183, -47, -183 ],
        ];
        
        int[][] remapTableTransparent = [
            [ 0,   8, 0,   -8 ],
            [ 0,  17, 0,  -17 ],
            [ 0,  29, 0,  -29 ],
            [ 0,  42, 0,  -42 ],
            [ 0,  60, 0,  -60 ],
            [ 0,  80, 0,  -80 ],
            [ 0, 106, 0, -106 ],
            [ 0, 183, 0, -183 ],
        ];
        
        var flipBit = (block & 0x100000000) != 0;
        var codeWord0 = (block >> 37) & 0x7;
        var codeWord1 = (block >> 34) & 0x7;
        
        var modifierTable = opaque ? remapTableOpaque : remapTableTransparent;
        
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                var x0 = flipBit ? i : j;
                var x1 = flipBit ? i + 2 : j;
                var y0 = flipBit ? j : i;
                var y1 = flipBit ? j : i + 2;
                
                // block A
                var m = x0 + y0 * 4;
                var idx = (((block >> (m + 16)) & 1) << 1) | ((block >> m) & 1);
                m = (x0 * pitch) + (y0 * 4);
                
                if (opaque || idx != 2)
                {
                    var r = Clamp255(r0 + modifierTable[codeWord0][idx]);
                    var g = Clamp255(g0 + modifierTable[codeWord0][idx]);
                    var b = Clamp255(b0 + modifierTable[codeWord0][idx]);
                    
                    decompressed[m + 0] = r;
                    decompressed[m + 1] = g;
                    decompressed[m + 2] = b;
                    decompressed[m + 3] = 0xFF;
                }
                else
                {
                    decompressed[m + 0] = 0x0;
                    decompressed[m + 1] = 0x0;
                    decompressed[m + 2] = 0x0;
                    decompressed[m + 3] = 0x0;
                }
                
                // block B
                m = x1 + y1 * 4;
                idx = (((block >> (m + 16)) & 1) << 1) | ((block >> m) & 1);
                m = (x1 * pitch) + (y1 * 4);
                
                if (opaque || idx != 2)
                {
                    var r = Clamp255(r1 + modifierTable[codeWord1][idx]);
                    var g = Clamp255(g1 + modifierTable[codeWord1][idx]);
                    var b = Clamp255(b1 + modifierTable[codeWord1][idx]);
                    
                    decompressed[m + 0] = r;
                    decompressed[m + 1] = g;
                    decompressed[m + 2] = b;
                    decompressed[m + 3] = 0xFF;
                }
                else
                {
                    decompressed[m + 0] = 0x0;
                    decompressed[m + 1] = 0x0;
                    decompressed[m + 2] = 0x0;
                    decompressed[m + 3] = 0x0;
                }
            }
        }
    }
    
    private static void ETC_T_H(ulong block, int mode, Span<byte> decompressed, bool opaque, int pitch)
    {
        int r0, g0, b0, r1, g1, b1;
        int ra, rb, ga, gb, ba, bb, da, db;
        
        int dist = 0;
        uint[] paintColors = new uint[4];
        byte[] distanceTable = [3, 6, 11, 16, 23, 31, 41, 64];
        
        if (mode == 1)
        {
            ra = (int)((block >> 59) & 0x3);
            rb = (int)((block >> 56) & 0x3);
            g0 = (int)((block >> 52) & 0xF);
            b0 = (int)((block >> 48) & 0xF);
            r1 = (int)((block >> 44) & 0xF);
            g1 = (int)((block >> 40) & 0xF);
            b1 = (int)((block >> 36) & 0xF);
            da = (int)((block >> 34) & 0x3);
            db = (int)((block >> 32) & 0x1);
    
            r0 = (ra << 2) | rb;
        }
        else
        {
            r0 = (int)((block >> 59) & 0xF);
            ga = (int)((block >> 56) & 0x7);
            gb = (int)((block >> 52) & 0x1);
            ba = (int)((block >> 51) & 0x1);
            bb = (int)((block >> 47) & 0x7);
            r1 = (int)((block >> 43) & 0xF);
            g1 = (int)((block >> 39) & 0xF);
            b1 = (int)((block >> 35) & 0xF);
            da = (int)((block >> 34) & 0x1);
            db = (int)((block >> 32) & 0x1);
    
            g0 = (ga << 1) | gb;
            b0 = (ba << 3) | bb;
        }
        
        r0 = (r0 << 4) | r0;
        g0 = (g0 << 4) | g0;
        b0 = (b0 << 4) | b0;
        r1 = (r1 << 4) | r1;
        g1 = (g1 << 4) | g1;
        b1 = (b1 << 4) | b1;
        
        if (mode == 1)
        {
            dist = (da << 1) | db;
            dist = distanceTable[dist];
            
            paintColors[0] = (uint)(0xFF000000 | (b0 << 16) | (g0 << 8) | r0);
            paintColors[2] = (uint)(0xFF000000 | (b1 << 16) | (g1 << 8) | r1);
            paintColors[1] = (uint)(0xFF000000 | (Clamp255(b1 + dist) << 16)
                                               | (Clamp255(g1 + dist) << 8)
                                               | Clamp255(r1 + dist));
            paintColors[3] = (uint)(0xFF000000 | (Clamp255(b1 - dist) << 16)
                                               | (Clamp255(g1 - dist) << 8)
                                               | Clamp255(r1 - dist));
        }
        else
        {
            dist = ((r0 << 16) | (g0 << 8) | b0) >= ((r1 << 16) | (g1 << 8) | b1) ? 1 : 0;
            dist |= (da << 2) | (db << 1);
            dist = distanceTable[dist];
            
            paintColors[0] = (uint)(0xFF000000 | (Clamp255(b0 + dist) << 16)
                                               | (Clamp255(g0 + dist) << 8)
                                               |  Clamp255(r0 + dist));
            paintColors[1] = (uint)(0xFF000000 | (Clamp255(b0 - dist) << 16)
                                               | (Clamp255(g0 - dist) << 8)
                                               |  Clamp255(r0 - dist));
            paintColors[2] = (uint)(0xFF000000 | (Clamp255(b1 + dist) << 16)
                                               | (Clamp255(g1 + dist) << 8)
                                               |  Clamp255(r1 + dist));
            paintColors[3] = (uint)(0xFF000000 | (Clamp255(b1 - dist) << 16)
                                               | (Clamp255(g1 - dist) << 8)
                                               |  Clamp255(r1 - dist));
        }
        
        int rIdx = 0;
        
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                var k = i + j * 4;
                var idx = (((block >> (k + 16)) & 1) << 1) | ((block >> k) & 1);
                
                var span = MemoryMarshal.Cast<byte, uint>(decompressed[rIdx..]);
                if (opaque || idx != 2)
                {
                    span[j] = paintColors[idx];
                }
                else
                {
                    span[j] = 0x0;
                }
            }
            
            rIdx += pitch;
        }
    }
    
    private static void ETCPlanar(ulong block, Span<byte> decompressed, int pitch)
    {
        int ro, go, bo, rh, gh, bh, rv, gv, bv;
        int go1, go2, bo1, bo2, bo3, rh1, rh2;
        
        ro  = (int)(block >> 57) & 0x3F;
        go1 = (int)(block >> 56) & 0x01;
        go2 = (int)(block >> 49) & 0x3F;
        bo1 = (int)(block >> 48) & 0x01;
        bo2 = (int)(block >> 43) & 0x03;
        bo3 = (int)(block >> 39) & 0x07;
        rh1 = (int)(block >> 34) & 0x1F;
        rh2 = (int)(block >> 32) & 0x01;
        gh  = (int)(block >> 25) & 0x7F;
        bh  = (int)(block >> 19) & 0x3F;
        rv  = (int)(block >> 13) & 0x3F;
        gv  = (int)(block >>  6) & 0x7F;
        bv  = (int)(block >>  0) & 0x3F;
    
        go = (go1 << 6) | go2;
        bo = (bo1 << 5) | (bo2 << 3) | bo3;
        rh = (rh1 << 1) | rh2;
    
        ro = (ro << 2) | (ro >> 4);
        rh = (rh << 2) | (rh >> 4);
        rv = (rv << 2) | (rv >> 4);
        go = (go << 1) | (go >> 6);
        gh = (gh << 1) | (gh >> 6);
        gv = (gv << 1) | (gv >> 6);
        bo = (bo << 2) | (bo >> 4);
        bh = (bh << 2) | (bh >> 4);
        bv = (bv << 2) | (bv >> 4);
        
        int rIdx = 0;
        
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                var rf = (x * (rh - ro) + y * (rv - ro) + (ro << 2) + 2) >> 2;
                var gf = (x * (gh - go) + y * (gv - go) + (go << 2) + 2) >> 2;
                var bf = (x * (bh - bo) + y * (bv - bo) + (bo << 2) + 2) >> 2;
                
                var r = Clamp255(rf);
                var g = Clamp255(gf);
                var b = Clamp255(bf);
                
                decompressed[rIdx + (x * 4) + 0] = r;
                decompressed[rIdx + (x * 4) + 1] = g;
                decompressed[rIdx + (x * 4) + 2] = b;
                decompressed[rIdx + (x * 4) + 3] = 0xFF;
            }
            
            rIdx += pitch;
        }
    }
    
    private static void DecompressETCBlock(Span<byte> compressed, Span<byte> decompressed, bool punchthrough, int pitch)
    {
        int r0, r1, g0, g1, b0, b1;
        ulong block = BSwap(MemoryMarshal.Cast<byte, ulong>(compressed)[0]);
        var diffBit = (block & 0x200000000) != 0;
        
        var mode = 0; // legacy mode
        if (!punchthrough && !diffBit) // "individual" mode
        {
            r0 = (int)((block >> 60) & 0xF);
            r1 = (int)((block >> 56) & 0xF);
            g0 = (int)((block >> 52) & 0xF);
            g1 = (int)((block >> 48) & 0xF);
            b0 = (int)((block >> 44) & 0xF);
            b1 = (int)((block >> 40) & 0xF);
            
            r0 = (r0 << 4) | r0;
            g0 = (g0 << 4) | g0;
            b0 = (b0 << 4) | b0;
            r1 = (r1 << 4) | r1;
            g1 = (g1 << 4) | g1;
            b1 = (b1 << 4) | b1;
        }
        else // "differential"/"T"/"H"/"planar" mode
        {
            r0 = (int)((block >> 59) & 0x1F);
            r1 = r0 + ExtendSign((int)((block >> 56) & 0x7), 3);
            g0 = (int)((block >> 51) & 0x1F);
            g1 = g0 + ExtendSign((int)((block >> 48) & 0x7), 3);
            b0 = (int)((block >> 43) & 0x1F);
            b1 = b0 + ExtendSign((int)((block >> 40) & 0x7), 3);
            
            if (r1 < 0 || r1 > 31)
            {
                mode = 1; // "T" mode
            }
            else if (g1 < 0 || g1 > 31)
            {
                mode = 2; // "H" mode
            }
            else if (b1 < 0 || b1 > 31)
            {
                mode = 3; // "planar" mode
            }
            else
            {
                // "differential" mode
                r0 = (r0 << 3) | (r0 >> 2);
                g0 = (g0 << 3) | (g0 >> 2);
                b0 = (b0 << 3) | (b0 >> 2);
                r1 = (r1 << 3) | (r1 >> 2);
                g1 = (g1 << 3) | (g1 >> 2);
                b1 = (b1 << 3) | (b1 >> 2);
            }
        }
        
        if (mode == 0)
        {
            LegacyETC(block, r0, g0, b0, r1, g1, b1, decompressed, !punchthrough || diffBit, pitch);
        }
        else if (mode < 3)
        {
            ETC_T_H(block, mode, decompressed, !punchthrough || diffBit, pitch);
        }
        else
        {
            ETCPlanar(block, decompressed, pitch);
        }
    }
    
    private static void DecompressEACBlock(byte[] compressed, Span<byte> decompressed, bool is11Bit, int pitch, int pixelSize)
    {
        int[][] modifierTable = [
            [ -3, -6,  -9, -15, 2, 5, 8, 14 ],
            [ -3, -7, -10, -13, 2, 6, 9, 12 ],
            [ -2, -5,  -8, -13, 1, 4, 7, 12 ],
            [ -2, -4,  -6, -13, 1, 3, 5, 12 ],
            [ -3, -6,  -8, -12, 2, 5, 7, 11 ],
            [ -3, -7,  -9, -11, 2, 6, 8, 10 ],
            [ -4, -7,  -8, -11, 3, 6, 7, 10 ],
            [ -3, -5,  -8, -11, 2, 4, 7, 10 ],
            [ -2, -6,  -8, -10, 1, 5, 7,  9 ],
            [ -2, -5,  -8, -10, 1, 4, 7,  9 ],
            [ -2, -4,  -8, -10, 1, 3, 7,  9 ],
            [ -2, -5,  -7, -10, 1, 4, 6,  9 ],
            [ -3, -4,  -7, -10, 2, 3, 6,  9 ],
            [ -1, -2,  -3, -10, 0, 1, 2,  9 ],
            [ -4, -6,  -8,  -9, 3, 5, 7,  8 ],
            [ -3, -5,  -7,  -9, 2, 4, 6,  8 ]
        ];
        
        ulong block = BSwap(MemoryMarshal.Cast<byte, ulong>(compressed)[0]);
        var baseCode = (block >> 56) & 0xFF;
        var mult = (block >> 52) & 0xF;
        var modifiers = modifierTable[(block >> 48) & 0xF];
        
        var rIdx = 0;
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                var idx = (block >> ((15 - (x * 4 + y)) * 3)) & 0x7;
                var modifier = modifiers[idx];
                
                if (is11Bit)
                {
                    // not implementing yet
                }
                else
                {
                    var dValue = (int)baseCode + (modifier * (int)mult);
                    var alpha = Clamp255(dValue);
                    var ind = rIdx + (x * pixelSize);
                    decompressed[ind] = alpha;
                }
            }
            
            rIdx += pitch;
        }
    }
}