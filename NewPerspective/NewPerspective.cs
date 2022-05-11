using System.Collections.Generic;
using System;
using MonoMod.RuntimeDetour;
using UnityEngine;
using Dungeonator;

namespace NewPerspective { 

    public class NewPerspective : ETGModule {
        
        public static readonly string ConsoleCommandName = "perspective";
        public static readonly string PerspectiveModeEnabled = "3DModeEnabled";
        public static readonly string RoomOcclusionDisabled = "RoomOcclusionLayerDisabled";
        public static readonly string AspectRatioOverrideEnabled = "4_3AspectRatioOverrideEnabled";
        public static readonly string ModNameInGreen = "<color=#00FF00>[NewPerspective]</color> ";
                
        public static GameObject OcclusionMonitorObject;
        public static OcclusionMonitor occlusionMonitor;
        
        
        public override void Init() { }
        
        public override void Start() {
            ETGModConsole.Commands.AddGroup(ConsoleCommandName, ConsoleInfo);
            ETGModConsole.Commands.GetGroup(ConsoleCommandName).AddUnit("toggle3d", Toggle3DSetting);
            ETGModConsole.Commands.GetGroup(ConsoleCommandName).AddUnit("occlusion", ToggleRoomOcclusion);
            ETGModConsole.Commands.GetGroup(ConsoleCommandName).AddUnit("aspectratio", Toggle43Aspect);
            
            CreateMonitor(true);
        }

        private void ConsoleInfo(string[] consoleText) {
            if (ETGModConsole.Commands.GetGroup(ConsoleCommandName) != null && ETGModConsole.Commands.GetGroup(ConsoleCommandName).GetAllUnitNames() != null) {
                List<string> m_CommandList = new List<string>();

                foreach (string Command in ETGModConsole.Commands.GetGroup(ConsoleCommandName).GetAllUnitNames()) { m_CommandList.Add(Command); }

                if (m_CommandList.Count <= 0) { return; }

                if (!m_IsCommandValid(consoleText, string.Empty, string.Empty)) {
                    ETGModConsole.Log(ModNameInGreen + "The following console commands are available for TheGarbageCollector:\n", false);
                    foreach (string Command in m_CommandList) { ETGModConsole.Log("    " + Command + "\n", false); }
                    return;
                } else if (!m_CommandList.Contains(consoleText[0].ToLower())) {
                    ETGModConsole.Log(ModNameInGreen + "Invalid sub-command! The following console commands are available for TheGarbageCollector:\n", false);
                    foreach (string Command in m_CommandList) { ETGModConsole.Log("    " + Command + "\n", false); }
                    return;
                }
            } else {
                return;
            }
        }

        private bool m_IsCommandValid(string[] CommandText, string validCommands, string sourceSubCommand) {
            if (CommandText == null) {
                if (!string.IsNullOrEmpty(validCommands) && !string.IsNullOrEmpty(sourceSubCommand)) { ETGModConsole.Log("[TheGarbageCollector] [" + sourceSubCommand + "] ERROR: Invalid console command specified! Valid Sub-Commands: \n" + validCommands); }
                return false;
            } else if (CommandText.Length <= 0) {
                if (!string.IsNullOrEmpty(validCommands) && !string.IsNullOrEmpty(sourceSubCommand)) { ETGModConsole.Log("[TheGarbageCollector] [" + sourceSubCommand + "] No sub-command specified. Valid Sub-Commands: \n" + validCommands); }
                return false;
            } else if (string.IsNullOrEmpty(CommandText[0])) {
                if (!string.IsNullOrEmpty(validCommands) && !string.IsNullOrEmpty(sourceSubCommand)) { ETGModConsole.Log("[TheGarbageCollector] [" + sourceSubCommand + "] No sub-command specified. Valid Sub-Commands: \n" + validCommands); }
                return false;
            } else if (CommandText.Length > 1) {
                if (!string.IsNullOrEmpty(validCommands) && !string.IsNullOrEmpty(sourceSubCommand)) { ETGModConsole.Log("[TheGarbageCollector] [" + sourceSubCommand + "] ERROR: Only one sub-command is accepted!. Valid Commands: \n" + validCommands); }
                return false;
            }
            return true;
        }

        private void Toggle3DSetting(string[] consoleText) {
            int enable3D = 0;
            if (!OcclusionMonitorObject | !occlusionMonitor) { CreateMonitor(); }

            occlusionMonitor.Configured = false;
            occlusionMonitor.state = OcclusionMonitor.State.ChangingSettings;

            if (occlusionMonitor.Enable3D) {
                occlusionMonitor.Enable3D = false;
                ETGModConsole.Log(ModNameInGreen + "Perspective mode disabled.\n");
            } else {
                occlusionMonitor.Enable3D = true;
                enable3D = 1;
                ETGModConsole.Log(ModNameInGreen + "Perspective mode enabled.\n");
            }

            PlayerPrefs.SetInt(PerspectiveModeEnabled, enable3D);
            PlayerPrefs.Save();

            occlusionMonitor.ToggleHooksAndPerspectiveMode(occlusionMonitor.Enable3D);

            occlusionMonitor.Configured = true;
        }

        private void ToggleRoomOcclusion(string[] consoleText) {
            if (!OcclusionMonitorObject | !occlusionMonitor) { CreateMonitor(); }
            occlusionMonitor.Configured = false;
            occlusionMonitor.state = OcclusionMonitor.State.ChangingSettings;

            if (PlayerPrefs.GetInt(RoomOcclusionDisabled) == 1) {
                PlayerPrefs.SetInt(RoomOcclusionDisabled, 0);
                occlusionMonitor.DisableRoomOcclusion = false;
                ETGModConsole.Log(ModNameInGreen + "Room Occlusion enabled!");
            } else {
                PlayerPrefs.SetInt(RoomOcclusionDisabled, 1);
                occlusionMonitor.DisableRoomOcclusion = true;
                ETGModConsole.Log(ModNameInGreen + "Room Occlusion disabled!");
            }
            PlayerPrefs.Save();
            occlusionMonitor.Configured = true;
        }

        private void Toggle43Aspect(string[] consoleText) {
            if (!OcclusionMonitorObject | !occlusionMonitor) { CreateMonitor(); }

            int aspectRatioEnabled = 1;

            occlusionMonitor.Configured = false;
            occlusionMonitor.state = OcclusionMonitor.State.ChangingSettings;

            if (occlusionMonitor.Enable43AspectRatio) {
                aspectRatioEnabled = 0;
                occlusionMonitor.Enable43AspectRatio = false;
                ETGModConsole.Log(ModNameInGreen + "Aspect Ratio restored to normal!");
            } else {
                occlusionMonitor.Enable43AspectRatio = true;                
                ETGModConsole.Log(ModNameInGreen + "Aspect Ratio forced to 4:3!");
            }
            PlayerPrefs.SetInt(AspectRatioOverrideEnabled, aspectRatioEnabled);
            PlayerPrefs.Save();
            occlusionMonitor.Configured = true;
        }

        public void CreateMonitor(bool Do3DEnable = false) {
            OcclusionMonitorObject = new GameObject("NewPerspective Monitor", new Type[] { typeof(OcclusionMonitor) });
            occlusionMonitor = OcclusionMonitorObject.GetComponent<OcclusionMonitor>();
            occlusionMonitor.PreviousZoomScaleOverride = GameManager.Instance.MainCameraController.OverrideZoomScale;

            if (PlayerPrefs.GetInt(RoomOcclusionDisabled) == 1) { occlusionMonitor.DisableRoomOcclusion = true; }
            if (PlayerPrefs.GetInt(AspectRatioOverrideEnabled) == 1) { occlusionMonitor.Enable43AspectRatio = true; }
            if (PlayerPrefs.GetInt(PerspectiveModeEnabled) == 1) { occlusionMonitor.Enable3D = true; }

            if (Do3DEnable) { occlusionMonitor.ToggleHooksAndPerspectiveMode(occlusionMonitor.Enable3D); }

            occlusionMonitor.state = OcclusionMonitor.State.ChangingSettings;
            occlusionMonitor.Configured = true;
        }
        

        public override void Exit() { }
    }

    public class OcclusionMonitor : BraveBehaviour {

        public OcclusionMonitor() {
            Configured = false;
            Enable3D = false;
            DisableRoomOcclusion = false;
            Enable43AspectRatio = false;

            state = State.ChangingSettings;
            
            if (BraveCameraUtility.OverrideAspect.HasValue) { m_PreviousAspectRatioOverride = BraveCameraUtility.OverrideAspect.Value; }
            m_AspectRatioZoomFactor = 0.675f;
            m_PreviousAspectRatioZoomFactor = GameManager.Instance.MainCameraController.OverrideZoomScale;
            m_previousScalingMode = GameManager.Options.CurrentPreferredScalingMode;

            DontDestroyOnLoad(gameObject);
        }

        public bool Configured;
        public bool Enable3D;
        public bool DisableRoomOcclusion;
        public bool Enable43AspectRatio;

        // private readonly float AspectRatioZoomFactor = 0.74777777777f;
        private readonly float m_AspectRatioZoomFactor;
        private readonly float m_PreviousAspectRatioZoomFactor;

        public float PreviousZoomScaleOverride;
        
        public enum State { ChangingSettings, DefaultWaitState, WaitForLevelLoad };

        public State state;

        public Hook zOffsetHook;

        private GameOptions.PreferredScalingMode m_previousScalingMode;

        private float? m_PreviousAspectRatioOverride;

        public float CurrentZOffsetHook(Func<CameraController, float> orig, CameraController self) {
            if (self.IsPerspectiveMode) { return self.transform.position.y - 20f; } // -40 was original value
            return orig(self);
        }
        
        public void ToggleHooksAndPerspectiveMode(bool mode) {
            if (mode) {
                GameManager.Instance.MainCameraController.Camera.orthographic = false;
                GameManager.Instance.MainCameraController.IsPerspectiveMode = true;
                if (DisableRoomOcclusion) {
                    GameManager.Instance.MainCameraController.OverrideZoomScale = 0.5f;
                } else {
                    GameManager.Instance.MainCameraController.OverrideZoomScale = 0.6f;
                }
                if (Pixelator.Instance.slavedCameras != null && Pixelator.Instance.slavedCameras.Count > 0) {
                    foreach (Camera camera in Pixelator.Instance.slavedCameras) {
                        camera.orthographic = false;
                    }
                }
                if (Pixelator.Instance.AdditionalPreBGCamera) {
                    Pixelator.Instance.AdditionalPreBGCamera.orthographic = false;
                }
                if (Pixelator.Instance.AdditionalBGCamera) {
                    Pixelator.Instance.AdditionalBGCamera.orthographic = false;
                }
                if (zOffsetHook == null) {
                    zOffsetHook = new Hook(
                        typeof(CameraController).GetProperty(nameof(GameManager.Instance.MainCameraController.CurrentZOffset)).GetGetMethod(),
                        typeof(OcclusionMonitor).GetMethod(nameof(CurrentZOffsetHook)),
                        typeof(CameraController)
                    );
                }
                GameManager.Options.CurrentPreferredScalingMode = GameOptions.PreferredScalingMode.UNIFORM_SCALING_FAST;
            } else {
                GameManager.Instance.MainCameraController.Camera.orthographic = true;
                GameManager.Instance.MainCameraController.IsPerspectiveMode = false;
                if (Enable43AspectRatio) {
                    GameManager.Instance.MainCameraController.OverrideZoomScale = m_AspectRatioZoomFactor;
                } else {
                    GameManager.Instance.MainCameraController.OverrideZoomScale = PreviousZoomScaleOverride;
                }
                if (Pixelator.Instance.slavedCameras != null && Pixelator.Instance.slavedCameras.Count > 0) {
                    foreach (Camera camera in Pixelator.Instance.slavedCameras) { camera.orthographic = true; }
                }
                
                if (Pixelator.Instance.AdditionalPreBGCamera) { Pixelator.Instance.AdditionalPreBGCamera.orthographic = true; }
                if (Pixelator.Instance.AdditionalBGCamera) { Pixelator.Instance.AdditionalBGCamera.orthographic = true; }
                
                if (zOffsetHook != null) { zOffsetHook.Dispose(); zOffsetHook = null; }
                GameManager.Options.CurrentPreferredScalingMode = m_previousScalingMode;
            }
            Enable3D = mode;
        }
        

        public void Update() {
            if (!Configured | Foyer.DoIntroSequence | Foyer.DoMainMenu) { return; }
            if (!GameManager.Instance?.PrimaryPlayer) { return; }

            switch (state) {
                case State.ChangingSettings:
                    if (PlayerPrefs.GetInt(NewPerspective.PerspectiveModeEnabled) == 1) {
                        Enable3D = true;                        
                    } else {
                        Enable3D = false;
                        if (!Enable43AspectRatio) {
                            GameManager.Instance.MainCameraController.OverrideZoomScale = m_PreviousAspectRatioZoomFactor;
                        }
                    }
                    if (PlayerPrefs.GetInt(NewPerspective.RoomOcclusionDisabled) == 1) {
                        DisableRoomOcclusion = true;
                    } else {
                        DisableRoomOcclusion = false;
                        if (!(bool)Pixelator.Instance?.DoOcclusionLayer) { Pixelator.Instance.DoOcclusionLayer = true; }
                    }
                    if (PlayerPrefs.GetInt(NewPerspective.AspectRatioOverrideEnabled) == 1) {
                        Enable43AspectRatio = true;
                    } else {
                        Enable43AspectRatio = false;
                        if (!Enable3D) { GameManager.Instance.MainCameraController.OverrideZoomScale = m_AspectRatioZoomFactor; }
                        // BraveCameraUtility.OverrideAspect = m_PreviousAspectRatioOverride;
                        BraveCameraUtility.OverrideAspect = null;
                    }
                    if (!DisableRoomOcclusion && !Enable3D && Pixelator.Instance) {
                        Pixelator.Instance.DoOcclusionLayer = true;
                    }
                    state = State.WaitForLevelLoad;
                    return;
                case State.DefaultWaitState:
                    if (DisableRoomOcclusion | Enable3D) {
                        if ((bool)Pixelator.Instance?.DoOcclusionLayer) { Pixelator.Instance.DoOcclusionLayer = false; }
                        if (GameManager.Instance?.Dungeon) {
                            foreach (RoomHandler room in GameManager.Instance?.Dungeon?.data?.rooms) { room.SetRoomActive(true); }
                        }
                    }
                    if (Enable43AspectRatio) {
                        if (Enable3D) {
                            if (GameManager.Instance.MainCameraController.OverrideZoomScale != 1) {
                                GameManager.Instance.MainCameraController.OverrideZoomScale = 1;
                            }
                        } else {
                            if (GameManager.Instance.MainCameraController.OverrideZoomScale != m_AspectRatioZoomFactor) {
                                GameManager.Instance.MainCameraController.OverrideZoomScale = m_AspectRatioZoomFactor;
                            }
                        }
                        if (BraveCameraUtility.OverrideAspect != 1.33333333333f) { BraveCameraUtility.OverrideAspect = 1.33333333333f; }
                    }
                    return;
                case State.WaitForLevelLoad:
                    if (Dungeon.IsGenerating | GameManager.Instance.IsLoadingLevel) { return; }
                    state = State.DefaultWaitState;
                    return;
            }
        }

        protected override void OnDestroy() {
            if (zOffsetHook != null) { zOffsetHook.Dispose(); zOffsetHook = null; }
            base.OnDestroy();
        }
    }
}

