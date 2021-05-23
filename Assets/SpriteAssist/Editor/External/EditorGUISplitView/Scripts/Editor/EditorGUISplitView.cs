using UnityEngine;
using UnityEditor;

public class EditorGUISplitView
{
	public enum Direction
	{
		Horizontal,
		Vertical
	}

	private readonly Direction _splitDirection;
	private readonly float _thickness;
	
	private Rect _availableRect;
	private Vector2 _beginScrollPosition;
	private float _splitNormalizedPosition;
	private bool _resize;

	public bool Resized => _resize;

	public EditorGUISplitView(Direction splitDirection, float thickness = 5f)
	{
		_splitNormalizedPosition = 0.5f;
		_splitDirection = splitDirection;
		_thickness = thickness;
	}

	public void BeginSplitView()
	{
		Rect tempRect;

		if(_splitDirection == Direction.Horizontal)
			tempRect = EditorGUILayout.BeginHorizontal (GUILayout.ExpandWidth(true));
		else 
			tempRect = EditorGUILayout.BeginVertical (GUILayout.ExpandHeight(true));
		
		if (tempRect.width > 0.0f)
		{
			_availableRect = tempRect;
		}
		
		if(_splitDirection == Direction.Horizontal)
			_beginScrollPosition = GUILayout.BeginScrollView(_beginScrollPosition, GUILayout.Width(_availableRect.width * _splitNormalizedPosition));
		else
			_beginScrollPosition = GUILayout.BeginScrollView(_beginScrollPosition, GUILayout.Height(_availableRect.height * _splitNormalizedPosition));
	}

	public Rect Split(Rect rect)
	{
		GUILayout.EndScrollView();
		return ResizeSplitFirstView(rect);
	}

	public void EndSplitView()
	{
		if(_splitDirection == Direction.Horizontal)
			EditorGUILayout.EndHorizontal ();
		else 
			EditorGUILayout.EndVertical ();
	}

	private Rect ResizeSplitFirstView(Rect rect)
	{
		Rect resizeHandleRect;

		if(_splitDirection == Direction.Horizontal)
			resizeHandleRect = new Rect (_availableRect.width * _splitNormalizedPosition, _availableRect.y, _thickness, _availableRect.height);
		else
			resizeHandleRect = new Rect (_availableRect.x,_availableRect.height * _splitNormalizedPosition, _availableRect.width, _thickness);

		var texture = EditorGUIUtility.FindTexture("d_ScrollShadow") ?? EditorGUIUtility.whiteTexture;
		GUI.DrawTexture(resizeHandleRect, texture);

		if(_splitDirection == Direction.Horizontal)
			EditorGUIUtility.AddCursorRect(resizeHandleRect,MouseCursor.ResizeHorizontal);
		else
			EditorGUIUtility.AddCursorRect(resizeHandleRect,MouseCursor.ResizeVertical);

		if( Event.current.type == EventType.MouseDown && resizeHandleRect.Contains(Event.current.mousePosition))
		{
			_resize = true;
		}
		
		if(_resize)
		{
			if(_splitDirection == Direction.Horizontal)
				_splitNormalizedPosition = Event.current.mousePosition.x / _availableRect.width;
			else
				_splitNormalizedPosition = Event.current.mousePosition.y / _availableRect.height;
		}

		if (Event.current.type == EventType.MouseUp)
		{
			_resize = false;
		}        
		
		return resizeHandleRect;
	}
}