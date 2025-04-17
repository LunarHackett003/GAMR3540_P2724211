Shader "InvisibleShadowCaster"
{
    SubShader
    {
        Pass
        {
            Tags { "LightMode" = "ShadowCaster" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f
            {
                V2F_SHADOW_CASTER;
            };

            v2f vert(appdata_base v)
            {
                v2f o;               

                // This only works as long as the Normal Bias setting for the light source is above 0
                #ifdef SHADOWS_DEPTH
                if (unity_LightShadowBias.z == 0.0)
                {
                    // camera
                    o = (v2f)0;
                    return o;
                }
                #endif

                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack Off
}