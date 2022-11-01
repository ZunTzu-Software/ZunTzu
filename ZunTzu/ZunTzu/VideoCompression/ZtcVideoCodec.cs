// Copyright (c) 2022 ZunTzu Software and contributors

using System;

namespace ZunTzu.VideoCompression {

    public class ZtcVideoCodec : IVideoCodec {

		/// <summary>Compresses a frame.</summary>
		/// <param name="frameBuffer">An uncompressed frame buffer in R8G8B8 format.</param>
		/// <param name="compressedBuffer">A buffer that will receive the compressed frame.</param>
		/// <param name="byteCount">The number of bytes written in the result buffer.</param>
		public unsafe void Encode(IntPtr frameBuffer, IntPtr compressedBuffer, out int byteCount) {
			// convert to YCbCr
			byte* YCbCr = stackalloc byte[64 * 64 + 32 * 32 * 2];
			convertToYCbCr((byte*) frameBuffer, YCbCr);

			// convert to blocks
			byte* blocks = stackalloc byte[(16 * 16 + 8 * 8 * 2) * (2 + 16)];
			convertToBlocks(YCbCr, blocks, quantizationCoefficient);

			// entropy encode
			byteCount = entropyEncode((byte*) compressedBuffer, null, blocks, quantizationCoefficient, codeBook60);

			// reverse the process to update initial frame accordingly =>
			convertFromBlocks(blocks, YCbCr, quantizationCoefficient);
			convertFromYCbCr(YCbCr, (byte*) frameBuffer);
		}

		/// <summary>Compresses a frame based on a reference frame.</summary>
		/// <param name="referenceFrameBuffer">A reference frame buffer in R8G8B8 format.</param>
		/// <param name="frameBuffer">An uncompressed frame buffer in R8G8B8 format.</param>
		/// <param name="compressedBuffer">A buffer that will receive the compressed frame.</param>
		/// <param name="byteCount">The number of bytes written in the result buffer.</param>
		public unsafe void Encode(IntPtr referenceFrameBuffer, IntPtr frameBuffer, IntPtr compressedBuffer, out int byteCount) {
			// convert to YCbCr
			byte* YCbCr = stackalloc byte[64 * 64 + 32 * 32 * 2];
			convertToYCbCr((byte*) frameBuffer, YCbCr);

			// motion compensation
			byte* motionVectors = stackalloc byte[16 * 16];
			evaluateMotion(YCbCr, (byte*) referenceFrameBuffer, motionVectors);
			byte* refYCbCr = stackalloc byte[64 * 64 + 32 * 32 * 2];
			byte* mcRefFrame = stackalloc byte[64 * 64 * 3];
			applyMotionCompensation((byte*) referenceFrameBuffer, motionVectors, mcRefFrame);
			// convert to YCbCr
			convertToYCbCr(mcRefFrame, refYCbCr);

			// diff between frame and reference frame
			byte* diffFrame = stackalloc byte[64 * 64 + 32 * 32 * 2];
			substract(YCbCr, refYCbCr, diffFrame);

			// convert to blocks
			byte* blocks = stackalloc byte[(16 * 16 + 8 * 8 * 2) * (2 + 16)];
			convertToBlocks(diffFrame, blocks, quantizationCoefficient);

			// entropy encode
			byteCount = entropyEncode((byte*) compressedBuffer, motionVectors, blocks, quantizationCoefficient, codeBook61);

			// reverse the process to update initial frame accordingly =>
			convertFromBlocks(blocks, diffFrame, quantizationCoefficient);
			add(refYCbCr, diffFrame, YCbCr);
			convertFromYCbCr(YCbCr, (byte*) frameBuffer);
		}

		/// <summary>Uncompresses a frame.</summary>
		/// <param name="compressedBuffer">A compressed frame.</param>
		/// <param name="frameBuffer">A buffer that will receive an uncompressed frame.</param>
		public unsafe void Decode(IntPtr compressedBuffer, IntPtr frameBuffer) {
			byte* blocks = stackalloc byte[(16 * 16 + 8 * 8 * 2) * (2 + 16)];
			entropyDecode((byte*) compressedBuffer, null, blocks, quantizationCoefficient, decodeTree60);
			byte* YCbCr = stackalloc byte[64 * 64 + 32 * 32 * 2];
			convertFromBlocks(blocks, YCbCr, quantizationCoefficient);
			convertFromYCbCr(YCbCr, (byte*) frameBuffer);
		}

		/// <summary>Uncompresses a frame based on a reference frame.</summary>
		/// <param name="referenceFrameBuffer">A reference frame buffer in R8G8B8 format.</param>
		/// <param name="compressedBuffer">A compressed frame.</param>
		/// <param name="frameBuffer">A buffer that will receive an uncompressed frame.</param>
		public unsafe void Decode(IntPtr referenceFrameBuffer, IntPtr compressedBuffer, IntPtr frameBuffer) {
			byte* motionVectors = stackalloc byte[16 * 16];
			byte* blocks = stackalloc byte[(16 * 16 + 8 * 8 * 2) * (2 + 16)];
			entropyDecode((byte*) compressedBuffer, motionVectors, blocks, quantizationCoefficient, decodeTree61);
			byte* diffFrame = stackalloc byte[64 * 64 + 32 * 32 * 2];
			convertFromBlocks(blocks, diffFrame, quantizationCoefficient);
			byte* refYCbCr = stackalloc byte[64 * 64 + 32 * 32 * 2];
			byte* mcRefFrame = stackalloc byte[64 * 64 * 3];
			applyMotionCompensation((byte*) referenceFrameBuffer, motionVectors, mcRefFrame);
			convertToYCbCr(mcRefFrame, refYCbCr);
			byte* YCbCr = stackalloc byte[64 * 64 + 32 * 32 * 2];
			add(refYCbCr, diffFrame, YCbCr);
			convertFromYCbCr(YCbCr, (byte*) frameBuffer);
		}

		private static int quantizationCoefficient = 6;
		private enum CodeBookPage { MotionVector, LuminanceMedian, LuminanceVariation, LuminanceNaryValue, LuminanceTrinaryValue, LuminanceBinaryValue, ChrominanceMedian, ChrominanceVariation, ChrominanceNaryValue, ChrominanceTrinaryValue, ChrominanceBinaryValue };
		private static int[][] stats60 = {
			null,
			new int[] { 1, 2, 3, 30004, 520005, 1074006, 3222007, 7872008, 12187009, 15226010, 19961011, 23670012, 26554013, 28758014, 28091015, 26378016, 24850017, 23003018, 20006019, 19537020, 17962021, 17616022, 16465023, 14264024, 13772025, 12965026, 12449027, 11865028, 11807029, 11485030, 11571031, 11210032, 11045033, 10853034, 10648035, 10310036, 10322037, 10279038, 10099039, 10059040, 10073041, 9879042, 9921043, 10158044, 9834045, 10067046, 9936047, 10053048, 10162049, 10306050, 10281051, 10267052, 10655053, 10749054, 10452055, 10250056, 10532057, 10623058, 10691059, 10919060, 11004061, 11353062, 11430063, 11531064, 11818064, 12363063, 12657062, 13485061, 13438060, 13920059, 13901058, 12568057, 11853056, 11425055, 11167054, 11079053, 10777052, 10524051, 10090050, 10201049, 9950048, 10006047, 9896046, 10043045, 10076044, 9834043, 10004042, 10253041, 10216040, 9830039, 9637038, 9463037, 9180036, 8756035, 8785034, 8965033, 8891032, 8878031, 8868030, 8791029, 9089028, 9293027, 9337026, 9342025, 10673024, 9900023, 9739022, 9877021, 10604020, 10520019, 10460018, 10710017, 10842016, 9908015, 10527014, 11819013, 11279012, 11138011, 12657010, 13120009, 12876008, 13254007, 14014006, 12492005, 12421004, 15884003, 17707002, 42267001 },
			new int[] { 301309043, 193572042, 114814041, 85049040, 73906039, 64399038, 58795037, 53762036, 48494035, 45122034, 41474033, 38386032, 35755031, 32662030, 29390029, 26916028, 24318027, 21804026, 19072025, 16847024, 15846023, 14368022, 13742021, 12888020, 11958019, 12041018, 11444017, 10546016, 9754015, 9358014, 9085013, 8773012, 8917011, 8590010, 7583009, 5896008, 4596007, 4249006, 4621005, 1770004, 577003, 2, 1 },
			new int[] { 598165, 303664, 121694, 111614, 297139, 86635, 166566, 107379, 123232, 100443, 32082, 14515, 87113, 87199, 14322, 9335, 279405, 138750, 100428, 54845, 80805, 42526, 49522, 29769, 127755, 137854, 24874, 7911, 93252, 39705, 7470, 6604, 130914, 97426, 37167, 20208, 144852, 48243, 18780, 13186, 34368, 27034, 10339, 4947, 14262, 7653, 2850, 2835, 119093, 57320, 18576, 12723, 102137, 29952, 13044, 11919, 14174, 8361, 6115, 3077, 8238, 7508, 2302, 1590 },
			new int[] { 122004, 30508, 39273, 13059, 37161, 8552, 15190, 1174, 38780, 26082, 7562, 1424, 11324, 1125, 1554, 666, 43719, 8838, 13098, 1948, 7890, 1672, 2498, 327, 15375, 1262, 2305, 548, 1164, 95, 288, 138, 29574, 10945, 6684, 1667, 27911, 2357, 1592, 831, 6698, 3545, 1565, 361, 1309, 276, 91, 136, 11737, 1981, 1854, 1051, 998, 360, 547, 58, 1098, 740, 363, 60, 692, 127, 135, 124 },
			new int[] { 137102, 38132, 63544, 15318, 61040, 7317, 8554, 2342, 64271, 15421, 8375, 1206, 9082, 1696, 2377, 642, 41940, 55025, 16925, 1389, 8575, 4242, 1996, 724, 5092, 2777, 2129, 447, 1420, 955, 523, 138 },
			new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 1038, 3039, 1040, 3041, 3042, 9043, 16044, 15045, 19046, 30047, 97048, 184049, 220050, 207051, 235052, 318053, 492054, 667055, 984056, 1372057, 1971058, 3293059, 7421060, 16074061, 39775062, 82093063, 100087064, 136458064, 92900063, 54188062, 40122061, 36372060, 32012059, 29772058, 21830057, 16482056, 13239055, 9864054, 6646053, 3878052, 2128051, 1374050, 908049, 651048, 496047, 325046, 239045, 194044, 170043, 156042, 111041, 66040, 30039, 13038, 4037, 2036, 3035, 1034, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 },
			new int[] { 458057043, 194499042, 58568041, 24193040, 10562039, 5676038, 2667037, 1194036, 464035, 170034, 96033, 44032, 23031, 6030, 2029, 3028, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 },
			new int[] { 31701, 17194, 5643, 5502, 16729, 3941, 8446, 5359, 5946, 5078, 1416, 573, 4633, 4693, 760, 388, 16183, 6611, 4003, 2192, 4125, 1717, 2516, 1226, 6008, 5691, 1061, 244, 4694, 2411, 262, 325, 6744, 3885, 1535, 946, 7366, 2270, 793, 614, 1829, 1401, 313, 142, 688, 341, 109, 69, 5838, 2294, 901, 487, 4579, 1388, 765, 583, 805, 391, 162, 41, 537, 333, 54, 26 },
			new int[] { 63504, 18746, 16621, 8564, 16064, 3559, 11887, 582, 14605, 10148, 4221, 877, 10029, 576, 1254, 400, 18786, 3585, 4257, 733, 4351, 654, 1538, 161, 12460, 557, 1217, 294, 758, 53, 123, 25, 17238, 4563, 3559, 1055, 11554, 971, 951, 279, 3146, 1776, 663, 177, 879, 121, 73, 36, 9483, 785, 969, 502, 342, 135, 396, 14, 730, 601, 162, 15, 337, 38, 58, 43 },
			new int[] { 138657, 29782, 61827, 12853, 66206, 7927, 10110, 2044, 71100, 18247, 10039, 1178, 9226, 1949, 2239, 591, 33108, 54148, 20293, 1834, 8296, 5090, 1936, 653, 4728, 3469, 2586, 414, 1262, 987, 588, 130 }
		};
		private static int[][] stats61 = {
			new int[] { 837991, 75563, 68921, 62362, 59580, 27051, 25140, 27369, 24191, 30339, 30060, 29088, 28871, 17229, 16508, 17177, 16392, 14723, 15246, 14695, 14628, 15536, 14532, 13999, 15257 },
			new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 1009, 2010, 6011, 3012, 8013, 8014, 8015, 15016, 15017, 19018, 18019, 23020, 31021, 21022, 31023, 42024, 54025, 45026, 64027, 77028, 93029, 98030, 108031, 124032, 145033, 170034, 202035, 233036, 209037, 268038, 279039, 310040, 309041, 366042, 432043, 465044, 505045, 512046, 640047, 761048, 806049, 934050, 1009051, 1221052, 1460053, 1880054, 2169055, 2708056, 3677057, 5095058, 7375059, 10767060, 17174061, 30032062, 60957063, 181160064, 796866064, 215242063, 68516062, 32916061, 18112060, 10986059, 7152058, 5011057, 3655056, 2767055, 2124054, 1616053, 1349052, 1152051, 994050, 861049, 761048, 664047, 600046, 518045, 492044, 444043, 426042, 365041, 354040, 302039, 287038, 278037, 246036, 241035, 224034, 191033, 148032, 147031, 138030, 133029, 110028, 106027, 90026, 91025, 90024, 75023, 74022, 69021, 62020, 44019, 40018, 32017, 30016, 26015, 20014, 22013, 16012, 9011, 8010, 7009, 4008, 1007, 6, 5, 4, 3, 2, 1 },
			new int[] { 736922043, 461656042, 157649041, 75834040, 38541039, 20280038, 10658037, 5512036, 2713035, 1342034, 656033, 311032, 188031, 86030, 45029, 31028, 19027, 3026, 2025, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 },
			new int[] { 58740, 34906, 22578, 16579, 32378, 21434, 17410, 13535, 22184, 16090, 12772, 6840, 14912, 12804, 6643, 5751, 33203, 21742, 15474, 11307, 20994, 14101, 11377, 8785, 17222, 14060, 8522, 4488, 13217, 10194, 5144, 4344, 22751, 15317, 12042, 7425, 16342, 11677, 6269, 5920, 13359, 8620, 6725, 3419, 6576, 5107, 2688, 2426, 17087, 11633, 7208, 4832, 12963, 8767, 5797, 5202, 7104, 4350, 3652, 2107, 5901, 4561, 2286, 1262 },
			new int[] { 88031, 33950, 31644, 23389, 33350, 21054, 15625, 6299, 31004, 14913, 19740, 6344, 22278, 7063, 7416, 8122, 34105, 19090, 13582, 7837, 21116, 13499, 8199, 4378, 14764, 2662, 8489, 4298, 6779, 1167, 4098, 2555, 31350, 13416, 17665, 7474, 15393, 8628, 2523, 4019, 19661, 8299, 12596, 4253, 7215, 4088, 1099, 2405, 22701, 7872, 7585, 6566, 6550, 4274, 4091, 557, 6457, 4520, 4007, 635, 8118, 2565, 2525, 2298 },
			new int[] { 111467, 58658, 63724, 47700, 70302, 39212, 42259, 34539, 70788, 43802, 40811, 33340, 46090, 31423, 32113, 26169, 58641, 59622, 48036, 37291, 39619, 38022, 34176, 27447, 41449, 38984, 33480, 27878, 31053, 28383, 26139, 22351 },
			new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 1054, 55, 2056, 6057, 26058, 40059, 114060, 297061, 880062, 3136063, 184672064, 548617064, 15396063, 1947062, 632061, 256060, 114059, 45058, 23057, 7056, 8055, 2054, 3053, 52, 51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 },
			new int[] { 687013043, 64605042, 3413041, 808040, 268039, 78038, 23037, 9036, 6035, 1034, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 },
			new int[] { 624064, 352063, 167062, 112061, 375060, 193059, 142058, 104057, 214056, 164055, 75054, 19053, 180052, 131051, 31050, 18049, 366048, 196047, 119046, 79045, 186044, 104043, 95042, 55041, 164040, 130039, 37038, 11037, 141036, 68035, 14034, 9033, 163032, 80031, 64030, 26029, 163028, 92027, 22026, 23025, 78024, 49023, 33022, 9021, 29020, 17019, 3018, 4017, 120016, 61015, 25014, 14013, 90012, 35011, 21010, 9009, 20008, 7007, 9006, 2005, 14004, 5003, 3002, 1 },
			new int[] { 3390, 948, 746, 448, 996, 401, 383, 59, 844, 306, 374, 87, 779, 114, 122, 66, 905, 301, 269, 78, 425, 182, 117, 33, 371, 55, 107, 31, 126, 13, 34, 16, 798, 238, 302, 89, 319, 120, 47, 18, 352, 138, 181, 38, 139, 38, 13, 13, 469, 103, 95, 40, 79, 35, 29, 3, 94, 30, 25, 2, 55, 12, 17, 8 },
			new int[] { 26591, 9214, 11553, 6510, 12482, 4966, 5984, 3527, 13500, 5539, 5722, 3398, 6462, 3052, 3417, 2237, 9689, 9332, 6604, 4127, 5135, 4196, 3454, 2481, 5059, 4509, 3420, 2381, 2995, 2583, 2146, 1550 }
		};
		private static DecodeTreeNode[][] decodeTree60 = {
			null,
			Huffman.BuildDecodeTree(stats60[1]),
			Huffman.BuildDecodeTree(stats60[2]),
			Huffman.BuildDecodeTree(stats60[3]),
			Huffman.BuildDecodeTree(stats60[4]),
			Huffman.BuildDecodeTree(stats60[5]),
			Huffman.BuildDecodeTree(stats60[6]),
			Huffman.BuildDecodeTree(stats60[7]),
			Huffman.BuildDecodeTree(stats60[8]),
			Huffman.BuildDecodeTree(stats60[9]),
			Huffman.BuildDecodeTree(stats60[10])
		};
		private static DecodeTreeNode[][] decodeTree61 = {
			Huffman.BuildDecodeTree(stats61[0]),
			Huffman.BuildDecodeTree(stats61[1]),
			Huffman.BuildDecodeTree(stats61[2]),
			Huffman.BuildDecodeTree(stats61[3]),
			Huffman.BuildDecodeTree(stats61[4]),
			Huffman.BuildDecodeTree(stats61[5]),
			Huffman.BuildDecodeTree(stats61[6]),
			Huffman.BuildDecodeTree(stats61[7]),
			Huffman.BuildDecodeTree(stats61[8]),
			Huffman.BuildDecodeTree(stats61[9]),
			Huffman.BuildDecodeTree(stats61[10])
		};
		private static CodeBookEntry[][] codeBook60 = {
			null,
			Huffman.BuildCodeBook(decodeTree60[1], stats60[1].Length),
			Huffman.BuildCodeBook(decodeTree60[2], stats60[2].Length),
			Huffman.BuildCodeBook(decodeTree60[3], stats60[3].Length),
			Huffman.BuildCodeBook(decodeTree60[4], stats60[4].Length),
			Huffman.BuildCodeBook(decodeTree60[5], stats60[5].Length),
			Huffman.BuildCodeBook(decodeTree60[6], stats60[6].Length),
			Huffman.BuildCodeBook(decodeTree60[7], stats60[7].Length),
			Huffman.BuildCodeBook(decodeTree60[8], stats60[8].Length),
			Huffman.BuildCodeBook(decodeTree60[9], stats60[9].Length),
			Huffman.BuildCodeBook(decodeTree60[10], stats60[10].Length)
		};
		private static CodeBookEntry[][] codeBook61 = {
			Huffman.BuildCodeBook(decodeTree61[0], stats61[0].Length),
			Huffman.BuildCodeBook(decodeTree61[1], stats61[1].Length),
			Huffman.BuildCodeBook(decodeTree61[2], stats61[2].Length),
			Huffman.BuildCodeBook(decodeTree61[3], stats61[3].Length),
			Huffman.BuildCodeBook(decodeTree61[4], stats61[4].Length),
			Huffman.BuildCodeBook(decodeTree61[5], stats61[5].Length),
			Huffman.BuildCodeBook(decodeTree61[6], stats61[6].Length),
			Huffman.BuildCodeBook(decodeTree61[7], stats61[7].Length),
			Huffman.BuildCodeBook(decodeTree61[8], stats61[8].Length),
			Huffman.BuildCodeBook(decodeTree61[9], stats61[9].Length),
			Huffman.BuildCodeBook(decodeTree61[10], stats61[10].Length)
		};

		private static unsafe void evaluateMotion(byte* YCbCr, byte* refFrame, byte* motionVectors) {
			// extract luminance
			byte* refYValues = stackalloc byte[68 * 68];	// with a 2 pixels guard band
			int offset1 = 0;
			for(int y = 0; y < 64; ++y) {
				for(int x = 0; x < 64; ++x) {
					byte r = refFrame[offset1 + 0];
					byte g = refFrame[offset1 + 1];
					byte b = refFrame[offset1 + 2];
					offset1 += 3;
					refYValues[(y + 2) * 68 + (x + 2)] = (byte) (0.299f * r + 0.587f * g + 0.114f * b);
				}
			}
			for(int x = 0; x < 64; ++x) {
				refYValues[0 * 68 + (x + 2)] = refYValues[2 * 68 + (x + 2)];
				refYValues[1 * 68 + (x + 2)] = refYValues[2 * 68 + (x + 2)];
				refYValues[66 * 68 + (x + 2)] = refYValues[65 * 68 + (x + 2)];
				refYValues[67 * 68 + (x + 2)] = refYValues[65 * 68 + (x + 2)];
			}
			for(int y = 0; y < 64; ++y) {
				refYValues[(y + 2) * 68 + 0] = refYValues[(y + 2) * 68 + 2];
				refYValues[(y + 2) * 68 + 1] = refYValues[(y + 2) * 68 + 2];
				refYValues[(y + 2) * 68 + 66] = refYValues[(y + 2) * 68 + 65];
				refYValues[(y + 2) * 68 + 67] = refYValues[(y + 2) * 68 + 65];
			}
			refYValues[0 * 68 + 0] = refYValues[2 * 68 + 2];
			refYValues[0 * 68 + 1] = refYValues[2 * 68 + 2];
			refYValues[1 * 68 + 0] = refYValues[2 * 68 + 2];
			refYValues[1 * 68 + 1] = refYValues[2 * 68 + 2];
			refYValues[66 * 68 + 0] = refYValues[65 * 68 + 2];
			refYValues[66 * 68 + 1] = refYValues[65 * 68 + 2];
			refYValues[67 * 68 + 0] = refYValues[65 * 68 + 2];
			refYValues[67 * 68 + 1] = refYValues[65 * 68 + 2];
			refYValues[0 * 68 + 66] = refYValues[2 * 68 + 65];
			refYValues[0 * 68 + 67] = refYValues[2 * 68 + 65];
			refYValues[1 * 68 + 66] = refYValues[2 * 68 + 65];
			refYValues[1 * 68 + 67] = refYValues[2 * 68 + 65];
			refYValues[66 * 68 + 66] = refYValues[65 * 68 + 65];
			refYValues[66 * 68 + 67] = refYValues[65 * 68 + 65];
			refYValues[67 * 68 + 66] = refYValues[65 * 68 + 65];
			refYValues[67 * 68 + 67] = refYValues[65 * 68 + 65];

			// for each block, find best vector
			for(int y = 0; y < 16; ++y) {
				for(int x = 0; x < 16; ++x) {
					int bestDiff = int.MaxValue;
					int bestVector = 0;
					for(int v = 0; v < 25; ++v) {
						int minDiff = int.MaxValue;
						int maxDiff = int.MinValue;
						for(int blockY = 0; blockY < 4; ++blockY) {
							for(int blockX = 0; blockX < 4; ++blockX) {
								int dx = vectors[v, 0];
								int dy = vectors[v, 1];
								int diff = (int) YCbCr[(y * 4 + blockY) * 64 + (x * 4 + blockX)] -
									(int) refYValues[(y * 4 + blockY + 2 + dy) * 68 + (x * 4 + blockX + 2 + dx)];
								minDiff = Math.Min(minDiff, diff);
								maxDiff = Math.Max(maxDiff, diff);
							}
						}
						if(maxDiff - minDiff < bestDiff) {
							bestDiff = maxDiff - minDiff;
							bestVector = v;
						}
					}
					motionVectors[y * 16 + x] = (byte) bestVector;
				}
			}
		}

		private static unsafe void applyMotionCompensation(byte* refFrame, byte* mc, byte* mcRefFrame) {
			for(int y = 0; y < 16; ++y) {
				for(int x = 0; x < 16; ++x) {
					int v = mc[y * 16 + x];
					for(int blockY = 0; blockY < 4; ++blockY) {
						for(int blockX = 0; blockX < 4; ++blockX) {
							int dx = vectors[v, 0];
							if(x == 0) {
								if(blockX + dx < 0)
									dx = -blockX;
							} else if(x == 15) {
								if(blockX + dx > 3)
									dx = 3 - blockX;
							}
							int dy = vectors[v, 1];
							if(y == 0) {
								if(blockY + dy < 0)
									dy = -blockY;
							} else if(y == 15) {
								if(blockY + dy > 3)
									dy = 3 - blockY;
							}
							int offset2 = ((y * 4 + blockY) * 64 + (x * 4 + blockX)) * 3;
							int offset3 = ((y * 4 + blockY + dy) * 64 + (x * 4 + blockX + dx)) * 3;
							mcRefFrame[offset2 + 0] = refFrame[offset3 + 0];
							mcRefFrame[offset2 + 1] = refFrame[offset3 + 1];
							mcRefFrame[offset2 + 2] = refFrame[offset3 + 2];
						}
					}
				}
			}
		}

		private static int[,] vectors = {
			{ 0,  0},
			{ 0, -1}, { 0, +1}, {-1,  0}, {+1,  0},
			{-1, -1}, {+1, -1}, {-1, +1}, {+1, +1},
			{ 0, -2}, { 0, +2}, {-2,  0}, {+2,  0},
			{-1, -2}, {+1, -2}, {-1, +2}, {+1, +2},
			{-2, -1}, {+2, -1}, {-2, +1}, {+2, +1},
			{-2, -2}, {+2, -2}, {-2, +2}, {+2, +2}
		};

		/// <summary>Converts a 64x64 RGB frame to YCbCr 4:2:2.</summary>
		/// <param name="frame">RGB frame.</param>
		/// <param name="YCbCr">YCbCr 4:2:2 frame.</param>
		private static unsafe void convertToYCbCr(byte* frame, byte* YCbCr) {
			int offset0 = 0;
			int offset1 = 0;
			for(int y = 0; y < 64; ++y) {
				for(int x = 0; x < 64; ++x) {
					byte r = frame[offset0 + 0];
					byte g = frame[offset0 + 1];
					byte b = frame[offset0 + 2];
					offset0 += 3;
					YCbCr[offset1] = clampToByte((int)(0.299f * r + 0.587f * g + 0.114f * b));
					++offset1;
				}
			}
			for(int y = 0; y < 32; ++y) {
				for(int x = 0; x < 32; ++x) {
					float sumCb = 0.0f;
					float sumCr = 0.0f;
					for(int subPixelY = 0; subPixelY < 2; ++subPixelY) {
						for(int subPixelX = 0; subPixelX < 2; ++subPixelX) {
							int offset = (y * 2 + subPixelY) * (64 * 3) + (x * 2 + subPixelX) * 3;
							byte r = frame[offset + 0];
							byte g = frame[offset + 1];
							byte b = frame[offset + 2];
							sumCb += 128.0f - 0.168736f * r - 0.331264f * g + 0.5f * b;
							sumCr += 128.0f + 0.5f * r - 0.418688f * g - 0.081312f * b;
						}
					}
					YCbCr[offset1 + 0] = clampToByte((int)(sumCb * 0.25f + 0.5f));
					YCbCr[offset1 + 1] = clampToByte((int)(sumCr * 0.25f + 0.5f));
					offset1 += 2;
				}
			}
		}

		private static unsafe void substract(byte* YCbCr, byte* refYCbCr, byte* diffFrame) {
			for(int i = 0; i < (64 * 64 + 32 * 32 * 2); ++i)
				diffFrame[i] = (byte) ((255 + (int) YCbCr[i] - (int) refYCbCr[i]) >> 1);
		}

		private static unsafe void convertToBlocks(byte* diffFrame, byte* blocks, int quantization) {
			// Y blocks
			byte* blockValues = stackalloc byte[4 * 4];
			for(int y = 0; y < 16; ++y) {
				for(int x = 0; x < 16; ++x) {
					for(int blockY = 0; blockY < 4; ++blockY)
						for(int blockX = 0; blockX < 4; ++blockX)
							blockValues[blockY * 4 + blockX] = diffFrame[(y * 4 + blockY) * 64 + (x * 4 + blockX)];
					writeBlock(blockValues, &blocks[(y * 16 + x) * 18], quantization);
				}
			}
			// Cb blocks
			for(int y = 0; y < 8; ++y) {
				for(int x = 0; x < 8; ++x) {
					for(int blockY = 0; blockY < 4; ++blockY)
						for(int blockX = 0; blockX < 4; ++blockX)
							blockValues[blockY * 4 + blockX] = diffFrame[64 * 64 + (y * 4 + blockY) * 64 + (x * 4 + blockX) * 2];
					writeBlock(blockValues, &blocks[16 * 16 * 18 + (y * 8 + x) * 18], quantization);
				}
			}
			// Cr blocks
			for(int y = 0; y < 8; ++y) {
				for(int x = 0; x < 8; ++x) {
					for(int blockY = 0; blockY < 4; ++blockY)
						for(int blockX = 0; blockX < 4; ++blockX)
							blockValues[blockY * 4 + blockX] = diffFrame[64 * 64 + (y * 4 + blockY) * 64 + (x * 4 + blockX) * 2 + 1];
					writeBlock(blockValues, &blocks[(16 * 16 + 8 * 8) * 18 + (y * 8 + x) * 18], quantization);
				}
			}
		}

		private static unsafe void writeBlock(byte* values, byte* block, int quantization) {
			int min = 255;
			int max = 0;
			for(int i = 0; i < 16; ++i) {
				int value = values[i];
				if(value < min) min = value;
				if(value > max) max = value;
			}
			block[0] = (byte) (((min + max) / 2) >> 1);
			block[1] = (byte) ((max - min) / quantization);
			int* pixels = stackalloc int[16];
			switch(block[1]) {
				case 0: {
					}
					break;

				case 1: {
						byte interpolant = (byte) ((min + max) / 2);
						for(int i = 0; i < 16; ++i)
							pixels[i] = (values[i] > interpolant ? 1 : 0);
					}
					break;

				case 2: {
						byte* interpolants = stackalloc byte[2];
						for(int i = 0; i < 2; ++i)
							interpolants[i] = (byte) (min + ((i + 1) * (max - min)) / 3);
						for(int i = 0; i < 16; ++i) {
							byte value = values[i];
							int interpolantIndex = 0;
							while(interpolantIndex < 2 && value > interpolants[interpolantIndex])
								++interpolantIndex;
							pixels[i] = interpolantIndex ^ (interpolantIndex >> 1);	// Hamming coding
						}
					}
					break;

				default: {
						byte* interpolants = stackalloc byte[3];
						for(int i = 0; i < 3; ++i)
							interpolants[i] = (byte) (min + ((2 * i + 1) * (max - min)) / 6);
						for(int i = 0; i < 16; ++i) {
							byte value = values[i];
							int interpolantIndex = 0;
							while(interpolantIndex < 3 && value > interpolants[interpolantIndex])
								++interpolantIndex;
							pixels[i] = interpolantIndex ^ (interpolantIndex >> 1);	// Hamming coding
						}
					}
					break;
			}
			// XOR differential encoding
			if(block[1] != 0) {
				block[2] = (byte) pixels[0];
				block[3] = (byte) (pixels[1] ^ pixels[0]);
				block[4] = (byte) (pixels[2] ^ pixels[1]);
				block[5] = (byte) (pixels[3] ^ pixels[2]);
				block[6] = (byte) (pixels[4] ^ pixels[0]);
				block[7] = (byte) (pixels[5] ^ pixels[4]);
				block[8] = (byte) (pixels[6] ^ pixels[5]);
				block[9] = (byte) (pixels[7] ^ pixels[6]);
				block[10] = (byte) (pixels[8] ^ pixels[4]);
				block[11] = (byte) (pixels[9] ^ pixels[8]);
				block[12] = (byte) (pixels[10] ^ pixels[9]);
				block[13] = (byte) (pixels[11] ^ pixels[10]);
				block[14] = (byte) (pixels[12] ^ pixels[8]);
				block[15] = (byte) (pixels[13] ^ pixels[12]);
				block[16] = (byte) (pixels[14] ^ pixels[13]);
				block[17] = (byte) (pixels[15] ^ pixels[14]);
			}
		}

		/// <summary>Writes between 1 to 32 bits into the stream.</summary>
		private static unsafe void writeBits(ref byte* currentByte, ref byte mask, CodeBookEntry entry) {
			int entryMask = (1 << (entry.BitCount - 1));
			while(entryMask != 0) {
				if((entry.Code & entryMask) != 0)
					*currentByte |= mask;
				else
					*currentByte &= (byte) ~mask;
				mask >>= 1;
				if(mask == 0) {
					++currentByte;
					mask = 0x80;
				}
				entryMask >>= 1;
			}
		}

		private static unsafe int entropyEncode(byte* outputBuffer, byte* motionVectors, byte* blocks, int quantization, CodeBookEntry[][] codeBook) {
			byte* currentByte = outputBuffer;
			byte mask = 0x80;

			if(motionVectors != null) {
				for(int j = 0; j < 16 * 16; ++j) {
					byte v = motionVectors[j];
					writeBits(ref currentByte, ref mask, codeBook[(int) CodeBookPage.MotionVector][v]);
				}
			}
			for(int j = 0; j < 16 * 16; ++j) {
				int offset = j * 18;
				byte median = blocks[offset];
				byte variation = blocks[offset + 1];
				writeBits(ref currentByte, ref mask, codeBook[(int) CodeBookPage.LuminanceMedian][median]);
				writeBits(ref currentByte, ref mask, codeBook[(int) CodeBookPage.LuminanceVariation][variation]);
				switch(variation) {
					case 0: {
						}
						break;

					case 1: {
							CodeBookEntry firstPixel;
							firstPixel.Code = blocks[offset + 2];
							firstPixel.BitCount = 1;
							writeBits(ref currentByte, ref mask, firstPixel);
							// group values into 3 5-bits words
							for(int i = 0; i < 3; ++i) {
								int word = (blocks[offset + i * 5 + 3] << 4) | (blocks[offset + i * 5 + 4] << 3) | (blocks[offset + i * 5 + 5] << 2) | (blocks[offset + i * 5 + 6] << 1) | blocks[offset + i * 5 + 7];
								writeBits(ref currentByte, ref mask, codeBook[(int) CodeBookPage.LuminanceBinaryValue][word]);
							}
						}
						break;

					case 2: {
							CodeBookEntry firstPixel;
							firstPixel.Code = blocks[offset + 2];
							firstPixel.BitCount = 2;
							writeBits(ref currentByte, ref mask, firstPixel);
							// group values into 5 6-bits words
							for(int i = 0; i < 5; ++i) {
								int word = (blocks[offset + i * 3 + 3] << 4) | (blocks[offset + i * 3 + 4] << 2) | blocks[offset + i * 3 + 5];
								writeBits(ref currentByte, ref mask, codeBook[(int) CodeBookPage.LuminanceTrinaryValue][word]);
							}
						}
						break;

					default: {
							CodeBookEntry firstPixel;
							firstPixel.Code = blocks[offset + 2];
							firstPixel.BitCount = 2;
							writeBits(ref currentByte, ref mask, firstPixel);
							// group values into 5 6-bits words
							for(int i = 0; i < 5; ++i) {
								int word = (blocks[offset + i * 3 + 3] << 4) | (blocks[offset + i * 3 + 4] << 2) | blocks[offset + i * 3 + 5];
								writeBits(ref currentByte, ref mask, codeBook[(int) CodeBookPage.LuminanceNaryValue][word]);
							}
						}
						break;
				}
			}
			for(int j = 0; j < 8 * 8 * 2; ++j) {
				int offset = 16 * 16 * 18 + j * 18;
				byte median = blocks[offset];
				byte variation = blocks[offset + 1];
				writeBits(ref currentByte, ref mask, codeBook[(int) CodeBookPage.ChrominanceMedian][median]);
				writeBits(ref currentByte, ref mask, codeBook[(int) CodeBookPage.ChrominanceVariation][variation]);
				switch(variation) {
					case 0: {
						}
						break;

					case 1: {
							CodeBookEntry firstPixel;
							firstPixel.Code = blocks[offset + 2];
							firstPixel.BitCount = 1;
							writeBits(ref currentByte, ref mask, firstPixel);
							// group values into 3 5-bits words
							for(int i = 0; i < 3; ++i) {
								int word = (blocks[offset + i * 5 + 3] << 4) | (blocks[offset + i * 5 + 4] << 3) | (blocks[offset + i * 5 + 5] << 2) | (blocks[offset + i * 5 + 6] << 1) | blocks[offset + i * 5 + 7];
								writeBits(ref currentByte, ref mask, codeBook[(int) CodeBookPage.ChrominanceBinaryValue][word]);
							}
						}
						break;

					case 2: {
							CodeBookEntry firstPixel;
							firstPixel.Code = blocks[offset + 2];
							firstPixel.BitCount = 2;
							writeBits(ref currentByte, ref mask, firstPixel);
							// group values into 5 6-bits words
							for(int i = 0; i < 5; ++i) {
								int word = (blocks[offset + i * 3 + 3] << 4) | (blocks[offset + i * 3 + 4] << 2) | blocks[offset + i * 3 + 5];
								writeBits(ref currentByte, ref mask, codeBook[(int) CodeBookPage.ChrominanceTrinaryValue][word]);
							}
						}
						break;

					default: {
							CodeBookEntry firstPixel;
							firstPixel.Code = blocks[offset + 2];
							firstPixel.BitCount = 2;
							writeBits(ref currentByte, ref mask, firstPixel);
							// group values into 5 6-bits words
							for(int i = 0; i < 5; ++i) {
								int word = (blocks[offset + i * 3 + 3] << 4) | (blocks[offset + i * 3 + 4] << 2) | blocks[offset + i * 3 + 5];
								writeBits(ref currentByte, ref mask, codeBook[(int) CodeBookPage.ChrominanceNaryValue][word]);
							}
						}
						break;
				}
			}

			return (int) (currentByte - outputBuffer);
		}

		private static unsafe int decodeBits(ref byte* currentByte, ref byte mask, DecodeTreeNode[] decodeTree, int range) {
			int node = decodeTree.Length - 1;
			while(true) {
				node = ((*currentByte & mask) != 0 ? decodeTree[node].Child1 : decodeTree[node].Child0);
				mask >>= 1;
				if(mask == 0) {
					++currentByte;
					mask = 0x80;
				}
				if(node < range)
					return node;
			}
		}

		private static unsafe int readBits(ref byte* currentByte, ref byte mask, int bitCount) {
			int bits = 0;
			for(int i = 0; i < bitCount; ++i) {
				bits <<= 1;
				if((*currentByte & mask) != 0)
					bits |= 1;
				mask >>= 1;
				if(mask == 0) {
					++currentByte;
					mask = 0x80;
				}
			}
			return bits;
		}

		private static unsafe void entropyDecode(byte* inputBuffer, byte* motionVectors, byte* blocks, int quantization, DecodeTreeNode[][] decodeTree) {
			byte* currentByte = inputBuffer;
			byte mask = 0x80;

			if(motionVectors != null) {
				for(int j = 0; j < 16 * 16; ++j) {
					motionVectors[j] = (byte) decodeBits(ref currentByte, ref mask, decodeTree[(int) CodeBookPage.MotionVector], 25);
				}
			}
			for(int j = 0; j < 16 * 16; ++j) {
				int offset = j * 18;
				blocks[offset] = (byte) decodeBits(ref currentByte, ref mask, decodeTree[(int) CodeBookPage.LuminanceMedian], 128);
				blocks[offset + 1] = (byte) decodeBits(ref currentByte, ref mask, decodeTree[(int) CodeBookPage.LuminanceVariation], (255 / quantization) + 1);
				switch(blocks[offset + 1]) {
					case 0: {
						}
						break;

					case 1: {
							blocks[offset + 2] = (byte) readBits(ref currentByte, ref mask, 1);
							// group values into 3 5-bits words
							for(int i = 0; i < 3; ++i) {
								int word = decodeBits(ref currentByte, ref mask, decodeTree[(int) CodeBookPage.LuminanceBinaryValue], 32);
								blocks[offset + i * 5 + 3] = (byte) (word >> 4);
								blocks[offset + i * 5 + 4] = (byte) ((word & 8) >> 3);
								blocks[offset + i * 5 + 5] = (byte) ((word & 4) >> 2);
								blocks[offset + i * 5 + 6] = (byte) ((word & 2) >> 1);
								blocks[offset + i * 5 + 7] = (byte) (word & 1);
							}
						}
						break;

					case 2: {
							blocks[offset + 2] = (byte) readBits(ref currentByte, ref mask, 2);
							// group values into 5 6-bits words
							for(int i = 0; i < 5; ++i) {
								int word = decodeBits(ref currentByte, ref mask, decodeTree[(int) CodeBookPage.LuminanceTrinaryValue], 64);
								blocks[offset + i * 3 + 3] = (byte) (word >> 4);
								blocks[offset + i * 3 + 4] = (byte) ((word & 12) >> 2);
								blocks[offset + i * 3 + 5] = (byte) (word & 3);
							}
						}
						break;

					default: {
							blocks[offset + 2] = (byte) readBits(ref currentByte, ref mask, 2);
							// group values into 5 6-bits words
							for(int i = 0; i < 5; ++i) {
								int word = decodeBits(ref currentByte, ref mask, decodeTree[(int) CodeBookPage.LuminanceNaryValue], 64);
								blocks[offset + i * 3 + 3] = (byte) (word >> 4);
								blocks[offset + i * 3 + 4] = (byte) ((word & 12) >> 2);
								blocks[offset + i * 3 + 5] = (byte) (word & 3);
							}
						}
						break;
				}
			}
			for(int j = 0; j < 8 * 8 * 2; ++j) {
				int offset = 16 * 16 * 18 + j * 18;
				blocks[offset] = (byte) decodeBits(ref currentByte, ref mask, decodeTree[(int) CodeBookPage.ChrominanceMedian], 128);
				blocks[offset + 1] = (byte) decodeBits(ref currentByte, ref mask, decodeTree[(int) CodeBookPage.ChrominanceVariation], (255 / quantization) + 1);
				switch(blocks[offset + 1]) {
					case 0: {
						}
						break;

					case 1: {
							blocks[offset + 2] = (byte) readBits(ref currentByte, ref mask, 1);
							// group values into 3 5-bits words
							for(int i = 0; i < 3; ++i) {
								int word = decodeBits(ref currentByte, ref mask, decodeTree[(int) CodeBookPage.ChrominanceBinaryValue], 32);
								blocks[offset + i * 5 + 3] = (byte) (word >> 4);
								blocks[offset + i * 5 + 4] = (byte) ((word & 8) >> 3);
								blocks[offset + i * 5 + 5] = (byte) ((word & 4) >> 2);
								blocks[offset + i * 5 + 6] = (byte) ((word & 2) >> 1);
								blocks[offset + i * 5 + 7] = (byte) (word & 1);
							}
						}
						break;

					case 2: {
							blocks[offset + 2] = (byte) readBits(ref currentByte, ref mask, 2);
							// group values into 5 6-bits words
							for(int i = 0; i < 5; ++i) {
								int word = decodeBits(ref currentByte, ref mask, decodeTree[(int) CodeBookPage.ChrominanceTrinaryValue], 64);
								blocks[offset + i * 3 + 3] = (byte) (word >> 4);
								blocks[offset + i * 3 + 4] = (byte) ((word & 12) >> 2);
								blocks[offset + i * 3 + 5] = (byte) (word & 3);
							}
						}
						break;

					default: {
							blocks[offset + 2] = (byte) readBits(ref currentByte, ref mask, 2);
							// group values into 5 6-bits words
							for(int i = 0; i < 5; ++i) {
								int word = decodeBits(ref currentByte, ref mask, decodeTree[(int) CodeBookPage.ChrominanceNaryValue], 64);
								blocks[offset + i * 3 + 3] = (byte) (word >> 4);
								blocks[offset + i * 3 + 4] = (byte) ((word & 12) >> 2);
								blocks[offset + i * 3 + 5] = (byte) (word & 3);
							}
						}
						break;
				}
			}
		}

		private static unsafe void convertFromBlocks(byte* blocks, byte* diffFrame, int quantization) {
			// Y blocks
			byte* blockValues = stackalloc byte[4 * 4];
			for(int y = 0; y < 16; ++y) {
				for(int x = 0; x < 16; ++x) {
					readBlock(&blocks[(y * 16 + x) * 18], blockValues, quantization);
					for(int blockY = 0; blockY < 4; ++blockY)
						for(int blockX = 0; blockX < 4; ++blockX)
							diffFrame[(y * 4 + blockY) * 64 + (x * 4 + blockX)] = blockValues[blockY * 4 + blockX];
				}
			}
			// Cb blocks
			for(int y = 0; y < 8; ++y) {
				for(int x = 0; x < 8; ++x) {
					readBlock(&blocks[16 * 16 * 18 + (y * 8 + x) * 18], blockValues, quantization);
					for(int blockY = 0; blockY < 4; ++blockY)
						for(int blockX = 0; blockX < 4; ++blockX)
							diffFrame[64 * 64 + (y * 4 + blockY) * 64 + (x * 4 + blockX) * 2] = blockValues[blockY * 4 + blockX];
				}
			}
			// Cr blocks
			for(int y = 0; y < 8; ++y) {
				for(int x = 0; x < 8; ++x) {
					readBlock(&blocks[(16 * 16 + 8 * 8) * 18 + (y * 8 + x) * 18], blockValues, quantization);
					for(int blockY = 0; blockY < 4; ++blockY)
						for(int blockX = 0; blockX < 4; ++blockX)
							diffFrame[64 * 64 + (y * 4 + blockY) * 64 + (x * 4 + blockX) * 2 + 1] = blockValues[blockY * 4 + blockX];
				}
			}
		}

		private static unsafe void readBlock(byte* block, byte* values, int quantization) {
			int min = (block[0] << 1) - ((int) block[1] * quantization) / 2;
			int max = min + (int) block[1] * quantization;
			int* pixels = stackalloc int[16];
			// XOR differential encoding
			if(block[1] != 0) {
				pixels[0] = block[2];
				pixels[1] = block[3] ^ pixels[0];
				pixels[2] = block[4] ^ pixels[1];
				pixels[3] = block[5] ^ pixels[2];
				pixels[4] = block[6] ^ pixels[0];
				pixels[5] = block[7] ^ pixels[4];
				pixels[6] = block[8] ^ pixels[5];
				pixels[7] = block[9] ^ pixels[6];
				pixels[8] = block[10] ^ pixels[4];
				pixels[9] = block[11] ^ pixels[8];
				pixels[10] = block[12] ^ pixels[9];
				pixels[11] = block[13] ^ pixels[10];
				pixels[12] = block[14] ^ pixels[8];
				pixels[13] = block[15] ^ pixels[12];
				pixels[14] = block[16] ^ pixels[13];
				pixels[15] = block[17] ^ pixels[14];
			}
			switch(block[1]) {
				case 0: {
						for(int i = 0; i < 16; ++i)
							values[i] = (byte) (block[0] << 1);
					}
					break;

				case 1: {
						for(int i = 0; i < 16; ++i)
							values[i] = (byte) (pixels[i] == 1 ? max : min);
					}
					break;

				case 2: {
						byte* interpolants = stackalloc byte[3];
						interpolants[0] = (byte) min;
						interpolants[1] = (byte) (block[0] << 1);
						interpolants[2] = (byte) max;
						for(int i = 0; i < 16; ++i) {
							int code = pixels[i];
							values[i] = interpolants[code ^ (code >> 1)];
						}
					}
					break;

				default: {
						byte* interpolants = stackalloc byte[4];
						interpolants[0] = (byte) min;
						interpolants[1] = (byte) ((min * 2 + max) / 3);
						interpolants[2] = (byte) ((min + max * 2) / 3);
						interpolants[3] = (byte) max;
						for(int i = 0; i < 16; ++i) {
							int code = pixels[i];
							values[i] = interpolants[code ^ (code >> 1)];
						}
					}
					break;
			}
		}

		private static unsafe void add(byte* refYCbCr, byte* diffFrame, byte* YCbCr) {
			for(int i = 0; i < (64 * 64 + 32 * 32 * 2); ++i)
				YCbCr[i] = clampToByte(((int)diffFrame[i] << 1) + (int) refYCbCr[i] - 255);
		}

		private static unsafe void convertFromYCbCr(byte* YCbCr, byte* frame) {
			for(int y = 0; y < 32; ++y) {
				for(int x = 0; x < 32; ++x) {
					// used for the bilinear interpolation of Cb and Cr
					int middleCb = YCbCr[64 * 64 + y * 64 + x * 2 + 0];
					int leftCb = (x == 0 ? middleCb : YCbCr[64 * 64 + y * 64 + (x - 1) * 2 + 0]);
					int rightCb = (x == 31 ? middleCb : YCbCr[64 * 64 + y * 64 + (x + 1) * 2 + 0]);
					int topCb = (y == 0 ? middleCb : YCbCr[64 * 64 + (y - 1) * 64 + x * 2 + 0]);
					int bottomCb = (y == 31 ? middleCb : YCbCr[64 * 64 + (y + 1) * 64 + x * 2 + 0]);
					int middleCr = YCbCr[64 * 64 + y * 64 + x * 2 + 1];
					int leftCr = (x == 0 ? middleCr : YCbCr[64 * 64 + y * 64 + (x - 1) * 2 + 1]);
					int rightCr = (x == 31 ? middleCr : YCbCr[64 * 64 + y * 64 + (x + 1) * 2 + 1]);
					int topCr = (y == 0 ? middleCr : YCbCr[64 * 64 + (y - 1) * 64 + x * 2 + 1]);
					int bottomCr = (y == 31 ? middleCr : YCbCr[64 * 64 + (y + 1) * 64 + x * 2 + 1]);

					for(int subY = 0; subY < 2; ++subY) {
						for(int subX = 0; subX < 2; ++subX) {
							byte Y = YCbCr[(y * 2 + subY) * 64 + x * 2 + subX];
							// bilinear interpolation
							byte Cb = clampToByte(middleCb +
								((subX == 0 ? leftCb - rightCb : rightCb - leftCb) +
								(subY == 0 ? topCb - bottomCb : bottomCb - topCb)) / 8);
							byte Cr = clampToByte(middleCr +
								((subX == 0 ? leftCr - rightCr : rightCr - leftCr) +
								(subY == 0 ? topCr - bottomCr : bottomCr - topCr)) / 8);

							frame[(y * 2 + subY) * (64 * 3) + (x * 2 + subX) * 3 + 0] = clampToByte((int) (Y + 1.402f * Cr - 179.456f));
							frame[(y * 2 + subY) * (64 * 3) + (x * 2 + subX) * 3 + 1] = clampToByte((int) (Y - 0.344136f * Cb - 0.714136f * Cr + 135.458816f));
							frame[(y * 2 + subY) * (64 * 3) + (x * 2 + subX) * 3 + 2] = clampToByte((int) (Y + 1.772f * Cb - 226.816f));
						}
					}
				}
			}
		}

		private static byte clampToByte(int value) {
			return (value < 0 ? (byte) 0 : (value > 255 ? (byte) 255 : (byte) value));
		}
	}
}
