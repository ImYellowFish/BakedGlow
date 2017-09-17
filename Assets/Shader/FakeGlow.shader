// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/FakeGlow"
{
	Properties
	{
		_GlowColor("Glow Color", Color) = (0,0,0,0)
		_GlowPower("Glow Power", Float) = 1
		_GlowOffset("Glow Offset", Float) = 0
		_GlowStrength("Glow Strength", Float) = 1
		_GlowAngleOffset("Glow Angle Offset", Float) = 1
	}
		SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		LOD 100
		Blend One One
		Cull Back Lighting Off ZWrite Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			fixed4 _GlowColor;
			fixed _GlowPower;
			fixed _GlowOffset;
			fixed _GlowStrength;
			fixed _GlowAngleOffset;

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				half4 worldNormal : TEXCOORD0;
				half3 worldViewDir : TEXCOORD1;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex + v.normal * _GlowOffset);
				o.worldNormal.xyz = UnityObjectToWorldNormal(v.normal);
				o.worldNormal.w = length(UnityObjectToViewPos(v.vertex.xyz));
				o.worldViewDir = UnityWorldSpaceViewDir(mul(unity_ObjectToWorld, v.vertex).xyz);
				
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed d = dot(i.worldNormal.xyz, i.worldViewDir);
				// sample the texture
				// fixed4 col = _GlowColor * pow(od * (od - _GlowAngleOffset)* (od - _GlowAngleOffset) * (1 - od) * (1 - od) * _GlowStrength, _GlowPower);
				fixed4 col = _GlowColor * pow(d * _GlowStrength / (i.worldNormal.w + 1) / (i.worldNormal.w + 1), _GlowPower);
				return col;
			}
			ENDCG
		}
	}
}
