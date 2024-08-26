using System;
using HalfMaid.Img.Compression;

namespace HalfMaid.Img.FileFormats.Png.Chunks
{
	/// <summary>
	/// A PNG chunk for storing an ICCP profile.
	/// </summary>
	public class PngIccpChunk : IPngChunk
	{
		/// <inheritdoc />
		public string Type => "iCCP";

		/// <summary>
		/// The name of this ICCP profile.
		/// </summary>
		public string ProfileName { get; }

		/// <summary>
		/// Which compression method has been used on this profile.
		/// </summary>
		public PngCompressionMethod CompressionMethod { get; }

		/// <summary>
		/// The raw, compressed profile data.
		/// </summary>
		public byte[] CompressedProfile { get; }

		/// <summary>
		/// The ICCP profile, decompressed.
		/// </summary>
		public byte[] Profile => _profile ??= Zlib.Inflate(CompressedProfile);
		private byte[]? _profile;

		/// <summary>
		/// Decode this chunk from the given raw byte array.
		/// </summary>
		/// <param name="data">The raw chunk data to decode.</param>
		public PngIccpChunk(ReadOnlySpan<byte> data)
		{
			int i;
			for (i = 0; i < data.Length; i++)
				if (data[i] == 0)
					break;
			ProfileName = PngLoader.Latin1.GetString(data.Slice(0, i).ToArray());
			i++;

			CompressionMethod = i < data.Length ? (PngCompressionMethod)data[i] : default;
			i++;

			if (i < data.Length)
				CompressedProfile = data.Slice(i, data.Length - i).ToArray();
			else
				CompressedProfile = new byte[0];
		}

		/// <summary>
		/// Construct a new ICCP profile chunk.
		/// </summary>
		/// <param name="profileName">The name of this ICCP profile.</param>
		/// <param name="method">Which compression method has been used on this profile.</param>
		/// <param name="compressedProfile">The raw, compressed profile data.</param>
		public PngIccpChunk(string profileName, PngCompressionMethod method, byte[] compressedProfile)
		{
			ProfileName = profileName;
			CompressionMethod = method;
			CompressedProfile = compressedProfile;
		}

		/// <summary>
		/// Construct a new ICCP profile chunk.
		/// </summary>
		/// <param name="profileName">The name of this ICCP profile.</param>
		/// <param name="uncompressedProfile">The ICCP profile, decompressed.</param>
		public PngIccpChunk(string profileName, ReadOnlySpan<byte> uncompressedProfile)
		{
			ProfileName = profileName;
			CompressionMethod = PngCompressionMethod.Deflate;
			CompressedProfile = Zlib.Deflate(uncompressedProfile);
		}

		/// <inheritdoc />
		public void WriteData(OutputWriter output)
		{
			byte[] profileName = PngLoader.Latin1.GetBytes(ProfileName);
			output.Write(profileName);
			output.WriteByte(0);

			output.WriteByte((byte)CompressionMethod);

			output.Write(CompressedProfile);
		}

		/// <summary>
		/// Convert this chunk to a string, primarily for debugging purposes.
		/// </summary>
		public override string ToString()
			=> $"iCCP: '{ProfileName}' ({CompressedProfile.Length} bytes)";
	}
}
