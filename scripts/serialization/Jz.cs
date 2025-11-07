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
		try
		{
			if (path.EndsWith(".json"))
				return LoadJson<T>(path);

			return LoadJz<T>(path);
		}
		catch (Exception e)
		{
			GD.PushError(e);
			
			return default;
		}
	}

	public static void Save<T>(string path, T data)
	{
		try
		{
			if (path.EndsWith(".json"))
			{
				SaveJson(path, data);
				return;
			}

			SaveJz(path, data);
		}
		catch (Exception e)
		{
			GD.PushError(e);
		}
	}

	private static T LoadJson<T>(string path)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		var text = file.GetAsText();
		
		return JsonConvert.DeserializeObject<T>(text);
	}
	
	private static void SaveJson<T>(string path, T data)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
		
		var text = JsonConvert.SerializeObject(data, Formatting.Indented);

		file.StoreString(text);
	}
	
	public static T LoadJz<T>(string path)
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

	public static void SaveJz<T>(string path, T data)
	{
		using (var file = FileAccess.Open(path, FileAccess.ModeFlags.Write))
		{
			var text = JsonConvert.SerializeObject(data, Formatting.Indented);
			var bytes = System.Text.Encoding.UTF8.GetBytes(text);
		
			using var outStream = new MemoryStream();
			using (var gzipStream = new GZipStream(outStream, CompressionMode.Compress))
			{
				gzipStream.Write(bytes, 0, bytes.Length);
			}

			var compressedBytes = outStream.ToArray();

			file.StoreBuffer(compressedBytes);
		}
	}
}