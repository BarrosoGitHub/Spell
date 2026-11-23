using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace MoreMountains.Tools
{
	/// <summary>
	/// This save load method saves and loads files as encrypted binary files.
	/// It uses the same binary format as MMSaveLoadManagerMethodBinary (JsonUtility serialization), encrypted with the specified Key.
	/// Files saved with the BinaryFormatter based encrypted binary method of older versions can't be read by this method, and will load as null.
	/// </summary>
	public class MMSaveLoadManagerMethodBinaryEncrypted : MMSaveLoadManagerEncrypter, IMMSaveLoadManagerMethod
	{
		/// <summary>
		/// Saves the specified object to disk at the specified location after encrypting it
		/// </summary>
		/// <param name="objectToSave"></param>
		/// <param name="saveFile"></param>
		public void Save(object objectToSave, FileStream saveFile)
		{
			MemoryStream memoryStream = new MemoryStream();
			MMSaveLoadManagerMethodBinary.WriteBinary(objectToSave, memoryStream);
			memoryStream.Position = 0;
			Encrypt(memoryStream, saveFile, Key);
			saveFile.Flush();
			memoryStream.Close();
			saveFile.Close();
		}

		/// <summary>
		/// Loads the specified file from disk, decrypts it, and deserializes it
		/// </summary>
		/// <param name="objectType"></param>
		/// <param name="saveFile"></param>
		/// <returns></returns>
		public object Load(System.Type objectType, FileStream saveFile)
		{
			object savedObject;
			MemoryStream memoryStream = new MemoryStream();
			try
			{
				Decrypt(saveFile, memoryStream, Key);
			}
			catch (CryptographicException ce)
			{
				Debug.LogError("[MMSaveLoadManager] Encryption key error: " + ce.Message);
				return null;
			}
			memoryStream.Position = 0;
			savedObject = MMSaveLoadManagerMethodBinary.ReadBinary(objectType, memoryStream);
			memoryStream.Close();
			saveFile.Close();
			return savedObject;
		}
	}
}
