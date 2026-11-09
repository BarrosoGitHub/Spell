# Mission states

`Mission` is the abstract MonoBehaviour base for mission modes. It owns Ready, Running, Completed, Failed, and Aborted state instances. A derived mission selects a state by assigning the `State` property, for example `State = RunningState`.

`MissionState` mirrors the controller `State` pattern. Attach behavior to its `OnEnterState`, `OnExitState`, `OnUpdate`, and `OnFixedUpdate` actions. `Mission` dispatches update callbacks for the currently selected state.

```csharp
protected override void Awake()
{
    RunningState.OnEnterState += StartRun;
    RunningState.OnUpdate += UpdateRun;
    RunningState.OnExitState += StopRun;
    CompletedState.OnEnterState += ShowResults;
    base.Awake(); // selects ReadyState and invokes its enter behavior
}

public void StartMission() => State = RunningState;
```

The transition order follows `Controller.State`: invoke the previous state's exit action, assign the new state, invoke its enter action, then raise `OnStateChanged`. Assigning the same state again does nothing. Configure state callbacks before calling `base.Awake()` so the initial Ready enter action is available.

`Survival` tracks elapsed time while Running and fails when the player dies or is missing. It resolves its player reference from `GameManager` if not assigned. Player death is forwarded by `MissionManager` through the virtual `OnPlayerDied` hook.

Derive new modes directly from `Mission`. Use their states for mode-specific behavior, while keeping shared gameplay systems such as movement, combat, and enemy spawning in their own components.

## MissionManager and results

### Run timestamps

Every mission inherits nullable `DateTimeOffset` properties `StartTimestamp` and
`EndTimestamp` from `Mission`. Both use UTC wall-clock time, independently of Unity's
time scale and the mission's gameplay elapsed time. Both are null while Ready.
Entering Running records the start and clears the end. Entering Completed, Failed,
or Aborted after a start records the end once. Resetting to Ready clears both for the
next run; rejected starts and repeated assignments of the same state do not change them.

`MissionManager.HandleMissionChangedState` records timestamps for its selected
mission when `OnStateChanged` fires, after state enter callbacks and before manager
notifications such as `OnMissionStarted`. Direct state assignments are tracked while
the manager is subscribed; unmanaged mission instances do not record timestamps.
Read them from the mission instance;
`MissionResult` continues to contain the existing gameplay result fields.

### Player death

`PlayerController` inherits `OnControllerDied` from `Controller`. Before starting a mission, `MissionManager` subscribes to the player's death event and calls its own `FailMission()` when the player dies. It unsubscribes when the mission stops running, when switching missions, or when the manager is destroyed.

Mission types do not handle player death separately. They can override `FailMission()` for failure-specific behavior; Exterminate cleans up its enemies before entering Failed.

Call `SelectMission(typeof(Survival))` or `SelectMission(index)` to create and select a discovered mission type, then `StartMission()` to run it. `selectedMission` exposes the selected instance. Switching types is blocked while Running; reset a stopped mission before restarting it.

The manager listens to the selected mission's `OnStateChanged`. Entering Running clears `LastResult` and invokes `OnMissionStarted` once. Entering Completed stores the mission's `Result` in `LastResult` and invokes `OnMissionCompleted`. Call `Mission.CompleteMission(result)` to set the result before entering Completed. The manager unsubscribes when replacing the selection or being destroyed.

## Endless waves

`EndlessWaves` is automatically discovered by MissionManager and its Play Mode selector. Select it by name or call `SelectMission(typeof(EndlessWaves))`, then start the mission. It has no automatic victory condition.

The default wave counts are 3, 5, 7, and so on: `firstWaveEnemies + (wave - 1) * enemiesAddedPerWave`. Each enemy uses the existing Normal pool and group spawn path, one spawn every 0.2 seconds. After every enemy in the wave has spawned and been despawned, a one-second break precedes the next wave. Enemies do not need to die before the next individual spawn. The four wave settings are editable on the selected mission component before starting. Timers use scaled time, so pausing freezes progress.

The mission owns wave progression and a set of its living enemies. It listens to ControllerSpawner's despawn notification, so distance-based combat group changes do not count as kills or clear a wave. EnemyManager continues to own combat groups, death handling and pooling. The group uses the Exterminate engagement settings (3 members, 15-unit range, 1 simultaneous attacker). Spawn positions follow the existing group spawning implementation.

Leaving Running stops the spawn coroutine and cleans up enemies and group coroutines. Disabling aborts the run and unsubscribes; reset clears wave/time counters before a restart. Player death or loss fails the mission. `CurrentWave`, `EnemiesInWave`, `EnemiesRemaining` (currently spawned and alive), and `SurvivalTime` provide read-only progress.
