using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenu("Post-processing/PostWarFog")]
public sealed class PostWarFog : VolumeComponent, IPostProcessComponent
{
    [HideInInspector]
    public TextureParameter warFogTexture = new TextureParameter(null, true);

    [HideInInspector]
    public Vector4Parameter corners = new Vector4Parameter(Vector4.zero, true);
    
    [HideInInspector]
    public FloatParameter landHeightWS = new FloatParameter(0, true);

    public ColorParameter warFogColor = new ColorParameter(Color.black,false, true, false);

    public ClampedFloatParameter fogSmoothStart = new ClampedFloatParameter(0, 0, 1);

    public ClampedFloatParameter fogSmoothEnd = new ClampedFloatParameter(1, 0, 1);

    public ColorParameter cloudColor = new ColorParameter(Color.white, false, true, false);
    
    public ColorParameter cloudColor2 = new ColorParameter(Color.white, false, false, false);

    public ClampedFloatParameter cloudScale = new ClampedFloatParameter(1f, 0.01f, 0.1f);

    public ClampedFloatParameter cloudIntensity = new ClampedFloatParameter(0.5f, 0f, 10f);
    
    [HideInInspector]
    public ClampedFloatParameter shapeSize = new ClampedFloatParameter(2f, 1f, 2f);
    
    [HideInInspector]
    public ClampedFloatParameter fractal = new ClampedFloatParameter(2f, 1f, 2f);

    public Vector2Parameter cloudMove = new Vector2Parameter(Vector2.zero);

    [HideInInspector]
    public ClampedFloatParameter transformSpeedX = new ClampedFloatParameter(1f, 0f, 5f);
    
    [HideInInspector]
    public ClampedFloatParameter transformSpeedY = new ClampedFloatParameter(1f, 0f, 5f);

    public TextureParameter noiseTex = new TextureParameter(null);

    [HideInInspector]
    public Vector2Parameter noiseTexScale = new Vector2Parameter(new Vector2(1, 1));

    [HideInInspector]
    public ClampedFloatParameter noiseIntensity = new ClampedFloatParameter(0f, 0f, 1f);
    
    [HideInInspector]
    public Vector2Parameter noiseUVSpeed = new Vector2Parameter(new Vector2(0, 0));

    public bool IsActive()
    {
        return warFogTexture.value != null;
    }

    public bool IsTileCompatible()
    {
        return false;
    }
}
