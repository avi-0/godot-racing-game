using System;
using Godot;

namespace racingGame;

public class GameModeUtils
{
    //--GAMEMODE SELECTION--//
    public void TimeAttack()
    {
        GameModeController.CurrentGameMode = new GameModeTimeAttack();
    }
    //----//

    //--UI--//
    public void UpdateLocalRaceTime(TimeSpan raceTime)
    {
        GameManager.Singleton.TimeLabel.Text = raceTime.ToString("mm") + ":" + raceTime.ToString("ss") + "." + raceTime.ToString("fff");
    }

    public void UpdateLocalPb(TimeSpan newPb)
    {
        GameManager.Singleton.PbLabel.Text = "PB: " + newPb.ToString("mm") + ":" + newPb.ToString("ss") + "." + newPb.ToString("fff");
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

    public void SetCheckPointCount(int current, int total)
    {
        if (total == 0)
        {
            GameManager.Singleton.CheckPointLabel.Text = "";
        }
        else
        {
            GameManager.Singleton.CheckPointLabel.Text = current + "/" + total;
        }
    }
    
    public void UnloadLocalStats()
    {
        GameManager.Singleton.PbLabel.Text = "PB: ";
        UpdateLocalRaceTime(TimeSpan.Zero);
        SetStartTimer(0);
        SetCheckPointCount(0,0);
    }
    //----//
    
    //PLAYER SAVES//
    private const string savePBPath = "user://userdata.mdat";
    public void SaveUserPB(TimeSpan time, string TrackUID)
    {
        if (time == TimeSpan.Zero || TrackUID == "0") { return;}
        
        var config = new ConfigFile();
        config.LoadEncrypted(savePBPath, "sosal?".Sha256Buffer());
        config.SetValue("PBS", TrackUID, time.TotalMilliseconds);
        config.SaveEncrypted(savePBPath, "sosal?".Sha256Buffer());
    }

    public TimeSpan LoadUserPB(string TrackUID)
    {
        var config = new ConfigFile();
        Error err = config.LoadEncrypted(savePBPath, "sosal?".Sha256Buffer());
        if (err == Error.Ok)
        {
            int ms = (int)config.GetValue("PBS", TrackUID, 0);
            if (ms != 0)
            {
                return TimeSpan.FromMilliseconds(ms);
            }
        }

        return TimeSpan.Zero;
    }
    //----//
}