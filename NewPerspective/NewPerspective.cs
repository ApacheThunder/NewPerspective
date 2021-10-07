using UnityEngine;
using System.Collections.Generic;
using System;
using MonoMod.RuntimeDetour;
using System.Reflection;
using System.Collections;
using Dungeonator;

namespace NewPerspective { 

    public class NewPerspective : ETGModule {

        public static bool Enable3D = false;
        public static bool DisableRoomOcclusion = false;
        public static bool Enable43AspectRatio = false;
        
        public static readonly string ConsoleCommandName = "perspective";
        public static readonly string PerspectiveModeEnabled = "3DModeEnabled";
        public static readonly string RoomOcclusionDisabled = "RoomOcclusionLayerDisabled";
        public static readonly string AspectRatioOverrideEnabled = "4_3AspectRatioOverrideEnabled";
        public static readonly string ModNameInGreen = "<color=#00FF00>[NewPerspective]</color> ";
        

        public static GameOptions.PreferredScalingMode previousScalingMode;
        public static float? PreviousAspectRatioOverride;
        public static float PreviousZoomScaleOverride;


        public static Hook zOffsetHook;
        public static Hook zSpriteDepthHook;
        public static Hook gameManagerHook;

        public static GameObject OcclusionMonitorObject;

        private void GameManager_Awake(Action<GameManager> orig, GameManager self) {
            orig(self);
            self.OnNewLevelFullyLoaded += OnLevelFullyLoaded;
        }
        
        public override void Init() { }
        
        public override void Start() {
            ETGModConsole.Commands.AddGroup(ConsoleCommandName, ConsoleInfo);
            ETGModConsole.Commands.GetGroup(ConsoleCommandName).AddUnit("toggle3d", Toggle3DSetting);
            ETGModConsole.Commands.GetGroup(ConsoleCommandName).AddUnit("occlusion", ToggleRoomOcclusion);
            ETGModConsole.Commands.GetGroup(ConsoleCommandName).AddUnit("aspectratio", Toggle43Aspect);
                        
            PreviousZoomScaleOverride = GameManager.Instance.MainCameraController.OverrideZoomScale;

            OcclusionMonitorObject = new GameObject("Occlusion Monitor", new Type[] { typeof(OcclusionMonitor) });
            UnityEngine.Object.DontDestroyOnLoad(OcclusionMonitorObject);

            GameManager.Instance.StartCoroutine(WaitForCharacterSelect());
        }

        private static IEnumerator WaitForCharacterSelect(bool skipInitialization = false) {
            while (Foyer.DoIntroSequence && Foyer.DoMainMenu) { yield return null; }
            while (!GameManager.Instance.PrimaryPlayer) { if (!GameManager.Instance) { break; } yield return null; }
            if (skipInitialization) {
                while (Dungeon.IsGenerating | GameManager.Instance.IsLoadingLevel) { yield return null; }
                if (DisableRoomOcclusion) { Pixelator.Instance.DoOcclusionLayer = false; }
                if (Enable43AspectRatio) { BraveCameraUtility.OverrideAspect = 1.33333333333f; }
                ToggleHooksAndPerspectiveMode(Enable3D);
            } else {
                if (BraveCameraUtility.OverrideAspect.HasValue) { PreviousAspectRatioOverride = BraveCameraUtility.OverrideAspect.Value; }
                previousScalingMode = GameManager.Options.CurrentPreferredScalingMode;
                if (PlayerPrefs.GetInt(PerspectiveModeEnabled) == 1) {
                    Enable3D = true;
                    ToggleHooksAndPerspectiveMode(Enable3D);
                    GameManager.Instance.OnNewLevelFullyLoaded += OnLevelFullyLoaded;
                }
                if (PlayerPrefs.GetInt(RoomOcclusionDisabled) == 1) { DisableRoomOcclusion = true; }
                if (PlayerPrefs.GetInt(AspectRatioOverrideEnabled) == 1) { Enable43AspectRatio = true; }
                if (DisableRoomOcclusion) { Pixelator.Instance.DoOcclusionLayer = false; }
                if (Enable43AspectRatio) { BraveCameraUtility.OverrideAspect = 1.33333333333f; }
            }
            yield break;
        }
        
        public static void OnLevelFullyLoaded() {
            if (gameManagerHook == null) {
                gameManagerHook = new Hook(
                    typeof(GameManager).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance),
                    typeof(NewPerspective).GetMethod("GameManager_Awake", BindingFlags.NonPublic | BindingFlags.Instance),
                    typeof(GameManager)
                );
            }
            GameManager.Instance.StartCoroutine(WaitForCharacterSelect(true));
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
            if (!Enable3D) {
                Enable3D = true;
                enable3D = 1;
                ETGModConsole.Log(ModNameInGreen + "Perspective mode enabled.\n");
                GameManager.Instance.OnNewLevelFullyLoaded += OnLevelFullyLoaded;
            } else {
                Enable3D = false;
                GameManager.Instance.OnNewLevelFullyLoaded -= OnLevelFullyLoaded;
                ETGModConsole.Log(ModNameInGreen + "Perspective mode disabled.\n");
            }
            ToggleHooksAndPerspectiveMode(Enable3D);
            PlayerPrefs.SetInt(PerspectiveModeEnabled, enable3D);
            PlayerPrefs.Save();
        }

        private void ToggleRoomOcclusion(string[] consoleText) {
            if (PlayerPrefs.GetInt(RoomOcclusionDisabled) == 1) {
                PlayerPrefs.SetInt(RoomOcclusionDisabled, 0);
                DisableRoomOcclusion = false;
                Pixelator.Instance.DoOcclusionLayer = true;
                if (Enable3D) { GameManager.Instance.MainCameraController.OverrideZoomScale = 0.5f; }
                ETGModConsole.Log(ModNameInGreen + "Room Occlusion enabled!");
            } else {
                PlayerPrefs.SetInt(RoomOcclusionDisabled, 1);
                DisableRoomOcclusion = true;
                Pixelator.Instance.DoOcclusionLayer = false;
                if (Enable3D) { GameManager.Instance.MainCameraController.OverrideZoomScale = 0.6f; }
                ETGModConsole.Log(ModNameInGreen + "Room Occlusion disabled!");
            }
            PlayerPrefs.Save();
        }

        private void Toggle43Aspect(string[] consoleText) {
            int aspectRatioEnabled = 1;
            if (Enable43AspectRatio) {
                aspectRatioEnabled = 0;
                Enable43AspectRatio = false;
                if (PreviousAspectRatioOverride.HasValue) {
                    BraveCameraUtility.OverrideAspect = PreviousAspectRatioOverride;
                } else {
                    BraveCameraUtility.OverrideAspect = null;
                }
                ETGModConsole.Log(ModNameInGreen + "Aspect Ratio restored to normal!");
            } else {
                Enable43AspectRatio = true;
                BraveCameraUtility.OverrideAspect = 1.33333333333f;
                ETGModConsole.Log(ModNameInGreen + "Aspect Ratio forced to 4:3!");
            }
            PlayerPrefs.SetInt(AspectRatioOverrideEnabled, aspectRatioEnabled);
            PlayerPrefs.Save();
        }


        public void UpdateZDepthInternal(Action<tk2dBaseSprite, float, float>orig, tk2dBaseSprite self, float targetZValue, float currentYValue) {
            float zValueOverride = targetZValue;
            if (GameManager.Instance.MainCameraController.IsPerspectiveMode && targetZValue < 0) {
                zValueOverride = 0;
                orig(self, zValueOverride, currentYValue);
            } else {
                orig(self, targetZValue, currentYValue);
            }
        }
        
        public float CurrentZOffsetHook(Func<CameraController, float> orig, CameraController self) {
            if (self.IsPerspectiveMode) { return self.transform.position.y - 20f; } // -40 was original value
            return orig(self);
        }
        
        public static void ToggleHooksAndPerspectiveMode(bool state) {
            if (state) {
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
                        // camera.transparencySortMode = TransparencySortMode.Perspective;
                    }
                }
                if (Pixelator.Instance.AdditionalPreBGCamera) {
                    Pixelator.Instance.AdditionalPreBGCamera.orthographic = false;
                    // Pixelator.Instance.AdditionalPreBGCamera.transparencySortMode = TransparencySortMode.Perspective;
                }
                if (Pixelator.Instance.AdditionalBGCamera) {
                    Pixelator.Instance.AdditionalBGCamera.orthographic = false;
                    // Pixelator.Instance.AdditionalBGCamera.transparencySortMode = TransparencySortMode.Perspective;
                }
                if (zOffsetHook == null) {
                    zOffsetHook = new Hook(
                        typeof(CameraController).GetProperty(nameof(GameManager.Instance.MainCameraController.CurrentZOffset)).GetGetMethod(),
                        typeof(NewPerspective).GetMethod(nameof(CurrentZOffsetHook)),
                        typeof(CameraController)
                    );
                }
                /*if (zSpriteDepthHook == null) {
                    zSpriteDepthHook = new Hook(
                        typeof(tk2dBaseSprite).GetMethod("UpdateZDepthInternal", BindingFlags.NonPublic | BindingFlags.Instance),
                        typeof(NewPerspective).GetMethod(nameof(UpdateZDepthInternal), BindingFlags.Public | BindingFlags.Instance),
                        typeof(tk2dBaseSprite)
                    );
                }*/
                GameManager.Options.CurrentPreferredScalingMode = GameOptions.PreferredScalingMode.UNIFORM_SCALING_FAST;
            } else {
                GameManager.Instance.MainCameraController.Camera.orthographic = true;
                GameManager.Instance.MainCameraController.IsPerspectiveMode = false;
                GameManager.Instance.MainCameraController.OverrideZoomScale = PreviousZoomScaleOverride;
                if (Pixelator.Instance.slavedCameras != null && Pixelator.Instance.slavedCameras.Count > 0) {
                    foreach (Camera camera in Pixelator.Instance.slavedCameras) { camera.orthographic = true; }
                }
                
                
                if (Pixelator.Instance.AdditionalPreBGCamera) { Pixelator.Instance.AdditionalPreBGCamera.orthographic = true; }
                if (Pixelator.Instance.AdditionalBGCamera) { Pixelator.Instance.AdditionalBGCamera.orthographic = true; }
                
                if (zOffsetHook != null) { zOffsetHook.Dispose(); zOffsetHook = null; }
                if (gameManagerHook != null) { gameManagerHook.Dispose(); gameManagerHook = null; }
                // if (zSpriteDepthHook != null) { zSpriteDepthHook.Dispose(); zSpriteDepthHook = null; }
                GameManager.Options.CurrentPreferredScalingMode = previousScalingMode;
            }
            Enable3D = state;
        }
        
        public override void Exit() { }
    }

    public class OcclusionMonitor : MonoBehaviour {
        public void Update() {
            if (!NewPerspective.DisableRoomOcclusion | Dungeon.IsGenerating | GameManager.Instance.IsLoadingLevel) { return; }
            if (Pixelator.Instance && Pixelator.Instance.DoOcclusionLayer) { Pixelator.Instance.DoOcclusionLayer = false; }
        }
    }
}

