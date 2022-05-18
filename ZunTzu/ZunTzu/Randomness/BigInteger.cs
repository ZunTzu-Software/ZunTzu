// Copyright (c) 2022 ZunTzu Software and contributors

using System;

namespace ZunTzu.Randomness {

	internal struct BigInteger {

		private uint length;
		private uint[] data;

		public static BigInteger Parse(string number) {
			if(number == null)
				throw new ArgumentNullException(number);
			int i = 0, len = number.Length;
			char c;
			bool digits_seen = false;
			BigInteger val = new BigInteger(0);
			if(number[i] == '+') {
				i++;
			} else if(number[i] == '-') {
				throw new FormatException("Only positive integers are allowed.");
			}
			for(; i < len; i++) {
				c = number[i];
				if(c == '\0') {
					i = len;
					continue;
				}
				if(c >= '0' && c <= '9') {
					val = val * 10u + (uint) (c - '0');
					digits_seen = true;
				} else {
					if(Char.IsWhiteSpace(c)) {
						for(i++; i < len; i++) {
							if(!Char.IsWhiteSpace(number[i]))
								throw new FormatException();
						}
						break;
					} else
						throw new FormatException();
				}
			}
			if(!digits_seen)
				throw new FormatException();
			return val;
		}
		
		public static BigInteger FromBytes(byte[] bytes) {
			BigInteger result;
			int leftOver = bytes.Length & 0x3;
			result.length = (uint) (bytes.Length >> 2 + (leftOver != 0 ? 1 : 0));
			result.data = new uint[result.length];
			for(int i = bytes.Length - 1, j = 0; i >= 3; i -= 4, ++j) {
				result.data[j] = (uint) (
					(bytes[i - 3] << (3 * 8)) |
					(bytes[i - 2] << (2 * 8)) |
					(bytes[i - 1] << (1 * 8)) |
					(bytes[i - 0] << (0 * 8)));
			}
			result.data[result.length - 1] = (uint)
				(leftOver < 2 ? (leftOver == 0 ? 0 : (int) bytes[0]) :
					(leftOver == 2 ? (bytes[0] << 8) | bytes[1] : (bytes[0] << 16) | (bytes[1] << 8) | bytes[2]));
			result.normalize();
			return result;
		}

		public BigInteger(uint value) {
			length = 1;
			data = new uint[] { value };
		}

		public static implicit operator BigInteger(uint value) {
			return new BigInteger(value);
		}

		public static implicit operator BigInteger(int value) {
			if(value < 0)
				throw new ArgumentOutOfRangeException();
			return new BigInteger((uint) value);
		}

		public static BigInteger operator +(BigInteger left, BigInteger right) {
			return (left == 0 ? right : (right == 0 ? left : Kernel.AddSameSign(left, right)));
		}

		public static BigInteger operator -(BigInteger left, BigInteger right) {
			if(right == 0)
				return left;
			if(left == 0)
				throw new ArithmeticException("Operation would return a negative value");
			switch(Kernel.Compare(left, right)) {
				case 0:
					return 0;
				case 1:
					return Kernel.Subtract(left, right);
				case -1:
					throw new ArithmeticException("Operation would return a negative value");
				default:
					throw new Exception();
			}
		}

		public static uint operator %(BigInteger dividend, uint divisor) {
			return Kernel.DwordMod(dividend, divisor);
		}

		public static int operator %(BigInteger dividend, int divisor) {
			return (divisor > 0 ? (int) (dividend % (uint) divisor) : -(int) (dividend % (uint) - divisor));
		}

		public static BigInteger operator /(BigInteger dividend, uint divisor) {
			if(divisor == 0)
				throw new DivideByZeroException();
			return Kernel.DwordDiv(dividend, divisor);
		}

		public static BigInteger operator /(BigInteger dividend, int divisor) {
			if(divisor < 0)
				throw new ArithmeticException("Operation would return a negative value");
			return dividend / (uint) divisor;
		}

		public static BigInteger operator *(BigInteger left, uint right) {
			if(right == 0) return 0;
			if(right == 1) return left;
			return Kernel.MultiplyByDword(left, right);
		}

		public static BigInteger operator *(BigInteger left, int right) {
			if(right < 0) throw new ArithmeticException("Operation would return a negative value");
			return left * (uint) right;
		}

		public static BigInteger operator <<(BigInteger value, int shiftVal) {
			return Kernel.LeftShift(value, shiftVal);
		}

		public static BigInteger operator >>(BigInteger value, int shiftVal) {
			return Kernel.RightShift(value, shiftVal);
		}

		public int BitCount {
			get {
				normalize();
				uint value = data[length - 1];
				uint mask = 0x80000000;
				uint bits = 32;
				while(bits > 0 && (value & mask) == 0) {
					--bits;
					mask >>= 1;
				}
				bits += ((length - 1) << 5);
				return (int) bits;
			}
		}

		/// <summary>Tests if the specified bit is 1.</summary>
		/// <param name="bitNum">The bit to test. The least significant bit is 0.</param>
		/// <returns>True if bitNum is set to 1, else false.</returns>
		//public bool testBit(uint bitNum) {
		//	uint bytePos = bitNum >> 5;             // divide by 32
		//	byte bitPos = (byte)(bitNum & 0x1F);    // get the lowest 5 bits
		//
		//	uint mask = (uint)1 << bitPos;
		//	return ((this.data[bytePos] & mask) != 0);
		//}

		//public bool testBit(int bitNum) {
		//	if (bitNum < 0) throw new IndexOutOfRangeException ("bitNum out of range");
		//
		//	uint bytePos = (uint)bitNum >> 5;             // divide by 32
		//	byte bitPos = (byte)(bitNum & 0x1F);    // get the lowest 5 bits
		//
		//	uint mask = (uint)1 << bitPos;
		//	return ((this.data[bytePos] | mask) == this.data[bytePos]);
		//}

		//public void setBit(uint bitNum) {
		//	setBit(bitNum, true);
		//}

		//public void clearBit(uint bitNum) {
		//	setBit(bitNum, false);
		//}

		//public void setBit(uint bitNum, bool val) {
		//	uint bytePos = bitNum >> 5;             // divide by 32
		//
		//	if(bytePos < this.length) {
		//		uint mask = (uint)1 << (int)(bitNum & 0x1F);
		//		if(val)
		//			this.data[bytePos] |= mask;
		//		else
		//			this.data[bytePos] &= ~mask;
		//	}
		//}

		//public int LowestSetBit() {
		//	if(this == 0) return -1;
		//	int i = 0;
		//	while(!testBit(i)) i++;
		//	return i;
		//}

		public byte[] GetBytes() {
			if(this == 0) return new byte[1] { 0 };
			int numBits = BitCount;
			int numBytes = numBits >> 3 + ((numBits & 0x7) != 0 ? 1 : 0);
			byte[] result = new byte[numBytes];
			int numBytesInWord = numBytes & 0x3;
			if(numBytesInWord == 0)
				numBytesInWord = 4;
			int pos = 0;
			for(int i = (int)length - 1; i >= 0; --i) {
				uint val = data[i];
				for(int j = numBytesInWord - 1; j >= 0; --j) {
					result[pos + j] = (byte) (val & 0xFF);
					val >>= 8;
				}
				pos += numBytesInWord;
				numBytesInWord = 4;
			}
			return result;
		}

		public static bool operator ==(BigInteger left, uint right) {
			if(left.length != 1)
				left.normalize();
			return left.length == 1 && left.data[0] == right;
		}

		public static bool operator !=(BigInteger left, uint right) {
			return !(left == right);
		}

		public static bool operator ==(BigInteger left, BigInteger right) {
			return (left.data == right.data || Kernel.Compare(left, right) == 0);
		}

		public static bool operator !=(BigInteger left, BigInteger right) {
			return !(left == right);
		}

		public static bool operator >(BigInteger left, BigInteger right) {
			return Kernel.Compare(left, right) > 0;
		}

		public static bool operator <(BigInteger left, BigInteger right) {
			return Kernel.Compare(left, right) < 0;
		}

		public static bool operator >=(BigInteger left, BigInteger right) {
			return !(left < right);
		}

		public static bool operator <=(BigInteger left, BigInteger right) {
			return !(left > right);
		}

		public int Compare(BigInteger value) {
			return Kernel.Compare(this, value);
		}

		public string ToString(uint radix) {
			return ToString(radix, "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
		}

		public string ToString(uint radix, string charSet) {
			if(charSet.Length < radix)
				throw new ArgumentException("charSet length less than radix", "charSet");
			if(radix == 1)
				throw new ArgumentException("There is no such thing as radix one notation", "radix");
			if(this == 0) return "0";
			if(this == 1) return "1";
			string result = "";
			BigInteger a = this;
			while(a != 0) {
				uint rem = Kernel.SingleByteDivideInPlace(a, radix);
				result = charSet[(int) rem] + result;
			}
			return result;
		}

		/// <summary>Sets the length to the actual number of uints used in data.</summary>
		private void normalize() {
			while(length > 1 && data[length - 1] == 0)
				--length;
		}

		//public void Clear() {
		//	for(int i=0; i < length; i++)
		//		data[i] = 0x00;
		//}

		public override int GetHashCode() {
			uint val = 0;
			for(uint i = 0; i < this.length; i++)
				val ^= this.data[i];
			return (int) val;
		}

		public override string ToString() {
			return ToString(10);
		}

		public override bool Equals(object o) {
			if(o == null) return false;
			if(o is int) return (int) o >= 0 && this == (uint)o;
			return Kernel.Compare(this, (BigInteger) o) == 0;
		}

		/// <summary>Low level functions for the BigInteger</summary>
		private sealed class Kernel {

			/// <summary>Adds two numbers with the same sign.</summary>
			/// <param name="bi1">A BigInteger</param>
			/// <param name="bi2">A BigInteger</param>
			/// <returns>bi1 + bi2</returns>
			public static BigInteger AddSameSign(BigInteger bi1, BigInteger bi2) {
				uint[] x, y;
				uint yMax, xMax, i = 0;

				// x should be bigger
				if(bi1.length < bi2.length) {
					x = bi2.data;
					xMax = bi2.length;
					y = bi1.data;
					yMax = bi1.length;
				} else {
					x = bi1.data;
					xMax = bi1.length;
					y = bi2.data;
					yMax = bi2.length;
				}
				
				BigInteger result;
				result.data = new uint[xMax + 1];
				result.length = xMax + 1;

				uint[] r = result.data;

				ulong sum = 0;

				// Add common parts of both numbers
				do {
					sum = ((ulong)x[i]) + ((ulong)y[i]) + sum;
					r[i] = (uint)sum;
					sum >>= 32;
				} while(++i < yMax);

				// Copy remainder of longer number while carry propagation is required
				bool carry = (sum != 0);

				if(carry) {

					if(i < xMax) {
						do
							carry = ((r[i] = x[i] + 1) == 0);
						while(++i < xMax && carry);
					}

					if(carry) {
						r[i] = 1;
						result.length = ++i;
						return result;
					}
				}

				// Copy the rest
				if(i < xMax) {
					do
						r[i] = x[i];
					while(++i < xMax);
				}

				result.normalize();
				return result;
			}

			public static BigInteger Subtract(BigInteger big, BigInteger small) {
				BigInteger result;
				result.data = new uint[big.length];
				result.length = big.length;

				uint[] r = result.data, b = big.data, s = small.data;
				uint i = 0, c = 0;

				do {

					uint x = s [i];
					if (((x += c) < c) | ((r [i] = b [i] - x) > ~x))
						c = 1;
					else
						c = 0;

				} while (++i < small.length);

				if (i == big.length) goto fixup;

				if (c == 1) {
					do
						r [i] = b [i] - 1;
					while (b [i++] == 0 && i < big.length);

					if (i == big.length) goto fixup;
				}

				do
					r [i] = b [i];
				while (++i < big.length);

			fixup:

				result.normalize();
				return result;
			}

			public static void MinusEq(BigInteger big, BigInteger small) {
				uint [] b = big.data, s = small.data;
				uint i = 0, c = 0;

				do {
					uint x = s [i];
					if (((x += c) < c) | ((b [i] -= x) > ~x))
						c = 1;
					else
						c = 0;
				} while (++i < small.length);

				if (i == big.length) goto fixup;

				if (c == 1) {
					do
						b [i]--;
					while (b [i++] == 0 && i < big.length);
				}

				fixup:

					// Normalize length
					while (big.length > 0 && big.data [big.length-1] == 0) big.length--;

				// Check for zero
				if (big.length == 0)
					big.length++;

			}

			public static void PlusEq(BigInteger bi1, BigInteger bi2) {
				uint [] x, y;
				uint yMax, xMax, i = 0;
				bool flag = false;

				// x should be bigger
				if (bi1.length < bi2.length){
					flag = true;
					x = bi2.data;
					xMax = bi2.length;
					y = bi1.data;
					yMax = bi1.length;
				} else {
					x = bi1.data;
					xMax = bi1.length;
					y = bi2.data;
					yMax = bi2.length;
				}

				uint [] r = bi1.data;

				ulong sum = 0;

				// Add common parts of both numbers
				do {
					sum += ((ulong)x [i]) + ((ulong)y [i]);
					r [i] = (uint)sum;
					sum >>= 32;
				} while (++i < yMax);

				// Copy remainder of longer number while carry propagation is required
				bool carry = (sum != 0);

				if (carry){

					if (i < xMax) {
						do
							carry = ((r [i] = x [i] + 1) == 0);
						while (++i < xMax && carry);
					}

					if (carry) {
						r [i] = 1;
						bi1.length = ++i;
						return;
					}
				}

				// Copy the rest
				if (flag && i < xMax - 1) {
					do
						r [i] = x [i];
					while (++i < xMax);
				}

				bi1.length = xMax + 1;
				bi1.normalize();
			}

			/// <summary>Compares two BigInteger</summary>
			/// <param name="bi1">A BigInteger</param>
			/// <param name="bi2">A BigInteger</param>
			/// <returns>The sign of bi1 - bi2</returns>
			public static int Compare(BigInteger bi1, BigInteger bi2) {
				//
				// Step 1. Compare the lengths
				//
				uint l1 = bi1.length, l2 = bi2.length;

				while (l1 > 0 && bi1.data [l1-1] == 0) l1--;
				while (l2 > 0 && bi2.data [l2-1] == 0) l2--;

				if (l1 == 0 && l2 == 0) return 0;

				// bi1 len < bi2 len
				if (l1 < l2) return -1;
				// bi1 len > bi2 len
				else if (l1 > l2) return 1;

				//
				// Step 2. Compare the bits
				//

				uint pos = l1 - 1;

				while (pos != 0 && bi1.data [pos] == bi2.data [pos]) pos--;
				
				if (bi1.data [pos] < bi2.data [pos])
					return -1;
				else if (bi1.data [pos] > bi2.data [pos])
					return 1;
				else
					return 0;
			}

			/// <summary>Performs n / d and n % d in one operation.</summary>
			/// <param name="n">A BigInteger, upon exit this will hold n / d</param>
			/// <param name="d">The divisor</param>
			/// <returns>n % d</returns>
			public static uint SingleByteDivideInPlace(BigInteger n, uint d) {
				ulong r = 0;
				uint i = n.length;

				while (i-- > 0) {
					r <<= 32;
					r |= n.data [i];
					n.data [i] = (uint)(r / d);
					r %= d;
				}
				n.normalize();

				return (uint)r;
			}

			public static uint DwordMod(BigInteger n, uint d) {
				ulong r = 0;
				uint i = n.length;

				while (i-- > 0) {
					r <<= 32;
					r |= n.data [i];
					r %= d;
				}

				return (uint)r;
			}

			public static BigInteger DwordDiv(BigInteger n, uint d) {
				BigInteger ret;
				ret.data = new uint[n.length];
				ret.length = n.length;

				ulong r = 0;
				uint i = n.length;

				while (i-- > 0) {
					r <<= 32;
					r |= n.data [i];
					ret.data [i] = (uint)(r / d);
					r %= d;
				}
				ret.normalize();

				return ret;
			}

			public static BigInteger[] DwordDivMod(BigInteger n, uint d) {
				BigInteger ret;
				ret.data = new uint[n.length];
				ret.length = n.length;

				ulong r = 0;
				uint i = n.length;

				while (i-- > 0) {
					r <<= 32;
					r |= n.data [i];
					ret.data [i] = (uint)(r / d);
					r %= d;
				}
				ret.normalize();

				BigInteger rem = (uint)r;

				return new BigInteger [] {ret, rem};
			}

			public static BigInteger[] multiByteDivide(BigInteger bi1, BigInteger bi2) {
				if (Kernel.Compare (bi1, bi2) == -1)
					return new BigInteger[2] { 0, bi1 };

				bi1.normalize(); bi2.normalize();

				if (bi2.length == 1)
					return DwordDivMod (bi1, bi2.data [0]);

				uint remainderLen = bi1.length + 1;
				int divisorLen = (int)bi2.length + 1;

				uint mask = 0x80000000;
				uint val = bi2.data [bi2.length - 1];
				int shift = 0;
				int resultPos = (int)bi1.length - (int)bi2.length;

				while (mask != 0 && (val & mask) == 0) {
					shift++; mask >>= 1;
				}

				BigInteger quot;
				quot.data = new uint[bi1.length - bi2.length + 1];
				quot.length = bi1.length - bi2.length + 1;
				BigInteger rem = (bi1 << shift);

				uint [] remainder = rem.data;

				bi2 = bi2 << shift;

				int j = (int)(remainderLen - bi2.length);
				int pos = (int)remainderLen - 1;

				uint firstDivisorByte = bi2.data [bi2.length-1];
				ulong secondDivisorByte = bi2.data [bi2.length-2];

				while (j > 0) {
					ulong dividend = ((ulong)remainder [pos] << 32) + (ulong)remainder [pos-1];

					ulong q_hat = dividend / (ulong)firstDivisorByte;
					ulong r_hat = dividend % (ulong)firstDivisorByte;

					do {

						if (q_hat == 0x100000000 ||
							(q_hat * secondDivisorByte) > ((r_hat << 32) + remainder [pos-2])) {
							q_hat--;
							r_hat += (ulong)firstDivisorByte;

							if (r_hat < 0x100000000)
								continue;
						}
						break;
					} while (true);

					//
					// At this point, q_hat is either exact, or one too large
					// (more likely to be exact) so, we attempt to multiply the
					// divisor by q_hat, if we get a borrow, we just subtract
					// one from q_hat and add the divisor back.
					//

					uint t;
					uint dPos = 0;
					int nPos = pos - divisorLen + 1;
					ulong mc = 0;
					uint uint_q_hat = (uint)q_hat;
					do {
						mc += (ulong)bi2.data [dPos] * (ulong)uint_q_hat;
						t = remainder [nPos];
						remainder [nPos] -= (uint)mc;
						mc >>= 32;
						if (remainder [nPos] > t) mc++;
						dPos++; nPos++;
					} while (dPos < divisorLen);

					nPos = pos - divisorLen + 1;
					dPos = 0;

					// Overestimate
					if (mc != 0) {
						uint_q_hat--;
						ulong sum = 0;

						do {
							sum = ((ulong)remainder [nPos]) + ((ulong)bi2.data [dPos]) + sum;
							remainder [nPos] = (uint)sum;
							sum >>= 32;
							dPos++; nPos++;
						} while (dPos < divisorLen);

					}

					quot.data [resultPos--] = (uint)uint_q_hat;

					pos--;
					j--;
				}

				quot.normalize();
				rem.normalize();
				BigInteger [] ret = new BigInteger [2] { quot, rem };

				if (shift != 0)
					ret [1] >>= shift;

				return ret;
			}

			public static BigInteger LeftShift(BigInteger bi, int n) {
				if (n == 0) return bi;

				int w = n >> 5;
				n &= ((1 << 5) - 1);

				BigInteger ret;
				ret.data = new uint[bi.length + 1 + (uint) w];
				ret.length = bi.length + 1 + (uint) w;

				uint i = 0, l = bi.length;
				if (n != 0) {
					uint x, carry = 0;
					while (i < l) {
						x = bi.data [i];
						ret.data [i + w] = (x << n) | carry;
						carry = x >> (32 - n);
						i++;
					}
					ret.data [i + w] = carry;
				} else {
					while (i < l) {
						ret.data [i + w] = bi.data [i];
						i++;
					}
				}

				ret.normalize();
				return ret;
			}

			public static BigInteger RightShift(BigInteger bi, int n) {
				if (n == 0) return bi;

				int w = n >> 5;
				int s = n & ((1 << 5) - 1);

				BigInteger ret;
				ret.data = new uint[bi.length - (uint)w + 1];
				ret.length = bi.length - (uint) w + 1;
				uint l = (uint)ret.data.Length - 1;

				if (s != 0) {

					uint x, carry = 0;

					while (l-- > 0) {
						x = bi.data [l + w];
						ret.data [l] = (x >> n) | carry;
						carry = x << (32 - n);
					}
				} else {
					while (l-- > 0)
						ret.data [l] = bi.data [l + w];

				}
				ret.normalize();
				return ret;
			}

			public static BigInteger MultiplyByDword(BigInteger n, uint f) {
				BigInteger ret;
				ret.data = new uint[n.length + 1];
				ret.length = n.length + 1;
				uint i = 0;
				ulong c = 0;
				do {
					c += (ulong) n.data[i] * (ulong)f;
					ret.data[i] = (uint) c;
					c >>= 32;
				} while(++i < n.length);
				ret.data[i] = (uint) c;
				ret.normalize();
				return ret;
			}

			/// <summary>
			/// Multiplies the data in x [xOffset:xOffset+xLen] by
			/// y [yOffset:yOffset+yLen] and puts it into
			/// d [dOffset:dOffset+xLen+yLen].
			/// </summary>
			/// <remarks>
			/// This code is unsafe! It is the caller's responsibility to make
			/// sure that it is safe to access x [xOffset:xOffset+xLen],
			/// y [yOffset:yOffset+yLen], and d [dOffset:dOffset+xLen+yLen].
			/// </remarks>
			public static unsafe void Multiply(uint [] x, uint xOffset, uint xLen, uint [] y, uint yOffset, uint yLen, uint [] d, uint dOffset) {
				fixed (uint* xx = x, yy = y, dd = d) {
					uint* xP = xx + xOffset,
						xE = xP + xLen,
						yB = yy + yOffset,
						yE = yB + yLen,
						dB = dd + dOffset;

					for (; xP < xE; xP++, dB++) {

						if (*xP == 0) continue;

						ulong mcarry = 0;

						uint* dP = dB;
						for (uint* yP = yB; yP < yE; yP++, dP++) {
							mcarry += ((ulong)*xP * (ulong)*yP) + (ulong)*dP;

							*dP = (uint)mcarry;
							mcarry >>= 32;
						}

						if (mcarry != 0)
							*dP = (uint)mcarry;
					}
				}
			}

			/// <summary>
			/// Multiplies the data in x [xOffset:xOffset+xLen] by
			/// y [yOffset:yOffset+yLen] and puts the low mod words into
			/// d [dOffset:dOffset+mod].
			/// </summary>
			/// <remarks>
			/// This code is unsafe! It is the caller's responsibility to make
			/// sure that it is safe to access x [xOffset:xOffset+xLen],
			/// y [yOffset:yOffset+yLen], and d [dOffset:dOffset+mod].
			/// </remarks>
			public static unsafe void MultiplyMod2p32pmod(uint [] x, int xOffset, int xLen, uint [] y, int yOffest, int yLen, uint [] d, int dOffset, int mod) {
				fixed (uint* xx = x, yy = y, dd = d) {
					uint* xP = xx + xOffset,
						xE = xP + xLen,
						yB = yy + yOffest,
						yE = yB + yLen,
						dB = dd + dOffset,
						dE = dB + mod;

					for (; xP < xE; xP++, dB++) {

						if (*xP == 0) continue;

						ulong mcarry = 0;
						uint* dP = dB;
						for (uint* yP = yB; yP < yE && dP < dE; yP++, dP++) {
							mcarry += ((ulong)*xP * (ulong)*yP) + (ulong)*dP;

							*dP = (uint)mcarry;
							mcarry >>= 32;
						}

						if (mcarry != 0 && dP < dE)
							*dP = (uint)mcarry;
					}
				}
			}

			public static unsafe void SquarePositive(BigInteger bi, ref uint[] wkSpace) {
				uint [] t = wkSpace;
				wkSpace = bi.data;
				uint [] d = bi.data;
				uint dl = bi.length;
				bi.data = t;

				fixed (uint* dd = d, tt = t) {

					uint* ttE = tt + t.Length;
					// Clear the dest
					for (uint* ttt = tt; ttt < ttE; ttt++)
						*ttt = 0;

					uint* dP = dd, tP = tt;

					for (uint i = 0; i < dl; i++, dP++) {
						if (*dP == 0)
							continue;

						ulong mcarry = 0;
						uint bi1val = *dP;

						uint* dP2 = dP + 1, tP2 = tP + 2*i + 1;

						for (uint j = i + 1; j < dl; j++, tP2++, dP2++) {
							// k = i + j
							mcarry += ((ulong)bi1val * (ulong)*dP2) + *tP2;

							*tP2 = (uint)mcarry;
							mcarry >>= 32;
						}

						if (mcarry != 0)
							*tP2 = (uint)mcarry;
					}

					// Double t. Inlined for speed.

					tP = tt;

					uint x, carry = 0;
					while (tP < ttE) {
						x = *tP;
						*tP = (x << 1) | carry;
						carry = x >> (32 - 1);
						tP++;
					}
					if (carry != 0) *tP = carry;

					// Add in the diagnals

					dP = dd;
					tP = tt;
					for (uint* dE = dP + dl; (dP < dE); dP++, tP++) {
						ulong val = (ulong)*dP * (ulong)*dP + *tP;
						*tP = (uint)val;
						val >>= 32;
						*(++tP) += (uint)val;
						if (*tP < (uint)val) {
							uint* tP3 = tP;
							// Account for the first carry
							(*++tP3)++;

							// Keep adding until no carry
							while ((*tP3++) == 0x0)
								(*tP3)++;
						}

					}

					bi.length <<= 1;

					// Normalize length
					while (tt [bi.length-1] == 0 && bi.length > 1) bi.length--;

				}
			}

		}
	}
}
