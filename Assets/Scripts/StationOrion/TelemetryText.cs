using TMPro;
using UnityEngine;

namespace StationOrion
{
    /// <summary>
    /// Fills a console screen with live-looking station data (numbers drift slowly).
    /// Pure set dressing: it makes the station feel alive and "working".
    /// </summary>
    public class TelemetryText : MonoBehaviour
    {
        public enum Mode { Navigation, LifeSupport, Engine, Reactor }

        public TextMeshPro text;
        public Mode mode = Mode.Navigation;
        public float interval = 0.7f;

        float next;
        float a, b, c, d;

        void Start()
        {
            if (text == null) text = GetComponent<TextMeshPro>();
            a = Random.value; b = Random.value; c = Random.value; d = Random.value;
        }

        void Update()
        {
            if (text == null || Time.time < next) return;
            next = Time.time + interval;
            a = Mathf.Clamp01(a + Random.Range(-0.05f, 0.05f));
            b = Mathf.Clamp01(b + Random.Range(-0.05f, 0.05f));
            c = Mathf.Clamp01(c + Random.Range(-0.05f, 0.05f));
            d = Mathf.Clamp01(d + Random.Range(-0.05f, 0.05f));
            text.text = Compose();
        }

        string Compose()
        {
            switch (mode)
            {
                case Mode.Navigation:
                    return "<b>NAVIGATION</b>\n" +
                           "ALT  " + (407.5f + a * 1.5f).ToString("0.00") + " km\n" +
                           "VEL  " + (7.655f + b * 0.01f).ToString("0.000") + " km/s\n" +
                           "INC  51.64°\n" +
                           "<color=#FF6655>ATT  DRIFT " + (c * 2f).ToString("0.0") + "°</color>";
                case Mode.LifeSupport:
                    return "<b>LIFE SUPPORT</b>\n" +
                           "O2   " + (20.2f + a).ToString("0.0") + " %\n" +
                           "CO2  " + (0.30f + b * 0.2f).ToString("0.00") + " %\n" +
                           "TEMP " + (21f + c * 2f).ToString("0.0") + " °C\n" +
                           "PRES " + (100.9f + d).ToString("0.0") + " kPa";
                case Mode.Engine:
                    return "<b>ENGINEERING</b>\n" +
                           "GEN  " + (3400 + (int)(a * 300)) + " rpm\n" +
                           "BUS  " + (118f + b * 4f).ToString("0.0") + " V\n" +
                           "<color=#FF6655>COOLANT FLOW " + (c * 12f).ToString("0") + " %</color>\n" +
                           "LOAD " + (60 + (int)(d * 30)) + " %";
                default:
                    return "<b>REACTOR</b>\n" +
                           "<color=#FF6655>CORE " + (2400 + (int)(a * 300)) + " K</color>\n" +
                           "FLUX " + (88f + b * 10f).ToString("0.0") + " %\n" +
                           "CONTAINMENT " + (40 + (int)(c * 20)) + " %";
            }
        }
    }
}
