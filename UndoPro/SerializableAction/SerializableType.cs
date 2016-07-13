namespace UndoPro.SerializableActionHelper
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.CompilerServices;
	using UnityEngine;

	/// <summary>
	/// Wrapper for System.Type that handles serialization.
	/// Serialized Data contains assembly type name and generic arguments (one level) only.
	/// </summary>
	[System.Serializable]
	public class SerializableType : ISerializationCallbackReceiver
	{
		public Type type;

		[SerializeField]
		private string typeName;
		[SerializeField]
		private string[] genericTypes;

		public bool isCompilerGenerated { get { return Attribute.GetCustomAttribute (type, typeof(CompilerGeneratedAttribute), false) != null; } }

		public SerializableType (Type Type)
		{
			type = Type;
		}

		#region Serialization

		public void OnBeforeSerialize ()
		{
			if (type == null)
			{
				typeName = String.Empty;
				genericTypes = null;
				return;
			}

			if (type.IsGenericType)
			{ // Generic type
				typeName = type.GetGenericTypeDefinition ().AssemblyQualifiedName;
				genericTypes = type.GetGenericArguments ().Select ((Type t) => t.AssemblyQualifiedName).ToArray ();
			}
			else
			{ // Normal type
				typeName = type.AssemblyQualifiedName;
				genericTypes = null;
			}
		}

		public void OnAfterDeserialize ()
		{
			if (String.IsNullOrEmpty (typeName))
				return;

			type = Type.GetType (typeName);
			if (type == null)
				throw new Exception ("Could not deserialize type '" + typeName + "'!");

			if (type.IsGenericTypeDefinition && genericTypes != null && genericTypes.Length > 0)
			{ // Generic type
				Type[] genArgs = new Type[genericTypes.Length];
				for (int i = 0; i < genericTypes.Length; i++)
					genArgs[i] = Type.GetType (genericTypes[i]);

				Type genType = type.MakeGenericType (genArgs);
				if (genType != null)
					type = genType;
				else 
					Debug.LogError ("Could not make generic-type definition '" + typeName + "' generic!");
			}
		}

		#endregion
	}
}