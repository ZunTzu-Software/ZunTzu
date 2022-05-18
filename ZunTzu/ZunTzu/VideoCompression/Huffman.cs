// Copyright (c) 2022 ZunTzu Software and contributors

using System;

namespace ZunTzu.VideoCompression {

	internal struct DecodeTreeNode {
		public int Count;
		public int Child0;
		public int Child1;
	}

	internal struct CodeBookEntry {
		public uint Code;
		public int BitCount;
	}

	internal class Huffman {

		public static unsafe DecodeTreeNode[] BuildDecodeTree(int[] counts) {
			if(counts.Length > 256)
				throw new ArgumentException();
			DecodeTreeNode* tree = stackalloc DecodeTreeNode[counts.Length * 2 - 1];
			for(int i = 0; i < counts.Length; ++i) {
				tree[i].Count = counts[i];
				tree[i].Child0 = -1;
				tree[i].Child1 = -1;
			}
			int nextFreeNode = counts.Length;
			int minNode0 = 0;
			int minNode1 = 0;
			while(true) {
				int minCount0 = int.MaxValue; // minCount0 <= minCount1
				int minCount1 = int.MaxValue;
				for(int i = 0; i < nextFreeNode; ++i) {
					int count = tree[i].Count;
					if(count != -1 && count < minCount1) {
						if(count < minCount0) {
							minCount1 = minCount0;
							minNode1 = minNode0;
							minCount0 = count;
							minNode0 = i;
						} else {
							minCount1 = count;
							minNode1 = i;
						}
					}
				}
				if(minCount1 == int.MaxValue)
					break;
				tree[nextFreeNode].Count = minCount0 + minCount1;
				tree[nextFreeNode].Child0 = minNode1;
				tree[nextFreeNode].Child1 = minNode0;
				tree[minNode0].Count = -1;
				tree[minNode1].Count = -1;
				++nextFreeNode;
			}
			DecodeTreeNode[] truncatedTree = new DecodeTreeNode[nextFreeNode];
			for(int i = 0; i < nextFreeNode; ++i)
				truncatedTree[i] = tree[i];
			return truncatedTree;	// root is last node
		}

		public static unsafe CodeBookEntry[] BuildCodeBook(DecodeTreeNode[] tree, int count) {
			if(tree.Length > 511 || count > 256)
				throw new ArgumentException();
			CodeBookEntry[] book = new CodeBookEntry[count];
			int node = tree.Length - 1;
			recurse(tree, book, tree.Length - 1, 0, 0);
			return book;
		}

		private static unsafe void recurse(DecodeTreeNode[] tree, CodeBookEntry[] book, int node, uint code, int bitCount) {
			if(bitCount > 32)
				throw new ArgumentException("code book overflow");
			if(node < book.Length) {
				book[node].Code = code;
				book[node].BitCount = bitCount;
			} else {
				recurse(tree, book, tree[node].Child0, (code << 1), bitCount + 1);
				recurse(tree, book, tree[node].Child1, (code << 1) | 1, bitCount + 1);
			}
		}

		public static void Compress(CodeBookEntry[] codeBook, byte symbol, out uint code, out int bitCount) {
			code = codeBook[symbol].Code;
			bitCount = codeBook[symbol].BitCount;
		}

		public static byte Expand(DecodeTreeNode[] decodeTree, int count, int data) {
			int node = decodeTree.Length - 1;
			int bits = data;
			while(true) {
				node = ((bits & 1) == 0 ? decodeTree[node].Child0 : decodeTree[node].Child1);
				if(node < count)
					return (byte) node;
				bits >>= 1;
			}
		}
	}
}
