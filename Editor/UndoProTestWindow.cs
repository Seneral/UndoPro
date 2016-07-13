using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

using UndoPro;
using UndoPro.SerializableActionHelper;

public class UndoProTestWindow : EditorWindow 
{
	[MenuItem ("Window/UndoPro Test")]
	private static void Open () 
	{
		EditorWindow.GetWindow<UndoProTestWindow> ("UndoPro Test");
	}

	private static int maxRecordCount = 10;
	private static string newRecordName = "";

	public void OnEnable () 
	{
		/*UndoProManager.OnUndoPerformed += (string[] performedUndos) => 
		{
			string undoList = "Performed " + performedUndos.Length + " undos: ";
			foreach (string performedUndo in performedUndos)
				undoList += performedUndo + "; ";
			Debug.Log (undoList);
		};

		UndoProManager.OnRedoPerformed += (string[] performedRedos) => 
		{
			string redoList = "Performed " + performedRedos.Length + " redos: ";
			foreach (string performedRedo in performedRedos)
				redoList += performedRedo + "; ";
			Debug.Log (redoList);
		};

		UndoProManager.OnAddUndoRecord += (string[] addedUndoRecords, bool significant) => 
		{
			string addedUndosList = "Added " + addedUndoRecords.Length + " " + (significant? "significant" : "unsignificant") + " undo records: ";
			foreach (string newRecord in addedUndoRecords)
				addedUndosList += newRecord + "; ";
			Debug.Log (addedUndosList);
		};*/
	}

	public void OnGUI () 
	{
		Repaint ();

		if (!UndoProManager.enabled)
			return;

		UndoProRecords records = UndoProManager.records;
		UndoState state = records.undoState;

		GUILayout.BeginHorizontal ();
		if (GUILayout.Button ("< Undo " + (state.undoRecords.Count > 0? ("'" + state.undoRecords[state.undoRecords.Count-1] + "'") : "not possible")))
			Undo.PerformUndo ();
		else if (GUILayout.Button ("Redo " + (state.redoRecords.Count > 0? ("'" + state.redoRecords[state.redoRecords.Count-1] + "'") : "not possible") + " >"))
			Undo.PerformRedo ();
		else // For some reason it throws an unimportant but distracting error when not put in the else condition
			GUILayout.EndHorizontal ();


		EditorGUILayout.Space ();


		GUILayout.BeginHorizontal ();
		newRecordName = EditorGUILayout.TextField (newRecordName);
		if (GUILayout.Button ("Add Record")) 
		{
			string recordName = newRecordName; // Have to store because actions will still reference original variable
			UndoProManager.RecordOperationAndPerform (() => Debug.Log ("Performed custom Action: " + recordName), () => Debug.Log ("Undid custom Action: " + recordName), recordName);
			//UndoProManager.RecordOperationAndPerform (() => Debug.Log ("Performed custom Action!"), () => Debug.Log ("Undid custom Action!"), recordName);
		}
		GUILayout.EndHorizontal ();


		EditorGUILayout.Space ();
		EditorGUILayout.Space ();


		maxRecordCount = EditorGUILayout.IntSlider ("Max shown records", maxRecordCount, 0, 20);

		EditorGUILayout.Space ();

		GUILayout.BeginHorizontal ();
		// Undo's
		GUILayout.BeginVertical ();
		GUILayout.Label ("UNDO:");
		for (int cnt = 0; cnt <  Math.Min (state.undoRecords.Count, maxRecordCount); cnt++)
		{
			int index = state.undoRecords.Count-1-cnt;
			GUILayout.Label ("Undo " + index + ": " + state.undoRecords[index]);
		}
		GUILayout.EndVertical ();
		// Redo's
		GUILayout.BeginVertical ();
		GUILayout.Label ("REDO:");
		for (int cnt = 0; cnt <  Math.Min (state.redoRecords.Count, maxRecordCount); cnt++)
		{
			int index = state.redoRecords.Count-1-cnt;
			GUILayout.Label ("Redo " + index + ": " + state.redoRecords[index]);
		}
		GUILayout.EndVertical ();
		GUILayout.EndHorizontal ();


		EditorGUILayout.Space ();
		EditorGUILayout.Space ();


		GUILayout.Label ("--- PRO RECORDS:");

		GUILayout.BeginHorizontal ();
		// Undo's
		GUILayout.BeginVertical ();
		GUILayout.Label ("UNDO:");
		List<UndoProRecord> undoStack = records.proUndoStack;
		for (int cnt = 0; cnt <  Math.Min (undoStack.Count, maxRecordCount); cnt++)
		{
			UndoProRecord rec = undoStack[undoStack.Count-1-cnt];
			GUILayout.Label ("Undo " + (state.undoRecords.Count-1+rec.relativeStackPos) + "(" + rec.relativeStackPos + "): " + rec.label);
		}
		GUILayout.EndVertical ();
		// Redo's
		GUILayout.BeginVertical ();
		GUILayout.Label ("REDO:");
		List<UndoProRecord> redoStack = records.proRedoStack;
		for (int cnt = 0; cnt <  Math.Min (redoStack.Count, maxRecordCount); cnt++)
		{
			UndoProRecord rec = redoStack[redoStack.Count-1-cnt];
			GUILayout.Label ("Redo " + (state.redoRecords.Count-rec.relativeStackPos) + "(" + rec.relativeStackPos + "): " + rec.label);
		}
		GUILayout.EndVertical ();
		GUILayout.EndHorizontal ();

		EditorGUILayout.Space ();

		GUILayout.Label ("Current Group " + Undo.GetCurrentGroupName () + " : " + Undo.GetCurrentGroup () + " ---");
	}


}
