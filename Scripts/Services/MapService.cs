﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Zenject;

public class MapService : IMapService
{
	private const int					c_MapComputationFactor = 10000;

	public float					s_TileSize = 10.0f;

	private List<GameObject> 		    m_Entities;
	private Hashtable       			m_Datas;
	private bool						m_IsInitialized = false;
	private float 						m_SqrTileFactor;

	public bool							initialized { get { return m_IsInitialized; } }


    private GameConfig m_GameConfig;
    private DiContainer m_Container;
    private ISceneEventsService m_SceneEventsService;
    
    [Inject]
    public void Construct(ISceneEventsService sceneEventsService)
    {
	    m_SceneEventsService = sceneEventsService;
	    m_SceneEventsService.OnAwake += OnAwake;
        Init();
    }
    
    private void Init()
    {
	    m_IsInitialized = true;
	    m_Datas = new Hashtable();
	    m_Entities = new List<GameObject> ();
	    m_SqrTileFactor = s_TileSize / Mathf.Sqrt (Mathf.PI);
	    m_SqrTileFactor *= m_SqrTileFactor;
    }

    private void OnAwake()
    {
	    m_Datas.Clear();
	    m_Entities.Clear();
    }


    #region External calls
	
	public int RegisterEntity(GameObject _Entity)
	{
		if (!initialized)
			Init ();

		return AddEntity (_Entity);
	}

	public void UnregisterEntity(int _LastKey, int _EntityIndex)
	{
		RemoveOldKey (_LastKey, _EntityIndex);
		m_Entities [_EntityIndex] = null;
	}

	public int UpdateEntity(int _LastKey, Vector3 _Position, int _EntityIndex)
	{
		int newKey = Mathf.RoundToInt(_Position.x / s_TileSize) * c_MapComputationFactor + Mathf.RoundToInt(_Position.z / s_TileSize);
		UpdateEntity(_LastKey, newKey, _EntityIndex);
		return newKey;
	}

	public void FindEntities(Vector3 _Position, float _SqrRadius, ref List<GameObject> _Results, int _Layer = -1)
	{
		Internal_FindEntities(_Position, _SqrRadius, ref _Results, _Layer);
	}

	public bool IsEntityPresent(Vector3 _Position, float _SqrRadius, int _Layer = -1)
	{
		return Internal_IsEntityPresent(_Position, _SqrRadius, _Layer);
	}

	public void Clear()
	{
		Internal_Clear ();
	}

	public Player FindNearestPlayer(Player _Player)
	{
		return Internal_FindNearestPlayer (_Player);
	}

	public PowerUp FindNearestPowerUp(Player _Player)
	{
		return Internal_FindNearestPowerUp (_Player);
	}

	#endregion

	private int AddEntity(GameObject _Entity)
	{
		m_Entities.Add (_Entity);
		return (m_Entities.Count - 1);
	}

	private void RemoveOldKey(int _OldKey, int _EntityIndex)
	{
		List<int> indexes = (List<int>) m_Datas [_OldKey];

		if (indexes == null)
			return;

		if (indexes.Count == 1)
		{
			indexes.Clear ();
			m_Datas [_OldKey] = null;
		}
		else
		{
			indexes.Remove (_EntityIndex);
			m_Datas [_OldKey] = indexes;
		}
	}

	private void AddNewKey(int _NewKey, int _EntityIndex)
	{
		if (m_Datas [_NewKey] != null)
		{
			List<int> indexes = (List<int>) m_Datas [_NewKey];
			indexes.Add (_EntityIndex);
		}
		else
		{
			List<int> indexes = new List<int> ();
			indexes.Add (_EntityIndex);
			m_Datas [_NewKey] = indexes;
		}
	}

	private void UpdateEntity(int _LastKey, int _NewKey, int _EntityIndex)
	{
		RemoveOldKey (_LastKey, _EntityIndex);
		AddNewKey (_NewKey, _EntityIndex);
	}

	private int GetPassCount(float _SqrRadius)
	{
		int passCount = Mathf.RoundToInt(_SqrRadius / (s_TileSize * s_TileSize));
		if (passCount == 0) passCount = 1;
		return passCount;
	}

	private bool Internal_IsEntityPresent(Vector3 _Position, float _SqrRadius, int _Layer)
	{
		if (m_Datas == null)
			return false;

		int passCount = GetPassCount(_SqrRadius);

		int XBase = Mathf.RoundToInt(_Position.x / s_TileSize);
		int ZBase = Mathf.RoundToInt(_Position.z / s_TileSize);

		for (int XIndex = -passCount; XIndex <= passCount; ++XIndex)
		{
			for (int ZIndex = -passCount; ZIndex <= passCount; ++ZIndex)
			{
				int XCurrent = XBase + XIndex;
				int ZCurrent = ZBase + ZIndex;

                float x = _Position.x - (s_TileSize * XCurrent);
                float z = _Position.z - (s_TileSize * ZCurrent);
                float sqrMagnitude = x * x + z * z;
				if (sqrMagnitude > _SqrRadius + m_SqrTileFactor)
					continue;

				int key = XCurrent * c_MapComputationFactor + ZCurrent;

				if (m_Datas[key] == null)
					continue;

				List<int> entityIndexes = (List<int>) m_Datas [key];

				for (int index = 0; index < entityIndexes.Count; index++)
				{
					int entityIndex = entityIndexes [index];
					GameObject entity = m_Entities [entityIndex];

					if (entity == null || (_Layer != -1 && entity.layer != _Layer))
						continue;

					Vector3 entityDiff = entity.transform.position - _Position;

					if (entityDiff.sqrMagnitude < _SqrRadius)
						return true;
				}
			}
		}

		return false;
	}

	private void Internal_FindEntities(Vector3 _Position, float _SqrRadius, ref List<GameObject> _Results, int _Layer)
	{
		if (_Results == null)
			_Results = new List<GameObject> ();
		else
			_Results.Clear ();

		if (m_Datas == null)
			return;

		int passCount = GetPassCount(_SqrRadius);

		int XBase = Mathf.RoundToInt(_Position.x / s_TileSize);
		int ZBase = Mathf.RoundToInt(_Position.z / s_TileSize);

		for (int XIndex = -passCount; XIndex <= passCount; ++XIndex)
		{
			for (int ZIndex = -passCount; ZIndex <= passCount; ++ZIndex)
			{
				int XCurrent = XBase + XIndex;
				int ZCurrent = ZBase + ZIndex;

                float x = _Position.x - (s_TileSize * XCurrent);
                float z = _Position.z - (s_TileSize * ZCurrent);
                float sqrMagnitude = x * x + z * z;
                if (sqrMagnitude > _SqrRadius + m_SqrTileFactor)
					continue;

				int key = XCurrent * c_MapComputationFactor + ZCurrent;

				if (m_Datas[key] == null)
					continue;

				List<int> entityIndexes = (List<int>) m_Datas [key];

				for (int index = 0; index < entityIndexes.Count; index++)
				{
					int entityIndex = entityIndexes [index];
					GameObject entity = m_Entities [entityIndex];

					if (entity == null || (_Layer != -1 && entity.layer != _Layer))
						continue;

                    Vector3 entityDiff = entity.transform.position - _Position;

					if (entityDiff.sqrMagnitude < _SqrRadius)
						_Results.Add (entity);
				}
			}
		}
	}

	private void Internal_Clear()
	{
		if (m_Entities != null)
			m_Entities.Clear ();

		if (m_Datas != null)
			m_Datas.Clear ();
	}

	private Player Internal_FindNearestPlayer(Player _Player)
	{
		return null;
	}

	private PowerUp Internal_FindNearestPowerUp(Player _Player)
	{
		return null;
	}

#if UNITY_EDITOR

    public string GetNames()
    {
        string names = "";
        for (int i = 0; i < m_Entities.Count; ++i)
        {
            if (m_Entities[i] == null)
                names += "<NULL>\n";
            else
                names += m_Entities[i].gameObject.name + "\n";
        }
        return names;
    }

#endif
}
