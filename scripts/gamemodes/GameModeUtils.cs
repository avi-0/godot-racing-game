using System;
using Godot;

namespace racingGame;

public class GameModeUtils
{
    public void TimeAttack()
    {
        GameModeController.CurrentGameMode = new GameModeTimeAttack();
    }

    public void UpdateLocalRaceTime(TimeSpan raceTime)
    {
        GameManager.Singleton.TimeLabel.Text = raceTime.ToString("mm") + ":" + raceTime.ToString("ss") + "." + raceTime.ToString("fff");
    }

    public void UpdateLocalPb(TimeSpan newPb)
    {
        GameManager.Singleton.PbLabel.Text = "PB: " + newPb.ToString("mm") + ":" + newPb.ToString("ss") + "." + newPb.ToString("fff");
    }	
    public void UnloadLocalPb()
    {
        GameManager.Singleton.PbLabel.Text = "PB: ";
    }

    public void OpenFinishWindow(TimeSpan finishTime, bool isPb)
    {
        GameManager.Singleton.FinishTimeLabel.Text = "Race Time: " + finishTime.ToString("mm") + ":" + finishTime.ToString("ss") + "." + finishTime.ToString("fff");
        if (isPb)
        {
            GameManager.Singleton.FinishTimeLabel.Text += "\nPersonal Best!!!";
        }

        GameManager.Singleton.FinishPanel.Show();
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    public void SetStartTimer(int time)
    {
        if (time > 0)
        {
            GameManager.Singleton.StartTimerLabel.Show();
            GameManager.Singleton.StartTimerLabel.Text = time.ToString();
        }
        else
        {
            GameManager.Singleton.StartTimerLabel.Hide();
        }
    }
}