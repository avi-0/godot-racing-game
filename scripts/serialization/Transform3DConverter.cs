using System;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace racingGame;

public class Transform3DConverter : JsonConverter<Transform3D>
{
	public override void WriteJson(JsonWriter writer, Transform3D value, JsonSerializer serializer)
	{
		writer.WriteStartArray();

		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				writer.WriteValue(value[i, j]);
			}
		}
		
		writer.WriteEndArray();
	}

	public override Transform3D ReadJson(JsonReader reader, Type objectType, Transform3D existingValue, bool hasExistingValue,
		JsonSerializer serializer)
	{
		var jo = JArray.Load(reader);
		var transform = new Transform3D();

		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				transform[i, j] = jo[i * 3 + j].ToObject<float>();
			}
		}

		return transform;
	}
}