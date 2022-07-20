using System.Collections.Generic;
using System;
using MonoMod.RuntimeDetour;
using UnityEngine;
using Dungeonator;
using BepInEx;

namespace NewPerspective {

    [BepInDependency("etgmodding.etg.mtgapi")]
    [BepInPlugin(GUID, ModName, VERSION)]
    public class NewPerspective : BaseUnityPlugin {

        public const string GUID = "ApacheThunder.etg.NewPerspective";
        public const string ModName = "NewPerspective";
        public const string VERSION = "1.3.0";

        public static readonly string ConsoleCommandName = "perspective";
        public static readonly string PerspectiveModeEnabled = "3DModeEnabled";
        public static readonly string RoomOcclusionDisabled = "RoomOcclusionLayerDisabled";
        public static readonly string AspectRatioOverrideEnabled = "4_3AspectRatioOverrideEnabled";
        public static readonly string ModNameInGreen = "<color=#00FF00>[NewPerspective]</color> ";
                
        public static GameObject OcclusionMonitorObject;
        public static OcclusionMonitor occlusionMonitor;
        
        public void Start() { ETGModMainBehaviour.WaitForGameManagerStart(GMStart); }

        public void GMStart(GameManager gameManager) {
            ETGModConsole.Commands.AddGroup(ConsoleCommandName, ConsoleInfo);
            ETGModConsole.Commands.GetGroup(ConsoleCommandName).AddUnit("toggle3d", Toggle3DSetting);
            ETGModConsole.Commands.GetGroup(ConsoleCommandName).AddUnit("occlusion", ToggleRoomOcclusion);
            ETGModConsole.Commands.GetGroup(ConsoleCommandName).AddUnit("aspectratio", Toggle43Aspect);            
            CreateMonitor();
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
            if (!OcclusionMonitorObject | !occlusionMonitor) { CreateMonitor(false); }

            if (!occlusionMonitor.Configured) {
                ETGModConsole.Log("[New Perspective] Please wait until you have selected a character before trying to configure settings.");
                occlusionMonitor.Enabled = true;
                return;
            }

            occlusionMonitor.Enabled = false;
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
            
            occlusionMonitor.Enabled = true;
        }

        private void ToggleRoomOcclusion(string[] consoleText) {
            if (!OcclusionMonitorObject | !occlusionMonitor) { CreateMonitor(false); }

            if (!occlusionMonitor.Configured) {
                ETGModConsole.Log("[New Perspective] Please wait until you have selected a character before trying to configure settings.");
                occlusionMonitor.Enabled = true;
                return;
            }

            occlusionMonitor.Enabled = false;
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
            occlusionMonitor.Enabled = true;
        }

        private void Toggle43Aspect(string[] consoleText) {
            if (!OcclusionMonitorObject | !occlusionMonitor) { CreateMonitor(false); }

            if (!occlusionMonitor.Configured) {
                ETGModConsole.Log("[New Perspective] Please wait until you have selected a character before trying to configure settings.");
                occlusionMonitor.Enabled = true;
                return;
            }

            int aspectRatioEnabled = 1;

            occlusionMonitor.Enabled = false;
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
            occlusionMonitor.Enabled = true;
        }

        public void CreateMonitor(bool CreateEnabled = true) {
            OcclusionMonitorObject = new GameObject("NewPerspective Monitor", new Type[] { typeof(OcclusionMonitor) });
            occlusionMonitor = OcclusionMonitorObject.GetComponent<OcclusionMonitor>();
            occlusionMonitor.Enabled = CreateEnabled;
        }
        
    }

    public class OcclusionMonitor : BraveBehaviour {

        public OcclusionMonitor() {
            Configured = false;
            Enable3D = false;
            DisableRoomOcclusion = false;
            Enable43AspectRatio = false;            
            Enabled = false;
            state = State.WaitForFoyerLoad;

            m_3DModeSet = false;
            m_ReturnedToFoyer = false;            
            m_AspectRatio = 1.33333333333f;
            m_AspectRatioZoomFactor = 0.675f;
                        
            DontDestroyOnLoad(gameObject);
        }

        public Hook zOffsetHook;

        public float CurrentZOffsetHook(Func<CameraController, float> orig, CameraController self) {
            if (self.IsPerspectiveMode) { return self.transform.position.y - 20f; } // -40 was original value
            return orig(self);
        }
        
        public bool Enabled;
        public bool Configured;
        public bool Enable3D;
        public bool DisableRoomOcclusion;
        public bool Enable43AspectRatio;
        
        public float PreviousZoomScaleOverride;
        
        public enum State { WaitForFoyerLoad, WaitForLevelLoad, ChangingSettings, DefaultWaitState };

        public State state;


        private GameOptions.PreferredScalingMode m_previousScalingMode;
        private bool m_3DModeSet;
        private bool m_ReturnedToFoyer;
        private float m_AspectRatio;
        private float m_AspectRatioZoomFactor;
        private float m_PreviousAspectRatioZoomFactor;
        private float? m_PreviousAspectRatioOverride;


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
            m_3DModeSet = mode;
        }
        
        public void Update() {
            if (!Enabled) { return; }
            switch (state) {
                case State.WaitForFoyerLoad:
                    if (!GameManager.Instance | !GameManager.Instance.PrimaryPlayer) { return; }
                    if (!Configured) {
                        state = State.ChangingSettings;
                        return;
                    }
                    state = State.WaitForLevelLoad;
                    return;
                case State.WaitForLevelLoad:
                    if (!GameManager.Instance | !GameManager.Instance.PrimaryPlayer | GameManager.Instance.IsLoadingLevel | !GameManager.Instance.Dungeon | Dungeon.IsGenerating) {
                        return;
                    }
                    state = State.DefaultWaitState;
                    return;
                case State.ChangingSettings:
                    if (!GameManager.Instance | !GameManager.Instance.MainCameraController | !Pixelator.Instance) { return; }
                    if (!Configured) {
                        PreviousZoomScaleOverride = GameManager.Instance.MainCameraController.OverrideZoomScale;
                        if (PlayerPrefs.GetInt(NewPerspective.RoomOcclusionDisabled) == 1) { DisableRoomOcclusion = true; }
                        if (PlayerPrefs.GetInt(NewPerspective.AspectRatioOverrideEnabled) == 1) { Enable43AspectRatio = true; }
                        if (PlayerPrefs.GetInt(NewPerspective.PerspectiveModeEnabled) == 1) { Enable3D = true; }
                        if (BraveCameraUtility.OverrideAspect.HasValue) { m_PreviousAspectRatioOverride = BraveCameraUtility.OverrideAspect.Value; }
                        m_PreviousAspectRatioZoomFactor = GameManager.Instance.MainCameraController.OverrideZoomScale;
                        m_previousScalingMode = GameManager.Options.CurrentPreferredScalingMode;
                    }
                    if (Enable3D && !m_3DModeSet) {
                        ToggleHooksAndPerspectiveMode(true);
                    } else if (!Enable3D && m_3DModeSet) {
                        ToggleHooksAndPerspectiveMode(false);
                    }
                    if (DisableRoomOcclusion | Enable3D) {
                        Pixelator.Instance.DoOcclusionLayer = false;
                    } else {
                        Pixelator.Instance.DoOcclusionLayer = true;
                    }
                    if (Enable43AspectRatio && (!BraveCameraUtility.OverrideAspect.HasValue | BraveCameraUtility.OverrideAspect != m_AspectRatio)) {
                        if (!Enable3D) { GameManager.Instance.MainCameraController.OverrideZoomScale = m_AspectRatioZoomFactor; }
                        BraveCameraUtility.OverrideAspect = m_AspectRatio;
                    } else {
                        if (!Enable3D) { GameManager.Instance.MainCameraController.OverrideZoomScale = m_PreviousAspectRatioZoomFactor; }
                        BraveCameraUtility.OverrideAspect = m_PreviousAspectRatioOverride;
                    }
                    if (!Configured) {
                        Configured = true;
                        state = State.WaitForFoyerLoad;
                    }
                    state = State.WaitForLevelLoad;
                    return;
                case State.DefaultWaitState:
                    if (!GameManager.Instance | !GameManager.Instance.Dungeon | !GameManager.Instance.MainCameraController | !Pixelator.Instance) {
                        m_3DModeSet = false;
                        return;
                    }
                    if (DisableRoomOcclusion | Enable3D) {
                        if (Pixelator.Instance.DoOcclusionLayer) { Pixelator.Instance.DoOcclusionLayer = false; }
                        if (GameManager.Instance.Dungeon) {
                            foreach (RoomHandler room in GameManager.Instance.Dungeon.data.rooms) { room.SetRoomActive(true); }
                        }
                        if (GameManager.Instance.PrimaryPlayer && m_ReturnedToFoyer) {
                            m_ReturnedToFoyer = false;
                        } else if (!m_ReturnedToFoyer && !GameManager.Instance.PrimaryPlayer) {
                            m_ReturnedToFoyer = true;
                            ToggleHooksAndPerspectiveMode(false);
                        }
                        if (Enable3D && !m_3DModeSet && !m_ReturnedToFoyer) { ToggleHooksAndPerspectiveMode(true); }
                    }
                    if (Enable43AspectRatio) {
                        if (!Enable3D) {
                            if (GameManager.Instance.PrimaryPlayer && GameManager.Instance.MainCameraController.OverrideZoomScale != m_AspectRatioZoomFactor) {
                                GameManager.Instance.MainCameraController.OverrideZoomScale = m_AspectRatioZoomFactor;
                            } else if (!GameManager.Instance.PrimaryPlayer && GameManager.Instance.MainCameraController.OverrideZoomScale == m_AspectRatioZoomFactor) {
                                GameManager.Instance.MainCameraController.OverrideZoomScale = m_PreviousAspectRatioZoomFactor;
                            }
                        }
                        if (GameManager.Instance.PrimaryPlayer && (!BraveCameraUtility.OverrideAspect.HasValue | BraveCameraUtility.OverrideAspect != m_AspectRatio)) {
                            BraveCameraUtility.OverrideAspect = m_AspectRatio;
                        } else if (!GameManager.Instance.PrimaryPlayer && BraveCameraUtility.OverrideAspect.HasValue && BraveCameraUtility.OverrideAspect == m_AspectRatio) {
                            BraveCameraUtility.OverrideAspect = m_PreviousAspectRatioOverride;
                        }
                    }
                    return;
            }
        }

        protected override void OnDestroy() {
            if (zOffsetHook != null) { zOffsetHook.Dispose(); zOffsetHook = null; }
            base.OnDestroy();
        }
    }
}

