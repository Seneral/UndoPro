namespace UndoPro.SerializableActionHelper
{
	using UnityEngine;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.IO;
	using System.Runtime.Serialization.Formatters.Binary;
	using System.Reflection;
	using System.Runtime.CompilerServices;

	/// <summary>
	/// Wrapper for an arbitrary object that handles basic serialization, both System.Object, UnityEngine.Object, and even basic unserializable types (the same way, but one-level only, unserializable members will be default or null if previously null)
	/// </summary>
	[Serializable]
	public class SerializableObject : SerializableObjectOneLevel
	{
		[SerializeField] 
		private List<SerializableObjectOneLevel> manuallySerializedMembers;

		/// <summary>
		/// Create a new SerializableObject from an arbitrary object
		/// </summary>
		public SerializableObject (object srcObject) : base (srcObject) { }
		/// <summary>
		/// Create a new SerializableObject from an arbitrary object with the specified name
		/// </summary>
		public SerializableObject (object srcObject, string name) : base (srcObject, name) { }

		#region Serialization

		/// <summary>
		/// Serializes the given object and stores it into this SerializableObject
		/// </summary>
		protected override void Serialize ()
		{
			if (isNullObject = Object == null)
				return;

			base.Serialize (); // Serialized normally

			if (unityObject == null && String.IsNullOrEmpty (serializedSystemObject))
			{ // Object is unserializable so it will later be recreated from the type, now serialize the serializable field values of the object
				FieldInfo[] fields = objectType.type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				manuallySerializedMembers = fields.Select ((FieldInfo field) => new SerializableObjectOneLevel (field.GetValue (Object), field.Name)).ToList ();
			}
		}

		/// <summary>
		/// Deserializes this SerializableObject
		/// </summary>
		protected override void Deserialize ()
		{
			Object = null;
			if (isNullObject)
				return;

			base.Deserialize (); // Deserialize normally

			if ((Object == null || !Object.GetType ().IsSerializable) && manuallySerializedMembers != null)
			{ // This object ha an unserializable type, and previously the object was recreated from that type
				// Now, restore the serialized field values of the object
				FieldInfo[] fields = objectType.type.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (fields.Length != manuallySerializedMembers.Count)
					Debug.LogError ("Field length and serialized member length doesn't match (" + fields.Length + ":" + manuallySerializedMembers.Count + ")!");
				foreach (FieldInfo field in fields)
				{
					SerializableObjectOneLevel matchObj = manuallySerializedMembers.Find ((SerializableObjectOneLevel obj) => obj.Name == field.Name && obj.Object.GetType () == field.FieldType);
					if (matchObj != null)
						field.SetValue (Object, matchObj.Object);
					else
						Debug.LogError ("Couldn't find a matching serialized field for '" + (field.IsPublic? "public" : "private") + (field.IsStatic? " static" : "") + " " + field.FieldType.FullName + "'!");
				}
				manuallySerializedMembers = null;
			}
		}

		#endregion
	}

	/// <summary>
	/// Wrapper for an arbitrary object that handles basic serialization, both System.Object, UnityEngine.Object; unserializable types will be default or null if previously null;
	/// NO RECOMMENDED TO USE, it is primarily built to support SerializableObject!
	/// </summary>
	[Serializable]
	public class SerializableObjectOneLevel : ISerializationCallbackReceiver
	{
		[SerializeField]
		public string Name; // Just to identify this object
		[NonSerialized]
		public object Object;

		// Serialized Data
		[SerializeField]
		protected bool isNullObject;
		[SerializeField] 
		protected SerializableType objectType;
		[SerializeField] 
		protected UnityEngine.Object unityObject;
		[SerializeField] 
		protected string serializedSystemObject;

		public SerializableObjectOneLevel (object srcObject)
		{
			Object = srcObject;
		}

		public SerializableObjectOneLevel (object srcObject, string name)
		{
			Object = srcObject;
			Name = name;
		}

		#region Serialization

		public void OnBeforeSerialize ()
		{
			Serialize ();
		}

		/// <summary>
		/// Serializes the given object and stores it into this SerializableObject
		/// </summary>
		protected virtual void Serialize () 
		{
			if (isNullObject = Object == null)
				return;

			unityObject = null;
			serializedSystemObject = String.Empty;
			objectType = new SerializableType (Object.GetType ());

			if (typeof(UnityEngine.Object).IsAssignableFrom (Object.GetType ()))
				unityObject = (UnityEngine.Object)Object;
			else if (Object.GetType ().IsSerializable)
				serializedSystemObject = SerializeToString<System.Object> (Object);
			// else default object (and even serializable members) will be restored from the type
		}

		public void OnAfterDeserialize ()
		{
			Deserialize ();
		}

		/// <summary>
		/// Deserializes this SerializableObject
		/// </summary>
		protected virtual void Deserialize () 
		{
			Object = null;
			if (isNullObject)
				return;

			if (unityObject != null)
				Object = unityObject;
			else if (!String.IsNullOrEmpty (serializedSystemObject))
				Object = DeserializeFromString<System.Object> (serializedSystemObject);
			else
			{ // Unserializable type, it will be recreated from the type (and even it's serializable members)
				if (objectType.type == null)
					throw new Exception ("Could not deserialize object as it's type could no be deserialized!");
				Object = Activator.CreateInstance (objectType.type);
				// If it is an inherited SerializableObject then serializable members will be restored, too
			}

			if (Object == null)
				throw new DataMisalignedException ("Could not deserialize object of type '" + objectType.type + "'!");
		}

		#endregion

		#region Embedded Util

		/// <summary>
		/// Serializes 'value' to a string, using BinaryFormatter
		/// </summary>
		protected static string SerializeToString<T> (T value)
		{
			if (value == null)
				return null;
			using (MemoryStream stream = new MemoryStream ())
			{
				new BinaryFormatter ().Serialize (stream, value);
				stream.Flush ();
				return Convert.ToBase64String (stream.ToArray());
			}
		}

		/// <summary>
		/// Deserializes an object of type T from the string 'data'
		/// </summary>
		protected static T DeserializeFromString<T> (string data)
		{
			if (String.IsNullOrEmpty (data))
				return default(T);
			byte[] bytes = Convert.FromBase64String (data);
			using (MemoryStream stream = new MemoryStream(bytes)) 
			{
				return (T)new BinaryFormatter().Deserialize (stream);
			}
		}

		#endregion
	}
}