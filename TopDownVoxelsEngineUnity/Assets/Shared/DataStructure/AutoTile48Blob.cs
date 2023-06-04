using System;
using System.Collections.Generic;
using Shared;

namespace Shared {
    public static class AutoTile48Blob {
        public static readonly Dictionary<int, int> BitmaskToBlobMappingsDic = new() {
            {Convert.ToInt32("00111000", 2), 0},
            {Convert.ToInt32("00111001", 2), 0},
            {Convert.ToInt32("00111100", 2), 0},
            {Convert.ToInt32("00111101", 2), 0},
            {Convert.ToInt32("01111000", 2), 0},
            {Convert.ToInt32("01111001", 2), 0},
            {Convert.ToInt32("01111100", 2), 0},
            {Convert.ToInt32("01111101", 2), 0},

            {Convert.ToInt32("11111000", 2), 1},
            {Convert.ToInt32("11111001", 2), 1},
            {Convert.ToInt32("11111100", 2), 1},
            {Convert.ToInt32("11111101", 2), 1},

            {Convert.ToInt32("11100000", 2), 2},
            {Convert.ToInt32("11100001", 2), 2},
            {Convert.ToInt32("11100100", 2), 2},
            {Convert.ToInt32("11100101", 2), 2},
            {Convert.ToInt32("11110000", 2), 2},
            {Convert.ToInt32("11110001", 2), 2},
            {Convert.ToInt32("11110100", 2), 2},
            {Convert.ToInt32("11110101", 2), 2},

            {Convert.ToInt32("00100000", 2), 3},
            {Convert.ToInt32("00100001", 2), 3},
            {Convert.ToInt32("00100100", 2), 3},
            {Convert.ToInt32("00100101", 2), 3},
            {Convert.ToInt32("00110000", 2), 3},
            {Convert.ToInt32("00110001", 2), 3},
            {Convert.ToInt32("00110100", 2), 3},
            {Convert.ToInt32("00110101", 2), 3},
            {Convert.ToInt32("01100000", 2), 3},
            {Convert.ToInt32("01100001", 2), 3},
            {Convert.ToInt32("01100100", 2), 3},
            {Convert.ToInt32("01100101", 2), 3},
            {Convert.ToInt32("01110000", 2), 3},
            {Convert.ToInt32("01110001", 2), 3},
            {Convert.ToInt32("01110100", 2), 3},
            {Convert.ToInt32("01110101", 2), 3},

            {Convert.ToInt32("00101000", 2), 4},
            {Convert.ToInt32("00101001", 2), 4},
            {Convert.ToInt32("00101100", 2), 4},
            {Convert.ToInt32("00101101", 2), 4},
            {Convert.ToInt32("01101000", 2), 4},
            {Convert.ToInt32("01101001", 2), 4},
            {Convert.ToInt32("01101100", 2), 4},
            {Convert.ToInt32("01101101", 2), 4},

            {Convert.ToInt32("11101000", 2), 5},
            {Convert.ToInt32("11101001", 2), 5},
            {Convert.ToInt32("11101100", 2), 5},
            {Convert.ToInt32("11101101", 2), 5},

            {Convert.ToInt32("10111000", 2), 6},
            {Convert.ToInt32("10111001", 2), 6},
            {Convert.ToInt32("10111100", 2), 6},
            {Convert.ToInt32("10111101", 2), 6},

            {Convert.ToInt32("10100000", 2), 7},
            {Convert.ToInt32("10100001", 2), 7},
            {Convert.ToInt32("10100100", 2), 7},
            {Convert.ToInt32("10100101", 2), 7},
            {Convert.ToInt32("10110000", 2), 7},
            {Convert.ToInt32("10110001", 2), 7},
            {Convert.ToInt32("10110100", 2), 7},
            {Convert.ToInt32("10110101", 2), 7},

            {Convert.ToInt32("10101000", 2), 8},
            {Convert.ToInt32("10101001", 2), 8},
            {Convert.ToInt32("10101100", 2), 8},
            {Convert.ToInt32("10101101", 2), 8},

            {Convert.ToInt32("10111011", 2), 9},


            {Convert.ToInt32("00111110", 2), 11},
            {Convert.ToInt32("00111111", 2), 11},
            {Convert.ToInt32("01111110", 2), 11},
            {Convert.ToInt32("01111111", 2), 11},

            {Convert.ToInt32("11111111", 2), 12},

            {Convert.ToInt32("11100011", 2), 13},
            {Convert.ToInt32("11100111", 2), 13},
            {Convert.ToInt32("11110011", 2), 13},
            {Convert.ToInt32("11110111", 2), 13},

            {Convert.ToInt32("00100010", 2), 14},
            {Convert.ToInt32("00100011", 2), 14},
            {Convert.ToInt32("00100110", 2), 14},
            {Convert.ToInt32("00100111", 2), 14},
            {Convert.ToInt32("00110010", 2), 14},
            {Convert.ToInt32("00110011", 2), 14},
            {Convert.ToInt32("00110110", 2), 14},
            {Convert.ToInt32("00110111", 2), 14},
            {Convert.ToInt32("01100010", 2), 14},
            {Convert.ToInt32("01100011", 2), 14},
            {Convert.ToInt32("01100110", 2), 14},
            {Convert.ToInt32("01100111", 2), 14},
            {Convert.ToInt32("01110010", 2), 14},
            {Convert.ToInt32("01110011", 2), 14},
            {Convert.ToInt32("01110110", 2), 14},
            {Convert.ToInt32("01110111", 2), 14},

            {Convert.ToInt32("00101110", 2), 15},
            {Convert.ToInt32("00101111", 2), 15},
            {Convert.ToInt32("01101110", 2), 15},
            {Convert.ToInt32("01101111", 2), 15},

            {Convert.ToInt32("11101111", 2), 16},
            {Convert.ToInt32("10111111", 2), 17},

            {Convert.ToInt32("10100011", 2), 18},
            {Convert.ToInt32("10100111", 2), 18},
            {Convert.ToInt32("10110011", 2), 18},
            {Convert.ToInt32("10110111", 2), 18},

            {Convert.ToInt32("10101111", 2), 19},
            {Convert.ToInt32("11101110", 2), 20},

            {Convert.ToInt32("00001110", 2), 22},
            {Convert.ToInt32("00001111", 2), 22},
            {Convert.ToInt32("00011110", 2), 22},
            {Convert.ToInt32("00011111", 2), 22},
            {Convert.ToInt32("01001110", 2), 22},
            {Convert.ToInt32("01001111", 2), 22},
            {Convert.ToInt32("01011110", 2), 22},
            {Convert.ToInt32("01011111", 2), 22},

            {Convert.ToInt32("10001111", 2), 23},
            {Convert.ToInt32("10011111", 2), 23},
            {Convert.ToInt32("11001111", 2), 23},
            {Convert.ToInt32("11011111", 2), 23},

            {Convert.ToInt32("10000011", 2), 24},
            {Convert.ToInt32("10000111", 2), 24},
            {Convert.ToInt32("10010011", 2), 24},
            {Convert.ToInt32("10010111", 2), 24},
            {Convert.ToInt32("11000011", 2), 24},
            {Convert.ToInt32("11000111", 2), 24},
            {Convert.ToInt32("11010011", 2), 24},
            {Convert.ToInt32("11010111", 2), 24},

            {Convert.ToInt32("00000010", 2), 25},
            {Convert.ToInt32("00000011", 2), 25},
            {Convert.ToInt32("00000110", 2), 25},
            {Convert.ToInt32("00000111", 2), 25},
            {Convert.ToInt32("00010010", 2), 25},
            {Convert.ToInt32("00010011", 2), 25},
            {Convert.ToInt32("00010110", 2), 25},
            {Convert.ToInt32("00010111", 2), 25},
            {Convert.ToInt32("01000010", 2), 25},
            {Convert.ToInt32("01000011", 2), 25},
            {Convert.ToInt32("01000110", 2), 25},
            {Convert.ToInt32("01000111", 2), 25},
            {Convert.ToInt32("01010010", 2), 25},
            {Convert.ToInt32("01010011", 2), 25},
            {Convert.ToInt32("01010110", 2), 25},
            {Convert.ToInt32("01010111", 2), 25},

            {Convert.ToInt32("00111010", 2), 26},
            {Convert.ToInt32("00111011", 2), 26},
            {Convert.ToInt32("01111010", 2), 26},
            {Convert.ToInt32("01111011", 2), 26},

            {Convert.ToInt32("11111011", 2), 27},
            {Convert.ToInt32("11111110", 2), 28},

            {Convert.ToInt32("11100010", 2), 29},
            {Convert.ToInt32("11100110", 2), 29},
            {Convert.ToInt32("11110010", 2), 29},
            {Convert.ToInt32("11110110", 2), 29},

            {Convert.ToInt32("11111010", 2), 30},
            {Convert.ToInt32("10111010", 2), 31},
            {Convert.ToInt32("11101010", 2), 32},

            {Convert.ToInt32("00001000", 2), 33},
            {Convert.ToInt32("00001001", 2), 33},
            {Convert.ToInt32("00001100", 2), 33},
            {Convert.ToInt32("00001101", 2), 33},
            {Convert.ToInt32("00011000", 2), 33},
            {Convert.ToInt32("00011001", 2), 33},
            {Convert.ToInt32("00011100", 2), 33},
            {Convert.ToInt32("00011101", 2), 33},
            {Convert.ToInt32("01001000", 2), 33},
            {Convert.ToInt32("01001001", 2), 33},
            {Convert.ToInt32("01001100", 2), 33},
            {Convert.ToInt32("01001101", 2), 33},
            {Convert.ToInt32("01011000", 2), 33},
            {Convert.ToInt32("01011001", 2), 33},
            {Convert.ToInt32("01011100", 2), 33},
            {Convert.ToInt32("01011101", 2), 33},

            {Convert.ToInt32("10001000", 2), 34},
            {Convert.ToInt32("10001001", 2), 34},
            {Convert.ToInt32("10001100", 2), 34},
            {Convert.ToInt32("10001101", 2), 34},
            {Convert.ToInt32("10011000", 2), 34},
            {Convert.ToInt32("10011001", 2), 34},
            {Convert.ToInt32("10011100", 2), 34},
            {Convert.ToInt32("10011101", 2), 34},
            {Convert.ToInt32("11001000", 2), 34},
            {Convert.ToInt32("11001001", 2), 34},
            {Convert.ToInt32("11001100", 2), 34},
            {Convert.ToInt32("11001101", 2), 34},
            {Convert.ToInt32("11011000", 2), 34},
            {Convert.ToInt32("11011001", 2), 34},
            {Convert.ToInt32("11011100", 2), 34},
            {Convert.ToInt32("11011101", 2), 34},

            {Convert.ToInt32("10000000", 2), 35},
            {Convert.ToInt32("10000001", 2), 35},
            {Convert.ToInt32("10000100", 2), 35},
            {Convert.ToInt32("10000101", 2), 35},
            {Convert.ToInt32("10010000", 2), 35},
            {Convert.ToInt32("10010001", 2), 35},
            {Convert.ToInt32("10010100", 2), 35},
            {Convert.ToInt32("10010101", 2), 35},
            {Convert.ToInt32("11000000", 2), 35},
            {Convert.ToInt32("11000001", 2), 35},
            {Convert.ToInt32("11000100", 2), 35},
            {Convert.ToInt32("11000101", 2), 35},
            {Convert.ToInt32("11010000", 2), 35},
            {Convert.ToInt32("11010001", 2), 35},
            {Convert.ToInt32("11010100", 2), 35},
            {Convert.ToInt32("11010101", 2), 35},

            {Convert.ToInt32("00000000", 2), 36},
            {Convert.ToInt32("00000001", 2), 36},
            {Convert.ToInt32("00000100", 2), 36},
            {Convert.ToInt32("00000101", 2), 36},
            {Convert.ToInt32("00010000", 2), 36},
            {Convert.ToInt32("00010001", 2), 36},
            {Convert.ToInt32("00010100", 2), 36},
            {Convert.ToInt32("00010101", 2), 36},
            {Convert.ToInt32("01000000", 2), 36},
            {Convert.ToInt32("01000001", 2), 36},
            {Convert.ToInt32("01000100", 2), 36},
            {Convert.ToInt32("01000101", 2), 36},
            {Convert.ToInt32("01010000", 2), 36},
            {Convert.ToInt32("01010001", 2), 36},
            {Convert.ToInt32("01010100", 2), 36},
            {Convert.ToInt32("01010101", 2), 36},

            {Convert.ToInt32("00001010", 2), 37},
            {Convert.ToInt32("00001011", 2), 37},
            {Convert.ToInt32("00011010", 2), 37},
            {Convert.ToInt32("00011011", 2), 37},
            {Convert.ToInt32("01001010", 2), 37},
            {Convert.ToInt32("01001011", 2), 37},
            {Convert.ToInt32("01011010", 2), 37},
            {Convert.ToInt32("01011011", 2), 37},

            {Convert.ToInt32("10001011", 2), 38},
            {Convert.ToInt32("11001011", 2), 38},
            {Convert.ToInt32("10011011", 2), 38},
            {Convert.ToInt32("11011011", 2), 38},

            {Convert.ToInt32("10001110", 2), 39},
            {Convert.ToInt32("11001110", 2), 39},
            {Convert.ToInt32("10011110", 2), 39},
            {Convert.ToInt32("11011110", 2), 39},

            {Convert.ToInt32("10000010", 2), 40},
            {Convert.ToInt32("10000110", 2), 40},
            {Convert.ToInt32("10010010", 2), 40},
            {Convert.ToInt32("10010110", 2), 40},
            {Convert.ToInt32("11000010", 2), 40},
            {Convert.ToInt32("11000110", 2), 40},
            {Convert.ToInt32("11010010", 2), 40},
            {Convert.ToInt32("11010110", 2), 40},

            {Convert.ToInt32("10001010", 2), 41},
            {Convert.ToInt32("11001010", 2), 41},
            {Convert.ToInt32("10011010", 2), 41},
            {Convert.ToInt32("11011010", 2), 41},

            {Convert.ToInt32("10101110", 2), 42},
            {Convert.ToInt32("10101011", 2), 43},

            {Convert.ToInt32("00101010", 2), 48},
            {Convert.ToInt32("00101011", 2), 48},
            {Convert.ToInt32("01101010", 2), 48},
            {Convert.ToInt32("01101011", 2), 48},

            {Convert.ToInt32("11101011", 2), 49},
            {Convert.ToInt32("10111110", 2), 50},

            {Convert.ToInt32("10100010", 2), 51},
            {Convert.ToInt32("10100110", 2), 51},
            {Convert.ToInt32("10110010", 2), 51},
            {Convert.ToInt32("10110110", 2), 51},

            {Convert.ToInt32("10101010", 2), 52},
        };

        public static readonly int[] BitmaskToBlobMappings = {
            /* 00000000 */ 36,
            /* 00000001 */ 36,
            /* 00000010 */ 25,
            /* 00000011 */ 25,
            /* 00000100 */ 36,
            /* 00000101 */ 36,
            /* 00000110 */ 25,
            /* 00000111 */ 25,
            /* 00001000 */ 33,
            /* 00001001 */ 33,
            /* 00001010 */ 37,
            /* 00001011 */ 37,
            /* 00001100 */ 33,
            /* 00001101 */ 33,
            /* 00001110 */ 22,
            /* 00001111 */ 22,
            /* 00010000 */ 36,
            /* 00010001 */ 36,
            /* 00010010 */ 25,
            /* 00010011 */ 25,
            /* 00010100 */ 36,
            /* 00010101 */ 36,
            /* 00010110 */ 25,
            /* 00010111 */ 25,
            /* 00011000 */ 33,
            /* 00011001 */ 33,
            /* 00011010 */ 37,
            /* 00011011 */ 37,
            /* 00011100 */ 33,
            /* 00011101 */ 33,
            /* 00011110 */ 22,
            /* 00011111 */ 22,
            /* 00100000 */ 3,
            /* 00100001 */ 3,
            /* 00100010 */ 14,
            /* 00100011 */ 14,
            /* 00100100 */ 3,
            /* 00100101 */ 3,
            /* 00100110 */ 14,
            /* 00100111 */ 14,
            /* 00101000 */ 4,
            /* 00101001 */ 4,
            /* 00101010 */ 48,
            /* 00101011 */ 48,
            /* 00101100 */ 4,
            /* 00101101 */ 4,
            /* 00101110 */ 15,
            /* 00101111 */ 15,
            /* 00110000 */ 3,
            /* 00110001 */ 3,
            /* 00110010 */ 14,
            /* 00110011 */ 14,
            /* 00110100 */ 3,
            /* 00110101 */ 3,
            /* 00110110 */ 14,
            /* 00110111 */ 14,
            /* 00111000 */ 0,
            /* 00111001 */ 0,
            /* 00111010 */ 26,
            /* 00111011 */ 26,
            /* 00111100 */ 0,
            /* 00111101 */ 0,
            /* 00111110 */ 11,
            /* 00111111 */ 11,
            /* 01000000 */ 36,
            /* 01000001 */ 36,
            /* 01000010 */ 25,
            /* 01000011 */ 25,
            /* 01000100 */ 36,
            /* 01000101 */ 36,
            /* 01000110 */ 25,
            /* 01000111 */ 25,
            /* 01001000 */ 33,
            /* 01001001 */ 33,
            /* 01001010 */ 37,
            /* 01001011 */ 37,
            /* 01001100 */ 33,
            /* 01001101 */ 33,
            /* 01001110 */ 22,
            /* 01001111 */ 22,
            /* 01010000 */ 36,
            /* 01010001 */ 36,
            /* 01010010 */ 25,
            /* 01010011 */ 25,
            /* 01010100 */ 36,
            /* 01010101 */ 36,
            /* 01010110 */ 25,
            /* 01010111 */ 25,
            /* 01011000 */ 33,
            /* 01011001 */ 33,
            /* 01011010 */ 37,
            /* 01011011 */ 37,
            /* 01011100 */ 33,
            /* 01011101 */ 33,
            /* 01011110 */ 22,
            /* 01011111 */ 22,
            /* 01100000 */ 3,
            /* 01100001 */ 3,
            /* 01100010 */ 14,
            /* 01100011 */ 14,
            /* 01100100 */ 3,
            /* 01100101 */ 3,
            /* 01100110 */ 14,
            /* 01100111 */ 14,
            /* 01101000 */ 4,
            /* 01101001 */ 4,
            /* 01101010 */ 48,
            /* 01101011 */ 48,
            /* 01101100 */ 4,
            /* 01101101 */ 4,
            /* 01101110 */ 15,
            /* 01101111 */ 15,
            /* 01110000 */ 3,
            /* 01110001 */ 3,
            /* 01110010 */ 14,
            /* 01110011 */ 14,
            /* 01110100 */ 3,
            /* 01110101 */ 3,
            /* 01110110 */ 14,
            /* 01110111 */ 14,
            /* 01111000 */ 0,
            /* 01111001 */ 0,
            /* 01111010 */ 26,
            /* 01111011 */ 26,
            /* 01111100 */ 0,
            /* 01111101 */ 0,
            /* 01111110 */ 11,
            /* 01111111 */ 11,
            /* 10000000 */ 35,
            /* 10000001 */ 35,
            /* 10000010 */ 40,
            /* 10000011 */ 24,
            /* 10000100 */ 35,
            /* 10000101 */ 35,
            /* 10000110 */ 40,
            /* 10000111 */ 24,
            /* 10001000 */ 34,
            /* 10001001 */ 34,
            /* 10001010 */ 41,
            /* 10001011 */ 38,
            /* 10001100 */ 34,
            /* 10001101 */ 34,
            /* 10001110 */ 39,
            /* 10001111 */ 23,
            /* 10010000 */ 35,
            /* 10010001 */ 35,
            /* 10010010 */ 40,
            /* 10010011 */ 24,
            /* 10010100 */ 35,
            /* 10010101 */ 35,
            /* 10010110 */ 40,
            /* 10010111 */ 24,
            /* 10011000 */ 34,
            /* 10011001 */ 34,
            /* 10011010 */ 41,
            /* 10011011 */ 38,
            /* 10011100 */ 34,
            /* 10011101 */ 34,
            /* 10011110 */ 39,
            /* 10011111 */ 23,
            /* 10100000 */ 7,
            /* 10100001 */ 7,
            /* 10100010 */ 51,
            /* 10100011 */ 18,
            /* 10100100 */ 7,
            /* 10100101 */ 7,
            /* 10100110 */ 51,
            /* 10100111 */ 18,
            /* 10101000 */ 8,
            /* 10101001 */ 8,
            /* 10101010 */ 52,
            /* 10101011 */ 43,
            /* 10101100 */ 8,
            /* 10101101 */ 8,
            /* 10101110 */ 42,
            /* 10101111 */ 19,
            /* 10110000 */ 7,
            /* 10110001 */ 7,
            /* 10110010 */ 51,
            /* 10110011 */ 18,
            /* 10110100 */ 7,
            /* 10110101 */ 7,
            /* 10110110 */ 51,
            /* 10110111 */ 18,
            /* 10111000 */ 6,
            /* 10111001 */ 6,
            /* 10111010 */ 31,
            /* 10111011 */ 9,
            /* 10111100 */ 6,
            /* 10111101 */ 6,
            /* 10111110 */ 50,
            /* 10111111 */ 17,
            /* 11000000 */ 35,
            /* 11000001 */ 35,
            /* 11000010 */ 40,
            /* 11000011 */ 24,
            /* 11000100 */ 35,
            /* 11000101 */ 35,
            /* 11000110 */ 40,
            /* 11000111 */ 24,
            /* 11001000 */ 34,
            /* 11001001 */ 34,
            /* 11001010 */ 41,
            /* 11001011 */ 38,
            /* 11001100 */ 34,
            /* 11001101 */ 34,
            /* 11001110 */ 39,
            /* 11001111 */ 23,
            /* 11010000 */ 35,
            /* 11010001 */ 35,
            /* 11010010 */ 40,
            /* 11010011 */ 24,
            /* 11010100 */ 35,
            /* 11010101 */ 35,
            /* 11010110 */ 40,
            /* 11010111 */ 24,
            /* 11011000 */ 34,
            /* 11011001 */ 34,
            /* 11011010 */ 41,
            /* 11011011 */ 38,
            /* 11011100 */ 34,
            /* 11011101 */ 34,
            /* 11011110 */ 39,
            /* 11011111 */ 23,
            /* 11100000 */ 2,
            /* 11100001 */ 2,
            /* 11100010 */ 29,
            /* 11100011 */ 13,
            /* 11100100 */ 2,
            /* 11100101 */ 2,
            /* 11100110 */ 29,
            /* 11100111 */ 13,
            /* 11101000 */ 5,
            /* 11101001 */ 5,
            /* 11101010 */ 32,
            /* 11101011 */ 49,
            /* 11101100 */ 5,
            /* 11101101 */ 5,
            /* 11101110 */ 20,
            /* 11101111 */ 16,
            /* 11110000 */ 2,
            /* 11110001 */ 2,
            /* 11110010 */ 29,
            /* 11110011 */ 13,
            /* 11110100 */ 2,
            /* 11110101 */ 2,
            /* 11110110 */ 29,
            /* 11110111 */ 13,
            /* 11111000 */ 1,
            /* 11111001 */ 1,
            /* 11111010 */ 30,
            /* 11111011 */ 27,
            /* 11111100 */ 1,
            /* 11111101 */ 1,
            /* 11111110 */ 28,
            /* 11111111 */ 12,
        };

        // Bitmask expected weights depending on "linked" adjacent tiles
        // 0    2   4
        // 128  x   8
        // 64  32  16
        // x, y, z are positive when going right, up and forward in unity
        /// <param name="bitMask"></param>
        /// <returns></returns>
        public static int GetBlobIndex(int bitMask) {
            if (bitMask < 0 || bitMask >= BitmaskToBlobMappings.Length) return 12;
            return BitmaskToBlobMappings[bitMask];
        }

        private static readonly Vector3Int[] Positions = new Vector3Int[8];

        public static int Get8SurroundingsBitmask(Direction dir, int x, int y, int z, BlockId referenceBlock, Func<Vector3Int, BlockId, bool> isBlockMatching) {
            // take the surrounding blocks positions from "top left" like if we are facing the side.
            // 0 1 2
            // 7 x 3
            // 6 5 4
            // x, y, z are positive when going right, up and forward in unity
            if (dir == Direction.Up) {
                // For the side facing up, we are looking down
                // z
                // ^
                // |
                // -----> x
                Positions[0] = new(x - 1, y, z + 1);
                Positions[1] = new(x, y, z + 1);
                Positions[2] = new(x + 1, y, z + 1);
                Positions[3] = new(x + 1, y, z);
                Positions[4] = new(x + 1, y, z - 1);
                Positions[5] = new(x, y, z - 1);
                Positions[6] = new(x - 1, y, z - 1);
                Positions[7] = new(x - 1, y, z);
            } else if (dir == Direction.Down) {
                // For the side facing down, we are looking up
                // -----> x
                // |
                // v
                // z
                Positions[0] = new(x - 1, y, z - 1);
                Positions[1] = new(x, y, z - 1);
                Positions[2] = new(x + 1, y, z - 1);
                Positions[3] = new(x + 1, y, z);
                Positions[4] = new(x + 1, y, z + 1);
                Positions[5] = new(x, y, z + 1);
                Positions[6] = new(x - 1, y, z + 1);
                Positions[7] = new(x - 1, y, z);
            } else if (dir == Direction.South) {
                // For the side facing North (Forward), we are looking south
                //      y
                //      ^
                //      |
                // x <---
                Positions[0] = new(x + 1, y + 1, z);
                Positions[1] = new(x, y + 1, z);
                Positions[2] = new(x - 1, y + 1, z);
                Positions[3] = new(x - 1, y, z);
                Positions[4] = new(x - 1, y - 1, z);
                Positions[5] = new(x, y - 1, z);
                Positions[6] = new(x + 1, y - 1, z);
                Positions[7] = new(x + 1, y, z);
            } else if (dir == Direction.North) {
                // For the side facing South (Backward), we are looking north
                // y
                // ^
                // |
                // -----> x
                Positions[0] = new(x - 1, y + 1, z);
                Positions[1] = new(x, y + 1, z);
                Positions[2] = new(x + 1, y + 1, z);
                Positions[3] = new(x + 1, y, z);
                Positions[4] = new(x + 1, y - 1, z);
                Positions[5] = new(x, y - 1, z);
                Positions[6] = new(x - 1, y - 1, z);
                Positions[7] = new(x - 1, y, z);
            } else if (dir == Direction.West) {
                // For the side facing West (Left), we are looking East
                //      y
                //      ^
                //      |
                // z <---
                Positions[0] = new(x, y + 1, z + 1);
                Positions[1] = new(x, y + 1, z);
                Positions[2] = new(x, y + 1, z - 1);
                Positions[3] = new(x, y, z - 1);
                Positions[4] = new(x, y - 1, z - 1);
                Positions[5] = new(x, y - 1, z);
                Positions[6] = new(x, y - 1, z + 1);
                Positions[7] = new(x, y, z + 1);
            } else if (dir == Direction.East) {
                // For the side facing East (Right), we are looking West
                // y
                // ^
                // |
                // -----> z
                Positions[0] = new(x, y + 1, z - 1);
                Positions[1] = new(x, y + 1, z);
                Positions[2] = new(x, y + 1, z + 1);
                Positions[3] = new(x, y, z + 1);
                Positions[4] = new(x, y - 1, z + 1);
                Positions[5] = new(x, y - 1, z);
                Positions[6] = new(x, y - 1, z - 1);
                Positions[7] = new(x, y, z - 1);
            }

            int bitmask = 0;
            for (var i = 0; i < Positions.Length; i++) {
                int isSameBlock = isBlockMatching(Positions[i], referenceBlock) ? 1 : 0;
                bitmask |= isSameBlock << i;
            }

            return bitmask;
        }
    }
}