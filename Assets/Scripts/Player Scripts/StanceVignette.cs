using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

public class StanceVignette : MonoBehaviour
{
    [SerializeField] private float min = 0.1f;
    [SerializeField] private float max = 0.35f;
    [SerializeField] private float response = 10f;

    private PostProcessProfile vProfile; //VolumeProfile
    private Vignette vignette;

    public void Initialize(PostProcessProfile profile)
    {
        vProfile = profile;

        if(!profile.TryGetSettings(out vignette)) //Video uses try get
        {
            vignette = profile.AddSettings<Vignette>(); //Video uses add
        }

        vignette.intensity.Override(min);
    }

    public void UpdateVignette(float deltatime, Stance stance)
    {
        var targetIntensity = stance is Stance.Stand ? min : max;
        vignette.intensity.value = Mathf.Lerp(a: vignette.intensity.value, b: targetIntensity, t: 1f - Mathf.Exp(-response * deltatime)); //first value is valid in video
    }
}
