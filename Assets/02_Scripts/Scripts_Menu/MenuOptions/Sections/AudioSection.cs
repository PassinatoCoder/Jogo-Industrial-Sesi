using Core.Settings;
using UnityEngine;

namespace Core.UI.Options
{
    public sealed class AudioSection : OptionsSection
    {
        protected override void Build()
        {
            Title = T("Áudio", "Audio");
            Subtitle = T("Volume geral, música e efeitos.", "Master volume, music and effects.");

            if (Menu.Mixer == null)
            {
                AddInfo(T("Arraste o AudioMixer no campo 'Mixer' do OptionsMenu pra os volumes funcionarem. Os valores já são salvos.",
                          "Drag the AudioMixer into the 'Mixer' field of the OptionsMenu so the volumes work. Values are already saved."), 96f);
            }

            AddHeader(T("Volume", "Volume"));

            AddSlider(T("Volume geral", "Master volume"),
                T("Controla tudo ao mesmo tempo.", "Controls everything at once."),
                () => GameAudioSettings.Get(AudioChannel.Master),
                v => GameAudioSettings.Set(Menu.Mixer, AudioChannel.Master, v));

            AddSlider(T("Música", "Music"),
                T("Volume da música de fundo.", "Background music volume."),
                () => GameAudioSettings.Get(AudioChannel.Music),
                v => GameAudioSettings.Set(Menu.Mixer, AudioChannel.Music, v));

            AddSlider(T("Efeitos sonoros", "Sound effects"),
                T("Volume dos sons de botões, máquinas e ações.", "Volume of buttons, machines and actions."),
                () => GameAudioSettings.Get(AudioChannel.Effects),
                v => GameAudioSettings.Set(Menu.Mixer, AudioChannel.Effects, v));

            AddButton(T("Restaurar padrão de áudio", "Reset audio to default"),
                T("Volta os três volumes para 100%.", "Sets all three volumes back to 100%."),
                () =>
                {
                    GameAudioSettings.ResetAll(Menu.Mixer);
                    RefreshRows();
                });
        }
    }
}