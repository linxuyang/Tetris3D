#ifndef UNITY_UI_INCLUDED
#define UNITY_UI_INCLUDED

inline float UnityGet2DClipping (in float2 position, in float4 clipRect)
{
    float2 inside = step(clipRect.xy, position.xy) * step(position.xy, clipRect.zw);
    return inside.x * inside.y;
}

inline half4 UnityGetUIDiffuseColor(in float2 position, in sampler2D mainTexture, in sampler2D alphaTexture, half4 textureSampleAdd)
{
    return half4(tex2D(mainTexture, position).rgb + textureSampleAdd.rgb, tex2D(alphaTexture, position).r + textureSampleAdd.a);
}
#endif
