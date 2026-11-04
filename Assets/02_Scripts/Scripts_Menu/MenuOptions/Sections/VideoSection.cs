using System;
using System.Collections.Generic;
using Core.Settings;
using UnityEngine;

namespace Core.UI.Options
{
    /// <summary>
    /// Vídeo: tela (modo + resolução, com confirmação), VSync, limite de FPS, qualidade e contador de FPS.
    /// Usa o seu VideoSettingsManager (Current, Resolutions, ApplyToEngine, Save, GetDefaultSettings).
    /// Modo e resolução ficam "pendentes" até apertar APLICAR TELA (aí aparece a confirmação de 15 s).
    /// As outras opções valem na hora e são salvas.
    /// </summary>
    public sealed class VideoSection : OptionsSection
    {
        private const float ConfirmSeconds = 15f;
        private const int WindowedMode = 2; // posição de FullScreenMode.Windowed em VideoSettingsManager.DisplayModes

        // Só Tela cheia (sem bordas) e Janela: a exclusiva troca o modo do monitor no driver e já travou um PC em teste.
        private static readonly int[] ModeValues = { 0, WindowedMode };

        private int _pendingMode;
        private int _pendingResolution;
        private List<string> _resolutionNames = new List<string>();

        protected override void Build()
        {
            Title = T("Vídeo", "Video");
            Subtitle = T("Imagem, tela e desempenho.", "Image, display and performance.");

            VideoSettingsManager vm = VideoSettingsManager.Instance;
            if (vm == null)
            {
                AddInfo(T("O gerenciador de vídeo só existe com o jogo rodando (Play).",
                          "The video manager only exists while the game is running (Play)."), 90f);
                return;
            }

            BuildResolutionNames(vm);
            SyncPending(vm);

            string[] modeNames =
            {
                T("Tela cheia", "Fullscreen"),
                T("Janela", "Windowed")
            };

            AddHeader(T("Tela", "Display"));

            AddChoice(T("Modo de tela", "Display mode"),
                T("Tela cheia sem bordas é a mais estável e troca de janela sem piscar.",
                  "Borderless fullscreen is the most stable and switches windows without flicker."),
                modeNames,
                () => Mathf.Max(0, Array.IndexOf(ModeValues, _pendingMode)),
                i =>
                {
                    _pendingMode = ModeValues[Mathf.Clamp(i, 0, ModeValues.Length - 1)];
                    AdaptResolution(vm);
                    RefreshRows();
                });

            AddChoice(T("Resolução", "Resolution"),
                T("Resoluções que o seu monitor aceita.", "Resolutions your monitor supports."),
                () => _resolutionNames,
                () => _pendingResolution,
                i => _pendingResolution = i);

            AddButton(T("Aplicar tela", "Apply display"),
                T("Aplica o modo e a resolução escolhidos. Se a imagem ficar ruim, ela volta sozinha.",
                  "Applies the chosen mode and resolution. If the image looks wrong, it reverts on its own."),
                () => ApplyDisplay(vm),
                () => HasPending(vm) ? T("Pendente", "Pending") : "",
                true);

            AddHeader(T("Desempenho", "Performance"));

            AddToggle("VSync",
                T("Sincroniza com o monitor e evita cortes na imagem. Com ele ligado, o limite de FPS é ignorado.",
                  "Syncs with the monitor and avoids tearing. While on, the FPS limit is ignored."),
                () => vm.Current.vSync,
                v =>
                {
                    VideoSettingsData data = vm.Current;
                    data.vSync = v;
                    Commit(vm, data);
                    RefreshRows();
                });

            string[] fpsNames = new string[VideoSettingsManager.FpsLimits.Length];
            for (int i = 0; i < fpsNames.Length; i++)
            {
                int limit = VideoSettingsManager.FpsLimits[i];
                fpsNames[i] = limit < 0 ? T("Ilimitado", "Unlimited") : limit + " FPS";
            }

            int monitorHz = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);

            ChoiceRow fpsRow = AddChoice(T("Limite de FPS", "FPS limit"),
                T("Só funciona com o VSync desligado. Monitor atual: ", "Only works with VSync off. Current monitor: ") + monitorHz + " Hz.",
                fpsNames,
                () =>
                {
                    int found = Array.IndexOf(VideoSettingsManager.FpsLimits, vm.Current.fpsLimit);
                    return found >= 0 ? found : VideoSettingsManager.FpsLimits.Length - 1;
                },
                i =>
                {
                    VideoSettingsData data = vm.Current;
                    data.fpsLimit = VideoSettingsManager.FpsLimits[i];
                    Commit(vm, data);
                });
            fpsRow.EnabledWhen = () => !vm.Current.vSync;

            AddChoice(T("Qualidade", "Quality"),
                T("Usa os níveis de qualidade do projeto (Project Settings > Quality).",
                  "Uses the project's quality levels (Project Settings > Quality)."),
                QualitySettings.names,
                () => Mathf.Clamp(vm.Current.qualityIndex, 0, QualitySettings.names.Length - 1),
                i =>
                {
                    VideoSettingsData data = vm.Current;
                    data.qualityIndex = i;
                    Commit(vm, data);
                });

            AddToggle(T("Mostrar FPS", "Show FPS"),
                T("Mostra o contador de FPS no canto da tela.", "Shows the FPS counter in the corner of the screen."),
                () => vm.Current.showFps,
                v =>
                {
                    VideoSettingsData data = vm.Current;
                    data.showFps = v;
                    Commit(vm, data);
                });

            AddButton(T("Restaurar padrão de vídeo", "Reset video to default"),
                T("Volta todas as opções de vídeo para o recomendado.", "Returns all video options to the recommended values."),
                () =>
                {
                    Commit(vm, vm.GetDefaultSettings());
                    SyncPending(vm);
                    RefreshRows();
                });
        }

        protected override void OnShown()
        {
            VideoSettingsManager vm = VideoSettingsManager.Instance;
            if (vm != null) SyncPending(vm);
        }

        // ---------- Aplicar ----------

        /// <summary>Aplica tudo na Unity, aplica a qualidade e salva.</summary>
        private static void Commit(VideoSettingsManager vm, VideoSettingsData data)
        {
            vm.ApplyToEngine(data);
            ApplyQuality(data.qualityIndex);
            vm.Save();
        }

        private static void ApplyQuality(int index)
        {
            int last = QualitySettings.names.Length - 1;
            if (last < 0) return;

            int clamped = Mathf.Clamp(index, 0, last);
            if (QualitySettings.GetQualityLevel() != clamped) QualitySettings.SetQualityLevel(clamped, true);
        }

        // ---------- Tela pendente ----------

        private void BuildResolutionNames(VideoSettingsManager vm)
        {
            _resolutionNames = new List<string>();
            foreach (Vector2Int r in vm.Resolutions) _resolutionNames.Add(r.x + " x " + r.y);
        }

        private void SyncPending(VideoSettingsManager vm)
        {
            VideoSettingsData current = vm.Current;
            // modo 1 (exclusiva, de versões antigas) aparece como Tela cheia
            _pendingMode = current.displayMode == WindowedMode ? WindowedMode : 0;
            _pendingResolution = vm.Resolutions.Count > 0
                ? vm.GetResolutionIndex(current.width, current.height)
                : 0;
        }

        private bool HasPending(VideoSettingsManager vm)
        {
            if (vm.Resolutions.Count == 0) return false;

            VideoSettingsData current = vm.Current;
            Vector2Int r = vm.Resolutions[Mathf.Clamp(_pendingResolution, 0, vm.Resolutions.Count - 1)];
            return _pendingMode != current.displayMode || r.x != current.width || r.y != current.height;
        }

        /// <summary>Ao ir pra Janela, evita a resolução do tamanho do monitor (não caberia na tela).</summary>
        private void AdaptResolution(VideoSettingsManager vm)
        {
            int last = vm.Resolutions.Count - 1;
            if (last <= 0) return;

            if (_pendingMode == WindowedMode && _pendingResolution >= last) _pendingResolution = last - 1;
        }

        private void ApplyDisplay(VideoSettingsManager vm)
        {
            if (!HasPending(vm) || vm.Resolutions.Count == 0) return;

            VideoSettingsData backup = vm.Current;

            Vector2Int target = vm.Resolutions[Mathf.Clamp(_pendingResolution, 0, vm.Resolutions.Count - 1)];
            VideoSettingsData next = backup;
            next.width = target.x;
            next.height = target.y;
            next.displayMode = _pendingMode;
            vm.ApplyToEngine(next);

            Menu.Modal.ShowConfirm(
                T("Manter estas configurações?", "Keep these settings?"),
                T("Se você não confirmar, a tela volta ao que era antes.", "If you do not confirm, the display goes back to how it was."),
                ConfirmSeconds,
                () =>
                {
                    vm.Save();
                    SyncPending(vm);
                    RefreshRows();
                },
                () =>
                {
                    vm.ApplyToEngine(backup);
                    vm.Save();
                    SyncPending(vm);
                    RefreshRows();
                });
        }
    }
}
