using System.Xml;
using MultiCraft.Scripts.Engine.Network;
using UnityEngine;

public class DayCycleManager : MonoBehaviour
{
    public static DayCycleManager Instance;
    [Range(0, 1)] public float TimeOfDay;
    public float DayDuration = 30f;

    public AnimationCurve SunCurve;
    public AnimationCurve MoonCurve;
    public AnimationCurve SkyboxCurve;

    public Material DaySkybox;
    public Material NightSkybox;

    public ParticleSystem Stars;

    public Light Sun;
    public Light Moon;

    private float sunIntensity;
    private float moonIntensity;

    public bool DayNight;
    private const string DayNightKey = "DayAndNightToggleState";

    private void Start()
    {
        Instance = this;
        sunIntensity = Sun.intensity;
        moonIntensity = Moon.intensity;
        LoadShadowToggleState();
    }

    private void LoadShadowToggleState()
    {
        DayNight = PlayerPrefs.GetInt(DayNightKey, 0) == 1;
    }

    private void Update()
    {
        if(!NetworkManager.Instance)
            if (DayNight)
                TimeOfDay += Time.deltaTime / DayDuration;
        if (TimeOfDay >= 1) TimeOfDay -= 1;

        // Настройки освещения (skybox и основное солнце)
        RenderSettings.skybox.Lerp(NightSkybox, DaySkybox, SkyboxCurve.Evaluate(TimeOfDay));
        RenderSettings.sun = SkyboxCurve.Evaluate(TimeOfDay) > 0.1f ? Sun : Moon;
        DynamicGI.UpdateEnvironment();

        // Прозрачность звёзд
        var mainModule = Stars.main;
        //mainModule.startColor = new Color(1, 1, 1, 1 - SkyboxCurve.Evaluate(TimeOfDay));

        // Поворот луны и солнца
        Sun.transform.localRotation = Quaternion.Euler(TimeOfDay * 360f, 180, 0);
        Moon.transform.localRotation = Quaternion.Euler(TimeOfDay * 360f + 180f, 180, 0);

        // Интенсивность свечения луны и солнца
        Sun.intensity = sunIntensity * SunCurve.Evaluate(TimeOfDay);
        Moon.intensity = moonIntensity * MoonCurve.Evaluate(TimeOfDay);
    }
}