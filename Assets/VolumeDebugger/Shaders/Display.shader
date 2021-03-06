﻿Shader "Custom/Display" {

	Properties{
		_MainTex("Texture", 2D) = "white" {}
		_Size("Size", Range(0, 1)) = 1
	}


	SubShader{
		Tags{ "RenderType" = "Transparent" }
		LOD 200

		CGINCLUDE

		#pragma target 5.0
		#include "UnityCG.cginc"

		struct CellData {
			float3 position;
			float3 data;
		};
		StructuredBuffer<CellData> _Datas;

		float _Size;
		sampler2D _MainTex;

		struct v2g {
			float4 vertex : POSITION;
			float4 data : COLOR;
		};

		struct g2f {
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			float4 data : COLOR;
		};

		v2g vert(uint id : SV_VertexID) {
			v2g OUT;

			OUT.vertex = float4(_Datas[id].position, 1);
			OUT.data = float4(_Datas[id].data, 1);

			return OUT;
		}

		[maxvertexcount(4)]
		void geom_square(point v2g IN[1], inout TriangleStream<g2f> OUT) {

			float size = _Size;
			float halfS = 0.5f * size;

			g2f pIn;

			for (int x = 0; x < 2; x++) {
				for (int y = 0; y < 2; y++) {
					float4x4 billboardMatrix = UNITY_MATRIX_V;
					billboardMatrix._m03 = billboardMatrix._m13 = billboardMatrix._m23 = billboardMatrix._m33 = 0;

					float2 uv = float2(x, y);

					pIn.pos = IN[0].vertex + mul(float4((uv * 2 - float2(1, 1)) * halfS, 0, 1), billboardMatrix);

					pIn.pos = mul(UNITY_MATRIX_VP, pIn.pos);

					pIn.uv = uv;

					pIn.data = IN[0].data;

					OUT.Append(pIn);
				}
			}

		}

		ENDCG

		Pass{
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off

			CGPROGRAM

			#pragma vertex vert
			#pragma geometry geom_square
			#pragma fragment frag

			fixed4 frag(g2f IN) : SV_Target{
				return tex2D(_MainTex, IN.uv) * IN.data;
			}

			ENDCG
		}

	}
}