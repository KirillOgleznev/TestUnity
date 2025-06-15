Shader "Custom/GroundFog"
{
    Properties
    {
        _MainTex ("Noise Texture", 2D) = "white" {}
        _SecondTex ("Second Noise", 2D) = "white" {}
        _FogColor ("Fog Color", Color) = (1,1,1,0.5)
        _Density ("Density", Range(0, 2)) = 0.5
        _Speed1 ("First Layer Speed", Vector) = (0.1, 0, 0.05, 0)
        _Speed2 ("Second Layer Speed", Vector) = (-0.05, 0, 0.1, 0)
        _HeightFade ("Height Fade", Range(0, 10)) = 2
        _DistanceFade ("Distance Fade", Range(0, 100)) = 50
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 200
        
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float height : TEXCOORD3;
            };
            
            sampler2D _MainTex;
            sampler2D _SecondTex;
            float4 _MainTex_ST;
            float4 _SecondTex_ST;
            fixed4 _FogColor;
            float _Density;
            float4 _Speed1;
            float4 _Speed2;
            float _HeightFade;
            float _DistanceFade;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                o.height = v.vertex.y;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Анимированные UV координаты для двух слоев
                float2 uv1 = i.uv + _Speed1.xz * _Time.y;
                float2 uv2 = i.uv + _Speed2.xz * _Time.y;
                
                // Сэмплируем два слоя шума
                fixed4 noise1 = tex2D(_MainTex, uv1);
                fixed4 noise2 = tex2D(_SecondTex, uv2);
                
                // Комбинируем шумы для более интересного эффекта
                float combinedNoise = (noise1.r * noise2.r) * 2;
                combinedNoise = saturate(combinedNoise);
                
                // Затухание по высоте (туман у земли плотнее)
                float heightFade = saturate(exp(-i.height * _HeightFade));
                
                // Затухание по расстоянию от камеры
                float distanceToCamera = distance(i.worldPos, _WorldSpaceCameraPos);
                float distanceFade = saturate(1.0 - (distanceToCamera / _DistanceFade));
                
                // Эффект Френеля для более реалистичного вида
                float fresnel = 1.0 - saturate(dot(normalize(i.viewDir), float3(0, 1, 0)));
                fresnel = pow(fresnel, 2);
                
                // Итоговая прозрачность
                float alpha = combinedNoise * _Density * heightFade * distanceFade * fresnel;
                alpha = saturate(alpha);
                
                // Небольшое свечение на краях
                _FogColor.rgb += fresnel * 0.1;
                
                return fixed4(_FogColor.rgb, alpha * _FogColor.a);
            }
            ENDCG
        }
    }
    
    Fallback "Transparent/Diffuse"
}