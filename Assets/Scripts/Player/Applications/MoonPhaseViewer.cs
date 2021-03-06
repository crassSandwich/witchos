﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WitchOS
{
    public class MoonPhaseViewer : MonoBehaviour
    {
        public List<Sprite> PhaseIcons;

        public Image PhaseIconImage;
        public TextMeshProUGUI TodayPhase, TomorrowPhase;

        public TimeState TimeState;

        void Update ()
        {
            MoonPhase
                today = TimeState.GetTodaysMoonPhase(),
                tomorrow = TimeState.GetTomorrowsMoonPhase();

            TodayPhase.text = today.ToString(true);
            TomorrowPhase.text = tomorrow.ToString(true);

            PhaseIconImage.sprite = PhaseIcons[(int) today];
        }
    }
}
