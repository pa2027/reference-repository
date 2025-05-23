﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Zenject;

public class PlayerLeaderboard : MonoBehaviour {

	private const float c_RefreshTime = 3f;

	public RectTransform m_DotLine;
	public List<PlayerPercentBar> m_PercentBars = new List<PlayerPercentBar>();
	public List<Transform> m_Dots = new List<Transform>();

    public Text m_NextElimination;

    public Image m_BackgroundImage;

    private RectTransform m_RectTransform;

    private bool m_Playing;
	private float m_Timer;
	private List<Player> m_RegisteredPlayers = new List<Player>();
    private List<PlayerPercentBar> m_CurrentPercentBars;
    private IBattleRoyaleService m_BattleRoyaleService;
    private IGameService m_GameService;

    [Inject]
    public void Construct(IBattleRoyaleService battleRoyaleService, IGameService gameService)
    {
	    m_BattleRoyaleService = battleRoyaleService;
	    m_GameService = gameService;
    }

	private void Awake()
	{
		m_GameService.onScoresCalculated += OnScoresCalculated;
        m_GameService.onEndGame += OnEndGame;
		m_BattleRoyaleService.onElimination += OnElimination;

        m_RectTransform = GetComponent<RectTransform>();

        m_CurrentPercentBars = new List<PlayerPercentBar>(m_PercentBars);
    }

	private void OnDestroy()
	{
		m_GameService.onScoresCalculated -= OnScoresCalculated;
		m_GameService.onEndGame -= OnEndGame;
		m_BattleRoyaleService.onElimination -= OnElimination;
	}

	void Refresh()
	{
		m_Timer = Time.time + c_RefreshTime;

		m_BattleRoyaleService.Order();

		m_RegisteredPlayers.Clear();

		float percent = (m_BattleRoyaleService.GetBestPlayer().Score);

		List<Player> toDisplay = m_BattleRoyaleService.m_Players;

		toDisplay.Sort((x, y) => (x.m_Rank - y.m_Rank));

		for (int i = 0; i < m_CurrentPercentBars.Count; ++i)
		{
            if (toDisplay.Count > i)
                UpdateBar(m_CurrentPercentBars[i], toDisplay[i], percent, i == toDisplay.Count - 1);
            else
                m_CurrentPercentBars[i].Hide();
		}
		m_NextElimination.text = "<color=#" + ColorUtility.ToHtmlStringRGBA(toDisplay[toDisplay.Count - 1].m_Color) + ">" + toDisplay[toDisplay.Count - 1].Name + "</color>" + " will be eliminated in";
		m_BackgroundImage.color = toDisplay[toDisplay.Count - 1].m_Color;

		LayoutRebuilder.ForceRebuildLayoutImmediate(m_RectTransform);
	}

    void OnScoresCalculated()
	{
		if (m_Timer > Time.time)
			return;
		
		Refresh();
	}

    void OnEndGame()
    {
        m_CurrentPercentBars.Clear();
        m_CurrentPercentBars.AddRange(m_PercentBars);
    }

    void OnElimination(Player _Player)
	{
        m_CurrentPercentBars[m_CurrentPercentBars.Count - 1].gameObject.SetActive(false);
        m_CurrentPercentBars.RemoveAt(m_CurrentPercentBars.Count - 1);
		m_DotLine.SetSiblingIndex (m_CurrentPercentBars.Count - 1);
		Refresh();
	}

    void UpdateBar(PlayerPercentBar _Bar, Player _Player, float _MaxPercent, bool _Last = false)
	{
		if (_Player == null || m_RegisteredPlayers.Contains(_Player))
			_Bar.Hide();
		else
		{
			m_RegisteredPlayers.Add(_Player);
			_Bar.UpdateBar(_Player.m_Rank + 1, _Player, _Last, GetRelativePercent(_Player.Score, _MaxPercent));         
		}
	}

	void UpdateBar(PlayerPercentBar _Bar, int _Ranking, float _MaxPercent, bool _Last = false)
	{
		UpdateBar(_Bar, m_BattleRoyaleService.GetPlayer(_Ranking), _MaxPercent, _Last);
	}

    float GetRelativePercent(float _Percent, float _MaxPercent)
	{
		if (Mathf.Approximately(_MaxPercent, 0))
			return (1f);
		
		return (_Percent / _MaxPercent);
	}  
}
