namespace FocusFlowFinal.Models.Sound;

public class ActiveSoundState
{
    public string SoundId      { get; set; } = string.Empty;
    public float  Volume       { get; set; } = 1.0f;
    public bool   IsPomodoroTrack { get; set; } = false;
}
