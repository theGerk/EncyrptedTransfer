﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gerk.Crypto.EncryptedTransfer
{
	public class SafeTunnel : Stream
	{
		Tunnel underlyingTunnel;
		BinaryWriter writer;
		BinaryReader reader;
		uint remainingInBlockToRead = 0;

		public SafeTunnel(
			Tunnel tunnel
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
			, bool leaveOpen = false
#endif
			)
		{
			underlyingTunnel = tunnel;
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
			writer = new BinaryWriter(tunnel, Encoding.UTF8, leaveOpen);
			reader = new BinaryReader(tunnel, Encoding.UTF8, leaveOpen);
#else
			writer = new BinaryWriter(tunnel);
			reader = new BinaryReader(tunnel);
#endif
		}

		/// <inheritdoc/>
		public override bool CanRead => underlyingTunnel.CanRead;

		/// <inheritdoc/>
		public override bool CanSeek => underlyingTunnel.CanSeek;

		/// <inheritdoc/>
		public override bool CanWrite => underlyingTunnel.CanWrite;

		/// <inheritdoc/>
		public override long Length => underlyingTunnel.Length;

		/// <inheritdoc/>
		public override long Position { get => underlyingTunnel.Position; set => underlyingTunnel.Position = value; }

		/// <inheritdoc/>
		public override void Flush()
		{
			underlyingTunnel.Flush();
		}

		/// <inheritdoc/>
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (remainingInBlockToRead == 0)
				remainingInBlockToRead = reader.ReadUInt32();

			if (count >= remainingInBlockToRead)
			{
				var result = reader.Read(buffer, offset, (int)remainingInBlockToRead);
				underlyingTunnel.FlushReader();
				remainingInBlockToRead = 0;
				return result;
			}
			else
			{
				remainingInBlockToRead -= (uint)count;
				return reader.Read(buffer, offset, count);
			}
		}

		/// <inheritdoc/>
		public override long Seek(long offset, SeekOrigin origin) => underlyingTunnel.Seek(offset, origin);

		/// <inheritdoc/>
		public override void SetLength(long value) => underlyingTunnel.SetLength(value);

		/// <inheritdoc/>
		public override void Write(byte[] buffer, int offset, int count)
		{
			writer.Write((uint)count);
			writer.Write(buffer, offset, count);
			underlyingTunnel.FlushWriter();
		}

		public async override ValueTask DisposeAsync()
		{
			await writer.DisposeAsync();
			reader.Dispose();
			await underlyingTunnel.DisposeAsync();
		}
	}
}
