/*
 * Copyright (c) 1989 The Regents of the University of California.
 * All rights reserved.
 *
 * This code is derived from software contributed to Berkeley by
 * Robert Paul Corbett.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. All advertising materials mentioning features or use of this software
 *    must display the following acknowledgement:
 *	This product includes software developed by the University of
 *	California, Berkeley and its contributors.
 * 4. Neither the name of the University nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE REGENTS AND CONTRIBUTORS ``AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE REGENTS OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace Yacc
{
	static class Warshall
	{
#if !lint
		static readonly string sccsid = "@(#)warshall.c	5.4 (Berkeley) 5/24/93";
#endif // not lint

		static void TransitiveClosure(uint[] R, int n)
		{
			int rowsize;
			int i;
			int rowj;
			int rp;
			int rend;
			int ccol;
			int relend;
			int cword;
			int rowi;

			rowsize = Defs.WORDSIZE(n);
			relend = n * rowsize;

			cword = 0;
			i = 0;
			rowi = 0;
			while (rowi < relend) {
				ccol = cword;
				rowj = 0;

				while (rowj < relend) {
					if ((R[ccol] & (1u << i)) != 0) {
						rp = rowi;
						rend = rowj + rowsize;
						while (rowj < rend)
							R[rowj++] |= R[rp++];
					}
					else {
						rowj += rowsize;
					}

					ccol += rowsize;
				}

				if (++i >= Defs.BITS_PER_WORD) {
					i = 0;
					cword++;
				}

				rowi += rowsize;
			}
		}

		public static void ReflexiveTransitiveClosure(uint[] R, int n)
		{
			int rowsize;
			int i;
			int rp;
			int relend;

			TransitiveClosure(R, n);

			rowsize = Defs.WORDSIZE(n);
			relend = n * rowsize;

			i = 0;
			rp = 0;
			while (rp < relend) {
				R[rp] |= (1u << i);
				if (++i >= Defs.BITS_PER_WORD) {
					i = 0;
					rp++;
				}

				rp += rowsize;
			}
		}
	}
}
