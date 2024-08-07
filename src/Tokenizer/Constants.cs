﻿namespace Lokad.Tokenizers.Tokenizer;

using System.Collections.Generic;

public static class Constants
{
    public static readonly HashSet<uint> WhitespaceChars = new HashSet<uint>(new uint[]
    {
            // Standard whitespace characters (unicode category Zs)
            0x0020, 0x00A0, 0x1680, 0x2000, 0x2001, 0x2002, 0x2003, 0x2004, 0x2005, 0x2006, 0x2007, 0x2008,
            0x2009, 0x200A, 0x202F, 0x205F, 0x3000,
            // Additional characters considered whitespace for BERT (tab, newline, carriage return)
            0x0009, 0x000D, 0x00A,
    });

    public static readonly char[] AdditionalWhitespaceChars = new char[]
    {
            '\t', '\n', '\r'
    };

    // constant for \u2581 (lower one eighth block)
    public const char LowerOneEighthBlock = '\u2581'; // ▁

    public static readonly HashSet<uint> PunctuationChars = new HashSet<uint>(new uint[]
    {
            0x21, 0x22, 0x23, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2c, 0x2d, 0x2e, 0x2f, 0x3a,
            0x3b, 0x3f, 0x40, 0x5b, 0x5c, 0x5d, 0x5f, 0x7b, 0x7d, 0xa1, 0xa7, 0xab, 0xb6, 0xb7,
            0xbb, 0xbf, 0x37e, 0x387, 0x55a, 0x55b, 0x55c, 0x55d, 0x55e, 0x55f, 0x589, 0x58a,
            0x5be, 0x5c0, 0x5c3, 0x5c6, 0x5f3, 0x5f4, 0x609, 0x60a, 0x60c, 0x60d, 0x61b, 0x61e,
            0x61f, 0x66a, 0x66b, 0x66c, 0x66d, 0x6d4, 0x700, 0x701, 0x702, 0x703, 0x704, 0x705,
            0x706, 0x707, 0x708, 0x709, 0x70a, 0x70b, 0x70c, 0x70d, 0x7f7, 0x7f8, 0x7f9, 0x830,
            0x831, 0x832, 0x833, 0x834, 0x835, 0x836, 0x837, 0x838, 0x839, 0x83a, 0x83b, 0x83c,
            0x83d, 0x83e, 0x85e, 0x964, 0x965, 0x970, 0x9fd, 0xa76, 0xaf0, 0xc84, 0xdf4, 0xe4f,
            0xe5a, 0xe5b, 0xf04, 0xf05, 0xf06, 0xf07, 0xf08, 0xf09, 0xf0a, 0xf0b, 0xf0c, 0xf0d,
            0xf0e, 0xf0f, 0xf10, 0xf11, 0xf12, 0xf14, 0xf3a, 0xf3b, 0xf3c, 0xf3d, 0xf85, 0xfd0,
            0xfd1, 0xfd2, 0xfd3, 0xfd4, 0xfd9, 0xfda, 0x104a, 0x104b, 0x104c, 0x104d, 0x104e,
            0x104f, 0x10fb, 0x1360, 0x1361, 0x1362, 0x1363, 0x1364, 0x1365, 0x1366, 0x1367, 0x1368,
            0x1400, 0x166d, 0x166e, 0x169b, 0x169c, 0x16eb, 0x16ec, 0x16ed, 0x1735, 0x1736, 0x17d4,
            0x17d5, 0x17d6, 0x17d8, 0x17d9, 0x17da, 0x1800, 0x1801, 0x1802, 0x1803, 0x1804, 0x1805,
            0x1806, 0x1807, 0x1808, 0x1809, 0x180a, 0x1944, 0x1945, 0x1a1e, 0x1a1f, 0x1aa0, 0x1aa1,
            0x1aa2, 0x1aa3, 0x1aa4, 0x1aa5, 0x1aa6, 0x1aa8, 0x1aa9, 0x1aaa, 0x1aab, 0x1aac, 0x1aad,
            0x1b5a, 0x1b5b, 0x1b5c, 0x1b5d, 0x1b5e, 0x1b5f, 0x1b60, 0x1bfc, 0x1bfd, 0x1bfe, 0x1bff,
            0x1c3b, 0x1c3c, 0x1c3d, 0x1c3e, 0x1c3f, 0x1c7e, 0x1c7f, 0x1cc0, 0x1cc1, 0x1cc2, 0x1cc3,
            0x1cc4, 0x1cc5, 0x1cc6, 0x1cc7, 0x1cd3, 0x2010, 0x2011, 0x2012, 0x2013, 0x2014, 0x2015,
            0x2016, 0x2017, 0x2018, 0x2019, 0x201a, 0x201b, 0x201c, 0x201d, 0x201e, 0x201f, 0x2020,
            0x2021, 0x2022, 0x2023, 0x2024, 0x2025, 0x2026, 0x2027, 0x2030, 0x2031, 0x2032, 0x2033,
            0x2034, 0x2035, 0x2036, 0x2037, 0x2038, 0x2039, 0x203a, 0x203b, 0x203c, 0x203d, 0x203e,
            0x203f, 0x2040, 0x2041, 0x2042, 0x2043, 0x2045, 0x2046, 0x2047, 0x2048, 0x2049, 0x204a,
            0x204b, 0x204c, 0x204d, 0x204e, 0x204f, 0x2050, 0x2051, 0x2053, 0x2054, 0x2055, 0x2056,
            0x2057, 0x2058, 0x2059, 0x205a, 0x205b, 0x205c, 0x205d, 0x205e, 0x207d, 0x207e, 0x208d,
            0x208e, 0x2308, 0x2309, 0x230a, 0x230b, 0x2329, 0x232a, 0x2768, 0x2769, 0x276a, 0x276b,
            0x276c, 0x276d, 0x276e, 0x276f, 0x2770, 0x2771, 0x2772, 0x2773, 0x2774, 0x2775, 0x27c5,
            0x27c6, 0x27e6, 0x27e7, 0x27e8, 0x27e9, 0x27ea, 0x27eb, 0x27ec, 0x27ed, 0x27ee, 0x27ef,
            0x2983, 0x2984, 0x2985, 0x2986, 0x2987, 0x2988, 0x2989, 0x298a, 0x298b, 0x298c, 0x298d,
            0x298e, 0x298f, 0x2990, 0x2991, 0x2992, 0x2993, 0x2994, 0x2995, 0x2996, 0x2997, 0x2998,
            0x29d8, 0x29d9, 0x29da, 0x29db, 0x29fc, 0x29fd, 0x2cf9, 0x2cfa, 0x2cfb, 0x2cfc, 0x2cfe,
            0x2cff, 0x2d70, 0x2e00, 0x2e01, 0x2e02, 0x2e03, 0x2e04, 0x2e05, 0x2e06, 0x2e07, 0x2e08,
            0x2e09, 0x2e0a, 0x2e0b, 0x2e0c, 0x2e0d, 0x2e0e, 0x2e0f, 0x2e10, 0x2e11, 0x2e12, 0x2e13,
            0x2e14, 0x2e15, 0x2e16, 0x2e17, 0x2e18, 0x2e19, 0x2e1a, 0x2e1b, 0x2e1c, 0x2e1d, 0x2e1e,
            0x2e1f, 0x2e20, 0x2e21, 0x2e22, 0x2e23, 0x2e24, 0x2e25, 0x2e26, 0x2e27, 0x2e28, 0x2e29,
            0x2e2a, 0x2e2b, 0x2e2c, 0x2e2d, 0x2e2e, 0x2e30, 0x2e31, 0x2e32, 0x2e33, 0x2e34, 0x2e35,
            0x2e36, 0x2e37, 0x2e38, 0x2e39, 0x2e3a, 0x2e3b, 0x2e3c, 0x2e3d, 0x2e3e, 0x2e3f, 0x2e40,
            0x2e41, 0x2e42, 0x2e43, 0x2e44, 0x2e45, 0x2e46, 0x2e47, 0x2e48, 0x2e49, 0x2e4a, 0x2e4b,
            0x2e4c, 0x2e4d, 0x2e4e, 0x3001, 0x3002, 0x3003, 0x3008, 0x3009, 0x300a, 0x300b, 0x300c,
            0x300d, 0x300e, 0x300f, 0x3010, 0x3011, 0x3014, 0x3015, 0x3016, 0x3017, 0x3018, 0x3019,
            0x301a, 0x301b, 0x301c, 0x301d, 0x301e, 0x301f, 0x3030, 0x303d, 0x30a0, 0x30fb, 0xa4fe,
            0xa4ff, 0xa60d, 0xa60e, 0xa60f, 0xa673, 0xa67e, 0xa6f2, 0xa6f3, 0xa6f4, 0xa6f5, 0xa6f6,
            0xa6f7, 0xa874, 0xa875, 0xa876, 0xa877, 0xa8ce, 0xa8cf, 0xa8f8, 0xa8f9, 0xa8fa, 0xa8fc,
            0xa92e, 0xa92f, 0xa95f, 0xa9c1, 0xa9c2, 0xa9c3, 0xa9c4, 0xa9c5, 0xa9c6, 0xa9c7, 0xa9c8,
            0xa9c9, 0xa9ca, 0xa9cb, 0xa9cc, 0xa9cd, 0xa9de, 0xa9df, 0xaa5c, 0xaa5d, 0xaa5e, 0xaa5f,
            0xaade, 0xaadf, 0xaaf0, 0xaaf1, 0xabeb, 0xfd3e, 0xfd3f, 0xfe10, 0xfe11, 0xfe12, 0xfe13,
            0xfe14, 0xfe15, 0xfe16, 0xfe17, 0xfe18, 0xfe19, 0xfe30, 0xfe31, 0xfe32, 0xfe33, 0xfe34,
            0xfe35, 0xfe36, 0xfe37, 0xfe38, 0xfe39, 0xfe3a, 0xfe3b, 0xfe3c, 0xfe3d, 0xfe3e, 0xfe3f,
            0xfe40, 0xfe41, 0xfe42, 0xfe43, 0xfe44, 0xfe45, 0xfe46, 0xfe47, 0xfe48, 0xfe49, 0xfe4a,
            0xfe4b, 0xfe4c, 0xfe4d, 0xfe4e, 0xfe4f, 0xfe50, 0xfe51, 0xfe52, 0xfe54, 0xfe55, 0xfe56,
            0xfe57, 0xfe58, 0xfe59, 0xfe5a, 0xfe5b, 0xfe5c, 0xfe5d, 0xfe5e, 0xfe5f, 0xfe60, 0xfe61,
            0xfe63, 0xfe68, 0xfe6a, 0xfe6b, 0xff01, 0xff02, 0xff03, 0xff05, 0xff06, 0xff07, 0xff08,
            0xff09, 0xff0a, 0xff0c, 0xff0d, 0xff0e, 0xff0f, 0xff1a, 0xff1b, 0xff1f, 0xff20, 0xff3b,
            0xff3c, 0xff3d, 0xff3f, 0xff5b, 0xff5d, 0xff5f, 0xff60, 0xff61, 0xff62, 0xff63, 0xff64,
            0xff65, 0x10100, 0x10101, 0x10102, 0x1039f, 0x103d0, 0x1056f, 0x10857, 0x1091f,
            0x1093f, 0x10a50, 0x10a51, 0x10a52, 0x10a53, 0x10a54, 0x10a55, 0x10a56, 0x10a57,
            0x10a58, 0x10a7f, 0x10af0, 0x10af1, 0x10af2, 0x10af3, 0x10af4, 0x10af5, 0x10af6,
            0x10b39, 0x10b3a, 0x10b3b, 0x10b3c, 0x10b3d, 0x10b3e, 0x10b3f, 0x10b99, 0x10b9a,
            0x10b9b, 0x10b9c, 0x10f55, 0x10f56, 0x10f57, 0x10f58, 0x10f59, 0x11047, 0x11048,
            0x11049, 0x1104a, 0x1104b, 0x1104c, 0x1104d, 0x110bb, 0x110bc, 0x110be, 0x110bf,
            0x110c0, 0x110c1, 0x11140, 0x11141, 0x11142, 0x11143, 0x11174, 0x11175, 0x111c5,
            0x111c6, 0x111c7, 0x111c8, 0x111cd, 0x111db, 0x111dd, 0x111de, 0x111df, 0x11238,
            0x11239, 0x1123a, 0x1123b, 0x1123c, 0x1123d, 0x112a9, 0x1144b, 0x1144c, 0x1144d,
            0x1144e, 0x1144f, 0x1145b, 0x1145d, 0x114c6, 0x115c1, 0x115c2, 0x115c3, 0x115c4,
            0x115c5, 0x115c6, 0x115c7, 0x115c8, 0x115c9, 0x115ca, 0x115cb, 0x115cc, 0x115cd,
            0x115ce, 0x115cf, 0x115d0, 0x115d1, 0x115d2, 0x115d3, 0x115d4, 0x115d5, 0x115d6,
            0x115d7, 0x11641, 0x11642, 0x11643, 0x11660, 0x11661, 0x11662, 0x11663, 0x11664,
            0x11665, 0x11666, 0x11667, 0x11668, 0x11669, 0x1166a, 0x1166b, 0x1166c, 0x1173c,
            0x1173d, 0x1173e, 0x1183b, 0x11a3f, 0x11a40, 0x11a41, 0x11a42, 0x11a43, 0x11a44,
            0x11a45, 0x11a46, 0x11a9a, 0x11a9b, 0x11a9c, 0x11a9e, 0x11a9f, 0x11aa0, 0x11aa1,
            0x11aa2, 0x11c41, 0x11c42, 0x11c43, 0x11c44, 0x11c45, 0x11c70, 0x11c71, 0x11ef7,
            0x11ef8, 0x12470, 0x12471, 0x12472, 0x12473, 0x12474, 0x16a6e, 0x16a6f, 0x16af5,
            0x16b37, 0x16b38, 0x16b39, 0x16b3a, 0x16b3b, 0x16b44, 0x16e97, 0x16e98, 0x16e99,
            0x16e9a, 0x1bc9f, 0x1da87, 0x1da88, 0x1da89, 0x1da8a, 0x1da8b, 0x1e95e, 0x1e95f,
    });

    public static readonly HashSet<uint> ControlChars = new HashSet<uint>(new uint[]
    {
             0x007F, 0x00AD, 0x0600, 0x0601, 0x0602, 0x0603, 0x0604, 0x0605, 0x061C, 0x06DD, 0x070F,
            0x08E2, 0x180E, 0x200B, 0x200C, 0x200D, 0x200E, 0x200F, 0x202A, 0x202B, 0x202C, 0x202D,
            0x202E, 0x2060, 0x2061, 0x2062, 0x2063, 0x2064, 0x2066, 0x2067, 0x2068, 0x2069, 0x206A,
            0x206B, 0x206C, 0x206D, 0x206E, 0x206F, 0xFEFF, 0xFFF9, 0xFFFA, 0xFFFB, 0x110BD,
            0x110CD, 0x13430, 0x13431, 0x13432, 0x13433, 0x13434, 0x13435, 0x13436, 0x13437,
            0x13438, 0x1BCA0, 0x1BCA1, 0x1BCA2, 0x1BCA3, 0x1D173, 0x1D174, 0x1D175, 0x1D176,
            0x1D177, 0x1D178, 0x1D179, 0x1D17A, 0xE0001,
    });

    public static readonly HashSet<uint> AccentMarkers = new HashSet<uint>(new uint[]
    {
            0x0300, 0x0301, 0x0302, 0x0303, 0x0304, 0x0305, 0x0306, 0x0307, 0x0308, 0x0309, 0x030A,
            0x030B, 0x030C, 0x030D, 0x030E, 0x030F, 0x0310, 0x0311, 0x0312, 0x0313, 0x0314, 0x0315,
            0x0316, 0x0317, 0x0318, 0x0319, 0x031A, 0x031B, 0x031C, 0x031D, 0x031E, 0x031F, 0x0320,
            0x0321, 0x0322, 0x0323, 0x0324, 0x0325, 0x0326, 0x0327, 0x0328, 0x0329, 0x032A, 0x032B,
            0x032C, 0x032D, 0x032E, 0x032F, 0x0330, 0x0331, 0x0332, 0x0333, 0x0334, 0x0335, 0x0336,
            0x0337, 0x0338, 0x0339, 0x033A, 0x033B, 0x033C, 0x033D, 0x033E, 0x033F, 0x0340, 0x0341,
            0x0342, 0x0343, 0x0344, 0x0345, 0x0346, 0x0347, 0x0348, 0x0349, 0x034A, 0x034B, 0x034C,
            0x034D, 0x034E, 0x034F, 0x0350, 0x0351, 0x0352, 0x0353, 0x0354, 0x0355, 0x0356, 0x0357,
            0x0358, 0x0359, 0x035A, 0x035B, 0x035C, 0x035D, 0x035E, 0x035F, 0x0360, 0x0361, 0x0362,
            0x0363, 0x0364, 0x0365, 0x0366, 0x0367, 0x0368, 0x0369, 0x036A, 0x036B, 0x036C, 0x036D,
            0x036E, 0x036F, 0x0483, 0x0484, 0x0485, 0x0486, 0x0487, 0x0591, 0x0592, 0x0593, 0x0594,
            0x0595, 0x0596, 0x0597, 0x0598, 0x0599, 0x059A, 0x059B, 0x059C, 0x059D, 0x059E, 0x059F,
            0x05A0, 0x05A1, 0x05A2, 0x05A3, 0x05A4, 0x05A5, 0x05A6, 0x05A7, 0x05A8, 0x05A9, 0x05AA,
            0x05AB, 0x05AC, 0x05AD, 0x05AE, 0x05AF, 0x05B0, 0x05B1, 0x05B2, 0x05B3, 0x05B4, 0x05B5,
            0x05B6, 0x05B7, 0x05B8, 0x05B9, 0x05BA, 0x05BB, 0x05BC, 0x05BD, 0x05BF, 0x05C1, 0x05C2,
            0x05C4, 0x05C5, 0x05C7, 0x0610, 0x0611, 0x0612, 0x0613, 0x0614, 0x0615, 0x0616, 0x0617,
            0x0618, 0x0619, 0x061A, 0x064B, 0x064C, 0x064D, 0x064E, 0x064F, 0x0650, 0x0651, 0x0652,
            0x0653, 0x0654, 0x0655, 0x0656, 0x0657, 0x0658, 0x0659, 0x065A, 0x065B, 0x065C, 0x065D,
            0x065E, 0x065F, 0x0670, 0x06D6, 0x06D7, 0x06D8, 0x06D9, 0x06DA, 0x06DB, 0x06DC, 0x06DF,
            0x06E0, 0x06E1, 0x06E2, 0x06E3, 0x06E4, 0x06E7, 0x06E8, 0x06EA, 0x06EB, 0x06EC, 0x06ED,
            0x0711, 0x0730, 0x0731, 0x0732, 0x0733, 0x0734, 0x0735, 0x0736, 0x0737, 0x0738, 0x0739,
            0x073A, 0x073B, 0x073C, 0x073D, 0x073E, 0x073F, 0x0740, 0x0741, 0x0742, 0x0743, 0x0744,
            0x0745, 0x0746, 0x0747, 0x0748, 0x0749, 0x074A, 0x07A6, 0x07A7, 0x07A8, 0x07A9, 0x07AA,
            0x07AB, 0x07AC, 0x07AD, 0x07AE, 0x07AF, 0x07B0, 0x07EB, 0x07EC, 0x07ED, 0x07EE, 0x07EF,
            0x07F0, 0x07F1, 0x07F2, 0x07F3, 0x07FD, 0x0816, 0x0817, 0x0818, 0x0819, 0x081B, 0x081C,
            0x081D, 0x081E, 0x081F, 0x0820, 0x0821, 0x0822, 0x0823, 0x0825, 0x0826, 0x0827, 0x0829,
            0x082A, 0x082B, 0x082C, 0x082D, 0x0859, 0x085A, 0x085B, 0x08D3, 0x08D4, 0x08D5, 0x08D6,
            0x08D7, 0x08D8, 0x08D9, 0x08DA, 0x08DB, 0x08DC, 0x08DD, 0x08DE, 0x08DF, 0x08E0, 0x08E1,
            0x08E3, 0x08E4, 0x08E5, 0x08E6, 0x08E7, 0x08E8, 0x08E9, 0x08EA, 0x08EB, 0x08EC, 0x08ED,
            0x08EE, 0x08EF, 0x08F0, 0x08F1, 0x08F2, 0x08F3, 0x08F4, 0x08F5, 0x08F6, 0x08F7, 0x08F8,
            0x08F9, 0x08FA, 0x08FB, 0x08FC, 0x08FD, 0x08FE, 0x08FF, 0x0900, 0x0901, 0x0902, 0x093A,
            0x093C, 0x0941, 0x0942, 0x0943, 0x0944, 0x0945, 0x0946, 0x0947, 0x0948, 0x094D, 0x0951,
            0x0952, 0x0953, 0x0954, 0x0955, 0x0956, 0x0957, 0x0962, 0x0963, 0x0981, 0x09BC, 0x09C1,
            0x09C2, 0x09C3, 0x09C4, 0x09CD, 0x09E2, 0x09E3, 0x09FE, 0x0A01, 0x0A02, 0x0A3C, 0x0A41,
            0x0A42, 0x0A47, 0x0A48, 0x0A4B, 0x0A4C, 0x0A4D, 0x0A51, 0x0A70, 0x0A71, 0x0A75, 0x0A81,
            0x0A82, 0x0ABC, 0x0AC1, 0x0AC2, 0x0AC3, 0x0AC4, 0x0AC5, 0x0AC7, 0x0AC8, 0x0ACD, 0x0AE2,
            0x0AE3, 0x0AFA, 0x0AFB, 0x0AFC, 0x0AFD, 0x0AFE, 0x0AFF, 0x0B01, 0x0B3C, 0x0B3F, 0x0B41,
            0x0B42, 0x0B43, 0x0B44, 0x0B4D, 0x0B56, 0x0B62, 0x0B63, 0x0B82, 0x0BC0, 0x0BCD, 0x0C00,
            0x0C04, 0x0C3E, 0x0C3F, 0x0C40, 0x0C46, 0x0C47, 0x0C48, 0x0C4A, 0x0C4B, 0x0C4C, 0x0C4D,
            0x0C55, 0x0C56, 0x0C62, 0x0C63, 0x0C81, 0x0CBC, 0x0CBF, 0x0CC6, 0x0CCC, 0x0CCD, 0x0CE2,
            0x0CE3, 0x0D00, 0x0D01, 0x0D3B, 0x0D3C, 0x0D41, 0x0D42, 0x0D43, 0x0D44, 0x0D4D, 0x0D62,
            0x0D63, 0x0DCA, 0x0DD2, 0x0DD3, 0x0DD4, 0x0DD6, 0x0E31, 0x0E34, 0x0E35, 0x0E36, 0x0E37,
            0x0E38, 0x0E39, 0x0E3A, 0x0E47, 0x0E48, 0x0E49, 0x0E4A, 0x0E4B, 0x0E4C, 0x0E4D, 0x0E4E,
            0x0EB1, 0x0EB4, 0x0EB5, 0x0EB6, 0x0EB7, 0x0EB8, 0x0EB9, 0x0EBA, 0x0EBB, 0x0EBC, 0x0EC8,
            0x0EC9, 0x0ECA, 0x0ECB, 0x0ECC, 0x0ECD, 0x0F18, 0x0F19, 0x0F35, 0x0F37, 0x0F39, 0x0F71,
            0x0F72, 0x0F73, 0x0F74, 0x0F75, 0x0F76, 0x0F77, 0x0F78, 0x0F79, 0x0F7A, 0x0F7B, 0x0F7C,
            0x0F7D, 0x0F7E, 0x0F80, 0x0F81, 0x0F82, 0x0F83, 0x0F84, 0x0F86, 0x0F87, 0x0F8D, 0x0F8E,
            0x0F8F, 0x0F90, 0x0F91, 0x0F92, 0x0F93, 0x0F94, 0x0F95, 0x0F96, 0x0F97, 0x0F99, 0x0F9A,
            0x0F9B, 0x0F9C, 0x0F9D, 0x0F9E, 0x0F9F, 0x0FA0, 0x0FA1, 0x0FA2, 0x0FA3, 0x0FA4, 0x0FA5,
            0x0FA6, 0x0FA7, 0x0FA8, 0x0FA9, 0x0FAA, 0x0FAB, 0x0FAC, 0x0FAD, 0x0FAE, 0x0FAF, 0x0FB0,
            0x0FB1, 0x0FB2, 0x0FB3, 0x0FB4, 0x0FB5, 0x0FB6, 0x0FB7, 0x0FB8, 0x0FB9, 0x0FBA, 0x0FBB,
            0x0FBC, 0x0FC6, 0x102D, 0x102E, 0x102F, 0x1030, 0x1032, 0x1033, 0x1034, 0x1035, 0x1036,
            0x1037, 0x1039, 0x103A, 0x103D, 0x103E, 0x1058, 0x1059, 0x105E, 0x105F, 0x1060, 0x1071,
            0x1072, 0x1073, 0x1074, 0x1082, 0x1085, 0x1086, 0x108D, 0x109D, 0x135D, 0x135E, 0x135F,
            0x1712, 0x1713, 0x1714, 0x1732, 0x1733, 0x1734, 0x1752, 0x1753, 0x1772, 0x1773, 0x17B4,
            0x17B5, 0x17B7, 0x17B8, 0x17B9, 0x17BA, 0x17BB, 0x17BC, 0x17BD, 0x17C6, 0x17C9, 0x17CA,
            0x17CB, 0x17CC, 0x17CD, 0x17CE, 0x17CF, 0x17D0, 0x17D1, 0x17D2, 0x17D3, 0x17DD, 0x180B,
            0x180C, 0x180D, 0x1885, 0x1886, 0x18A9, 0x1920, 0x1921, 0x1922, 0x1927, 0x1928, 0x1932,
            0x1939, 0x193A, 0x193B, 0x1A17, 0x1A18, 0x1A1B, 0x1A56, 0x1A58, 0x1A59, 0x1A5A, 0x1A5B,
            0x1A5C, 0x1A5D, 0x1A5E, 0x1A60, 0x1A62, 0x1A65, 0x1A66, 0x1A67, 0x1A68, 0x1A69, 0x1A6A,
            0x1A6B, 0x1A6C, 0x1A73, 0x1A74, 0x1A75, 0x1A76, 0x1A77, 0x1A78, 0x1A79, 0x1A7A, 0x1A7B,
            0x1A7C, 0x1A7F, 0x1AB0, 0x1AB1, 0x1AB2, 0x1AB3, 0x1AB4, 0x1AB5, 0x1AB6, 0x1AB7, 0x1AB8,
            0x1AB9, 0x1ABA, 0x1ABB, 0x1ABC, 0x1ABD, 0x1B00, 0x1B01, 0x1B02, 0x1B03, 0x1B34, 0x1B36,
            0x1B37, 0x1B38, 0x1B39, 0x1B3A, 0x1B3C, 0x1B42, 0x1B6B, 0x1B6C, 0x1B6D, 0x1B6E, 0x1B6F,
            0x1B70, 0x1B71, 0x1B72, 0x1B73, 0x1B80, 0x1B81, 0x1BA2, 0x1BA3, 0x1BA4, 0x1BA5, 0x1BA8,
            0x1BA9, 0x1BAB, 0x1BAC, 0x1BAD, 0x1BE6, 0x1BE8, 0x1BE9, 0x1BED, 0x1BEF, 0x1BF0, 0x1BF1,
            0x1C2C, 0x1C2D, 0x1C2E, 0x1C2F, 0x1C30, 0x1C31, 0x1C32, 0x1C33, 0x1C36, 0x1C37, 0x1CD0,
            0x1CD1, 0x1CD2, 0x1CD4, 0x1CD5, 0x1CD6, 0x1CD7, 0x1CD8, 0x1CD9, 0x1CDA, 0x1CDB, 0x1CDC,
            0x1CDD, 0x1CDE, 0x1CDF, 0x1CE0, 0x1CE2, 0x1CE3, 0x1CE4, 0x1CE5, 0x1CE6, 0x1CE7, 0x1CE8,
            0x1CED, 0x1CF4, 0x1CF8, 0x1CF9, 0x1DC0, 0x1DC1, 0x1DC2, 0x1DC3, 0x1DC4, 0x1DC5, 0x1DC6,
            0x1DC7, 0x1DC8, 0x1DC9, 0x1DCA, 0x1DCB, 0x1DCC, 0x1DCD, 0x1DCE, 0x1DCF, 0x1DD0, 0x1DD1,
            0x1DD2, 0x1DD3, 0x1DD4, 0x1DD5, 0x1DD6, 0x1DD7, 0x1DD8, 0x1DD9, 0x1DDA, 0x1DDB, 0x1DDC,
            0x1DDD, 0x1DDE, 0x1DDF, 0x1DE0, 0x1DE1, 0x1DE2, 0x1DE3, 0x1DE4, 0x1DE5, 0x1DE6, 0x1DE7,
            0x1DE8, 0x1DE9, 0x1DEA, 0x1DEB, 0x1DEC, 0x1DED, 0x1DEE, 0x1DEF, 0x1DF0, 0x1DF1, 0x1DF2,
            0x1DF3, 0x1DF4, 0x1DF5, 0x1DF6, 0x1DF7, 0x1DF8, 0x1DF9, 0x1DFB, 0x1DFC, 0x1DFD, 0x1DFE,
            0x1DFF, 0x20D0, 0x20D1, 0x20D2, 0x20D3, 0x20D4, 0x20D5, 0x20D6, 0x20D7, 0x20D8, 0x20D9,
            0x20DA, 0x20DB, 0x20DC, 0x20E1, 0x20E5, 0x20E6, 0x20E7, 0x20E8, 0x20E9, 0x20EA, 0x20EB,
            0x20EC, 0x20ED, 0x20EE, 0x20EF, 0x20F0, 0x2CEF, 0x2CF0, 0x2CF1, 0x2D7F, 0x2DE0, 0x2DE1,
            0x2DE2, 0x2DE3, 0x2DE4, 0x2DE5, 0x2DE6, 0x2DE7, 0x2DE8, 0x2DE9, 0x2DEA, 0x2DEB, 0x2DEC,
            0x2DED, 0x2DEE, 0x2DEF, 0x2DF0, 0x2DF1, 0x2DF2, 0x2DF3, 0x2DF4, 0x2DF5, 0x2DF6, 0x2DF7,
            0x2DF8, 0x2DF9, 0x2DFA, 0x2DFB, 0x2DFC, 0x2DFD, 0x2DFE, 0x2DFF, 0x302A, 0x302B, 0x302C,
            0x302D, 0x3099, 0x309A, 0xA66F, 0xA674, 0xA675, 0xA676, 0xA677, 0xA678, 0xA679, 0xA67A,
            0xA67B, 0xA67C, 0xA67D, 0xA69E, 0xA69F, 0xA6F0, 0xA6F1, 0xA802, 0xA806, 0xA80B, 0xA825,
            0xA826, 0xA8C4, 0xA8C5, 0xA8E0, 0xA8E1, 0xA8E2, 0xA8E3, 0xA8E4, 0xA8E5, 0xA8E6, 0xA8E7,
            0xA8E8, 0xA8E9, 0xA8EA, 0xA8EB, 0xA8EC, 0xA8ED, 0xA8EE, 0xA8EF, 0xA8F0, 0xA8F1, 0xA8FF,
            0xA926, 0xA927, 0xA928, 0xA929, 0xA92A, 0xA92B, 0xA92C, 0xA92D, 0xA947, 0xA948, 0xA949,
            0xA94A, 0xA94B, 0xA94C, 0xA94D, 0xA94E, 0xA94F, 0xA950, 0xA951, 0xA980, 0xA981, 0xA982,
            0xA9B3, 0xA9B6, 0xA9B7, 0xA9B8, 0xA9B9, 0xA9BC, 0xA9BD, 0xA9E5, 0xAA29, 0xAA2A, 0xAA2B,
            0xAA2C, 0xAA2D, 0xAA2E, 0xAA31, 0xAA32, 0xAA35, 0xAA36, 0xAA43, 0xAA4C, 0xAA7C, 0xAAB0,
            0xAAB2, 0xAAB3, 0xAAB4, 0xAAB7, 0xAAB8, 0xAABE, 0xAABF, 0xAAC1, 0xAAEC, 0xAAED, 0xAAF6,
            0xABE5, 0xABE8, 0xABED, 0xFB1E, 0xFE00, 0xFE01, 0xFE02, 0xFE03, 0xFE04, 0xFE05, 0xFE06,
            0xFE07, 0xFE08, 0xFE09, 0xFE0A, 0xFE0B, 0xFE0C, 0xFE0D, 0xFE0E, 0xFE0F, 0xFE20, 0xFE21,
            0xFE22, 0xFE23, 0xFE24, 0xFE25, 0xFE26, 0xFE27, 0xFE28, 0xFE29, 0xFE2A, 0xFE2B, 0xFE2C,
            0xFE2D, 0xFE2E, 0xFE2F, 0x101FD, 0x102E0, 0x10376, 0x10377, 0x10378, 0x10379, 0x1037A,
            0x10A01, 0x10A02, 0x10A03, 0x10A05, 0x10A06, 0x10A0C, 0x10A0D, 0x10A0E, 0x10A0F,
            0x10A38, 0x10A39, 0x10A3A, 0x10A3F, 0x10AE5, 0x10AE6, 0x10D24, 0x10D25, 0x10D26,
            0x10D27, 0x10F46, 0x10F47, 0x10F48, 0x10F49, 0x10F4A, 0x10F4B, 0x10F4C, 0x10F4D,
            0x10F4E, 0x10F4F, 0x10F50, 0x11001, 0x11038, 0x11039, 0x1103A, 0x1103B, 0x1103C,
            0x1103D, 0x1103E, 0x1103F, 0x11040, 0x11041, 0x11042, 0x11043, 0x11044, 0x11045,
            0x11046, 0x1107F, 0x11080, 0x11081, 0x110B3, 0x110B4, 0x110B5, 0x110B6, 0x110B9,
            0x110BA, 0x11100, 0x11101, 0x11102, 0x11127, 0x11128, 0x11129, 0x1112A, 0x1112B,
            0x1112D, 0x1112E, 0x1112F, 0x11130, 0x11131, 0x11132, 0x11133, 0x11134, 0x11173,
            0x11180, 0x11181, 0x111B6, 0x111B7, 0x111B8, 0x111B9, 0x111BA, 0x111BB, 0x111BC,
            0x111BD, 0x111BE, 0x111C9, 0x111CA, 0x111CB, 0x111CC, 0x1122F, 0x11230, 0x11231,
            0x11234, 0x11236, 0x11237, 0x1123E, 0x112DF, 0x112E3, 0x112E4, 0x112E5, 0x112E6,
            0x112E7, 0x112E8, 0x112E9, 0x112EA, 0x11300, 0x11301, 0x1133B, 0x1133C, 0x11340,
            0x11366, 0x11367, 0x11368, 0x11369, 0x1136A, 0x1136B, 0x1136C, 0x11370, 0x11371,
            0x11372, 0x11373, 0x11374, 0x11438, 0x11439, 0x1143A, 0x1143B, 0x1143C, 0x1143D,
            0x1143E, 0x1143F, 0x11442, 0x11443, 0x11444, 0x11446, 0x1145E, 0x114B3, 0x114B4,
            0x114B5, 0x114B6, 0x114B7, 0x114B8, 0x114BA, 0x114BF, 0x114C0, 0x114C2, 0x114C3,
            0x115B2, 0x115B3, 0x115B4, 0x115B5, 0x115BC, 0x115BD, 0x115BF, 0x115C0, 0x115DC,
            0x115DD, 0x11633, 0x11634, 0x11635, 0x11636, 0x11637, 0x11638, 0x11639, 0x1163A,
            0x1163D, 0x1163F, 0x11640, 0x116AB, 0x116AD, 0x116B0, 0x116B1, 0x116B2, 0x116B3,
            0x116B4, 0x116B5, 0x116B7, 0x1171D, 0x1171E, 0x1171F, 0x11722, 0x11723, 0x11724,
            0x11725, 0x11727, 0x11728, 0x11729, 0x1172A, 0x1172B, 0x1182F, 0x11830, 0x11831,
            0x11832, 0x11833, 0x11834, 0x11835, 0x11836, 0x11837, 0x11839, 0x1183A, 0x119D4,
            0x119D5, 0x119D6, 0x119D7, 0x119DA, 0x119DB, 0x119E0, 0x11A01, 0x11A02, 0x11A03,
            0x11A04, 0x11A05, 0x11A06, 0x11A07, 0x11A08, 0x11A09, 0x11A0A, 0x11A33, 0x11A34,
            0x11A35, 0x11A36, 0x11A37, 0x11A38, 0x11A3B, 0x11A3C, 0x11A3D, 0x11A3E, 0x11A47,
            0x11A51, 0x11A52, 0x11A53, 0x11A54, 0x11A55, 0x11A56, 0x11A59, 0x11A5A, 0x11A5B,
            0x11A8A, 0x11A8B, 0x11A8C, 0x11A8D, 0x11A8E, 0x11A8F, 0x11A90, 0x11A91, 0x11A92,
            0x11A93, 0x11A94, 0x11A95, 0x11A96, 0x11A98, 0x11A99, 0x11C30, 0x11C31, 0x11C32,
            0x11C33, 0x11C34, 0x11C35, 0x11C36, 0x11C38, 0x11C39, 0x11C3A, 0x11C3B, 0x11C3C,
            0x11C3D, 0x11C3F, 0x11C92, 0x11C93, 0x11C94, 0x11C95, 0x11C96, 0x11C97, 0x11C98,
            0x11C99, 0x11C9A, 0x11C9B, 0x11C9C, 0x11C9D, 0x11C9E, 0x11C9F, 0x11CA0, 0x11CA1,
            0x11CA2, 0x11CA3, 0x11CA4, 0x11CA5, 0x11CA6, 0x11CA7, 0x11CAA, 0x11CAB, 0x11CAC,
            0x11CAD, 0x11CAE, 0x11CAF, 0x11CB0, 0x11CB2, 0x11CB3, 0x11CB5, 0x11CB6, 0x11D31,
            0x11D32, 0x11D33, 0x11D34, 0x11D35, 0x11D36, 0x11D3A, 0x11D3C, 0x11D3D, 0x11D3F,
            0x11D40, 0x11D41, 0x11D42, 0x11D43, 0x11D44, 0x11D45, 0x11D47, 0x11D90, 0x11D91,
            0x11D95, 0x11D97, 0x11EF3, 0x11EF4, 0x16AF0, 0x16AF1, 0x16AF2, 0x16AF3, 0x16AF4,
            0x16B30, 0x16B31, 0x16B32, 0x16B33, 0x16B34, 0x16B35, 0x16B36, 0x16F4F, 0x16F8F,
            0x16F90, 0x16F91, 0x16F92, 0x1BC9D, 0x1BC9E, 0x1D167, 0x1D168, 0x1D169, 0x1D17B,
            0x1D17C, 0x1D17D, 0x1D17E, 0x1D17F, 0x1D180, 0x1D181, 0x1D182, 0x1D185, 0x1D186,
            0x1D187, 0x1D188, 0x1D189, 0x1D18A, 0x1D18B, 0x1D1AA, 0x1D1AB, 0x1D1AC, 0x1D1AD,
            0x1D242, 0x1D243, 0x1D244, 0x1DA00, 0x1DA01, 0x1DA02, 0x1DA03, 0x1DA04, 0x1DA05,
            0x1DA06, 0x1DA07, 0x1DA08, 0x1DA09, 0x1DA0A, 0x1DA0B, 0x1DA0C, 0x1DA0D, 0x1DA0E,
            0x1DA0F, 0x1DA10, 0x1DA11, 0x1DA12, 0x1DA13, 0x1DA14, 0x1DA15, 0x1DA16, 0x1DA17,
            0x1DA18, 0x1DA19, 0x1DA1A, 0x1DA1B, 0x1DA1C, 0x1DA1D, 0x1DA1E, 0x1DA1F, 0x1DA20,
            0x1DA21, 0x1DA22, 0x1DA23, 0x1DA24, 0x1DA25, 0x1DA26, 0x1DA27, 0x1DA28, 0x1DA29,
            0x1DA2A, 0x1DA2B, 0x1DA2C, 0x1DA2D, 0x1DA2E, 0x1DA2F, 0x1DA30, 0x1DA31, 0x1DA32,
            0x1DA33, 0x1DA34, 0x1DA35, 0x1DA36, 0x1DA3B, 0x1DA3C, 0x1DA3D, 0x1DA3E, 0x1DA3F,
            0x1DA40, 0x1DA41, 0x1DA42, 0x1DA43, 0x1DA44, 0x1DA45, 0x1DA46, 0x1DA47, 0x1DA48,
            0x1DA49, 0x1DA4A, 0x1DA4B, 0x1DA4C, 0x1DA4D, 0x1DA4E, 0x1DA4F, 0x1DA50, 0x1DA51,
            0x1DA52, 0x1DA53, 0x1DA54, 0x1DA55, 0x1DA56, 0x1DA57, 0x1DA58, 0x1DA59, 0x1DA5A,
            0x1DA5B, 0x1DA5C, 0x1DA5D, 0x1DA5E, 0x1DA5F, 0x1DA60, 0x1DA61, 0x1DA62, 0x1DA63,
            0x1DA64, 0x1DA65, 0x1DA66, 0x1DA67, 0x1DA68, 0x1DA69, 0x1DA6A, 0x1DA6B, 0x1DA6C,
            0x1DA75, 0x1DA84, 0x1DA9B, 0x1DA9C, 0x1DA9D, 0x1DA9E, 0x1DA9F, 0x1DAA1, 0x1DAA2,
            0x1DAA3, 0x1DAA4, 0x1DAA5, 0x1DAA6, 0x1DAA7, 0x1DAA8, 0x1DAA9, 0x1DAAA, 0x1DAAB,
            0x1DAAC, 0x1DAAD, 0x1DAAE, 0x1DAAF, 0x1E000, 0x1E001, 0x1E002, 0x1E003, 0x1E004,
            0x1E005, 0x1E006, 0x1E008, 0x1E009, 0x1E00A, 0x1E00B, 0x1E00C, 0x1E00D, 0x1E00E,
            0x1E00F, 0x1E010, 0x1E011, 0x1E012, 0x1E013, 0x1E014, 0x1E015, 0x1E016, 0x1E017,
            0x1E018, 0x1E01B, 0x1E01C, 0x1E01D, 0x1E01E, 0x1E01F, 0x1E020, 0x1E021, 0x1E023,
            0x1E024, 0x1E026, 0x1E027, 0x1E028, 0x1E029, 0x1E02A, 0x1E130, 0x1E131, 0x1E132,
            0x1E133, 0x1E134, 0x1E135, 0x1E136, 0x1E2EC, 0x1E2ED, 0x1E2EE, 0x1E2EF, 0x1E8D0,
            0x1E8D1, 0x1E8D2, 0x1E8D3, 0x1E8D4, 0x1E8D5, 0x1E8D6, 0x1E944, 0x1E945, 0x1E946,
            0x1E947, 0x1E948, 0x1E949, 0x1E94A, 0xE0100, 0xE0101, 0xE0102, 0xE0103, 0xE0104,
            0xE0105, 0xE0106, 0xE0107, 0xE0108, 0xE0109, 0xE010A, 0xE010B, 0xE010C, 0xE010D,
            0xE010E, 0xE010F, 0xE0110, 0xE0111, 0xE0112, 0xE0113, 0xE0114, 0xE0115, 0xE0116,
            0xE0117, 0xE0118, 0xE0119, 0xE011A, 0xE011B, 0xE011C, 0xE011D, 0xE011E, 0xE011F,
            0xE0120, 0xE0121, 0xE0122, 0xE0123, 0xE0124, 0xE0125, 0xE0126, 0xE0127, 0xE0128,
            0xE0129, 0xE012A, 0xE012B, 0xE012C, 0xE012D, 0xE012E, 0xE012F, 0xE0130, 0xE0131,
            0xE0132, 0xE0133, 0xE0134, 0xE0135, 0xE0136, 0xE0137, 0xE0138, 0xE0139, 0xE013A,
            0xE013B, 0xE013C, 0xE013D, 0xE013E, 0xE013F, 0xE0140, 0xE0141, 0xE0142, 0xE0143,
            0xE0144, 0xE0145, 0xE0146, 0xE0147, 0xE0148, 0xE0149, 0xE014A, 0xE014B, 0xE014C,
            0xE014D, 0xE014E, 0xE014F, 0xE0150, 0xE0151, 0xE0152, 0xE0153, 0xE0154, 0xE0155,
            0xE0156, 0xE0157, 0xE0158, 0xE0159, 0xE015A, 0xE015B, 0xE015C, 0xE015D, 0xE015E,
            0xE015F, 0xE0160, 0xE0161, 0xE0162, 0xE0163, 0xE0164, 0xE0165, 0xE0166, 0xE0167,
            0xE0168, 0xE0169, 0xE016A, 0xE016B, 0xE016C, 0xE016D, 0xE016E, 0xE016F, 0xE0170,
            0xE0171, 0xE0172, 0xE0173, 0xE0174, 0xE0175, 0xE0176, 0xE0177, 0xE0178, 0xE0179,
            0xE017A, 0xE017B, 0xE017C, 0xE017D, 0xE017E, 0xE017F, 0xE0180, 0xE0181, 0xE0182,
            0xE0183, 0xE0184, 0xE0185, 0xE0186, 0xE0187, 0xE0188, 0xE0189, 0xE018A, 0xE018B,
            0xE018C, 0xE018D, 0xE018E, 0xE018F, 0xE0190, 0xE0191, 0xE0192, 0xE0193, 0xE0194,
            0xE0195, 0xE0196, 0xE0197, 0xE0198, 0xE0199, 0xE019A, 0xE019B, 0xE019C, 0xE019D,
            0xE019E, 0xE019F, 0xE01A0, 0xE01A1, 0xE01A2, 0xE01A3, 0xE01A4, 0xE01A5, 0xE01A6,
            0xE01A7, 0xE01A8, 0xE01A9, 0xE01AA, 0xE01AB, 0xE01AC, 0xE01AD, 0xE01AE, 0xE01AF,
            0xE01B0, 0xE01B1, 0xE01B2, 0xE01B3, 0xE01B4, 0xE01B5, 0xE01B6, 0xE01B7, 0xE01B8,
            0xE01B9, 0xE01BA, 0xE01BB, 0xE01BC, 0xE01BD, 0xE01BE, 0xE01BF, 0xE01C0, 0xE01C1,
            0xE01C2, 0xE01C3, 0xE01C4, 0xE01C5, 0xE01C6, 0xE01C7, 0xE01C8, 0xE01C9, 0xE01CA,
            0xE01CB, 0xE01CC, 0xE01CD, 0xE01CE, 0xE01CF, 0xE01D0, 0xE01D1, 0xE01D2, 0xE01D3,
            0xE01D4, 0xE01D5, 0xE01D6, 0xE01D7, 0xE01D8, 0xE01D9, 0xE01DA, 0xE01DB, 0xE01DC,
            0xE01DD, 0xE01DE, 0xE01DF, 0xE01E0, 0xE01E1, 0xE01E2, 0xE01E3, 0xE01E4, 0xE01E5,
            0xE01E6, 0xE01E7, 0xE01E8, 0xE01E9, 0xE01EA, 0xE01EB, 0xE01EC, 0xE01ED, 0xE01EE,
            0xE01EF, 0x04F5, 0x22AD, 0x1F7A, 0x1F08, 0x1E13, 0x1E2E, 0x0201, 0x1EB7, 0x1EF5,
            0x219A, 0x1F95, 0x014C, 0x014E, 0x30B4, 0x1E10, 0x0B94, 0x00DC, 0x1EB9, 0x307C, 0x01D1,
            0x1E21, 0x01F0, 0x1EE4, 0x1EDB, 0x1EF4, 0x00C0, 0x30BA, 0x04F0, 0x015A, 0x30DA, 0x1EE7,
            0x1E74, 0x0156, 0x04DF, 0x1EC0, 0x21AE, 0x1FB0, 0x1F67, 0x2241, 0x30D3, 0x1FAB, 0x0130,
            0x1EA0, 0x1EE6, 0x0171, 0x3054, 0x305E, 0x1E36, 0x01E0, 0x0477, 0x012E, 0x1F23, 0x1F65,
            0x1F27, 0x011D, 0x0143, 0x00D1, 0x1FDA, 0x03CC, 0x0400, 0x1F61, 0x22AC, 0x30DC, 0x1F59,
            0x0210, 0x1E46, 0x1F8D, 0x1ED9, 0x01DA, 0x022A, 0x1F04, 0x30D0, 0x04D7, 0x220C, 0x1EF2,
            0x04DE, 0x0148, 0x013D, 0x1E79, 0x04D2, 0x1EDA, 0x0200, 0x0155, 0x1EBC, 0x1B0E, 0x1E16,
            0x0134, 0x1F78, 0x305A, 0x0150, 0x0105, 0x30D9, 0x1F9B, 0x1F44, 0x0205, 0x1FC4, 0x1E78,
            0x021B, 0x1FD6, 0x1FAF, 0x038E, 0x1F89, 0x00D9, 0x01CD, 0x00C7, 0x1E1E, 0x03AD, 0x0D4B,
            0x0203, 0x1E4E, 0x1F7C, 0x045E, 0x1FDE, 0x038A, 0x1E8F, 0x1E5C, 0x0215, 0x09CB, 0x1B08,
            0x1E2D, 0x307D, 0x30C9, 0x22EB, 0x1EE9, 0x1EF3, 0x1E86, 0x012F, 0x1E92, 0x1FE6, 0x1ECE,
            0x1F2F, 0x1EDC, 0x1F9A, 0x1E53, 0x21CD, 0x0172, 0x1E96, 0x021F, 0x0139, 0x2288, 0x1F49,
            0x1FE8, 0x0B48, 0x3076, 0x1E5D, 0x1FAA, 0x0162, 0x1EB4, 0x1F48, 0x1F0F, 0x04D0, 0x1E51,
            0x04EF, 0x219B, 0x2262, 0x022D, 0x1F30, 0x00F6, 0x1E61, 0x01B0, 0x30B8, 0x1E68, 0x0102,
            0x1F29, 0x0109, 0x0135, 0x01FB, 0x0219, 0x1FC2, 0x1EBB, 0x017D, 0x2226, 0x1F68, 0x0157,
            0x1F0B, 0x04DD, 0x1F88, 0x1E07, 0x2275, 0x1E7E, 0x1ECC, 0x0230, 0x06C0, 0x1F00, 0x00D5,
            0x1F05, 0x0113, 0x1EB8, 0x0BCA, 0x2271, 0x1F09, 0x1E14, 0x1E44, 0x1E27, 0x020E, 0x0168,
            0x1FEC, 0x03AC, 0x0214, 0x226F, 0x1E67, 0x1F84, 0x00C8, 0x1B0A, 0x30FE, 0x1F6C, 0x1EBD,
            0x1FD1, 0x09CC, 0x04F8, 0x0145, 0x1FA3, 0x0439, 0x1F01, 0x021A, 0x1E6B, 0x03CE, 0x1F39,
            0x30B2, 0x00FB, 0x0CC8, 0x30BE, 0x01F9, 0x1F3F, 0x01E1, 0x2209, 0x0419, 0x010E, 0x016F,
            0x0623, 0x016D, 0x1F74, 0x1FA6, 0x1FA9, 0x1E65, 0x1F2E, 0x1FB2, 0x1F8F, 0x1E6E, 0x2281,
            0x2289, 0x1F07, 0x03AB, 0x1FA5, 0x1ED2, 0x022B, 0x0CC0, 0x1F3E, 0x22EA, 0x0211, 0x30C0,
            0x04EB, 0x2285, 0x1FE2, 0x1EBE, 0x0232, 0x00ED, 0x3060, 0x1E0A, 0x1ED8, 0x1F91, 0x0202,
            0x0213, 0x1FEA, 0x1F5B, 0x01DF, 0x1F52, 0x226D, 0x0144, 0x1E77, 0x1ED1, 0x1FF6, 0x03AE,
            0x1E45, 0x0100, 0x1F1D, 0x1FB1, 0x307A, 0x1E88, 0x04F1, 0x1E18, 0x0401, 0x01EA, 0x04DB,
            0x0407, 0x1ED7, 0x1FD0, 0x0117, 0x1F6E, 0x04E3, 0x1F43, 0x1FDD, 0x00D3, 0x1E11, 0x1E1D,
            0x017A, 0x1E05, 0x1FF2, 0x1EF1, 0x1F64, 0x3067, 0x2270, 0x1F33, 0x1EA1, 0x1F4A, 0x00DA,
            0x0C48, 0x1FD2, 0x01EE, 0x1E5B, 0x1F26, 0x04D3, 0x2284, 0x1F5D, 0x1F0E, 0x1E0C, 0x1026,
            0x1FB9, 0x1E34, 0x00EC, 0x1E2A, 0x0107, 0x1F6F, 0x1F10, 0x30D1, 0x0147, 0x0B4C, 0x0386,
            0x0119, 0x1FCD, 0x1FC6, 0x0116, 0x1F3C, 0x3077, 0x1E30, 0x1EB1, 0x1EAB, 0x00C2, 0x00EB,
            0x1E8A, 0x0164, 0x1F50, 0x1F11, 0x1B40, 0x1F14, 0x3074, 0x1F98, 0x2279, 0x2280, 0x011C,
            0x00CB, 0x1E72, 0x1F9C, 0x0136, 0x00CE, 0x0125, 0x1E58, 0x00E1, 0x01E8, 0x1EC8, 0x013E,
            0x1E40, 0x1E38, 0x1FE1, 0x1E99, 0x1F28, 0x1E70, 0x1F4B, 0x1B43, 0x1F20, 0x1E2F, 0x1E09,
            0x1E7A, 0x1EC4, 0x1F70, 0x06D3, 0x30D6, 0x1F9F, 0x1F72, 0x1E73, 0x04D6, 0x011B, 0x0174,
            0x1EB6, 0x1F87, 0x0385, 0x0177, 0x1E4B, 0x01D2, 0x1FF3, 0x0108, 0x1EF9, 0x1E6D, 0x1EA9,
            0x1F32, 0x1E31, 0x22E3, 0x1F19, 0x1F2D, 0x0118, 0x1FB7, 0x022C, 0x04D1, 0x1E4F, 0x1EAC,
            0x1F6A, 0x1FCA, 0x00C9, 0x012B, 0x0BCC, 0x014D, 0x00D6, 0x0BCB, 0x01D3, 0x1FB4, 0x1FCF,
            0x30D4, 0x1E1F, 0x01F8, 0x00FF, 0x1E2B, 0x010F, 0x022E, 0x1F5F, 0x30B0, 0x01E3, 0x03CD,
            0x1E85, 0x1F31, 0x1EE5, 0x1EF6, 0x01EF, 0x016C, 0x01FD, 0x1E7B, 0x020C, 0x1F40, 0x01FE,
            0x1EAA, 0x1F53, 0x1EED, 0x1E4A, 0x1ECF, 0x040D, 0x22AE, 0x1FA8, 0x1B3D, 0x1F42, 0x1F8B,
            0x1FE0, 0x1E12, 0x0CCB, 0x1EC5, 0x040E, 0x1E08, 0x017E, 0x0121, 0x0206, 0x00F2, 0x30BC,
            0x04E4, 0x3052, 0x1E37, 0x1F21, 0x1E98, 0x1ECB, 0x00E3, 0x30F9, 0x1FB3, 0x0D4A, 0x1F76,
            0x21CE, 0x1FA2, 0x0175, 0x01D6, 0x1E17, 0x3069, 0x1F66, 0x1E25, 0x1F18, 0x1EC1, 0x1FE7,
            0x2244, 0x3079, 0x1F38, 0x1E2C, 0x1E87, 0x00E9, 0x1E29, 0x22ED, 0x1FB6, 0x1FB8, 0x1E3E,
            0x1EA2, 0x1B06, 0x226E, 0x0178, 0x1EB5, 0x1E28, 0x1E43, 0x3065, 0x0457, 0x0176, 0x06C2,
            0x045C, 0x1EA7, 0x0388, 0x00E7, 0x1E9B, 0x0624, 0x30AC, 0x1EE2, 0x04DA, 0x1E94, 0x01E7,
            0x1FF8, 0x0229, 0x1ECA, 0x1F13, 0x1F57, 0x1E42, 0x1F34, 0x013C, 0x1F85, 0x1EDD, 0x04DC,
            0x1E0B, 0x1E3C, 0x1FE5, 0x01EC, 0x1F0A, 0x00EA, 0x1FBA, 0x1E55, 0x1F82, 0x1EB2, 0x0169,
            0x1F8C, 0x03B0, 0x1E97, 0x1FC8, 0x22E1, 0x1E93, 0x020F, 0x01EB, 0x1F22, 0x1F1B, 0x1E7C,
            0x01E9, 0x04EA, 0x1FFA, 0x1FC1, 0x04C2, 0x01D7, 0x016B, 0x2247, 0x012A, 0x04E5, 0x0D4C,
            0x00F9, 0x22EC, 0x00FC, 0x1F51, 0x1E66, 0x04C1, 0x01AF, 0x22AF, 0x1E50, 0x1F37, 0x1F96,
            0x22E2, 0x1E54, 0x013B, 0x0179, 0x1E02, 0x1FE4, 0x1F45, 0x1E56, 0x010C, 0x1E63, 0x3056,
            0x3070, 0x04ED, 0x1FA7, 0x1EE8, 0x1F2B, 0x1B41, 0x00CA, 0x014F, 0x01D8, 0x30FA, 0x1F9D,
            0x1E52, 0x00E4, 0x1FA0, 0x1F8A, 0x0103, 0x1F62, 0x1EEA, 0x04E7, 0x2224, 0x0390, 0x30D7,
            0x1F92, 0x1E4C, 0x1FAC, 0x1E6A, 0x1E39, 0x0DDA, 0x1EE1, 0x021E, 0x1EC3, 0x0158, 0x1ED6,
            0x011A, 0x30F7, 0x0124, 0x1E5A, 0x1EA6, 0x1E71, 0x04EC, 0x0DDE, 0x00C4, 0x0476, 0x01FC,
            0x1F35, 0x1F63, 0x0CCA, 0x1EA4, 0x03CB, 0x1EA5, 0x1F56, 0x00E0, 0x020A, 0x0123, 0x1E8E,
            0x1FCC, 0x1ED4, 0x1EE0, 0x1FDF, 0x040C, 0x0165, 0x01DB, 0x1E8D, 0x01D5, 0x00E2, 0x1E90,
            0x0934, 0x01A0, 0x0212, 0x00F1, 0x0204, 0x1EEC, 0x0112, 0x1F25, 0x0231, 0x1FD8, 0x1E48,
            0x1F3A, 0x012C, 0x012D, 0x1F86, 0x00CD, 0x1E80, 0x30F8, 0x0160, 0x00CC, 0x3073, 0x1F2A,
            0x1E22, 0x1EDE, 0x01ED, 0x1E69, 0x1ECD, 0x00C3, 0x1FBC, 0x0104, 0x1E23, 0x1E49, 0x0DDD,
            0x1E8C, 0x00C1, 0x30C5, 0x1E04, 0x015D,
    });

    public static readonly Dictionary<byte, char> BytesToUnicode = new Dictionary<byte, char>
        {
            {33, '!'},
            {34, '"'},
            {35, '#'},
            {36, '$'},
            {37, '%'},
            {38, '&'},
            {39, '\''},
            {40, '{'},
            {41, '}'},
            {42, '*'},
            {43, '+'},
            {44, ','},
            {45, '-'},
            {46, '.'},
            {47, '/'},
            {48, '0'},
            {49, '1'},
            {50, '2'},
            {51, '3'},
            {52, '4'},
            {53, '5'},
            {54, '6'},
            {55, '7'},
            {56, '8'},
            {57, '9'},
            {58, ':'},
            {59, ';'},
            {60, '<'},
            {61, '='},
            {62, '>'},
            {63, '?'},
            {64, '@'},
            {65, 'A'},
            {66, 'B'},
            {67, 'C'},
            {68, 'D'},
            {69, 'E'},
            {70, 'F'},
            {71, 'G'},
            {72, 'H'},
            {73, 'I'},
            {74, 'J'},
            {75, 'K'},
            {76, 'L'},
            {77, 'M'},
            {78, 'N'},
            {79, 'O'},
            {80, 'P'},
            {81, 'Q'},
            {82, 'R'},
            {83, 'S'},
            {84, 'T'},
            {85, 'U'},
            {86, 'V'},
            {87, 'W'},
            {88, 'X'},
            {89, 'Y'},
            {90, 'Z'},
            {91, '['},
            {92, '\\'},
            {93, ']'},
            {94, '^'},
            {95, '_'},
            {96, '`'},
            {97, 'a'},
            {98, 'b'},
            {99, 'c'},
            {100, 'd'},
            {101, 'e'},
            {102, 'f'},
            {103, 'g'},
            {104, 'h'},
            {105, 'i'},
            {106, 'j'},
            {107, 'k'},
            {108, 'l'},
            {109, 'm'},
            {110, 'n'},
            {111, 'o'},
            {112, 'p'},
            {113, 'q'},
            {114, 'r'},
            {115, 's'},
            {116, 't'},
            {117, 'u'},
            {118, 'v'},
            {119, 'w'},
            {120, 'x'},
            {121, 'y'},
            {122, 'z'},
            {123, '{'},
            {124, '|'},
            {125, '}'},
            {126, '~'},
            {161, '¡'},
            {162, '¢'},
            {163, '£'},
            {164, '¤'},
            {165, '¥'},
            {166, '¦'},
            {167, '§'},
            {168, '¨'},
            {169, '©'},
            {170, 'ª'},
            {171, '«'},
            {172, '¬'},
            {174, '®'},
            {175, '¯'},
            {176, '°'},
            {177, '±'},
            {178, '²'},
            {179, '³'},
            {180, '´'},
            {181, 'µ'},
            {182, '¶'},
            {183, '·'},
            {184, '¸'},
            {185, '¹'},
            {186, 'º'},
            {187, '»'},
            {188, '¼'},
            {189, '½'},
            {190, '¾'},
            {191, '¿'},
            {192, 'À'},
            {193, 'Á'},
            {194, 'Â'},
            {195, 'Ã'},
            {196, 'Ä'},
            {197, 'Å'},
            {198, 'Æ'},
            {199, 'Ç'},
            {200, 'È'},
            {201, 'É'},
            {202, 'Ê'},
            {203, 'Ë'},
            {204, 'Ì'},
            {205, 'Í'},
            {206, 'Î'},
            {207, 'Ï'},
            {208, 'Ð'},
            {209, 'Ñ'},
            {210, 'Ò'},
            {211, 'Ó'},
            {212, 'Ô'},
            {213, 'Õ'},
            {214, 'Ö'},
            {215, '×'},
            {216, 'Ø'},
            {217, 'Ù'},
            {218, 'Ú'},
            {219, 'Û'},
            {220, 'Ü'},
            {221, 'Ý'},
            {222, 'Þ'},
            {223, 'ß'},
            {224, 'à'},
            {225, 'á'},
            {226, 'â'},
            {227, 'ã'},
            {228, 'ä'},
            {229, 'å'},
            {230, 'æ'},
            {231, 'ç'},
            {232, 'è'},
            {233, 'é'},
            {234, 'ê'},
            {235, 'ë'},
            {236, 'ì'},
            {237, 'í'},
            {238, 'î'},
            {239, 'ï'},
            {240, 'ð'},
            {241, 'ñ'},
            {242, 'ò'},
            {243, 'ó'},
            {244, 'ô'},
            {245, 'õ'},
            {246, 'ö'},
            {247, '÷'},
            {248, 'ø'},
            {249, 'ù'},
            {250, 'ú'},
            {251, 'û'},
            {252, 'ü'},
            {253, 'ý'},
            {254, 'þ'},
            {255, 'ÿ'},
            {0, 'Ā'},
            {1, 'ā'},
            {2, 'Ă'},
            {3, 'ă'},
            {4, 'Ą'},
            {5, 'ą'},
            {6, 'Ć'},
            {7, 'ć'},
            {8, 'Ĉ'},
            {9, 'ĉ'},
            {10, 'Ċ'},
            {11, 'ċ'},
            {12, 'Č'},
            {13, 'č'},
            {14, 'Ď'},
            {15, 'ď'},
            {16, 'Đ'},
            {17, 'đ'},
            {18, 'Ē'},
            {19, 'ē'},
            {20, 'Ĕ'},
            {21, 'ĕ'},
            {22, 'Ė'},
            {23, 'ė'},
            {24, 'Ę'},
            {25, 'ę'},
            {26, 'Ě'},
            {27, 'ě'},
            {28, 'Ĝ'},
            {29, 'ĝ'},
            {30, 'Ğ'},
            {31, 'ğ'},
            {32, 'Ġ'},
            {127, 'ġ'},
            {128, 'Ģ'},
            {129, 'ģ'},
            {130, 'Ĥ'},
            {131, 'ĥ'},
            {132, 'Ħ'},
            {133, 'ħ'},
            {134, 'Ĩ'},
            {135, 'ĩ'},
            {136, 'Ī'},
            {137, 'ī'},
            {138, 'Ĭ'},
            {139, 'ĭ'},
            {140, 'Į'},
            {141, 'į'},
            {142, 'İ'},
            {143, 'ı'},
            {144, 'Ĳ'},
            {145, 'ĳ'},
            {146, 'Ĵ'},
            {147, 'ĵ'},
            {148, 'Ķ'},
            {149, 'ķ'},
            {150, 'ĸ'},
            {151, 'Ĺ'},
            {152, 'ĺ'},
            {153, 'Ļ'},
            {154, 'ļ'},
            {155, 'Ľ'},
            {156, 'ľ'},
            {157, 'Ŀ'},
            {158, 'ŀ'},
            {159, 'Ł'},
            {160, 'ł'},
            {173, 'Ń'},
        };

    public static readonly Dictionary<char, byte> UnicodeToBytes;

    static Constants()
    {
        UnicodeToBytes = new Dictionary<char, byte>();
        foreach (var pair in BytesToUnicode)
        {
            UnicodeToBytes[pair.Value] = pair.Key;
        }
    }
}