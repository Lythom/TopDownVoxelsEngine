// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/10/06

namespace DG.Tweening.Timeline
{
    public enum StartupBehaviour
    {
        /// <summary>Does not create the tween</summary>
        DoNothing,
        /// <summary>Creates the tween without initializing it</summary>
        Create,
        /// <summary>Creates the tween and initializes the startup values of its targets</summary>
        ForceInitialization,
        /// <summary>Creates the tween and immediately completes it but without firing any internal callback</summary>
        Complete,
        /// <summary>Creates the tween and immediately completes it while also firing internal callbacks</summary>
        CompleteWithInternalCallbacks,
    }

    public enum TimeMode
    {
        TimeScale,
        DurationOverload
    }

    public enum OnEnableBehaviour
    {
        /// <summary>Does nothing, follows one-time startup behaviour</summary>
        DoNothing,
        /// <summary>Creates the tween if it hadn't been already created, otherwise restarts it</summary>
        CreateOrRestart,
        /// <summary>Plays the tween if it was generated and wasn't killed</summary>
        PlayIfExists,
        /// <summary>Restarts the tween if it was generated and wasn't killed</summary>
        RestartIfExists
    }

    public enum OnDisableBehaviour
    {
        /// <summary>Does nothing, follows one-time startup behaviour</summary>
        DoNothing,
        /// <summary>Kills the tween</summary>
        Kill,
        /// <summary>Pauses the tween if it was generated and wasn't killed</summary>
        PauseIfExists,
        /// <summary>Rewinds the tween if it hadn't been already created, otherwise restarts it</summary>
        RewindIfExists
    }
}