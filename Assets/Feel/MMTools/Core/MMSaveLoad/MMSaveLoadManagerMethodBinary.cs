using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

namespace MoreMountains.Tools
{
	/// <summary>
	/// This save load method saves and loads files as binary files.
	/// The object is serialized with JsonUtility (so it follows the same rules as the Json method : a [Serializable] class or struct, public or [SerializeField] fields),
	/// and the result is written to disk in a binary format, preceded by a small header.
	/// Files saved with the BinaryFormatter based binary method of older versions can't be read by this method, and will load as null.
	/// </summary>
	public class MMSaveLoadManagerMethodBinary : IMMSaveLoadManagerMethod
	{
		/// the header written at the start of every binary save file, used to recognize the format
		public const string BinaryHeader = "MMSL";
		/// the version of the binary format, written right after the header
		public const int BinaryFormatVersion = 1;

		/// <summary>
		/// Saves the specified object to disk at the specified location after serializing it
		/// </summary>
		/// <param name="objectToSave"></param>
		/// <param name="saveFile"></param>
		public void Save(object objectToSave, FileStream saveFile)
		{
			WriteBinary(objectToSave, saveFile);
			saveFile.Close();
		}

		/// <summary>
		/// Loads the specified file from disk and deserializes it
		/// </summary>
		/// <param name="objectType"></param>
		/// <param name="saveFile"></param>
		/// <returns></returns>
		public object Load(System.Type objectType, FileStream saveFile)
		{
			object savedObject = ReadBinary(objectType, saveFile);
			saveFile.Close();
			return savedObject;
		}

		/// <summary>
		/// Serializes the specified object and writes it to the target stream in the binary format (header, version, data).
		/// The stream is left open.
		/// </summary>
		/// <param name="objectToSave"></param>
		/// <param name="targetStream"></param>
		public static void WriteBinary(object objectToSave, Stream targetStream)
		{
			string json = JsonUtility.ToJson(objectToSave);
			using (BinaryWriter writer = new BinaryWriter(targetStream, Encoding.UTF8, true))
			{
				writer.Write(Encoding.ASCII.GetBytes(BinaryHeader));
				writer.Write(BinaryFormatVersion);
				writer.Write(json);
			}
		}

		/// <summary>
		/// Reads an object of the specified type from a stream written by WriteBinary.
		/// Returns null (and logs a warning) if the stream isn't in that format, for example a file saved with BinaryFormatter by an older version.
		/// The stream is left open.
		/// </summary>
		/// <param name="objectType"></param>
		/// <param name="sourceStream"></param>
		/// <returns></returns>
		public static object ReadBinary(System.Type objectType, Stream sourceStream)
		{
			using (BinaryReader reader = new BinaryReader(sourceStream, Encoding.UTF8, true))
			{
				byte[] header = reader.ReadBytes(BinaryHeader.Length);
				if ((header.Length != BinaryHeader.Length) || (Encoding.ASCII.GetString(header) != BinaryHeader))
				{
					Debug.LogWarning("[MMSaveLoadManager] This file wasn't saved in the current binary format, it can't be loaded. " +
					                 "Files saved by older versions of the binary method (using BinaryFormatter) aren't supported anymore, you'll need to delete them and save again.");
					return null;
				}
				int version = reader.ReadInt32();
				if (version > BinaryFormatVersion)
				{
					Debug.LogWarning("[MMSaveLoadManager] This file was saved with a newer binary format version ("+version+"), it can't be loaded.");
					return null;
				}
				string json = reader.ReadString();
				return JsonUtility.FromJson(json, objectType);
			}
		}
	}
}
