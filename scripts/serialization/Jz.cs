using System;
using System.IO;
using System.IO.Compression;
using Godot;
using Newtonsoft.Json;
using FileAccess = Godot.FileAccess;

namespace racingGame;

public static class Jz
{
	public static T Load<T>(string path)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		var bytes = file.GetBuffer((long) file.GetLength());
			
		using var memoryStream = new MemoryStream(bytes);
		using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
		using var outStream = new MemoryStream();

		gzipStream.CopyTo(outStream);
		var decompressedBytes = outStream.ToArray();
		var text = System.Text.Encoding.UTF8.GetString(decompressedBytes);
		
		return JsonConvert.DeserializeObject<T>(text);
	}

	public static void Save<T>(string path, T data)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);

		var text = JsonConvert.SerializeObject(data);
		GD.Print(text);
		var bytes = System.Text.Encoding.UTF8.GetBytes(text);
		GD.Print(bytes.Length);
		
		using var outStream = new MemoryStream();
		using (var gzipStream = new GZipStream(outStream, CompressionMode.Compress))
		{
			gzipStream.Write(bytes, 0, bytes.Length);
		}

		var compressedBytes = outStream.ToArray();
		GD.Print(compressedBytes.Length);

		file.StoreBuffer(compressedBytes);
	}
}