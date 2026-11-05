using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core.Settings;

namespace Core.Minigames.Welding
{
    /// <summary>
    /// Eventos aleatórios DURANTE a solda (tela treme pra passar urgência):
    ///  - Tocha engasga: aparece um "R" piscando, aperte R várias vezes.
    ///  - Luz de foco pisca e apaga: clique no interruptor (ou aperte L) pra religar.
    ///  - Colega chega perto do arco: clique nele pra AVISAR antes de soldar, senão ele se queima.
    ///  - Sujeira na mesa: cresce a cada solda; clique pra pegar e jogar fora (se acumular, atrapalha a solda).
    /// Pra criar um evento novo: copie um bloco (Start/Update) e adicione na lista de sorteio em Pick().
    /// </summary>
    public sealed class WeldEvents
    {
        private sealed class Trash
        {
            public Image Image;
            public WeldingInput.Hit Hit;
        }

        private readonly WeldingMinigame _c;
        private readonly RectTransform _layer;
        private readonly Image _dim;
        private readonly List<Trash> _trash = new List<Trash>();

        private float _next;
        private int _beads;

        // tocha engasgada
        private bool _jam;
        private int _jamCount;
        private GameObject _jamUi;
        private TextMeshProUGUI _jamKey;
        private TextMeshProUGUI _jamCounter;
        private float _jamBlink;

        // luz
        private float _flicker;
        private bool _lightsOut;
        private Image _switch;
        private WeldingInput.Hit _switchHit;

        // colega
        private Image _npc;
        private WeldingInput.Hit _npcHit;
        private float _npcT;
        private bool _npcWarned;
        private bool _npcLeaving;

        public WeldEvents(WeldingMinigame c, RectTransform layer)
        {
            _c = c;
            _layer = layer;

            _dim = WeldUI.MakeImage(layer, "LightDim", null, new Color(0.02f, 0.02f, 0.05f, 0f));
            WeldUI.Fill(_dim.rectTransform);
            _dim.transform.SetAsFirstSibling();

            ScheduleNext(4f);
        }

        public bool BlocksArc { get { return _jam; } }
        public bool LightsOut { get { return _lightsOut; } }
        public int TrashCount { get { return _trash.Count; } }

        /// <summary>Multiplicador da tolerância da solda (sujeira e escuro atrapalham).</summary>
        public float ToleranceMultiplier
        {
            get
            {
                float m = 1f;
                if (_trash.Count >= 3) m *= 0.8f;
                if (_trash.Count >= 5) m *= 0.85f;
                return m;
            }
        }

        private void ScheduleNext(float extra = 0f)
        {
            _next = Random.Range(_c.Settings.eventIntervalMin, _c.Settings.eventIntervalMax) + extra;
        }

        /// <summary>Chame a cada pedacinho de solda feito: a mesa vai ficando suja.</summary>
        public void OnBead()
        {
            _beads++;
            if (_beads % 9 == 0 && Random.value <= Mathf.Clamp01(_c.Settings.randomEventChance))
                SpawnTrash();
        }

        public void Tick(float dt, bool arcOn)
        {
            _next -= dt;
            if (_next <= 0f && !_jam && !_lightsOut && _npc == null)
            {
                // A chance below 1 creates breathing room; hard mode can push it to certainty.
                if (Random.value <= Mathf.Clamp01(_c.Settings.randomEventChance))
                    Pick(arcOn);
                ScheduleNext();
            }

            UpdateJam(dt);
            UpdateLight(dt);
            UpdateNpc(dt, arcOn);
        }

        private void Pick(bool arcOn)
        {
            float roll = Random.value;

            if (roll < 0.30f && arcOn) StartJam();
            else if (roll < 0.55f) StartLights();
            else if (roll < 0.80f) StartNpc();
            else
            {
                SpawnTrash();
                SpawnTrash();
                _c.Hud.Toast(WeldingMinigame_T("A mesa está suja de novo!", "The table is dirty again!"), WeldingMinigame.Accent);
            }
        }

        private static string WeldingMinigame_T(string pt, string en)
        {
            return GameLanguage.T(pt, en);
        }

        // ---------- Tocha engasgada ----------

        private void StartJam()
        {
            _jam = true;
            _jamCount = 0;
            _c.Audio.Play(_c.Sounds.torchJam);
            _c.Audio.Play(_c.Sounds.alert);
            _c.Shake(16f, 0.5f);
            _c.Hud.Toast(WeldingMinigame_T("A TOCHA ENGASGOU!", "THE TORCH JAMMED!"), WeldingMinigame.Bad, 3f);

            GameObject root = new GameObject("Jam", typeof(RectTransform));
            RectTransform rt = (RectTransform)root.transform;
            rt.SetParent(_layer, false);
            WeldUI.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(380f, 380f));

            Image bg = WeldUI.MakeImage(rt, "Bg", WeldUI.Circle, new Color(0.7f, 0.08f, 0.05f, 0.9f));
            WeldUI.Fill(bg.rectTransform);

            _jamKey = WeldUI.MakeText(rt, "Key", "R", 190f, Color.white, TextAlignmentOptions.Center);
            WeldUI.Fill(_jamKey.rectTransform, 0f, 40f, 0f, 0f);

            _jamCounter = WeldUI.MakeText(rt, "Count", "0/" + _c.Settings.jamMashGoal, 40f, Color.white,
                TextAlignmentOptions.Bottom);
            WeldUI.Fill(_jamCounter.rectTransform, 0f, 40f, 0f, 0f);

            _jamUi = root;
            _c.Hud.SetHint(WeldingMinigame_T("APERTE R VÁRIAS VEZES pra destravar a tocha!", "MASH R to unjam the torch!"));
        }

        private void UpdateJam(float dt)
        {
            if (!_jam) return;

            _jamBlink += dt * 14f;
            float minAlpha = AccessibilitySettings.ReduceFlashes ? 0.75f : 0.25f;
            float a = Mathf.Lerp(minAlpha, 1f, 0.5f + 0.5f * Mathf.Sin(_jamBlink));
            _jamKey.color = new Color(1f, 1f, 1f, a);

            _c.SetStrain(0.7f);

            if (Input.GetKeyDown(KeyCode.R))
            {
                _jamCount++;
                _c.Audio.Play(_c.Sounds.screenStrain);
                _c.Shake(10f, 0.12f);
                _jamCounter.text = _jamCount + "/" + _c.Settings.jamMashGoal;
            }

            if (_jamCount >= _c.Settings.jamMashGoal)
            {
                _jam = false;
                _c.SetStrain(0f);
                Object.Destroy(_jamUi);
                _c.Audio.Play(_c.Sounds.stepDone);
                _c.Hud.Toast(WeldingMinigame_T("Tocha liberada!", "Torch cleared!"), WeldingMinigame.Good);
                _c.Hud.SetHint("");
            }
        }

        // ---------- Luz de foco ----------

        private void StartLights()
        {
            _flicker = 2.2f;
            _c.Audio.Play(_c.Sounds.lightFlicker);
            _c.Shake(6f, 0.4f);
        }

        private void UpdateLight(float dt)
        {
            if (_flicker > 0f)
            {
                _flicker -= dt;
                float amplitude = AccessibilitySettings.ReduceFlashes ? 0.3f : 0.75f;
                bool on = Random.value < 0.5f;
                _dim.color = new Color(0.02f, 0.02f, 0.05f, on ? 0f : Random.Range(0.2f, amplitude));
                _c.LampLevel = on ? 1f : 0.15f;

                if (_flicker <= 0f) LightsGoOut();
            }
            else if (_lightsOut)
            {
                _c.LampLevel = 0f;
                float a = 0.78f + Mathf.Sin(Time.unscaledTime * 3f) * 0.03f;
                _dim.color = new Color(0.02f, 0.02f, 0.05f, a);

                if (Input.GetKeyDown(KeyCode.L)) LightsOn();
            }
        }

        private void LightsGoOut()
        {
            _lightsOut = true;
            _c.Audio.Play(_c.Sounds.lightsOut);
            _c.Shake(14f, 0.5f);
            _c.Hud.Toast(WeldingMinigame_T("A LUZ APAGOU! Religue o foco (clique no interruptor ou tecla L).",
                "THE LIGHT WENT OUT! Switch it back on (click the switch or press L)."), WeldingMinigame.Bad, 4f);

            _switch = WeldUI.MakeProp(_layer, "Switch", null, new Vector2(760f, 250f), new Vector2(190f, 120f),
                new Color(0.9f, 0.75f, 0.1f, 1f), GameLanguage.T("LIGAR\nLUZ (L)", "LIGHT\nON (L)"), 30f);
            _switchHit = _c.Clicks.Register(_switch.rectTransform, LightsOn, null, 80);
        }

        private void LightsOn()
        {
            if (!_lightsOut) return;

            _lightsOut = false;
            _dim.color = new Color(0.02f, 0.02f, 0.05f, 0f);
            _c.LampLevel = 1f;
            _c.Audio.Play(_c.Sounds.lightsOn);
            _c.Hud.Toast(WeldingMinigame_T("Luz religada.", "Light back on."), WeldingMinigame.Good);

            _c.Clicks.Unregister(_switchHit);
            if (_switch != null) Object.Destroy(_switch.gameObject);
        }

        // ---------- Colega em perigo ----------

        private void StartNpc()
        {
            _npcT = 0f;
            _npcWarned = false;
            _npcLeaving = false;

            _npc = WeldUI.MakeProp(_layer, "Colleague", _c.Art.npcIntruder, new Vector2(1000f, -140f), new Vector2(300f, 540f),
                new Color(0.3f, 0.5f, 0.75f, 1f), GameLanguage.T("COLEGA\nSEM EPI", "COWORKER\nNO PPE"), 34f);
            _npcHit = _c.Clicks.Register(_npc.rectTransform, WarnNpc, null, 70);

            _c.Audio.Play(_c.Sounds.intruderVoice);
            _c.Shake(5f, 0.3f);
            _c.Hud.Toast(WeldingMinigame_T("Um colega está chegando perto! CLIQUE NELE pra avisar.",
                "A coworker is walking in! CLICK him to warn him."), WeldingMinigame.Accent, 4f);
        }

        private void WarnNpc()
        {
            if (_npc == null || _npcWarned) return;

            _npcWarned = true;
            _npcLeaving = true;
            _c.Audio.Play(_c.Sounds.stepDone);
            _c.Hud.Toast(WeldingMinigame_T("Você avisou o colega. Ele saiu da área.", "You warned him. He left the area."),
                WeldingMinigame.Good);
        }

        private bool NpcNearArc
        {
            get { return _npc != null && !_npcWarned && _npcT >= _c.Settings.npcArriveSeconds * 0.55f; }
        }

        private void UpdateNpc(float dt, bool arcOn)
        {
            if (_npc == null) return;

            RectTransform rt = _npc.rectTransform;
            Vector2 pos = rt.anchoredPosition;

            if (_npcLeaving)
            {
                pos.x += 700f * dt;
                rt.anchoredPosition = pos;
                if (pos.x > 1200f) DestroyNpc();
                return;
            }

            _npcT += dt;
            float k = Mathf.Clamp01(_npcT / _c.Settings.npcArriveSeconds);
            pos.x = Mathf.Lerp(1000f, 520f, k);
            pos.y = -140f + Mathf.Sin(_npcT * 7f) * 6f;
            rt.anchoredPosition = pos;

            if (NpcNearArc && arcOn)
            {
                _c.Penalize(_c.Settings.penaltyBurnNpc,
                    "Soldou com um colega perto sem avisar: ele se queimou.",
                    "Welded with a coworker nearby without warning: he got burned.");
                _c.Audio.Play(_c.Sounds.burnOuch);
                _c.Shake(20f, 0.6f);
                _c.Flash(new Color(1f, 0.6f, 0.2f), 0.5f, 0.5f);
                _npcWarned = true;
                _npcLeaving = true;
            }
            else if (k >= 1f && !_npcWarned)
            {
                // Chegou e ficou parado ao lado: continua em perigo até ser avisado
                _npcT = _c.Settings.npcArriveSeconds;
            }
        }

        private void DestroyNpc()
        {
            _c.Clicks.Unregister(_npcHit);
            if (_npc != null) Object.Destroy(_npc.gameObject);
            _npc = null;
        }

        // ---------- Sujeira da mesa ----------

        private void SpawnTrash()
        {
            if (_trash.Count >= 8) return;

            int type = Random.Range(0, 3);
            Sprite sprite = type == 0 ? _c.Art.rustyScrap : (type == 1 ? _c.Art.sprayCan : _c.Art.oilPuddle);
            string label = type == 0 ? GameLanguage.T("SUCATA", "SCRAP") : (type == 1 ? GameLanguage.T("SPRAY", "SPRAY") : GameLanguage.T("ÓLEO", "OIL"));
            Color color = type == 0 ? new Color(0.55f, 0.3f, 0.15f, 1f) : (type == 1 ? new Color(0.8f, 0.2f, 0.15f, 1f) : new Color(0.08f, 0.08f, 0.1f, 1f));

            float side = Random.value < 0.5f ? -1f : 1f;
            Vector2 pos = new Vector2(side * Random.Range(560f, 880f), Random.Range(-340f, -200f));

            Trash trash = new Trash();
            trash.Image = WeldUI.MakeProp(_layer, "Trash", sprite, pos, new Vector2(170f, 130f), color, label, 24f);
            trash.Image.transform.SetAsLastSibling();
            WeldUI.AddShadow(trash.Image, 0.35f);
            _dim.transform.SetAsFirstSibling();
            Trash captured = trash;
            trash.Hit = _c.Clicks.Register(trash.Image.rectTransform, () => Remove(captured), null, 20);
            _trash.Add(trash);
        }

        private void Remove(Trash trash)
        {
            _c.Audio.Play(_c.Sounds.pickup);
            _c.Audio.Play(_c.Sounds.trashThrow);
            if (trash.Image != null)
                _c.AnimateHandAction(false, trash.Image.rectTransform.anchoredPosition);
            _c.Clicks.Unregister(trash.Hit);
            if (trash.Image != null) Object.Destroy(trash.Image.gameObject);
            _trash.Remove(trash);
        }
    }
}
