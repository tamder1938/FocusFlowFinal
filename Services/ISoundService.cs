using FocusFlowFinal.Models.Sound;
using System.Collections.Generic;

namespace FocusFlowFinal.Services;

public enum PomodoroPhase { Work, Break, Stopped }

public interface ISoundService
{
    IReadOnlyList<SoundPreset> Presets { get; }

    bool  IsPlaying { get; }
    float MasterVolume { get; set; }
    bool  MixWithPomodoro { get; set; }

    bool   IsActive(string soundId);
    float  GetVolume(string soundId);

    void   Play(string soundId, string filePath);
    void   Stop(string soundId);
    void   StopAll();
    void   SetVolume(string soundId, float volume);

    void   PauseAll();
    void   ResumeAll();

    void   OnPomodoroPhaseChanged(PomodoroPhase phase);

    void   SaveState();
    void   LoadState();
}
